using EnvDTE;
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
		/// <summary>Depth level for this node</summary>
		public int d { get; private set; } = 0;

		/// <summary>Stores parent node that recferences givne node of T.</summary>
		private Dictionary<T, RefTree<T>> c;
		private HashSet<RefTree<T>> all;

		public RefTree(T i, Func<T, IEnumerable<T>> t){
			this.i = i;
			this.t = t;
			c = new Dictionary<T, RefTree<T>>();
			//fill();
			all = new HashSet<RefTree<T>>();
			all.Add(this);
			add(t(i));
		}

		private RefTree(T i, RefTree<T> r, Func<T, IEnumerable<T>> t, int d) {
			this.i = i;
			this.t = t;
			this.d = d;
			this.all = r.all;
			c = r.c;
			all.Add(this);
			add(t(i));
		}

		public void add(T i) {
			var ap = (i as Project)?.Name;
			if (c.ContainsKey(i)) {
				var r = c[i]; //parent of refTree containing the i
				RefTree<T> tr = r.subOf(i); //refTree containing the i
				Debug.Assert(tr != null, "Value suppose to contain i");
				subs.Add(tr);
				if (r.d > d)  return; //do steal parent if it is deeper in tree.
				//r.subs.Remove(tr);
				tr.d = d + 1;
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

		/// <summary>Returns only direct sub nodes that are on specified level.</summary>
		/// <param name="d">Depth level - negative value will retrun subs on level next to this node level.</param>
		public List<RefTree<T>> subsOnLevel(int d = -1) {
			if (d < 0) d = this.d + 1;
			var ls = new List<RefTree<T>>();
			foreach (var s in subs) if (s.d == d) ls.Add(s);
			return ls;
		}

		public List<RefTree<T>> allOnLevel(int d = -1) {
			if (d < 0) d = this.d + 1;
			var ls = new List<RefTree<T>>();
			foreach (var s in all) if (s.d == d) ls.Add(s);
			if (ls.Count == 0) return null;
			return ls;
		}

		public RefTree<T> getDeepestReference() {
			if (!c.ContainsKey(i)) return null;
			return c[i];
		}

		public RefTree<T> subOf(T i) {
			foreach (var sr in subs)
				if (sr.i.Equals(i)) return sr;
			return null;
		}

		private void fill() {
			add(t(i));

		}

		public override string ToString() {
			return (toString?.Invoke(this)??base.ToString())+$"({d})";
		}

		public static implicit operator bool(RefTree<T> r) => r!=null;
	}

}
