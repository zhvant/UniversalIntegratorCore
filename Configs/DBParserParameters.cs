using SBAST.UniversalIntegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Configs
{
    /// <summary>
    /// Методата парсера
    /// </summary>
    public class DBParserParameters: ModuleParameters
    {
        public string ParsingProcedure { get; set; }
        public string EntityType { get; set; }
        public int BatchSize { get; set; }
        public string FilesGetProcedure { get; set; }
    }
}
