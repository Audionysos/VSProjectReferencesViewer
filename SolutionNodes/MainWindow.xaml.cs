using EnvDTE;
using SolutionNodes.gui;
using SolutionScan;
using System.Windows.Input;
using P = SolutionScan.Program;

namespace SolutionNodes {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : System.Windows.Window {

		private ProjectsNodesView ns;
		private ProjectsNodesSettings sets = new ProjectsNodesSettings() {
			solutionName = "SoundsPlayground.sln",
			projectName = "SolutionNodes",
		};

		public MainWindow() {
			InitializeComponent();
			setupContols();
			display();
		}

		public MainWindow(ProjectsNodesSettings settings) {
			InitializeComponent();
			setupContols();
			display(settings);
		}

		public void display(ProjectsNodesSettings settings = null) {
			sets = settings ?? sets;
			Title = $@"{sets.projectName} project reference tree";
			ns = new ProjectsNodesView(sets.viewSize.w, sets.viewSize.h);
			ns.nodeSize = (sets.nodeSize.w, sets.nodeSize.h);
			Content = ns.view;
			ns.refTree = getProjectReferences();

		}

		private RefTree<Project> getProjectReferences() {
			P.solutionName = sets.solutionName;
			VisualStudioAttacher.VSPorces = sets.VSPorces;
			VisualStudioAttacher.VSName = sets.VSName;
			return P.getProjectReferences(sets.projectName);
		}

		private void setupContols() {
			if (!ns) return;
			PreviewKeyUp += (s, e) => {
				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
					if(e.Key == Key.S)
						ImageSaver.asPNG(ns.view, sets.solutionName);
				}
			};
		}

	}

	public class ProjectsNodesSettings {
		public string VSPorces = "devenv";
		public string VSName = "!VisualStudio";

		public string solutionName;
		public string projectName;

		public (double w, double h) viewSize = (800, 600);
		public (double w, double h) nodeSize = (10, 150);

	}
}
