using EnvDTE;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using SolutionScan;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Controls;
using System.Windows.Media;
using static SolutionNodes.gui.ConnectionsVisibility;

namespace SolutionNodes.gui {
	public class ProjectsNodesView {
		public event Action<string> SHOW_PROJECT_REQUEST;

		/// <summary>Size of view (specifed in consturctor). This is only used for layout purposes.</summary>
		public (double w, double h) size { get; }
		/// <summary>Estimate size of node for layout - I don't know how to get acutall note size or even a node view.
		/// Actual width is simply <see cref="emptyNodeWidth"/> + <see cref="nodeSize"/> witdth multipled by number of characters in project's name.</summary>
		public (double w, double h) nodeSize { get; set; } = (10, 200);
		/// <summary>Estatmited width for empty node (without name).</summary>
		public double emptyNodeWidth = 100; 
		/// <summary>Outer WPF element</summary>
		public NetworkView view { get; }


		private ConnectionsVisibility _dispMeth = Deepest;
		public ConnectionsVisibility displayMeth {
			get => _dispMeth;
			set {
				_dispMeth = value;
				showConnections();
			}
		}

		private RefTree<Project> rt;
		/// <summary>Projects reference tree to be displayed.</summary>
		public RefTree<Project> refTree {
			get => rt;
			set {
				if (rt) throw new Exception("Resetting reference tree is not suppored");
				rt = value;
				displayTree(rt);
				showConnections();
			}
		}

		private NetworkViewModel net;
		public NetworkViewModel network => net;

		public ProjectsNodesView(double w, double h) {
			this.size = (w, h);
			net = new NetworkViewModel();
			view = new NetworkView();
			view.ViewModel = net;
			setDisplayMethods();
		}


		private Dictionary<RefTree<Project>, RefContext> nodes = new Dictionary<RefTree<Project>, RefContext>();

		private void displayTree(RefTree<Project> rt) {
			var cl = rt.d; var rol = rt.allOnLevel(cl);
			var cx = 0d; var ln = 0;
			while (rol != null) {
				var nw = nodeSize.w * ln + emptyNodeWidth; //node width
				var nh = nodeSize.h;
				var th = rol.Count * nh; //total height
				var top = (size.h - th) / 2;
				cx += nw;

				var i = 0; foreach (var r in rol) {
					var nc = createRefContext(r);
					if (cl == 0) nc.node.BackColor = Colors.DarkOliveGreen;
					nc.node.Position = new Point(cx, top + nh * i++);
				}
				ln = rol.Max(sr => sr.i.Name.Length);
				rol = rt.allOnLevel(++cl);
			}
		}

		#region Connections

		public void showConnections() {
			net.Connections.Clear();
			displayMethods[displayMeth]();
		}

		private Dictionary<ConnectionsVisibility, Action> displayMethods;
		private void setDisplayMethods() {
			displayMethods = new Dictionary<ConnectionsVisibility, Action>() {
				{ Deepest, showDeepestConnections },
				{ All, showAllConnections },
				{ Custom, showCustomConnections },
			};
		}


		private void showDeepestConnections() {
			foreach (var nc in nodes.Values) {
				var rn = nc.reff.getDeepestReference(); //referenced node;
				if (rn) connect(nodes[rn].node, nc.node);
			}
		}

		private void showAllConnections() {
			foreach (var nc in nodes.Values)
				foreach (var r in nc.reff.subs)
					connect(nc.node, nodes[r].node);
		}

		private void showCustomConnections() {
			showDeepestConnections();
			//foreach (var nc in nodes.Values) {
			//	nc.
			//}
		}
		

		private void clearConnections(NodeViewModel n) {
			foreach (var i in n.Inputs) {
				while (i.Connections.Count > 0) {
					net.Connections.Remove(i.Connections[0]);
				}
			}
		}

		private void connect(NodeViewModel tn, NodeViewModel n) {
			var c = new ConnectionViewModel(net,
					n.Inputs[0], tn.Outputs[0]);
			net.Connections.Add(c);
		}
		#endregion

		#region Highlighting
		public void resetHighlights() {
			foreach (var n in nodes.Values) {
				if (n.reff.d == 0) n.node.BackColor = Colors.DarkOliveGreen;
				else n.node.BackColor = Colors.RoyalBlue;
			}
		}

		public void highlightReferencingNodes(RefTree<Project> r) {
			resetHighlights();
			r.withEach(i => {
				if (i.subs.Contains(r))
					nodes[i].node.BackColor = Colors.LightBlue;
			});
		}

		public void highlightReferencedNodes(RefTree<Project> r) {
			resetHighlights();
			foreach (var sr in r.subs)
				nodes[sr].node.BackColor = Colors.Orange;
		}
		#endregion

		public void requestShow(RefTree<Project> r) {
			SHOW_PROJECT_REQUEST?.Invoke(r.i.Name);
		}

		/// <summary>Returns new context only when node was not already created, othwerwise null.</summary>
		/// <param name="r"></param>
		/// <returns></returns>
		public RefContext createRefContext(RefTree<Project> r) {
			if (nodes.ContainsKey(r)) return null;
			return getRefContext(r);
		}

		private RefContext getRefContext(RefTree<Project> r) {
			if (nodes.ContainsKey(r)) return nodes[r];
			var nc = new RefContext() {
				node = createNode(r),
				reff = r,
			};
			nc.node.context = nc;
			r.data = nc;
			nodes.Add(r, nc);
			return nc;
		}

		private CustomNodeViewModel createNode(RefTree<Project> r) {
			var n = new CustomNodeViewModel();
			n.nodes = this;
			n.Name = r.i.Name;
			//n.BackColor = Colors.DarkOliveGreen;
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

	public enum ConnectionsVisibility {
		Deepest,
		All,
		Custom
	}

	public class RefContext {
		public CustomNodeViewModel node;
		public RefTree<Project> reff;

		public override string ToString() {
			return reff?.ToString()??base.ToString();
		}

		public static implicit operator bool(RefContext c) => c!=null;
	}






}
