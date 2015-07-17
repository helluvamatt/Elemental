using Elemental.Async;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Elemental.Model
{
	public class ElementDatabase
	{
		public ElementDatabase()
		{
			// TODO Actually build the element database from a data file
			_Elements.Add(0, new Element { Id = 0, Name = "Christmas Tree", Icon = "icons/Christmas_Tree.png", Prime = true });
			_Elements.Add(1, new Element { Id = 1, Name = "1UP", Icon = "icons/1UP.png", Prime = true });
			_Elements.Add(2, new Element { Id = 2, Name = "Mario", Icon = "icons/Mario.png", Prime = false });
		}

		private Dictionary<int, Element> _Elements = new Dictionary<int, Element>();
		private Dictionary<int, bool> _ElementsDiscovered = new Dictionary<int, bool>();

		public bool OnlyUndiscovered { get; set; }

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
					string exePath = Path.GetDirectoryName(Path.GetFullPath(Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath)));
                    jsonFile = Path.Combine(exePath, jsonFile);
				}
				using (JsonTextReader jsonReader = new JsonTextReader(new StreamReader(jsonFile)))
				{
					var newElements = new JsonSerializer().Deserialize<Dictionary<int, Element>>(jsonReader);
					_Elements = newElements;
					_ElementsDiscovered = _Elements.Where(kvp => kvp.Value.Prime).ToDictionary(kvp => kvp.Key, kvp => true);
				}
				return new BasicResult { Success = true, Exception = null };
			}
			catch (Exception ex)
			{
				return new BasicResult { Success = false, Exception = ex };
			}
		}

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

		public event Action DatabaseLoaded;
		public event Action<Exception> DatabaseError;
		public event Action<List<Element>> DiscoveredElementsChanged;

		public Element GetElementById(int id)
		{
			return _Elements.ContainsKey(id) ? _Elements[id] : null;
		}

		public bool IsElementDiscovered(int id)
		{
			return _ElementsDiscovered.ContainsKey(id) ? _ElementsDiscovered[id] : false;
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

		public class ExperimentResult
		{
			public List<Element> ElementsCreated { get; set; }
			public bool Success { get; set; }
		}
	}
}
