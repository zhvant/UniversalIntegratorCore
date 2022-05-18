using NLog;
using SBAST.UniversalIntegrator.Configs;
using SBAST.UniversalIntegrator.Helpers;
using SBAST.UniversalIntegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SBAST.UniversalIntegrator.Services.Converters
{
    /// <summary>
    /// Сервис для конвертации CSV в XML
    /// </summary>
    public class CsvToXmlConverter : IFileConverterService
    {
        private ILogger _logger;
        private readonly CsvToXmlParameters _parameters;

        public CsvToXmlConverter(ILogger logger, CsvToXmlParameters parameters)
        {
            _logger = logger;
            _parameters = parameters;
        }

        public string Convert(string fileName, string fileBody)
        {
            char delimeter = _parameters.Delimeter;
            bool hasHeaders = _parameters.HasHeaders;
            string rootTagFromFilename = _parameters.RootTagFromFilename;
            string rootName = _parameters.DefaultRootTag;
            TagCaseEnum tagcase = _parameters.TagCase;

            string[] lines = fileBody.Split('\n');
            string[] titles = lines[0].Split(delimeter);

            string itemTag = "item";
            if (tagcase == TagCaseEnum.Upper)
                itemTag = "ITEM";
  
            for (int i = 0; i < titles.Length; i++)
            {
                if (!hasHeaders)
                    titles[i] = itemTag + i;

                if (tagcase == TagCaseEnum.Lower)
                    titles[i] = titles[i].ToLower();
                else if (tagcase == TagCaseEnum.Upper)
                    titles[i] = titles[i].ToUpper();
            }  
            
            if (!rootTagFromFilename.IsNullOrEmpty())
            {
                var regex = new Regex(rootTagFromFilename, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
                var match = regex.Match(fileName);
                if (rootTagFromFilename.IndexOf("<rootTag>") < 0)
                    throw new ArgumentException("Параметр RootTagFromFileName не был обработан. Регулярное выражение должно содержать группу <rootTag>.");
                else
                    rootName = match.Groups["rootTag"].Value;

            }

            if (tagcase == TagCaseEnum.Upper)
                rootName = rootName.ToUpper();
            else
                rootName = rootName.ToLower();

            XElement xml = new XElement(rootName);
            for (int i = hasHeaders ? 1 : 0; i < lines.Length; i++)
            {
                XElement item = new XElement(itemTag);
                string[] elems = lines[i].Split(_parameters.Delimeter);
                for (int k = 0; k < titles.Length; k++)
                {
                    item.Add(new XElement(titles[k], elems[k]));
                }
                xml.Add(item);
            }

            return xml.ToString();
        }
    }
}
