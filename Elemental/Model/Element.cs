using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Elemental.Model
{
	public class Element
	{
		public Element()
		{
			Sources = new List<ElementPair>();
		}

		public string Id { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }
		public List<ElementPair> Sources { get; private set; }
	}
}
