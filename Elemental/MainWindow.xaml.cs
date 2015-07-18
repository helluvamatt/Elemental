using Elemental.Data;
using Elemental.Data.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
			_ElementDatabase.DatabaseLoaded += _ElementDatabase_DatabaseLoaded;
			_ElementDatabase.DatabaseError += _ElementDatabase_DatabaseError;
			_ElementDatabase.DiscoveredElementsChanged += _ElementDatabase_DiscoveredElementsChanged;
			_ElementDatabase.ProgressLoaded += _ElementDatabase_ProgressLoaded;
			_ElementDatabase.ProgressSaved += _ElementDatabase_ProgressSaved;

			_LastLocation = ExePath;
		}

		private string ExePath
		{
			get
			{
				return System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Uri.UnescapeDataString(new Uri(Assembly.GetEntryAssembly().CodeBase).AbsolutePath)));
			}
		}

		private ElementDatabase _ElementDatabase;

		private string _LastLocation;
		private string _CurrentSaveGameFile;
		private Action _AfterSaveAction;

		private Point _MouseStartPoint;
		private Point _ItemStartPoint;
		private bool _DragNew;
		private ElementContentItem _DragStartedItem;

		#region Event handlers

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			LoadingAnimation.Visibility = Visibility.Visible;
			_ElementDatabase.LoadDatabaseAsync("elements.json");
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_ElementDatabase.ProgressDirty)
			{
				var result = MessageBox.Show(this, Properties.Resources.SaveQuestion, Properties.Resources.Question, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result)
				{
					case MessageBoxResult.Yes:
						_AfterSaveAction = () => Close();
						e.Cancel = true;
						AttemptSave();
						break;
					case MessageBoxResult.No:
						// Just quit, lose progress
						break;
					case MessageBoxResult.Cancel:
						// Don't save, but cancel
						e.Cancel = true;
						break;
				}
			}
		}

		private void MenuItem_File_NewGame_Click(object sender, RoutedEventArgs e)
		{
			if (_ElementDatabase.ProgressDirty)
			{
				var result = MessageBox.Show(this, Properties.Resources.SaveQuestion, Properties.Resources.Question, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result)
				{
					case MessageBoxResult.Yes:
						_AfterSaveAction = () => DoNewGame();
						AttemptSave();
						break;
					case MessageBoxResult.No:
						DoNewGame();
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			}
			else
			{
				DoNewGame();
			}
		}

		private void MenuItem_File_LoadGame_Click(object sender, RoutedEventArgs e)
		{
			if (_ElementDatabase.ProgressDirty)
			{
				var result = MessageBox.Show(this, Properties.Resources.SaveQuestion, Properties.Resources.Question, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result)
				{
					case MessageBoxResult.Yes:
						_AfterSaveAction = () => DoLoad();
						AttemptSave();
						break;
					case MessageBoxResult.No:
						DoLoad();
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			}
			else
			{
				DoLoad();
			}
		}

		private void MenuItem_File_SaveGame_Click(object sender, RoutedEventArgs e)
		{
			AttemptSave();
		}

		private void MenuItem_File_SaveGameAs_Click(object sender, RoutedEventArgs e)
		{
			DoSaveAs();
		}

		private void MenuItem_Preferences_OnlyCombineNew_Checked(object sender, RoutedEventArgs e)
		{
			_ElementDatabase.OnlyUndiscovered = MenuItem_Preferences_OnlyCombineNew.IsChecked;
		}

		private void MenuItem_File_Quit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void _ElementDatabase_DatabaseError(Exception ex)
		{
			LoadingAnimation.Visibility = Visibility.Hidden;
			MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void _ElementDatabase_DatabaseLoaded()
		{
			LoadingAnimation.Visibility = Visibility.Hidden;
			DoNewGame();
		}

		private void _ElementDatabase_DiscoveredElementsChanged(List<Element> knownElements)
		{
			Toolbox.ItemsSource = knownElements;
		}

		private void _ElementDatabase_ProgressSaved()
		{
			// TODO Notify Progress saved

			// Perform the AfterSaveAction
			if (_AfterSaveAction != null)
			{
				_AfterSaveAction.Invoke();
				_AfterSaveAction = null;
			}
		}

		private void _ElementDatabase_ProgressLoaded(List<ElementDatabase.SavedProgress.ElementOnWorkbench> obj)
		{
			// Populate workbench
			ClearAllElements();
			double sX = (Workbench.ActualWidth + Sidebar.ActualWidth) / 2;
			double sY = Workbench.ActualHeight / 2;
			foreach (var element in obj)
			{
				Point clipped = ClipPoint(new Point(element.X, element.Y));
				AddElement(sX, sY, clipped.X, clipped.Y, element.Element);
			}
        }

		private void Workbench_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
			{
				Point mousePos = e.GetPosition(null);
				AddBaseElements(mousePos.X, mousePos.Y);
			}
			else if (e.ChangedButton == MouseButton.Right)
			{
				// TODO Possible handle a context menu?
				Console.WriteLine("Right-click detected...");
			}
		}

		private void Workbench_MouseMove(object sender, MouseEventArgs e)
		{
			Point mousePos = e.GetPosition(null);
			Vector diff = mousePos - _MouseStartPoint;
			if (_DragStartedItem != null && e.LeftButton == MouseButtonState.Pressed)
			{
				double newX = _ItemStartPoint.X + diff.X;
				double newY = _ItemStartPoint.Y + diff.Y;
				Point newPt = ClipPoint(new Point(newX, newY));
				Canvas.SetLeft(_DragStartedItem, newPt.X);
				Canvas.SetTop(_DragStartedItem, newPt.Y);

				foreach (UIElement uiElement in Workbench.Children)
				{
					if (uiElement is ElementContentItem && uiElement != _DragStartedItem)
					{
						ElementContentItem elementItem = uiElement as ElementContentItem;
						elementItem.Collision = CollisionDetection(elementItem, mousePos);
					}
				}

				SolidColorBrush brush = new SolidColorBrush();
				brush.Color = InRecycleBin(mousePos) ? Color.FromArgb(0x55, 0xFF, 0, 0) : Colors.Transparent;
				RecycleBin.Background = brush;
			}
		}

		private void Workbench_MouseUp(object sender, MouseButtonEventArgs e)
		{
			Console.WriteLine("Workbench_MouseUp called");
			if (_DragStartedItem != null)
			{
				var item = _DragStartedItem;
				Point mousePos = e.GetPosition(null);
				if (InRecycleBin(mousePos))
				{
					// Recycle element
					var anim = new DoubleAnimation
					{
						To = 0,
						Duration = TimeSpan.FromMilliseconds(100),
						FillBehavior = FillBehavior.Stop
					};
					
					anim.Completed += (s, a) =>
					{
						Workbench.Children.Remove(item);
					};
					item.BeginAnimation(UIElement.OpacityProperty, anim);
				}
				else
				{
					foreach (UIElement uiElement in Workbench.Children)
					{
						if (uiElement is ElementContentItem && uiElement != item)
						{
							ElementContentItem elementItem = uiElement as ElementContentItem;
							if (CollisionDetection(elementItem, mousePos))
							{
								// Find center point between two elements
								Rect dragItemRect = GetItemRect(item);
								Rect otherItemRect = GetItemRect(elementItem);
								Point dragItemCenter = GetCenterPoint(dragItemRect);
								Point otherItemCenter = GetCenterPoint(otherItemRect);
								Rect bothItemsRect = new Rect(dragItemCenter, otherItemCenter);
								Point centerPoint = GetCenterPoint(bothItemsRect);

								// Do expirement
								ElementDatabase.ExperimentResult result = _ElementDatabase.DoExperiment(item.Element, elementItem.Element);

								// Provide feedback for success
								if (result.Success)
								{
									// Remove items from workbench
									Workbench.Children.Remove(elementItem);
									Workbench.Children.Remove(item);

									// Add results back
									AddElements(centerPoint.X, centerPoint.Y, result.ElementsCreated);
								}

								// Break out of the foreach (...) so we don't process
								break;
							}
						}
					}
				}

				// Reset borders
				foreach (UIElement uiElement in Workbench.Children)
				{
					if (uiElement is ElementContentItem)
					{
						(uiElement as ElementContentItem).Collision = false;
					}
				}

				// Reset RecycleBin background
				RecycleBin.Background = new SolidColorBrush(Colors.Transparent);

				// Done dragging
				Point toolboxOrigin = Toolbox.TranslatePoint(new Point(0, 0), Workbench);
				Rect toolboxBounds = new Rect(toolboxOrigin.X, toolboxOrigin.Y, Toolbox.ActualWidth, Toolbox.ActualHeight);
				if (_DragNew && !!toolboxBounds.Contains(mousePos))
				{
					Workbench.Children.Remove(_DragStartedItem);
				}
				_DragNew = false;
				_DragStartedItem = null;
			}
		}

		private void Element_MouseDown(object sender, MouseEventArgs e)
		{
			var item = e.Source as ElementContentItem;
			if (item != null)
			{
				_MouseStartPoint = e.GetPosition(null);
				_ItemStartPoint = new Point(Canvas.GetLeft(item), Canvas.GetTop(item));
				BringToFront(item);
				_DragStartedItem = item;
			}
		}

		private void Element_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = e.Source as ElementContentItem;
			double offset = 20;
			double fromX = Canvas.GetLeft(item);
			double fromY = Canvas.GetTop(item);
			double toX = fromX + offset;
			double toY = fromY + offset;
			AddElement(fromX, fromY, toX, toY, item.Element);
			e.Handled = true;
		}

		private void ElementContentItem_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var item = sender as ElementContentItem;
			if (item != null && e.ChangedButton == MouseButton.Left)
			{
				Point p = item.TranslatePoint(new Point(0, 0), Workbench);
				_DragNew = true;
				_DragStartedItem = CreateItem(p.X, p.Y, item.Element);
				Workbench.Children.Add(_DragStartedItem);
				_MouseStartPoint = e.GetPosition(null);
				_ItemStartPoint = new Point(Canvas.GetLeft(_DragStartedItem), Canvas.GetTop(_DragStartedItem));
				BringToFront(_DragStartedItem);
			}
		}

		private void ElementContentItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = e.Source as ElementContentItem;
			Point p = item.TranslatePoint(new Point(0, 0), Workbench);
			Point center = new Point(Workbench.ActualWidth / 2, Workbench.ActualHeight / 2);
			AddElement(p.X, p.Y, center.X, center.Y, item.Element);
			e.Handled = true;
		}

		private void RecycleBin_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
			{
				ClearAllElements();
			}
			else if (e.ChangedButton == MouseButton.Right)
			{
				// TODO Possible handle a context menu?
				Console.WriteLine("Right-click on Recycle Bin detected...");
			}
			e.Handled = true;
		}

		#endregion

		#region Utility methods

		private void AttemptSave()
		{
			if (_CurrentSaveGameFile != null)
			{
				DoSave(_CurrentSaveGameFile);
			}
			else
			{
				DoSaveAs();
			}
		}

		private void DoSaveAs()
		{
			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.Title = Properties.Resources.SaveTitle;
			saveDialog.InitialDirectory = _LastLocation;
			saveDialog.CheckPathExists = true;
			saveDialog.OverwritePrompt = true;
			saveDialog.Filter = "JSON Files (.json)|*.json";
			bool? isOk = saveDialog.ShowDialog(this);
			if (isOk.HasValue && isOk.Value)
			{
				_CurrentSaveGameFile = saveDialog.FileName;
				DoSave(_CurrentSaveGameFile);
			}
			else if (_AfterSaveAction != null)
			{
				_AfterSaveAction = null;
			}
		}

		private void DoSave(string savedGameFile)
		{
			var workbenchState = Workbench.Children.OfType<ElementContentItem>().Select(item => new ElementDatabase.SavedProgress.ElementOnWorkbench { Element = item.Element, X = Canvas.GetLeft(item), Y = Canvas.GetTop(item) }).ToList();
            _ElementDatabase.SaveProgressAsync(savedGameFile, workbenchState);
		}

		private void DoLoad()
		{
			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.Title = Properties.Resources.LoadTitle;
			openDialog.Multiselect = false;
			openDialog.InitialDirectory = _LastLocation;
			openDialog.CheckPathExists = true;
			openDialog.Filter = "JSON Files (.json)|*.json";
			bool? isOk = openDialog.ShowDialog(this);
			if (isOk.HasValue && isOk.Value)
			{
				_CurrentSaveGameFile = openDialog.FileName;
				_ElementDatabase.LoadProgressAsync(_CurrentSaveGameFile);
			}
		}

		private void DoNewGame()
		{
			_ElementDatabase.NewProgress();
			AddBaseElements(Workbench.ActualWidth / 2, Workbench.ActualHeight / 2);
		}

		private bool CollisionDetection(ElementContentItem item, Point point)
		{
			Rect itemRect = GetItemRect(item);
			return itemRect.Contains(point);
		}

		private bool CollisionDetection(ElementContentItem dragItem, ElementContentItem otherItem)
		{
			Rect dragRect = GetItemRect(dragItem);
			Rect otherRect = GetItemRect(otherItem);
			return dragRect.IntersectsWith(otherRect);
		}

		private bool InRecycleBin(Point point)
		{
			Point originRecycleBin = RecycleBin.TranslatePoint(new Point(0, 0), Workbench);
			Rect recycleBinRect = new Rect(0, 0, originRecycleBin.X + RecycleBin.ActualWidth, originRecycleBin.Y + RecycleBin.ActualHeight);
			return recycleBinRect.Contains(point);
		}

		private Rect GetItemRect(ElementContentItem item)
		{
			return new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.ActualWidth, item.ActualHeight);
        }

		private Point GetCenterPoint(Rect rect)
		{
			return new Point(rect.Width / 2 + rect.X, rect.Height / 2 + rect.Y);
		}

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
				Canvas.SetZIndex(control, 0);
			}
			Canvas.SetZIndex(item, 1);
		}

		private void ClearAllElements()
		{
			// Delete all elements
			foreach (UIElement uiElement in Workbench.Children.OfType<ElementContentItem>().ToList())
			{
				Workbench.Children.Remove(uiElement);
			}
		}

		private void AddElement(double sX, double sY, double dX, double dY, Element element)
		{
			var clone = CreateItem(dX, dY, element);
			Workbench.Children.Add(clone);

			var opAnim = new DoubleAnimation
			{
				From = 0,
				To = 1,
				Duration = TimeSpan.FromMilliseconds(50),
				FillBehavior = FillBehavior.Stop
			};

			var xAnim = new DoubleAnimation
			{
				From = sX,
				To = dX,
				Duration = TimeSpan.FromMilliseconds(150),
				FillBehavior = FillBehavior.Stop
			};

			var yAnim = new DoubleAnimation
			{
				From = sY,
				To = dY,
				Duration = TimeSpan.FromMilliseconds(150),
				FillBehavior = FillBehavior.Stop
			};

			clone.BeginAnimation(UIElement.OpacityProperty, opAnim);
			clone.BeginAnimation(Canvas.LeftProperty, xAnim);
			clone.BeginAnimation(Canvas.TopProperty, yAnim);
		}

		private void AddBaseElements(double cx, double cy)
		{
			// Get the base elements
			List<Element> baseElements = _ElementDatabase.GetBaseElements();
			AddElements(cx, cy, baseElements);
		}

		private void AddElements(double cx, double cy, List<Element> elements)
		{
			// Lay the base elements in a circle around the center
			int n = elements.Count;
			if (n == 1)
			{
				var item = CreateItem(cx - 36, cy - 36, elements[0]);
				Workbench.Children.Add(item);
			}
			else if (n > 1)
			{
				double radOffset = (2 * Math.PI) / n;
				double itemRadius = 50.91168824543142;
				double theta = (Math.PI - radOffset) / 2;
				double distance = itemRadius / Math.Cos(theta);
				for (int i = 0; i < n; i++)
				{
					double angle = radOffset * i;
					double x = cx - 36 + (Math.Cos(angle) * distance);
					double y = cy - 36 - (Math.Sin(angle) * distance);
					var item = CreateItem(x, y, elements[i]);

					var opAnim = new DoubleAnimation
					{
						From = 0,
						To = 1,
						Duration = TimeSpan.FromMilliseconds(50),
						FillBehavior = FillBehavior.Stop
					};

					var xAnim = new DoubleAnimation
					{
						From = cx,
						To = x,
						Duration = TimeSpan.FromMilliseconds(150),
						FillBehavior = FillBehavior.Stop
					};

					var yAnim = new DoubleAnimation
					{
						From = cy,
						To = y,
						Duration = TimeSpan.FromMilliseconds(150),
						FillBehavior = FillBehavior.Stop
					};

					item.BeginAnimation(UIElement.OpacityProperty, opAnim);
					item.BeginAnimation(Canvas.LeftProperty, xAnim);
					item.BeginAnimation(Canvas.TopProperty, yAnim);

					Workbench.Children.Add(item);
				}
			}
		}

		private ElementContentItem CreateItem( double x, double y, Element element)
		{
			ElementContentItem item = new ElementContentItem();
			Canvas.SetLeft(item, x);
			Canvas.SetTop(item, y);
			item.Template = (ControlTemplate) Workbench.Resources["ElementItemTemplate"];
			item.Element = element;
			item.MouseDown += Element_MouseDown;
			item.MouseDoubleClick += Element_MouseDoubleClick;
			item.AllowDrop = true;

			return item;
		}

		#endregion
	}
}
