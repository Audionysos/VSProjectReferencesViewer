using EnvDTE;
using NodeNetwork.ViewModels;
using ReactiveUI;
using SolutionScan;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SolutionNodes.gui {
	/// <summary>
	/// Interaction logic for MyCustomNodeView.xaml
	/// </summary>
	public partial class MyCustomNodeView : UserControl, IViewFor<CustomNodeViewModel> {
		#region ViewModel
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
			typeof(CustomNodeViewModel), typeof(MyCustomNodeView), new PropertyMetadata(null));

		public CustomNodeViewModel ViewModel {
			get => (CustomNodeViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		object IViewFor.ViewModel {
			get => ViewModel;
			set => ViewModel = (CustomNodeViewModel)value;
		}
		#endregion

		public MyCustomNodeView() {
			InitializeComponent();

			this.WhenActivated(d => {
				this.WhenAnyValue(v => v.ViewModel).BindTo(this,
					v => v.NodeView.ViewModel).DisposeWith(d);
				this.OneWayBind(ViewModel,
					vm => vm.BackColor,
					v => v.NodeView.Background,
					color => new SolidColorBrush(color)
				).DisposeWith(d);
			});

			#region Context menu
			NodeView.ContextMenu = new ContextMenu() {
				Items = {
					new MenuItem() { Visibility = Visibility.Collapsed },
					new MenuItem() {
						Header = "Highlight referencing projects",
					}.set(i=>{
						i.Click += (s,e) => {
							ViewModel.nodes.network.ClearSelection();
							ViewModel.IsSelected = true;
							ViewModel.nodes.highlightReferencingNodes(
								ViewModel.context.reff
							);
						};
					}),
					new MenuItem() {
						Header = "Highlight referenced projects",
					}.set(i=>{
						i.Click += (s,e) => {
							ViewModel.nodes.network.ClearSelection();
							ViewModel.IsSelected = true;
							ViewModel.nodes.highlightReferencedNodes(
								ViewModel.context.reff
							);
						};
					}),
					new MenuItem() {
						Header = "Show references graph",
					}.set(i=>{
						i.Click += (s,e) => {
							ViewModel.nodes.requestShow(
								ViewModel.context.reff);
						};
					}),
				}
			};
			//var cm = NodeView.ContextMenu;
			//NodeView.ContextMenuOpening += (s, e) => {
			//	cm.Items.Clear();
			//	cm.Items.Add(new MenuItem() { Header = "aaaa" });
			//	NodeView.ContextMenu = cm;
			//};
			#endregion
		}
	}

	public class CustomNodeViewModel : NodeViewModel {
		static CustomNodeViewModel() {
			Splat.Locator.CurrentMutable.Register(
				() => new MyCustomNodeView(),
				typeof(IViewFor<CustomNodeViewModel>));
		}

		private Color _backColor = Colors.RoyalBlue;
		public Color BackColor {
			get => _backColor;
			set => this.RaiseAndSetIfChanged(ref _backColor, value);
		}

		public RefContext context;
		public ProjectsNodesView nodes;

		public CustomNodeViewModel() {
			//this.BackColor
			//this.PropertyChanged += onChanged;
		}

		private void onChanged(object sender, PropertyChangedEventArgs e) {
			Debug.WriteLine($"property changed {e.PropertyName}");
		}

	}

	public static class ObjectsExtensions {

		public static T set<T>(this T o, Action<T> s) {
			s(o); return o;
		}
	}
}
