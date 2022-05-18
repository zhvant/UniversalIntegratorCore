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
    /// Сервис для скачивания архивов с фтп и сохранения их в бд
    /// </summary>
    public class FilesDownloader : IFilesDownloader
    {
        private readonly string _ftpDownloadFolder;
        private readonly string _downloadFolder;
        private readonly IFtpClient _ftpClient;
        private readonly ILogger _logger;
        private readonly ICheckSumService _checkSumService;
        private readonly IArchiveRepository _archiveRepository;
		private readonly int _downloadInTheLastDays;
        private readonly string[] _ftpDownloadFolderIgnoreList;
        private readonly string _regexPatternByDate;
        private readonly FolderStructureEnum _folderStructure;

        public FilesDownloader(string ftpDownloadFolder,
            string downloadFolder,
            IFtpClient ftpClient,
            ILogger logger,
            ICheckSumService checkSumService,
            IArchiveRepository archiveRepository,
            string[] ftpDownloadFolderIgnoreList,
            int downloadInTheLastDays,
            string regexPatternByDate,
            FolderStructureEnum folderStructure)
        {
            _ftpDownloadFolder = ftpDownloadFolder;
            _downloadFolder = downloadFolder;
            _ftpClient = ftpClient;
            _logger = logger;
            _checkSumService = checkSumService;
            _archiveRepository = archiveRepository;
			_ftpDownloadFolderIgnoreList = ftpDownloadFolderIgnoreList;
			_downloadInTheLastDays = downloadInTheLastDays;
            _regexPatternByDate = string.IsNullOrWhiteSpace(regexPatternByDate)? @"(.+?)_(.+?)_(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})_(.+?)\s?$" : regexPatternByDate;
            _folderStructure = folderStructure;

            //ReloadFile("ETP_SBAST", "bankGuarantee_2021.6.zip");
        }

		/// <summary>
		/// Проверят директорию на необходимость её игнорирования
		/// </summary>
		/// <param name="fullPath">Проверяемый католог</param>
		/// <returns>Не игнорируем</returns>
		private bool IsNotIgnore(string fullPath)
		{
			if (_ftpDownloadFolderIgnoreList != null)
			{
				foreach (string folder in _ftpDownloadFolderIgnoreList)
					if (!String.IsNullOrEmpty(folder))
					{
						if (folder.StartsWith("/") && fullPath == folder.Substring(1).Replace("/", "\\"))
							return (false);
						else if (fullPath.EndsWith(folder.Replace("/", "\\")))
							return (false);
					}
			}
			return (true);
		}

		/// <summary>
		/// Входной метод, проходит по папка из параметров и ищет нужные файлы
		/// </summary>
		public void DownloadFiles()
        {
            try
            {
                var folder = (_ftpDownloadFolder ?? "").Replace("/", "\\");
                try
                {
                    EnterFolder("", folder);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error with downloading files from ftp folder '{folder}'\n{ex.Message}");
                    _logger.Error($"Error with downloading files from ftp folder '{folder}'\n{ex}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error with downloading files from ftp folder level 2\n {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивно заходим в папку на фтп, для возможности задания сложных путей типа /out/*/CurrentMonth
        /// </summary>
        /// <param name="folder">Папка</param>
        /// <param name="lastPartOfFolder">Оставшаяся часть папки</param>
        private void EnterFolder(string folder, string lastPartOfFolder)
		{
            if (IsNotIgnore(folder))
			{
				var lastIndexSlash = lastPartOfFolder.IndexOf('\\', 1);
				if (lastIndexSlash != -1)
				{
					var subFolder = lastPartOfFolder.Substring(0, lastIndexSlash);
                    if (!subFolder.Replace(@"\", "").Contains("*"))
                    {
                        EnterFolder(Path.Combine(folder, subFolder.TrimStart('\\')), lastPartOfFolder.Substring(lastIndexSlash, lastPartOfFolder.Length - lastIndexSlash));
                    }
                    else
                    {
                        var searchFolder = Path.Combine(folder, subFolder.Replace(@"\", ""));
                        var directoryDetailsRows = _ftpClient.ListDirectoryDetails(searchFolder);

                        var filesList = SelectFolders(directoryDetailsRows);
                        filesList
                            .Where(x => x.Type == FileTypeEnum.Directory)
                            .ForEach(x =>
                            {
                                var flStart = x.Name.TrimStart('\\');
                                var flFolder = Path.Combine(folder, flStart);
                                var flLastPartOfFolder = lastPartOfFolder.Substring(lastIndexSlash, lastPartOfFolder.Length - lastIndexSlash);
                                EnterFolder(flFolder, flLastPartOfFolder);
                            });
                    }
				}
				else
				{
					var asteriskIndex = lastPartOfFolder.IndexOf('*');
					if (!lastPartOfFolder.IsNullOrEmpty() && asteriskIndex == -1)
						DownloadFiles(Path.Combine(folder, lastPartOfFolder.TrimStart('\\')));
					else if (asteriskIndex == -1)
						DownloadFiles(folder);
					else
						DownloadFiles(folder, lastPartOfFolder.Substring(asteriskIndex + 1, lastPartOfFolder.Length - asteriskIndex - 1));
				}
			}
		}
		/// <summary>
		/// Скачиваем файлы из папки фтп
		/// </summary>
		/// <param name="ftpDownloadedFolder">Папка</param>
		/// <param name="filesExt">Расширение файла</param>
		private void DownloadFiles(string ftpDownloadedFolder, string filesExt = "")
        {
            int IdArchive = 0;
            uint nDownloadeCount = 0;
            ArchiveFile archiveFile = null;
            var DirectoryDetailsRows = _ftpClient.ListDirectoryDetails(ftpDownloadedFolder);
            var fileList = SelectFilesToDownload(ftpDownloadedFolder, filesExt, DirectoryDetailsRows);

            _logger.Info($"Begin download {fileList.Count} files from {ftpDownloadedFolder}");
            foreach (var file in fileList)
            {
                if(file.Type != FileTypeEnum.File)
                {
                    continue;
                }
                // Был ли уже скачан?
                archiveFile = _archiveRepository.FindArchive(file.Name, ftpDownloadedFolder, file.FileSizeReal, file.FileDateReal);
                if ((archiveFile != null) && (archiveFile.IdArchive > 0) &&
                    (archiveFile.LoadStatus != FileLoadStatusEnum.NotLoaded) &&
                    (archiveFile.LoadStatus != FileLoadStatusEnum.Error) &&
                    (archiveFile.LoadStatus != FileLoadStatusEnum.Repeat))
                {
                    continue;
                }
                IdArchive = archiveFile?.IdArchive ?? 0;
                IdArchive = _archiveRepository.ArchiveUpdate(IdArchive, file.Name, Models.FileLoadStatusEnum.NotLoaded, file.FileSize, file.Date, file.FileSizeReal, file.FileDateReal, "", ftpDownloadedFolder);

                string fileFolder = "";

                switch (_folderStructure)
                {
                    default:
                    case FolderStructureEnum.Default:
                    case FolderStructureEnum.Single:
                        fileFolder = _downloadFolder;
                        break;
                    case FolderStructureEnum.Copy:
                        fileFolder = Path.Combine(_downloadFolder, ftpDownloadedFolder);
                        break;
                }

                // Скачивание
                if (Directory.Exists(fileFolder) == false)
                {
                    Directory.CreateDirectory(fileFolder);
                }
                _ftpClient.DownloadFile(Path.Combine(ftpDownloadedFolder, file.Name), Path.Combine(fileFolder, file.Name));
                nDownloadeCount++;
                var md5 = _checkSumService.GetMD5(Path.Combine(fileFolder, file.Name));
                _archiveRepository.ArchiveUpdate(archiveFile?.IdArchive ?? 0, file.Name, Models.FileLoadStatusEnum.Downloaded, file.FileSize, file.Date, file.FileSizeReal, file.FileDateReal, md5, ftpDownloadedFolder);
                _logger.Info($"File {file.Name} FileSize = {file.FileSize} file.Date = {file.Date} file.FileSizeReal = {file.FileSizeReal} file.FileDateReal = {file.FileDateReal.ToString("yyyy.MM.dd H:mm:ss.fff")} downloaded");
            }
            _logger.Info($"{nDownloadeCount} files from {ftpDownloadedFolder} downloaded");
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
        /// Отбор из папки то, что надо скачать
        /// </summary>
        /// <param name="ftpDownloadedFolder">Папка</param>
        /// <param name="filesExt">Расширение файла</param>
        /// <param name="directoryDetailsRows">Список того, что есть в папке</param>
        private List<FileDirectoryInfo> SelectFilesToDownload(string ftpDownloadedFolder, string filesExt, string [] directoryDetailsRows)
        {
            List<FileDirectoryInfo> fileList = new List<FileDirectoryInfo>();
            var regex = new Regex(@"^([d-])([rwxt-]{3}){3}\s+\d{1,}\s+.*?(?<size>\d{1,})\s+(?<date>\w+\s+\d{1,2}\s+(?:\d{4})?)(?<time>\d{1,2}:\d{2})?\s+(?<filename>.+?\s?)$",
                                    RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            foreach(string s in directoryDetailsRows)
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
                    if ((filesExt.IsNullOrEmpty() == false) && (fileName.EndsWith(filesExt) == false))
                    {
                        continue;
                    }
                    if (_downloadInTheLastDays > 0)
                    {
                        try
                        {
                            var regexForName = new Regex(_regexPatternByDate, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                            var matchForName = regexForName.Match(fileName);
                            string dateString;
                            string day, month;

                            day = (_regexPatternByDate.IndexOf("<day>") < 0) ? "01" : matchForName.Groups["day"].Value.PadLeft(2, '0');
                            month = (_regexPatternByDate.IndexOf("<month>") < 0) ? "01" : month = matchForName.Groups["month"].Value.PadLeft(2, '0');

                            if (_regexPatternByDate.IndexOf("<year>") < 0)
                            {
                                dateString = $"{matchForName.Groups[3].Value}-{matchForName.Groups[4].Value}-{matchForName.Groups[5].Value}";
                            }
                            else
                            {
                                dateString = $"{matchForName.Groups["year"].Value}-{month}-{day}";
                            }
                            DateTime dateFromName = DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                            if (DateTime.Now.AddDays(-_downloadInTheLastDays) > dateFromName)
                            {
                                continue;
                            }
                        }
                        catch (System.FormatException ex)
                        {
                            _logger.Info($"Parsing date from title of {fileName} from {ftpDownloadedFolder} failed");
                            _logger.Info(ex.ToString());
                        }
                    }
                    fileSize = 0;
                    if (match.Groups["size"] != null)
                    {
                        long.TryParse(match.Groups["size"].Value.Trim(), out fileSize);
                    }
                    if(fileSize == 0)
                    {// Если никак не удалось достать длину файла, то отдельно запросить
                        fileSize = _ftpClient.GetFileSize(Path.Combine(ftpDownloadedFolder, fileName));
                    }
                    fileSizeKb = (int)(fileSize / 1024);

                    // Дата файла
                    dateFileLastModified = _ftpClient.GetFileLastModified(Path.Combine(ftpDownloadedFolder, fileName));

                    fileList.Add(new FileDirectoryInfo(fileSizeKb, type, fileName, match.Groups["date"].Value, fileSize, dateFileLastModified));
                }
                else
                {
                    _logger.Warn($"Cannot recognize ftp directiory record {s}");
                }
            }
            return (fileList);
        }
        /// <summary>
        /// Взять из папки все подпапки
        /// </summary>
        /// <param name="directoryDetailsRows">Список того, что есть в папке</param>
        private List<FileDirectoryInfo> SelectFolders(string[] directoryDetailsRows)
        {
            List<FileDirectoryInfo> fileList = new List<FileDirectoryInfo>();
            var regex = new Regex(@"^([d-])([rwxt-]{3}){3}\s+\d{1,}\s+.*?(?<size>\d{1,})\s+(?<date>\w+\s+\d{1,2}\s+(?:\d{4})?)(?<time>\d{1,2}:\d{2})?\s+(?<filename>.+?\s?)$",
                                    RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            foreach (string s in directoryDetailsRows)
            {
                string fileName = string.Empty;
                var match = regex.Match(s);

                if (match.Length > 5)
                {
                    var type = match.Groups[1].Value == "d" ? FileTypeEnum.Directory : FileTypeEnum.File;
                    if (type == FileTypeEnum.File)
                    {
                        continue;
                    }
                    fileName = match.Groups["filename"].Value;
                    fileList.Add(new FileDirectoryInfo(0, type, fileName, match.Groups["date"].Value, 0, DateTime.MinValue));
                }
                else
                {
                    _logger.Warn($"Cannot recognize ftp directiory record {s}");
                }
            }
            return (fileList);
        }
    }
}
