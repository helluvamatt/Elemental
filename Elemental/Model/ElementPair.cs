using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elemental.Model
{
	public class ElementPair
	{
		public int Element1 { get; set; }
		public int Element2 { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is ElementPair)
			{
				var e = obj as ElementPair;
				return (e.Element1 == this.Element1 && e.Element2 == this.Element2) || (e.Element1 == this.Element2 && e.Element2 == this.Element1);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Element1 ^ Element2;
		}
	}
}
