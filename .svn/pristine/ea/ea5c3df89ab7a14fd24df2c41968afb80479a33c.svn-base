using System;

namespace SBAST.UniversalIntegrator.Ftp
{
    /// <summary>
    /// Служебный класс для информации о Файлах и папках ФТП
    /// </summary>
    public class FileDirectoryInfo
    { 
        public int FileSize { get; set; }
        public FileTypeEnum Type { get; set; }
        public string Name { get; set; }
        public string Date { get; }
        public long FileSizeReal { get; set; }
        public DateTime FileDateReal { get; set; }

        public FileDirectoryInfo() { }

        public FileDirectoryInfo(int fileSize, FileTypeEnum type, string name, string date, long fileSizeReal, DateTime fileDateReal)
        {
            FileSize = fileSize;
            Type = type;
            Name = name;
            Date = date;
            FileSizeReal = fileSizeReal;
            FileDateReal = fileDateReal;
        }

    }

    public enum FileTypeEnum
    {
        Directory,
        File
    }
}