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
			this.size = (w, h);
			net = new NetworkViewModel();
			view = new NetworkView();
			view.ViewModel = net;
		}

		private Dictionary<RefTree<Project>, RefContext> nodes = new Dictionary<RefTree<Project>, RefContext>();

		/// <summary></summary>
		/// <param name="rt">Referenc tree node</param>
		/// <param name="cx">Current positon on X-Axis.</param>
		/// <param name="cn">Child count on current level (sum from all parent nodes).</param>
		/// <param name="ii">Inital index for the node at current level.</param>
		/// <param name="ln">Longest node (in characters count</param>
		private void displayRefTree(RefTree<Project> rt, double cx = 0, int cn = -1, int ii = 0, double ln = 0) {
			if (rt.subs.Count == 0) return;
			if (cn < 0) cn = rt.allOnLevel()?.Count??0;

			if (ln == 0) ln = rt.i.Name.Length;
			var nw = nodeSize.w * ln + emptyNodeWidth; //node width
			var nh = nodeSize.h;
			var tw = cn * nw; //total width
			var th = cn * nh; //total width
			var spc = size.w - tw; //remaining space
			var left = (size.w - tw) / 2;
			var top = (size.h - th) / 2;
			cn = 0; //reset child count for next level

			var s = ii;
			var tn = (rt.data as RefContext)?.node; //model assigned by parent
			if (tn == null) { //first node
				rt.data = createRefContext(rt).node;
				tn = rt.data as NodeViewModel;
				tn.Position = new Point(nw + cx, (size.h - nh) / 2);
				cx += nw;
			}

			foreach (var r in rt.subs) {
				if (r.d != rt.d + 1) continue;
				var nc = createRefContext(r);
				if (!nc) continue;
				connect(tn, nc.node);
				nc.node.Position = new Point(nw + cx, top + nh * s);
				cn += r.allOnLevel()?.Count ?? 0;
				s++;
			}

			ln = rt.allOnLevel()?.Max(r => r.i.Name.Length)??0; //longest node name
			ii = 0;
			foreach (var r in rt.subs) {
				if (r.d != rt.d + 1) continue;
				displayRefTree(r, cx + nw, -1, ii, ln);
				ii += r.subsOnLevel().Count;
			}
		}

		#region Connections
		public void showConnections() {
			net.Connections.Clear();
			foreach (var nc in nodes.Values) {
				//clearConnections(nc.node);
				foreach (var r in nc.reff.subs) {
					var rn = nodes[r];
					connect(nc.node, rn.node);
				}
			}
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
			r.data = nc;
			nodes.Add(r, nc);
			return nc;
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

	public class RefContext {
		public NodeViewModel node;
		public RefTree<Project> reff;

		public override string ToString() {
			return reff?.ToString()??base.ToString();
		}

		public static implicit operator bool(RefContext c) => c!=null;
	}


}
