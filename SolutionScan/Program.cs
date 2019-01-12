using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
			var items = ide.Solution.Projects;
			foreach (Project p in items) {
				expandProjectsFolder(p, pros);
			}
			foreach (var p in pros) {
				WriteLine($@"project: {p.Name}");
				var vsp = p.Object as VSProject2;
				//printReferences(vsp);
				vpros.Add(vsp);
			}
		}

		public static RefTree<Project> getProjectReferences(string name) {
			var p = getProject(name);
			if (p == null) return null;
			var rt = new RefTree<Project>(p, i => i.projectReferences());
			RefTree<Project>.toString = (r) => r.i.Name;
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

		public T i;
		public Dictionary<T, RefTree<T>> c;
		public HashSet<RefTree<T>> subs = new HashSet<RefTree<T>>();
		public Func<T, IEnumerable<T>> t;
		public object data;

		public RefTree(T i, Func<T, IEnumerable<T>> t){
			this.i = i;
			this.t = t;
			c = new Dictionary<T, RefTree<T>>();
			add(t(i));
		}

		public void add(T i) {
			if (c.ContainsKey(i)) return;
			else c.Add(i, this);
			subs.Add(new RefTree<T>(i, this, t));
		}

		public void add(IEnumerable<T> its) {
			foreach (var i in its) add(i);
		}

		private RefTree(T i, RefTree<T> r, Func<T, IEnumerable<T>> t) {
			this.i = i;
			this.t = t;
			c = r.c;
			add(t(i));
		}

		public override string ToString() {
			return toString?.Invoke(this)??base.ToString();
		}

		public static implicit operator bool(RefTree<T> r) => r!=null;
	}

}
