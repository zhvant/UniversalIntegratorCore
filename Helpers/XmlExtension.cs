using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SBAST.UniversalIntegrator.Helpers
{
    /// <summary>
    /// Сервис для определения типа файла
    /// </summary>
    public class XmlExtension 
    {
        private XDocument _xDoc;
        
        public XmlExtension (string xml)
        {
			_xDoc = XDocument.Parse(xml); 
        }

        /// <summary>
        /// Возвращает наименование корневого тэга
        /// </summary>
        public string GetRootType(string excludeName = "export")
        {
            var rootName = _xDoc.Root.Name.ToString();
            if (rootName != excludeName)
                return rootName;

            if (_xDoc.Root.FirstNode.NodeType == XmlNodeType.Element)
                return ((XElement)_xDoc.Root.FirstNode).Name.ToString();

            return rootName;
        }

        /// <summary>
        /// Возвращает обработанную xml
        /// </summary>
        public string GetXml()
        {
            return _xDoc.ToString();
        }

		/// <summary>
		/// Убирает все встреченные в файле namespac'ы в XmlNode'е
		/// </summary>
		public void RemoveNamespaces()
		{
            var root = _xDoc.Root;
            root.Attributes().Remove();
            root.Name = XNamespace.None.GetName(root.Name.LocalName);
            root.Descendants()
				   .Attributes()
				   .Where(x => x.IsNamespaceDeclaration)
				   .Remove();
			foreach (var el in root.Descendants())
			{
				if (el.Name != el.Name.LocalName)
					el.Name = el.Name.LocalName;
				if (el.HasAttributes)
				{ 
					foreach (var attr in el.Attributes())
					{
						if (attr.Name != attr.Name.LocalName)
						{
							var elem = attr.Parent;
							attr.Remove();
							elem.Add(new XAttribute(attr.Name.LocalName, attr.Value));
						}
					}
				}
			}
		}
    }
}
