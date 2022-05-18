using SBAST.UniversalIntegrator.DB;
using SBAST.UniversalIntegrator.Helpers;
using SBAST.UniversalIntegrator.Models;
using SBAST.UniversalIntegrator.Services;
using NLog;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SBAST.UniversalIntegrator.Services.Converters;
using System.Text.RegularExpressions;

namespace SBAST.UniversalIntegrator.Zip
{

    /// <summary>
    /// Сервис разархивирует скачанные архивы и складывает их в бд. меняет статусы если в процессе обработки произошла ошибка
    /// </summary>
    public class ZipService : IZipService
    {
        private readonly string _downloadFolder;
        private readonly ILogger _logger;
        private readonly IFilesRepository _filesRepository;
        private readonly IArchiveRepository _archiveRepository;
        private readonly ICheckSumService _checkSumService;
        private readonly bool _removeNamespaces;
        private readonly string _entityType;
        private readonly int _maxDegreeOfParallelism;
        private readonly FolderStructureEnum _folderStructure;
        private readonly FileFormatEnum _fileFormat;
        private readonly IFileConverterService _fileConverterService;
        private readonly string _entityTypeFromFilename;


        public ZipService(string downloadFolder,
            ILogger logger,
            IFilesRepository filesRepository,
            IArchiveRepository archiveRepository,
            ICheckSumService checkSumService,
            bool removeNamespaces,
            string entityType,
            int maxDegreeOfParallelism,
            FolderStructureEnum folderStructure,
            FileFormatEnum fileFormat,
            IFileConverterService fileConverterService,
            string entityTypeFromFilename)
        {
            _downloadFolder = downloadFolder;
            _logger = logger;
            _filesRepository = filesRepository;
            _archiveRepository = archiveRepository;
            _checkSumService = checkSumService;
            _removeNamespaces = removeNamespaces;
            _entityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType;
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _folderStructure = folderStructure;
            _fileFormat = fileFormat;
            _fileConverterService = fileConverterService;
            _entityTypeFromFilename = string.IsNullOrWhiteSpace(entityTypeFromFilename) ? null : entityTypeFromFilename;
        }
        /// <summary>
        /// Получаем файлы из архива
        /// </summary>
        /// <param name="filePath">Путь к архиву</param>
        /// <returns></returns>
        private (bool IsError, (string FileName, string data)[] Files) GetFiles(string filePath)
        {
            try
            {
                _logger.Info($"Unarchive {filePath}");
                using (var zipToOpen = new FileStream(filePath, FileMode.Open))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        string format = "." + _fileFormat.ToString().ToLower();
                        if (_fileFormat == FileFormatEnum.Undefined)
                            format = ".xml";

                        return (false, archive.Entries.Where(x => x.Name.Contains(format)).Select(readEntry =>
                        {
                            string val;
                            using (StreamReader reader = new StreamReader(readEntry.Open()))
                            {
                                val = reader.ReadToEnd();
                            }

                            string name = readEntry.Name;
                            if (_fileConverterService != null)
                            {
                                val = _fileConverterService.Convert(readEntry.Name, val);
                                name = readEntry.Name.Replace(format, ".xml");
                            } 

                            return (FileName: name, Data: val);
                        }).ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Can't process {filePath}\n{ex}");
                return (true, new (string, string)[0]);
            }
        }
        /// <summary>
        /// Обработать один архив
        /// </summary>
        /// <param name="archiveTuple">Кортеж с полным путем, именем и идентификатором переданного архива</param>
        /// <returns></returns>
        private void HandleArchive((string FullFileName, string FileName, int IdFile) archiveTuple)
        {
            var archiveTupleExt = (UnzippedFiles: GetFiles(archiveTuple.FullFileName), archiveTuple.FullFileName, archiveTuple.FileName, archiveTuple.IdFile);
            try
            {

                if (archiveTupleExt.UnzippedFiles.IsError)
                {
                    _archiveRepository.ArchiveStatusUpdate(archiveTupleExt.IdFile, FileLoadStatusEnum.Error);
                    return;
                }
                _logger.Info($"Begin process archive {archiveTupleExt.FileName}");
                var archiveLoadId = _archiveRepository.ArchiveStatusUpdate(archiveTupleExt.IdFile, FileLoadStatusEnum.InProgress);
                foreach (var uf in archiveTupleExt.UnzippedFiles.Files)
                {
                    var md5 = _checkSumService.GetMD5Hash(uf.data);
                    if (_filesRepository.FindFile(uf.FileName, md5) != 0)
                        _logger.Warn($"File {uf.FileName} ({archiveTupleExt.FileName}) contains in another archive");
                    else if (string.IsNullOrEmpty(uf.data))
                        _logger.Warn($"File {uf.FileName} ({archiveTupleExt.FileName}) is empty");
                    else
                    {
                        var xmlHelper = new XmlExtension(uf.data);
                        if (_removeNamespaces)
                            xmlHelper.RemoveNamespaces();
                        var fileType = _entityType ?? xmlHelper.GetRootType();
                        var clearXml = xmlHelper.GetXml();

                        //if (_entityType == null)
                        //    fileType = uf.FileName.Replace(".xml", "");

                        if (!_entityTypeFromFilename.IsNullOrEmpty())
                        {
                            Regex regex = new Regex(_entityTypeFromFilename, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
                            var match = regex.Match(uf.FileName);

                            if (_entityTypeFromFilename.IndexOf("<entityType>") < 0)
                                throw new ArgumentException("Параметр EntityTypeFromFileName не был обработан. Регулярное выражение должно содержать группу <entityType>.");
                            else
                                fileType = match.Groups["entityType"].Value;
                        }

                        _filesRepository.SaveFile(archiveLoadId, uf.FileName, clearXml, fileType, md5);
                    }
                }
                _archiveRepository.ArchiveStatusUpdate(archiveTupleExt.IdFile, FileLoadStatusEnum.Processed);
                File.Delete(archiveTupleExt.FullFileName);
                _logger.Info($"End process archive {archiveTupleExt.FileName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Can't process archive {archiveTupleExt.FileName}\n{ex}");
                _archiveRepository.ArchiveStatusUpdate(archiveTupleExt.IdFile, FileLoadStatusEnum.Error);
            }
        }

        /// <summary>
        /// Основной входной метод берет файлы из папки, проверяет статус, разархивирует их и складывает в бд, после чего удаляет из папки
        /// </summary>
        public void ProcessArchives()
        {
            try
            {
                var archiveTuples = GetArchiveTuples(_downloadFolder, _folderStructure);

                // &&&&&
                _logger.Info($"ProcessArchives contains {archiveTuples.Length} elements");
                foreach (var arh in archiveTuples)
                {
                    _logger.Info($"FullFileName = {arh.FullFileName} FileName = {arh.FileName} IdFile = {arh.IdFile}");
                }
                // &&&&&

                if (_maxDegreeOfParallelism > 1)
                    Parallel.ForEach(archiveTuples, new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, (archiveTuple) => HandleArchive(archiveTuple));
                else
                    archiveTuples.ForEach(HandleArchive);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Can't process archives");
            }
        }

        /// <summary>
        /// Получение всех архивов из папки и подпапок
        /// </summary>
        private string[] GetArchivesRecursively(string downloadFolder)
        {
            List<string> resultFilesList = new List<string>();

            void ParseFiles(string folderPath)
            {
                var files = Directory.GetFiles(folderPath);
                resultFilesList.AddRange(files);

                var directories = Directory.GetDirectories(folderPath);
                foreach (var directory in directories)
                    ParseFiles(directory);
            };

            ParseFiles(downloadFolder);
            return resultFilesList.ToArray();
        }

        private (string FullFileName, string FileName, int IdFile)[] GetArchiveTuples(string downloadFolder, FolderStructureEnum folderStructure)
        {
            switch (folderStructure)
            {
                default:
                case FolderStructureEnum.Default:
                case FolderStructureEnum.Single:
                    return Directory.GetFiles(_downloadFolder)
                        .Join(_archiveRepository.GetUnProcessedArchives(), x => x.Replace(_downloadFolder, "").TrimStart('\\'), x => x.FileName, (x, y) => (y.FileName, y.IdFile))
                        .Select(file => (FullFileName: Path.Combine(_downloadFolder, file.FileName), file.FileName, file.IdFile)).ToArray();
                case FolderStructureEnum.Copy:
                    return GetArchivesRecursively(_downloadFolder)
                        .Join(_archiveRepository.GetUnProcessedArchives(), x => x.Replace(_downloadFolder, "").TrimStart('\\'), x => Path.Combine(x.FileOriginalPath, x.FileName), (x, y) => (y.FileName, y.IdFile, y.FileOriginalPath))
                        .Select(file => (FullFileName: Path.Combine(_downloadFolder, file.FileOriginalPath, file.FileName), file.FileName, file.IdFile)).ToArray();
            }
        }
    }
}
