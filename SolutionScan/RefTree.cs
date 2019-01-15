using EnvDTE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionScan {
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

		public RefTree(T i, Func<T, IEnumerable<T>> t) {
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
				if (r.d > d) return; //do steal parent if it is deeper in tree.
				 //r.subs.Remove(tr);
				//tr.d = d + 1;
				tr.setLevel(d + 1);
				c[i] = this;
				return;
			}
			c.Add(i, this);
			var nr = new RefTree<T>(i, this, t, d + 1);
			subs.Add(nr);
		}

		public void add(IEnumerable<T> its) {
			foreach (var i in its) add(i);
			//foreach (var s in subs) s.fill();
		}

		private void setLevel(int d) {
			this.d = d;
			foreach (var s in subs) {
				var pr = c[s.i];
				if (pr != this || pr.d > d) continue;
				c[s.i] = this;
				s.setLevel(d + 1);
			}
		}

		#region Utils
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

		/// <summary>Perform action on each node in the tree</summary>
		/// <param name="a"></param>
		public void withEach(Action<RefTree<T>> a) {
			foreach (var r in all) a(r);
		}
		#endregion

		public override string ToString() {
			return (toString?.Invoke(this) ?? base.ToString()) + $"({d})";
		}

		public static implicit operator bool(RefTree<T> r) => r != null;
	}
}
