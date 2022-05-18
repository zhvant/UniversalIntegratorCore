using SBAST.UniversalIntegrator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Configs
{
    /// <summary>
    /// Метадата модуля для скачивания архивов
    /// </summary>
    class FtpDownloaderParameters : ModuleParameters
    {
        public string DownloadFolder { get; set; }
        public string FtpAddress { get; set; }
        public string FtpLogin { get; set; }
        public string FtpPassword { get; set; }
        public string FtpDownloadFolder { get; set; }
		public string[] FtpDownloadFolderIgnoreList { get; set; }
		public int DownloadInTheLastDays { get; set; }
        public string RegexPatternByDate { get; set; } = string.Empty;
        public int RequestDelay { get; set; }
        public FolderStructureEnum FolderStructure { get; set; }
    }
}
