namespace SBAST.UniversalIntegrator.Ftp
{
    public interface IFilesDownloader
    {
        /// <summary>
        /// Входной метод, проходит по папка из параметров и ищет нужные файлы
        /// </summary>
        void DownloadFiles();
    }
}