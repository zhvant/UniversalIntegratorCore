using Dapper;
using SBAST.UniversalIntegrator.Models;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace SBAST.UniversalIntegrator.DB
{
    /// <summary>
    /// Репозиторий скачанных архивов 
    /// </summary>
    class ArchiveRepository : IArchiveRepository
    {
        private readonly string _connectionString;
        private readonly int _connectionTimeout;

        public ArchiveRepository(string connectionString, int connectionTimeout)
        {
            _connectionString = connectionString;
            _connectionTimeout = connectionTimeout;
        }

        /// <summary>
        /// Обновляем статус архива и создаем если еще не существует 
        /// </summary>
        /// <param name="fileName">Имя архива</param>
        /// <param name="status">Статус</param>
        /// <param name="fileSize">Размер архива</param>
        /// <param name="fileDate">Дата создания архива на фтп</param>
        /// <param name="checksum">md5</param>
        /// <param name="originalFileLocation">Оригинальное расположение на фтп</param>
        /// <returns>ИД Архива</returns>
        public int ArchiveUpdate(int idArchive, string fileName, FileLoadStatusEnum status, int fileSize, string fileDate, long fileSizeReal, DateTime fileDateReal, string checksum, string originalFileLocation)
        {
            int newIdArchive = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                newIdArchive = connection.Query<int>(
                @"MERGE dbo.UnIntArchive AS target 
                USING(values(@IdArchive, @ArchiveName, @LoadStatus, @FileSize, @FileDate, @FileSizeReal, @FileDateReal, @CheckSum, @OriginalFileLocation)) 
                    AS source(IdArchive, archiveName, LoadStatus, FileSize, FileDate, FileSizeReal, FileDateReal, CheckSum, OriginalFileLocation) 
                ON
                (
                    (target.IdArchive = source.IdArchive) or
                    (
                        (target.archiveName = source.ArchiveName) and
                        (target.OriginalFileLocation = source.OriginalFileLocation) and
                        (
                            (target.FileSizeReal = source.FileSizeReal and target.FileDateReal = source.FileDateReal) OR 
                            (target.FileSize = source.FileSize and target.FileDate = source.FileDate)
                        )
                    )
                )
                WHEN MATCHED THEN 
                    UPDATE SET LoadStatus = source.LoadStatus, 
                        UpdateDate = getdate(), 
                        FileSize = source.FileSize, 
                        FileDate = source.FileDate, 
                        FileSizeReal = source.FileSizeReal, 
                        FileDateReal = source.FileDateReal, 
                        CheckSum = source.CheckSum, 
                        OriginalFileLocation = source.OriginalFileLocation
                WHEN NOT MATCHED THEN 
                    INSERT(archiveName, LoadStatus, FileSize, FileDate, FileSizeReal, FileDateReal, CheckSum, OriginalFileLocation) 
                    VALUES(source.ArchiveName, source.LoadStatus, source.FileSize, source.FileDate, source.FileSizeReal, source.FileDateReal, source.CheckSum, source.OriginalFileLocation)
                output inserted.IdArchive; ",
                new {IdArchive = idArchive,
                    ArchiveName = fileName,
                    LoadStatus = (int)status,
                    FileSize = fileSize,
                    FileDate = fileDate,
                    FileSizeReal = fileSizeReal,
                    FileDateReal = fileDateReal,
                    Checksum = checksum,
                    OriginalFileLocation = originalFileLocation },
                commandTimeout: _connectionTimeout
                ).First();
            }
            if(newIdArchive > 0)
            {
                return (newIdArchive);
            }
            return (idArchive);
        }

        /// <summary>
        /// Обновляем статус архива по ИД 
        /// </summary>
        /// <param name="IdFile">ИД Архива</param>
        /// <param name="status">Статус</param>
        public int ArchiveStatusUpdate(int IdArchive, FileLoadStatusEnum status)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute("update dbo.UnIntArchive set LoadStatus = @LoadStatus, UpdateDate = getdate() where IdArchive = @IdArchive", new { IdArchive = IdArchive, LoadStatus = (int)status }, commandTimeout: _connectionTimeout);
            }
            return IdArchive;
        }

        /// <summary>
        /// Проверяем существует ли архив с таким именем + путем + размером + датой иначе возвращаем 0
        /// </summary>
        /// <param name="name">Имя архива</param>
        /// <param name="originalPath">Путь до файла на ftp</param>
        /// <param name="fileSizeReal">Размер архива в байтах</param>
        /// <param name="fileDateReal">Дата создания архива считанная отдельным запросом</param>
        public ArchiveFile FindArchive(string name, string originalPath, long fileSizeReal, DateTime fileDateReal)
        {
            ArchiveFile archiveFile;

            using (var connection = new SqlConnection(_connectionString))
            {
                // Новый вариант
                archiveFile = connection.Query<ArchiveFile>
                    (
                        "select IdArchive, LoadStatus from dbo.UnIntArchive (NOLOCK) where ArchiveName = @name and OriginalFileLocation = @originalPath and FileSizeReal = @fileSizeReal and FileDateReal = @fileDateReal", 
                        new { name, originalPath, fileSizeReal, fileDateReal }, 
                        commandTimeout: _connectionTimeout
                    ).FirstOrDefault();
            }
            return (archiveFile);
        }

        /// <summary>
        /// Список не обработанных архивов 
        /// </summary>
        public FileModel[] GetUnProcessedArchives()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<FileModel>
                    (
                        "select IdArchive as IdFile, ArchiveName as FileName, OriginalFileLocation as FileOriginalPath from dbo.UnIntArchive (NOLOCK) where LoadStatus = @LoadStatus", 
                        new { LoadStatus = FileLoadStatusEnum.Downloaded },
                        commandTimeout: _connectionTimeout
                    ).ToArray();
            }
        }
    }
}
