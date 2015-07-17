using Common.Data.Async;
using Elemental.Data.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Elemental.Data
{
	public class ElementDatabase
	{
		public ElementDatabase()
		{
			// Empty constructor, database load happens later
		}

		#region Private members / helpers

		private Dictionary<int, Element> _Elements = new Dictionary<int, Element>();
		private Dictionary<int, bool> _ElementsDiscovered = new Dictionary<int, bool>();

		private string ExePath
		{
			get
			{
				return Path.GetDirectoryName(Path.GetFullPath(Uri.UnescapeDataString(new Uri(Assembly.GetEntryAssembly().CodeBase).AbsolutePath)));
            }
		}

		#endregion

		#region Public properties

		public bool OnlyUndiscovered { get; set; }

		private bool _DatabaseOpen;
		public bool DatabaseOpen
		{
			get
			{
				return _DatabaseOpen;
			}
			private set
			{
				_DatabaseOpen = value;
				if (DatabaseOpenChanged != null) DatabaseOpenChanged.Invoke(_DatabaseOpen);
			}
		}

		public bool ProgressDirty { get; set; }
		public bool DatabaseDirty { get; set; }

		#endregion

		#region Loading and Saving

		public void LoadDatabaseAsync(string jsonFile)
		{
			new AsyncRunner<BasicResult, string>().AsyncRun(LoadDatabase, HandledDatabaseLoaded, jsonFile);
        }

		public BasicResult LoadDatabase(string jsonFile)
		{
			try
			{
				if (!Path.IsPathRooted(jsonFile))
				{
                    jsonFile = Path.Combine(ExePath, jsonFile);
				}
				using (JsonTextReader jsonReader = new JsonTextReader(new StreamReader(jsonFile)))
				{
					var newElements = new JsonSerializer().Deserialize<Dictionary<int, Element>>(jsonReader);
					_Elements = newElements;
				}
				DatabaseOpen = true;
				DatabaseDirty = false;
				ProgressDirty = false;
				return new BasicResult { Success = true, Exception = null };
			}
			catch (Exception ex)
			{
				return new BasicResult { Success = false, Exception = ex };
			}
		}

		public void SaveDatabaseAsync(string jsonFile)
		{
			new AsyncRunner<BasicResult, string>().AsyncRun(SaveDatabase, HandledDatabaseSaved, jsonFile);
		}

		public BasicResult SaveDatabase(string jsonFile)
		{
			try
			{
				if (!Path.IsPathRooted(jsonFile))
				{
					jsonFile = Path.Combine(ExePath, jsonFile);
				}
				using (JsonTextWriter jsonWriter = new JsonTextWriter(new StreamWriter(jsonFile)))
				{
					new JsonSerializer().Serialize(jsonWriter, _Elements);
				}
				DatabaseDirty = false;
				return new BasicResult { Success = true, Exception = null };
			}
			catch (Exception ex)
			{
				return new BasicResult { Success = false, Exception = ex };
			}
		}

		public void LoadProgressAsync(string jsonFile)
		{
			new AsyncRunner<LoadProgressResult, string>().AsyncRun(LoadProgress, HandledProgressLoaded, jsonFile);
		}

		public LoadProgressResult LoadProgress(string jsonFile)
		{
			try
			{
				if (!Path.IsPathRooted(jsonFile))
				{
					jsonFile = Path.Combine(ExePath, jsonFile);
				}
				SavedProgress progress = null;
				if (File.Exists(jsonFile))
				{
					// Existing progress file
					using (JsonTextReader jsonReader = new JsonTextReader(new StreamReader(jsonFile)))
					{
						progress = new JsonSerializer().Deserialize<SavedProgress>(jsonReader);
						_ElementsDiscovered = progress.DiscoveredElements;
					}
				}
				else
				{
					// New game
					progress = new SavedProgress();
					SaveProgress(new SaveProgressRequest { JsonFile = jsonFile, WorkbenchState = progress.WorkbenchState });
				}
				ProgressDirty = false;
				return new LoadProgressResult { Success = true, Exception = null, WorkbenchState = progress.WorkbenchState };
			}
			catch (Exception ex)
			{
				return new LoadProgressResult { Success = false, Exception = ex, WorkbenchState = null };
			}
		}

		public void SaveProgressAsync(string jsonFile, List<SavedProgress.ElementOnWorkbench> workbenchState)
		{
			new AsyncRunner<BasicResult, SaveProgressRequest>().AsyncRun(SaveProgress, HandledProgressSaved, new SaveProgressRequest { JsonFile = jsonFile, WorkbenchState = workbenchState });
		}

		public BasicResult SaveProgress(SaveProgressRequest request)
		{
			string jsonFile = request.JsonFile;
			try
			{
				if (!Path.IsPathRooted(jsonFile))
				{
					jsonFile = Path.Combine(ExePath, jsonFile);
				}
				SavedProgress progress = new SavedProgress();
				progress.DiscoveredElements = _ElementsDiscovered;
				progress.WorkbenchState = request.WorkbenchState;
				using (JsonTextWriter jsonWriter = new JsonTextWriter(new StreamWriter(jsonFile)))
				{
					new JsonSerializer().Serialize(jsonWriter, progress);
				}
				ProgressDirty = false;
				return new BasicResult { Success = true, Exception = null };
			}
			catch (Exception ex)
			{
				return new BasicResult { Success = false, Exception = ex };
			}
		}

		public void NewDatabase()
		{
			_Elements.Clear();
			_ElementsDiscovered.Clear();
			DatabaseOpen = true;
			DatabaseDirty = false;
			ProgressDirty = false;
			HandledDatabaseLoaded(new BasicResult { Success = true });
		}

		public void CloseDatabase()
		{
			_Elements.Clear();
			_ElementsDiscovered.Clear();
			DatabaseOpen = false;
		}

		public void NewProgress()
		{
			ProgressDirty = false;
			_ElementsDiscovered = _Elements.Where(kvp => kvp.Value.Prime).ToDictionary(kvp => kvp.Key, kvp => true);
			HandledProgressLoaded(new LoadProgressResult { Success = true, Exception = null, WorkbenchState = new List<SavedProgress.ElementOnWorkbench>() });
		}

		#endregion

		#region Private utility methods

		private void HandledDatabaseLoaded(BasicResult result)
		{
			if (result.Success)
			{
				if (DatabaseLoaded != null) DatabaseLoaded.Invoke();
				DoDiscoveredElementsChanged();
			}
			else
			{
				if (DatabaseError != null) DatabaseError.Invoke(result.Exception);
			}
		}

		private void HandledDatabaseSaved(BasicResult result)
		{
			if (result.Success)
			{
				if (DatabaseSaved != null) DatabaseSaved.Invoke();
			}
			else
			{
				if (DatabaseError != null) DatabaseError.Invoke(result.Exception);
			}
		}

		private void HandledProgressLoaded(LoadProgressResult result)
		{
			if (result.Success)
			{
				if (ProgressLoaded != null) ProgressLoaded.Invoke(result.WorkbenchState);
				DoDiscoveredElementsChanged();
			}
			else
			{
				if (DatabaseError != null) DatabaseError.Invoke(result.Exception);
			}
		}

		private void HandledProgressSaved(BasicResult result)
		{
			if (result.Success)
			{
				if (ProgressSaved != null) ProgressSaved.Invoke();
			}
			else
			{
				if (DatabaseError != null) DatabaseError.Invoke(result.Exception);
			}
		}

		private void DoDiscoveredElementsChanged()
		{
			if (DiscoveredElementsChanged != null)
			{
				List<Element> discoveredElements = _ElementsDiscovered
					.Where(kvp => kvp.Value && _Elements.ContainsKey(kvp.Key))
					.Select(kvp => _Elements[kvp.Key])
					.OrderBy(e => e.Name)
					.ToList();
				DiscoveredElementsChanged.Invoke(discoveredElements);
			}
		}

		private void CopySources(Element src, Element dest)
		{
			// Perform a deep copy (ints are copy-by-value)
			foreach (ElementPair pair in src.Sources)
			{
				dest.Sources.Add(new ElementPair { Element1 = pair.Element1, Element2 = pair.Element2 });
			}
		}

		private void ChangeDatabase()
		{
			DatabaseDirty = true;
			if (DatabaseChanged != null) DatabaseChanged.Invoke();
		}

		#endregion

		#region Events

		public event Action DatabaseLoaded;
		public event Action DatabaseSaved;
		public event Action DatabaseChanged;
		public event Action<List<SavedProgress.ElementOnWorkbench>> ProgressLoaded;
		public event Action ProgressSaved;
		public event Action<Exception> DatabaseError;
		public event Action<List<Element>> DiscoveredElementsChanged;
		public event Action<bool> DatabaseOpenChanged;

		#endregion

		#region Public methods

		public Element GetElementById(int id)
		{
			return _Elements.ContainsKey(id) ? _Elements[id] : null;
		}

		public bool IsElementDiscovered(int id)
		{
			return _ElementsDiscovered.ContainsKey(id) ? _ElementsDiscovered[id] : false;
		}

		public List<Element> GetAllElements()
		{
			return _Elements.Select(kvp => kvp.Value).ToList();
		}

		public ExperimentResult DoExperiment(Element element1, Element element2)
		{
			// Start by finding all the elements which can be created by this pair
			List<Element> elements = new List<Element>();
			ElementPair reagents = new ElementPair() { Element1 = element1.Id, Element2 = element2.Id };
			foreach (KeyValuePair<int, Element> kvp in _Elements)
			{
				if (kvp.Value.Sources.Contains(reagents))
				{
					// Possibly filter based on whether we have discovered this element
					if (!OnlyUndiscovered || !IsElementDiscovered(kvp.Key))
					{
						elements.Add(kvp.Value);
						_ElementsDiscovered[kvp.Key] = true;
					}
				}
			}

			// Experiment is a success if we found (new) elements
			bool success = elements.Count > 0;
			if (success)
			{
				ProgressDirty = true;
				DoDiscoveredElementsChanged();
			}
			else
			{
				// If it wasn't a success, return the original two elements
				elements.Add(element1);
				elements.Add(element2);
			}

			// Return the result
			return new ExperimentResult() { Success = success, ElementsCreated = elements };
		}

        public List<Element> GetBaseElements()
		{
			return _Elements.Where(kvp => kvp.Value.Prime).Select(kvp => kvp.Value).ToList();
		}

		public int GetNextElementId()
		{
			return _Elements.Max(kvp => kvp.Key) + 1;
		}

		public void SaveElement(Element element)
		{
			_Elements[element.Id] = element;
			ChangeDatabase();
		}

		public Element Duplicate(Element e)
		{
			var clone = new Element
			{
				Id = GetNextElementId(),
				Name = string.Copy(e.Name),
				Prime = e.Prime,
				Icon = string.Copy(e.Icon),
			};
			CopySources(e, clone);
			return clone;
		}

		public void Delete(int id)
		{
			if (_Elements.ContainsKey(id))
			{
				_Elements.Remove(id);
				ChangeDatabase();
			}
		}

		public void Delete(IEnumerable<int> ids)
		{
			foreach (int id in ids)
			{
				if (_Elements.ContainsKey(id))
				{
					_Elements.Remove(id);
				}
			}
			ChangeDatabase();
		}

		#endregion

		#region Utility classes

		public class SavedProgress
		{
			public SavedProgress()
			{
				DiscoveredElements = new Dictionary<int, bool>();
				WorkbenchState = new List<ElementOnWorkbench>();
			}

			public Dictionary<int, bool> DiscoveredElements { get; set; }
			public List<ElementOnWorkbench> WorkbenchState { get; set; }

			public class ElementOnWorkbench
			{
				public Element Element { get; set; }
				public double X { get; set; }
				public double Y { get; set; }
			}
		}

		public class SaveProgressRequest
		{
			public string JsonFile { get; set; }
			public List<SavedProgress.ElementOnWorkbench> WorkbenchState { get; set; }
		}

		public class LoadProgressResult : BasicResult
		{
			public List<SavedProgress.ElementOnWorkbench> WorkbenchState { get; set; }
		}

		public class ExperimentResult
		{
			public List<Element> ElementsCreated { get; set; }
			public bool Success { get; set; }
		}

		#endregion
	}
}
