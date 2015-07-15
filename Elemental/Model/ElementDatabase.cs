using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elemental.Model
{
	public class ElementDatabase
	{
		public ElementDatabase()
		{
			// TODO Actually build the element database from a data file
			_Elements.Add("Christmas_Tree", new Element { Name = "Christmas Tree", Id = "Christmas_Tree", Icon = "icons/Christmas_Tree.png" });
			_Elements.Add("1UP", new Element { Name = "1UP", Id = "1UP", Icon = "icons/1UP.png" });
			_Elements.Add("Mario", new Element { Name = "Mario", Id = "Mario", Icon = "icons/Mario.png" });
		}

		private Dictionary<string, Element> _Elements = new Dictionary<string, Element>();
		private Dictionary<string, bool> _ElementsDiscovered = new Dictionary<string, bool>();

		public Element GetElementById(string id)
		{
			return _Elements.ContainsKey(id) ? _Elements[id] : null;
		}

		public bool IsElementDiscovered(string id)
		{
			return _ElementsDiscovered.ContainsKey(id) ? _ElementsDiscovered[id] : false;
		}

		public ExperimentResult DoExperiment(Element element1, Element element2)
		{
			// TODO Actually process and do the experiment
			return new ExperimentResult() { Success = false, ElementsCreated = null };
		}

        public List<Element> GetBaseElements()
		{
			List<Element> elements = new List<Element>();
			foreach (KeyValuePair<string, Element> kvp in _Elements)
			{
				if (kvp.Value.Sources.Count < 1)
				{
					elements.Add(kvp.Value);
				}
			}
			return elements;
		}

		public class ExperimentResult
		{
			public List<Element> ElementsCreated { get; set; }
			public bool Success { get; set; }
		}
	}
}
