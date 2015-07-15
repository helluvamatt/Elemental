using Elemental.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Elemental
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			_ElementDatabase = new ElementDatabase();
		}

		private const string DRAGDROP_DATA = "ElementId";

		private ElementDatabase _ElementDatabase;

		private Point _MouseStartPoint;
		private Point _ItemStartPoint;
		private bool _Dragging = false;

		#region Event handlers

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			AddBaseElements(Width / 2, Height / 2);
		}

		// Not used
		private void Element_Drop(object sender, DragEventArgs e)
		{
			ElementContentItem item = sender as ElementContentItem;
			Console.WriteLine("Drop handler called, item = {0}", item);
			if (item != null && e.Data.GetDataPresent(DRAGDROP_DATA))
			{
				string droppedElementId = (string) e.Data.GetData(DRAGDROP_DATA);
				Element dropped = _ElementDatabase.GetElementById(droppedElementId);
				if (dropped != null)
				{
					Console.WriteLine(dropped.Name + " dropped on " + item.Element.Name);
				}
			}
		}

		private void Element_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var item = sender as ElementContentItem;
            _MouseStartPoint = e.GetPosition(null);
			_ItemStartPoint = new Point(item.Margin.Left, item.Margin.Top);
			BringToFront(item);
		}

		private void Element_MouseMove(object sender, MouseEventArgs e)
		{
			Point mousePos = e.GetPosition(null);
			Vector diff = mousePos - _MouseStartPoint;
			ElementContentItem item = sender as ElementContentItem;
			if (item != null && e.LeftButton == MouseButtonState.Pressed)
			{
				double newX = _ItemStartPoint.X + diff.X;
				double newY = _ItemStartPoint.Y + diff.Y;
				Point newPt = ClipPoint(new Point(newX, newY));
				item.Margin = new Thickness(newPt.X, newPt.Y, 0, 0);
				_Dragging = true;
			}
		}

		private void Element_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (_Dragging)
			{
				// TODO Handle mouse up after drag?
				_Dragging = false;
			}
		}

		private void Element_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = (e.Source as ElementContentItem);
			Workbench.Children.Add(CreateItem(item.Margin.Left + 10, item.Margin.Top + 10, item.Element));
            e.Handled = true;
		}

		#endregion

		#region Utility methods

		private Point ClipPoint(Point pt)
		{
			if (pt.X < 0) pt.X = 0;
			if (pt.X > (Workbench.ActualWidth - 72)) pt.X = Workbench.ActualWidth - 72;
			if (pt.Y < 0) pt.Y = 0;
			if (pt.Y > (Workbench.ActualHeight - 72)) pt.Y = Workbench.ActualHeight - 72;
			return pt;
		}

		private void BringToFront(ElementContentItem item)
		{
			foreach (UIElement control in Workbench.Children)
			{
				Grid.SetZIndex(control, 0);
			}
			Grid.SetZIndex(item, 1);
		}

		private void AddBaseElements(double cx, double cy)
		{
			// Get the base elements
			List<Element> baseElements = _ElementDatabase.GetBaseElements();

			// Lay the base elements in a circle around the center
			double radOffset = (2 * Math.PI) / baseElements.Count;
			double itemRadius = 50.91168824543142;
			double theta = (Math.PI - radOffset) / 2;
			double distance = itemRadius / Math.Cos(theta);
			int n = baseElements.Count;
            for (int i = 0; i < n; i++)
			{
				double angle = radOffset * i;
				double x = cx - 36 + Math.Cos(angle) * distance;
				double y = cy - 36 - Math.Sin(angle) * distance;
				Workbench.Children.Add(CreateItem(x, y, baseElements[i]));
            }
		}

		private ElementContentItem CreateItem(double x, double y, Element element)
		{
			ElementContentItem item = new ElementContentItem();
			item.Margin = new Thickness(x, y, 0, 0);
			item.HorizontalAlignment = HorizontalAlignment.Left;
			item.VerticalAlignment = VerticalAlignment.Top;
			item.Template = (ControlTemplate) Workbench.Resources["ElementItemTemplate"];
			item.Element = element;
			item.Drop += Element_Drop;
			item.MouseDown += Element_MouseDown;
			item.MouseMove += Element_MouseMove;
			item.MouseUp += Element_MouseUp;
			item.MouseDoubleClick += Element_MouseDoubleClick;
			item.AllowDrop = true;

			return item;
		}

		#endregion
	}
}
