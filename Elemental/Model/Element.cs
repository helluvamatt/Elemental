using System.Collections.Generic;

namespace Elemental.Model
{
	public class Element
	{
		public Element()
		{
			Sources = new List<ElementPair>();
		}

		public int Id { get; set; }
		public bool Prime { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }
		public List<ElementPair> Sources { get; private set; }
	}
}
