using SBAST.UniversalIntegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Configs
{
    public class UnArchiverParameters:ModuleParameters
    {
        public string FolderFrom { get; set; }
		public bool RemoveNamespaces { get; set; }
		public string EntityType { get; set; } = "";
        public int MaxDegreeOfParallelism { get; set; }
        public FolderStructureEnum FolderStructure { get; set; }
        public string EntityTypeFromFilename { get; set; } = "";
        public ConverterParameters ConverterParameters { get; set; }
    }
}
