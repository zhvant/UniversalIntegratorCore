using SBAST.UniversalIntegrator.Ftp;
using Quartz;

namespace SBAST.UniversalIntegrator.Modules
{
    /// <summary>
    /// Модуль для скачивания котнкретных файлов с фтп
    /// </summary>
    [DisallowConcurrentExecution]
    public class FtpSingleFileDownloader : BaseModule
    {
        protected override void RunModule()
        {
            WithMesure<IFilesDownloader>(service => service.DownloadFiles());
        }
    }
}
