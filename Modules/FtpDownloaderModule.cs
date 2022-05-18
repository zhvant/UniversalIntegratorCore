using SBAST.UniversalIntegrator.Ftp;
using Quartz;

namespace SBAST.UniversalIntegrator.Modules
{
    /// <summary>
    /// Модуль для скачивания файлов с фтп
    /// </summary>
    [DisallowConcurrentExecution]
    public class FtpDownloaderModule : BaseModule
    {
        protected override void RunModule()
        {
            WithMesure<IFilesDownloader>(service => service.DownloadFiles());
        }
    }
}
