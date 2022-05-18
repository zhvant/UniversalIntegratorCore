using SBAST.UniversalIntegrator.Models;
using System;

namespace SBAST.UniversalIntegrator.DB
{
    public interface IArchiveRepository
    {
        /// <summary>
        /// Список не обработанных архивов 
        /// </summary>
        FileModel[] GetUnProcessedArchives();

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
        int ArchiveUpdate(int IdArchive, string fileName, FileLoadStatusEnum status, int fileSize, string fileDate, long FileSizeReal, DateTime FileDateReal, string checksum, string originalFileLocation);

        /// <summary>
        /// Обновляем статус архива по ИД 
        /// </summary>
        /// <param name="IdFile">ИД Архива</param>
        /// <param name="status">Статус</param>
        int ArchiveStatusUpdate(int idArchive, FileLoadStatusEnum fileStatus);

        /// <summary>
        /// Проверяем существует ли архив с таким именем + путем + размером + датой иначе возвращаем 0
        /// </summary>
        /// <param name="name">Имя архива</param>
        /// <param name="originalPath">Путь до файла на ftp</param>
        /// <param name="fileSizeReal">Размер архива в байтах</param>
        /// <param name="fileDateReal">Дата создания архива считанная отдельным запросом</param>
        ArchiveFile FindArchive(string name, string originalPath, long fileSizeReal, DateTime fileDateReal);

    }
}
