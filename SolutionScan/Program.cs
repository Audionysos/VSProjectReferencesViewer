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
				WriteLine($@"project: {p.Name}");
				var vsp = p.Object as VSProject2;
				//printReferences(vsp);
				vpros.Add(vsp);
			}
		}

		public static RefTree<Project> getProjectReferences(string name) {
			var p = getProject(name);
			if (p == null) {
				if (ide != null) WriteLine($@"Couldn't find project named ""{name}"" in ""{solutionName}"" solution.");
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
			if(isProject(p)) { pros.Add(p); return false; }
			for (int i = 1; i < p.ProjectItems.Count; i++) {
				var sp = p.ProjectItems.Item(i).Object as Project;
				if (sp == null) continue;
				expandProjectsFolder(sp, pros);
			}
			return true;
		}

		private static bool isProject(Project p) {
			//var pn = p.Name; 
			return p.Kind == "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
			//return p.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

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

}
