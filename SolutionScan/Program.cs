﻿using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLangProj;
using VSLangProj80;
using static System.Console;

namespace SolutionScan {
	public class Program {

		public static string solutionName;

		private static DTE2 ide;
		private static List<Project> pros = new List<Project>();
		private static List<VSProject2> vpros = new List<VSProject2>();

		static void Main(string[] args) {
			solutionName = args[0];
			getSolution();
			ReadLine();
		}

		private static void getSolution() {
			if (ide != null) return;
			var vs = VisualStudioAttacher.getVSForSolutions(
				new List<string> { solutionName });
			ide = vs as DTE2;
			if(ide == null) {
				WriteLine($@"Couldn't get visual studio instance running ""{solutionName}"" solution.");
				return;
			}

			var items = ide.Solution.Projects;
			foreach (Project p in items)
				expandProjectsFolder(p, pros);

			foreach (var p in pros) {
				//WriteLine($@"project: {p.Name}");
				var vsp = p.Object as VSProject2;
				//printReferences(vsp);
				vpros.Add(vsp);
			}
		}

		public static RefTree<Project> getProjectReferences(string name) {
			var p = getProject(name);
			if (p == null) {
				if (ide != null) WriteLine($@"Couldn't find project named ""{name}"" in current solution.");
				return null;
			}
			RefTree<Project>.toString = (r) => r.i.Name;
			var rt = new RefTree<Project>(p, i => i.projectReferences());
			return rt;
		}

		public static Project getProject(string name) {
			getSolution();
			return pros.Find(p => p.Name == name);
		}

		private static void printReferences(VSProject2 vsp) {
			if (vsp == null) return;
			foreach (Reference r in vsp.References) {
				if (r.SourceProject == null) continue;
				WriteLine($@"	reference: {r.Name}");
				//WriteLine($@"		type: {r.SourceProject?.Name}");
			}
		}

		#region Scanning
		private static bool expandProjectsFolder(Project p, List<Project> pros) {
			if(!isSolutionFolder(p)) { pros.Add(p); return false; }
			for (int i = 1; i < p.ProjectItems.Count; i++) {
				var sp = p.ProjectItems.Item(i) as Project;
				if (sp == null) continue;
				expandProjectsFolder(p, pros);
			}
			return true;
		}

		private static bool isSolutionFolder(Project p) {
			return p.Kind == "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

		}
		#endregion
	}

	public static class ProjectExtensions {

		public static List<Project> projectReferences(this Project p) {
			var l = new List<Project>();
			Debug.WriteLine($"cheking references of: {p.Name}");
			foreach (Reference r in p.ass<VSProject>().References) {
				if (r.SourceProject == null) continue;
				l.Add(r.SourceProject);
			}return l;
		}

		public static T ass<T>(this Project p) where T : class {
			return p.Object as T;
		}
	}

	public class RefTree<T> {
		public static Func<RefTree<T>, string> toString;
		/// <summary>Item holded in this node.</summary>
		public T i;
		/// <summary>List of sub items for this node.</summary>
		public HashSet<RefTree<T>> subs = new HashSet<RefTree<T>>();
		///// <summary>List of parent items for this node.</summary>
		//public HashSet<RefTree<T>> parents = new HashSet<RefTree<T>>();

		public Func<T, IEnumerable<T>> t;
		/// <summary>Arbitrary additional data.</summary>
		public object data;

		/// <summary>Stores parent node that recferences givne node of T.</summary>
		private Dictionary<T, RefTree<T>> c;
		private int d = 0;

		public RefTree(T i, Func<T, IEnumerable<T>> t){
			this.i = i;
			this.t = t;
			c = new Dictionary<T, RefTree<T>>();
			add(t(i));
			//fill();
		}

		public void add(T i) {
			var ap = (i as Project)?.Name;
			if (c.ContainsKey(i)) {
				var r = c[i]; RefTree<T> tr = null;
				if (r.d > d) return;
				foreach (var sr in r.subs) if (sr.i.Equals(i)) tr = sr;
				Debug.Assert(tr != null, "Value suppose to contain i");
				r.subs.Remove(tr);
				r.d = d + 1;
				subs.Add(tr);
				c[i] = this;
				return;
			}
			c.Add(i, this);
			var nr = new RefTree<T>(i, this, t, d+1);
			subs.Add(nr);
		}

		public void add(IEnumerable<T> its) {
			foreach (var i in its) add(i);
			//foreach (var s in subs) s.fill();
		}

		private RefTree(T i, RefTree<T> r, Func<T, IEnumerable<T>> t, int d) {
			this.i = i;
			this.t = t;
			this.d = d;
			c = r.c;
			add(t(i));
		}

		private void fill() {
			add(t(i));

		}

		public override string ToString() {
			return toString?.Invoke(this)??base.ToString();
		}

		public static implicit operator bool(RefTree<T> r) => r!=null;
	}

}
