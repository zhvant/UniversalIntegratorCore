using NLog;
using SBAST.UniversalIntegrator.Configs;
using SBAST.UniversalIntegrator.DB;
using SBAST.UniversalIntegrator.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SBAST.UniversalIntegrator.Services;
using System.Globalization;
using SBAST.UniversalIntegrator.Models;

namespace SBAST.UniversalIntegrator.Ftp
{
    /// <summary>
    /// Сервис для скачивания конкретных архивов с фтп и сохранения их в бд
    /// </summary>
    public class SingleFilesDownloader : IFilesDownloader
    {
        private readonly string _downloadFolder;
        private readonly IFtpClient _ftpClient;
        private readonly ILogger _logger;
        private readonly ICheckSumService _checkSumService;
        private readonly IArchiveRepository _archiveRepository;
        private readonly string[] _ftpDownloadFileList;

        public SingleFilesDownloader(string downloadFolder,
            IFtpClient ftpClient,
            ILogger logger,
            ICheckSumService checkSumService,
            IArchiveRepository archiveRepository,
            string[] ftpDownloadFileList)
        {
            _downloadFolder = downloadFolder;
            _ftpClient = ftpClient;
            _logger = logger;
            _checkSumService = checkSumService;
            _archiveRepository = archiveRepository;
            _ftpDownloadFileList = ftpDownloadFileList;
        }

        /// <summary>
		/// Скачивание файлов с указанными путями
		/// </summary>
		public void DownloadFiles()
        {
            if (_ftpDownloadFileList != null)
            {
                int IdArchive = 0;
                uint nDownloadeCount = 0;
                ArchiveFile archiveFile = null;

                if (Directory.Exists(_downloadFolder) == false)
                {
                    Directory.CreateDirectory(_downloadFolder);
                }

                foreach (var path in _ftpDownloadFileList)
                {
                    var fileName = Path.GetFileName(path);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        continue;
                    }
                    var fileFolder = Path.GetDirectoryName(path).TrimStart('\\');

                    var directoryDetailsRows = _ftpClient.ListDirectoryDetails(fileFolder);
                    var file = SelectFileToDownload(fileFolder, fileName, "", directoryDetailsRows);

                    archiveFile = _archiveRepository.FindArchive(file.Name, fileFolder, file.FileSizeReal, file.FileDateReal);
                    if ((archiveFile != null) && (archiveFile.IdArchive > 0) &&
                        (archiveFile.LoadStatus != FileLoadStatusEnum.NotLoaded) &&
                        (archiveFile.LoadStatus != FileLoadStatusEnum.Error) &&
                        (archiveFile.LoadStatus != FileLoadStatusEnum.Repeat))
                    {
                        continue;
                    }
                    IdArchive = archiveFile?.IdArchive ?? 0;
                    IdArchive = _archiveRepository.ArchiveUpdate(IdArchive, file.Name, Models.FileLoadStatusEnum.NotLoaded, file.FileSize, file.Date, file.FileSizeReal, file.FileDateReal, "", fileFolder);

                    // Скачивание
                    _ftpClient.DownloadFile(Path.Combine(fileFolder, file.Name), Path.Combine(_downloadFolder, file.Name));
                    nDownloadeCount++;
                    var md5 = _checkSumService.GetMD5(Path.Combine(_downloadFolder, file.Name));
                    _archiveRepository.ArchiveUpdate(archiveFile?.IdArchive ?? 0, file.Name, Models.FileLoadStatusEnum.Downloaded, file.FileSize, file.Date, file.FileSizeReal, file.FileDateReal, md5, fileFolder);
                    _logger.Info($"File {file.Name} FileSize = {file.FileSize} file.Date = {file.Date} file.FileSizeReal = {file.FileSizeReal} file.FileDateReal = {file.FileDateReal.ToString("yyyy.MM.dd H:mm:ss.fff")} downloaded");
                }
            }
        }

        /// <summary>
        /// Перезакачиваем файл из папки фтп
        /// </summary>
        /// <param name="ftpDownloadedFolder">Папка</param>
        /// <param name="fileName">Имя файла</param>
        public void ReloadFile(string ftpDownloadedFolder, string fileName)
        {
            var directoryDetailsRows = _ftpClient.ListDirectoryDetails(ftpDownloadedFolder);
            List<FileDirectoryInfo> fileList = new List<FileDirectoryInfo>();
            var regex = new Regex(@"^([d-])([rwxt-]{3}){3}\s+\d{1,}\s+.*?(?<size>\d{1,})\s+(?<date>\w+\s+\d{1,2}\s+(?:\d{4})?)(?<time>\d{1,2}:\d{2})?\s+(?<filename>.+?\s?)$",
                                    RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            for (int i = 0; i < directoryDetailsRows.Length; i++)
            {
                string foundFileName = string.Empty;
                var match = regex.Match(directoryDetailsRows[i]);
                long fileSize = 0;
                int fileSizeKb = 0;
                DateTime dateFileLastModified = DateTime.MinValue;

                if (match.Length > 5)
                {
                    var type = match.Groups[1].Value == "d" ? FileTypeEnum.Directory : FileTypeEnum.File;
                    if (type == FileTypeEnum.Directory)
                    {
                        continue;
                    }

                    foundFileName = match.Groups["filename"].Value;
                    bool a = !foundFileName.Equals(fileName);
                    if (!foundFileName.Equals(fileName))
                    {
                        if (i == directoryDetailsRows.Length - 1)
                        {
                            _logger.Warn($"File {Path.Combine(ftpDownloadedFolder, fileName)} not found.");
                        }
                        continue;
                    }

                    fileSize = 0;
                    if (match.Groups["size"] != null)
                    {
                        long.TryParse(match.Groups["size"].Value.Trim(), out fileSize);
                    }
                    if (fileSize == 0)
                    {// Если никак не удалось достать длину файла, то отдельно запросить
                        fileSize = _ftpClient.GetFileSize(Path.Combine(ftpDownloadedFolder, fileName));
                    }
                    fileSizeKb = (int)(fileSize / 1024);

                    // Дата файла
                    dateFileLastModified = _ftpClient.GetFileLastModified(Path.Combine(ftpDownloadedFolder, fileName));

                    var file = new FileDirectoryInfo(fileSizeKb, type, fileName, match.Groups["date"].Value, fileSize, dateFileLastModified);

                    if (file.Type != FileTypeEnum.File)
                    {
                        return;
                    }
                    // Был ли уже скачан?
                    var archiveFile = _archiveRepository.FindArchive(file.Name, ftpDownloadedFolder, file.FileSizeReal, file.FileDateReal);
                    if ((archiveFile == null) ||
                        (archiveFile.LoadStatus == FileLoadStatusEnum.NotLoaded) ||
                        (archiveFile.LoadStatus == FileLoadStatusEnum.Error) ||
                        (archiveFile.LoadStatus == FileLoadStatusEnum.Repeat))
                    {
                        _logger.Warn($"File {Path.Combine(ftpDownloadedFolder, fileName)} not found or marked to be reloaded already.");
                        return;
                    }
                    int IdArchive = archiveFile?.IdArchive ?? 0;
                    IdArchive = _archiveRepository.ArchiveUpdate(IdArchive, file.Name, Models.FileLoadStatusEnum.NotLoaded, file.FileSize, file.Date, file.FileSizeReal, file.FileDateReal, "", ftpDownloadedFolder);
                }
                else
                {
                    _logger.Warn($"Cannot recognize ftp directiory record {directoryDetailsRows[i]}");
                }
            }
        }

        /// <summary>
        /// Выбор файла для скачивания
        /// </summary>
        /// <param name="ftpDownloadedFolder">Имя файла</param>
        /// <param name="filesExt">Расширение файла</param>
        /// <param name="directoryDetailsRows">Список того, что есть в папке</param>
        private FileDirectoryInfo SelectFileToDownload(string ftpFileDirectory, string ftpFileName, string fileExt, string[] directoryDetailsRows)
        {
            List<FileDirectoryInfo> fileList = new List<FileDirectoryInfo>();
            var regex = new Regex(@"^([d-])([rwxt-]{3}){3}\s+\d{1,}\s+.*?(?<size>\d{1,})\s+(?<date>\w+\s+\d{1,2}\s+(?:\d{4})?)(?<time>\d{1,2}:\d{2})?\s+(?<filename>.+?\s?)$",
                                    RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            foreach (string s in directoryDetailsRows)
            {
                string fileName = string.Empty;
                var match = regex.Match(s);
                long fileSize = 0;
                int fileSizeKb = 0;
                DateTime dateFileLastModified = DateTime.MinValue;

                if (match.Length > 5)
                {
                    var type = match.Groups[1].Value == "d" ? FileTypeEnum.Directory : FileTypeEnum.File;
                    if (type == FileTypeEnum.Directory)
                    {
                        continue;
                    }

                    fileName = match.Groups["filename"].Value;
                    if ((fileExt.IsNullOrEmpty() == false)
                        && (fileName.EndsWith(fileExt) == false))
                    {
                        continue;
                    }

                    if (fileName != ftpFileName)
                    {
                        continue;
                    }

                    fileSize = 0;
                    if (match.Groups["size"] != null)
                    {
                        long.TryParse(match.Groups["size"].Value.Trim(), out fileSize);
                    }
                    if (fileSize == 0)
                    {// Если никак не удалось достать длину файла, то отдельно запросить
                        fileSize = _ftpClient.GetFileSize(Path.Combine(ftpFileDirectory, fileName));
                    }
                    fileSizeKb = (int)(fileSize / 1024);

                    // Дата файла
                    dateFileLastModified = _ftpClient.GetFileLastModified(Path.Combine(ftpFileDirectory, fileName));

                    return new FileDirectoryInfo(fileSizeKb, type, fileName, match.Groups["date"].Value, fileSize, dateFileLastModified);
                }
                else
                {
                    _logger.Warn($"Cannot recognize ftp directiory record {s}");
                }
            }
            return null;
        }
    }
}
