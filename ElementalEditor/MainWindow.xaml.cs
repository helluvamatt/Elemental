using Elemental.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace ElementalEditor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ElementDatabase _ElementDatabase;

		private string _LastLocation;
		private string _CurrentDatabaseFile;
		private Action _AfterSaveAction;

		public MainWindow()
		{
			InitializeComponent();

			_ElementDatabase = new ElementDatabase();
			_ElementDatabase.DatabaseLoaded += _ElementDatabase_DatabaseLoaded;
			_ElementDatabase.DatabaseChanged += _ElementDatabase_DatabaseChanged;
			_ElementDatabase.DatabaseSaved += _ElementDatabase_DatabaseSaved;
			_ElementDatabase.DatabaseError += _ElementDatabase_DatabaseError;
			_ElementDatabase.DatabaseOpenChanged += _ElementDatabase_DatabaseOpenChanged;

			_LastLocation = Path.GetDirectoryName(Path.GetFullPath(Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath)));
        }

		#region Event handlers

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			DoLoad();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_ElementDatabase.DatabaseDirty)
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

		private void _ElementDatabase_DatabaseError(Exception ex)
		{
			// TODO Hide Working...
			Console.WriteLine("DatabaseError: " + ex.Message);
			Console.WriteLine(ex.StackTrace);

			MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void _ElementDatabase_DatabaseLoaded()
		{
			// TODO Hide Working...
			_ElementDatabase_DatabaseChanged();
		}

		private void _ElementDatabase_DatabaseChanged()
		{
			ElementData.ItemsSource = _ElementDatabase.GetAllElements().OrderBy(e => e.Name).Select(e => new ElementContentItem { Element = e });
		}

		private void _ElementDatabase_DatabaseSaved()
		{
			// TODO Hide Working...
		}

		private void _ElementDatabase_DatabaseOpenChanged(bool isOpen)
		{
			Dispatcher.Invoke(() =>
			{
				// If we have a database loaded, enabled certain menu items
				MenuItem_File_New_Element.IsEnabled = isOpen;
				MenuItem_File_CloseDatabase.IsEnabled = isOpen;

				// If a database is closed
				if (!isOpen)
				{
					// Empty the grid view
					ElementData.ItemsSource = null;
				}
			});
		}

		private void MenuItem_File_New_Database_Click(object sender, RoutedEventArgs e)
		{
			if (_ElementDatabase.ProgressDirty)
			{
				var result = MessageBox.Show(this, Properties.Resources.SaveQuestion, Properties.Resources.Question, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result)
				{
					case MessageBoxResult.Yes:
						_AfterSaveAction = () => _ElementDatabase.NewDatabase();
						AttemptSave();
						break;
					case MessageBoxResult.No:
						_ElementDatabase.NewDatabase();
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			}
			else
			{
				_ElementDatabase.NewDatabase();
			}
		}

		private void MenuItem_File_New_Element_Click(object sender, RoutedEventArgs e)
		{
			// Only if there is a database loaded
			if (_ElementDatabase.DatabaseOpen)
			{
				// TODO Launch Element editor dialog
			}
		}

		private void MenuItem_File_OpenDatabase_Click(object sender, RoutedEventArgs e)
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

		private void MenuItem_File_CloseDatabase_Click(object sender, RoutedEventArgs e)
		{
			if (_ElementDatabase.ProgressDirty)
			{
				var result = MessageBox.Show(this, Properties.Resources.SaveQuestion, Properties.Resources.Question, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				switch (result)
				{
					case MessageBoxResult.Yes:
						_AfterSaveAction = () => _ElementDatabase.CloseDatabase();
						AttemptSave();
						break;
					case MessageBoxResult.No:
						_ElementDatabase.CloseDatabase();
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			}
			else
			{
				_ElementDatabase.CloseDatabase();
			}
		}

		private void MenuItem_File_Quit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void MenuItem_Edit_Duplicate_Click(object sender, RoutedEventArgs e)
		{
			// TODO Check selection, make duplicate(s) of selected element(s)
		}

		private void MenuItem_Edit_Delete_Click(object sender, RoutedEventArgs e)
		{
			// Check selection, delete selected element(s)
			if (ElementData.SelectedItems.Count > 0)
			{
				_ElementDatabase.Delete(ElementData.SelectedItems.Cast<ElementContentItem>().Select(item => item.Element.Id));
			}
		}

		private void ElementData_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// If we have a selection, enable certain menu items
			bool hasSelection = ElementData.SelectedItems.Count > 0;
			MenuItem_Edit_Delete.IsEnabled = hasSelection;
			MenuItem_Edit_Duplicate.IsEnabled = hasSelection;
			e.Handled = true;
		}

		private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			DataGridRow row = sender as DataGridRow;
			ElementContentItem item = row.Item as ElementContentItem;
			// TODO Do open edit dialog with the element

			Console.WriteLine("Edit requested for element: '{0}' ({1})", item.Element.Name, item.Element.Id);
		}

		#endregion

		#region Utility methods

		private void AttemptSave()
		{
			if (_CurrentDatabaseFile != null)
			{
				DoSave(_CurrentDatabaseFile);
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
				_CurrentDatabaseFile = saveDialog.FileName;
				_LastLocation = Path.GetDirectoryName(_CurrentDatabaseFile);
				DoSave(_CurrentDatabaseFile);
			}
			else if (_AfterSaveAction != null)
			{
				_AfterSaveAction = null;
			}
		}

		private void DoSave(string databaseFile)
		{
			// TODO Show working...
			_ElementDatabase.SaveDatabaseAsync(databaseFile);
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
				_CurrentDatabaseFile = openDialog.FileName;
				_LastLocation = Path.GetDirectoryName(_CurrentDatabaseFile);
				LoadDatabase(_CurrentDatabaseFile);
			}
		}

		private void LoadDatabase(string filename)
		{
			// TODO Show working...
			_ElementDatabase.LoadDatabaseAsync(filename);
		}

		#endregion
	}
}
