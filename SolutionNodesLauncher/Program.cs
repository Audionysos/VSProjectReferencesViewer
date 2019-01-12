using SolutionNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SolutionNodesLauncher {

	class Program {
		public static Action<ProjectsNodesSettings, string>[] argSetters =
			new Action<ProjectsNodesSettings, string>[] {
				(s, v) => s.solutionName = v,
				(s, v) => s.projectName = v,
				(s, v) => s.viewSize = (Double.Parse(v), s.viewSize.h),
				(s, v) => s.viewSize = (s.viewSize.w, Double.Parse(v)),
				(s, v) => s.nodeSize = (Double.Parse(v), s.nodeSize.h),
				(s, v) => s.nodeSize = (s.nodeSize.w, Double.Parse(v)),
				(s, v) => s.VSPorces = v,
				(s, v) => s.VSName = v,
			};

		[STAThread]
		public static void Main(string[] args) {
			var sets = new ProjectsNodesSettings();
			for (int i = 0; i < args.Length; i++)
				argSetters[i](sets, args[i]);

			var w = new MainWindow(sets);
			Application app = new Application();
			app.Run(w);
		}
	}
}
