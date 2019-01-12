using EnvDTE;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using SolutionScan;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SolutionNodes.gui {
	public class ProjectsNodesView {

		/// <summary>Size of view (specifed in consturctor). This is only used for layout purposes.</summary>
		public (double w, double h) size { get; }
		/// <summary>Estimate size of node for layout - I don't know how to get acutall note size or even a node view.
		/// Actual width is simply <see cref="emptyNodeWidth"/> + <see cref="nodeSize"/> witdth multipled by number of characters in project's name.</summary>
		public (double w, double h) nodeSize { get; set; } = (10, 200);
		/// <summary>Estatmited width for empty node (without name).</summary>
		public double emptyNodeWidth = 100; 
		/// <summary>Outer WPF element</summary>
		public NetworkView view { get; }

		private RefTree<Project> rt;
		/// <summary>Projects reference tree to be displayed.</summary>
		public RefTree<Project> refTree {
			get => rt;
			set {
				if (rt) throw new Exception("Resetting reference tree is not suppored");
				rt = value;
				displayRefTree(rt);
			}
		} 

		private NetworkViewModel net;

		public ProjectsNodesView(double w, double h) {
			this.size = size;
			net = new NetworkViewModel();
			view = new NetworkView();
			view.ViewModel = net;
		}

		private double cx;
		private void displayRefTree(RefTree<Project> rt) {
			if (rt.subs.Count == 0) return;

			var nw = nodeSize.w * rt.i.Name.Length + emptyNodeWidth; //node width
			var nh = nodeSize.h;
			var tw = (rt.subs.Count - 0) * nw; //total width
			var th = (rt.subs.Count - 0) * nh; //total width
			var spc = size.w - tw; //remaining space
			var left = (size.w - tw) / 2;
			var top = (size.h - th) / 2;

			var s = 0;
			var tn = rt.data as NodeViewModel;
			if (rt.data == null) {
				rt.data = createNode(rt);
				tn = rt.data as NodeViewModel;
				tn.Position = new Point(nw + cx, (size.h - nh) / 2);
				cx += nw;
			}

			foreach (var r in rt.subs) {
				var n = createNode(r);
				r.data = n;
				var c = new ConnectionViewModel(net,
					n.Inputs[0], tn.Outputs[0]);
				net.Connections.Add(c);
				n.Position = new Point(nw + cx, top + nh * s);
				s++;
			}
			cx+= nw;
			foreach (var r in rt.subs) displayRefTree(r);
		}

		private NodeViewModel createNode(RefTree<Project> r) {
			var n = new NodeViewModel();
			n.Name = r.i.Name;
			//n.IsCollapsed = true;
			net.Nodes.Add(n);

			var inp = new NodeInputViewModel();
			inp.Name = "Referenced by";
			inp.PortPosition = PortPosition.Left;
			inp.MaxConnections = int.MaxValue;
			n.Inputs.Add(inp);

			var ou = new NodeOutputViewModel();
			ou.Name = "References";
			ou.PortPosition = PortPosition.Right;
			ou.MaxConnections = int.MaxValue;
			n.Outputs.Add(ou);
			return n;
		}


		public static implicit operator bool(ProjectsNodesView n) => n!=null;
	}
}
