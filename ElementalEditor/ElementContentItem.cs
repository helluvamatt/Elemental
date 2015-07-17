using Elemental.Data.Model;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ElementalEditor
{
	public class ElementContentItem : ContentControl
	{
		private const string BASE64_IMAGE_DATA_HEADER = "data:image/png;base64,";

		public Element Element
		{
			get
			{
				return (Element)GetValue(ElementProperty);
			}
			set
			{
				SetValue(ElementProperty, value);
			}
		}

		public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
			"Element",
			typeof(Element),
			typeof(ElementContentItem),
			new PropertyMetadata()
			);

		private bool _Collision;
		public bool Collision
		{
			get
			{
				return _Collision;
			}
			set
			{
				_Collision = value;
				Rectangle border = GetTemplateChild("BorderRect") as Rectangle;
				if (border != null)
				{
					border.Stroke = new SolidColorBrush(_Collision ? Colors.Red : Colors.Black);
				}
			}
		}

		public ImageSource IconSource
		{
			get
			{
				if (Element != null)
				{
					BitmapImage bi = new BitmapImage();
					bi.BeginInit();
					if (Element.Icon.StartsWith(BASE64_IMAGE_DATA_HEADER))
					{
						byte[] binaryData = Convert.FromBase64String(Element.Icon.Substring(BASE64_IMAGE_DATA_HEADER.Length));
						bi.StreamSource = new MemoryStream(binaryData);
					}
					else
					{
						bi.UriSource = new Uri(Element.Icon);
					}
					bi.EndInit();
					return bi;
				}
				return null;
			}
		}

		public string SourcesCount
		{
			get
			{
				return string.Format("{0} combinations", Element.Sources != null ? Element.Sources.Count : 0);
			}
		}
	}
}
