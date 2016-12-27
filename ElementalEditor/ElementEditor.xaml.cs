using Elemental.Data.Model;
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
using System.Windows.Shapes;

namespace ElementalEditor
{
	/// <summary>
	/// Interaction logic for ElementEditor.xaml
	/// </summary>
	public partial class ElementEditor : Window
	{
		public Element Element { get; private set; }

		public ElementEditor(Element element)
		{
			Element = element;
			InitializeComponent();
		}


	}
}
