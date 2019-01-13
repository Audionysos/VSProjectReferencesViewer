using EnvDTE;
using SolutionNodes.gui;
using SolutionScan;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using P = SolutionScan.Program;

namespace SolutionNodes {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : System.Windows.Window {

		private ProjectsNodesView ns;
		private ProjectsNodesSettings sets = new ProjectsNodesSettings() {
			solutionName = "VSProjectReferencesViewer.sln",
			projectName = "SolutionNodes",
			//projectName = "DFMCExe",
			//solutionName = "DFM.sln",
		};

		public MainWindow() {
			InitializeComponent();
			setupContols();
			display();
			ns.showConnections();
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
			ns.SHOW_PROJECT_REQUEST += onProjectShowRequest;
			mainGrid.Children.Insert(0, ns.view);
			var rt = getProjectReferences();
			if (!rt) { Close(); return; }
			ns.refTree = rt;

		}

		private void onProjectShowRequest(string name) {
			var c = sets.copy(); sets.projectName = name;
			var nw = new MainWindow(sets);
			//nw.Show(); //TODO: somehow projects "X" and "A" could not be found in solution.
			//Turns out getProjectReferences() dont work for all projects that are placed in solution - folder.
		}

		private RefTree<Project> getProjectReferences() {
			P.solutionName = sets.solutionName;
			VisualStudioAttacher.VSPorces = sets.VSPorces;
			VisualStudioAttacher.VSName = sets.VSName;
			return P.getProjectReferences(sets.projectName);
		}

		private void setupContols() {
			PreviewKeyUp += (s, e) => {
				if (!ns) return;
				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
					if(e.Key == Key.S)
						ImageSaver.asPNG(ns.view, sets.projectName + "References");
				}
			};
			connVisButt.Click += (s, e) => {
				alternate(connVisButt,
					"Show all references",
					"Show deepest references only");
				if ((string)connVisButt.Content == "Show all references")
					ns.displayMeth = ConnectionsVisibility.Deepest;
				else ns.displayMeth = ConnectionsVisibility.All;
			};
		}

		#region Helper methods
		public static T alternate<T>(T e, string s1, string s2) where T : FrameworkElement {
			if (e is Label l) l.Content = (string)l.Content == s1 ? s2 : s1;
			else if (e is Button b) b.Content = (string)b.Content == s1 ? s2 : s1;
			return e;
		}
		#endregion

	}

	public class ProjectsNodesSettings {
		public string VSPorces = "devenv";
		public string VSName = "!VisualStudio";

		public string solutionName;
		public string projectName;

		public (double w, double h) viewSize = (800, 600);
		public (double w, double h) nodeSize = (10, 150);

		public ProjectsNodesSettings copy() {
			var fs = this.GetType().GetFields();
			var c = new ProjectsNodesSettings();
			foreach (var f in fs)
				f.SetValue(c, f.GetValue(this));
			return c;
		}

	}

}
