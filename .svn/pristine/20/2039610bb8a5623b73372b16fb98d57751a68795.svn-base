using System;

namespace SBAST.UniversalIntegrator.Ftp
{
    public interface IFtpClient
    {
        DateTime LastFtpAccess { get; set; }
        string DownloadFile(string source, string dest);
        long GetFileSize(string fileName);
        DateTime GetFileLastModified(string fileName);
        string[] ListDirectory(string folderPath = "");
        string[] ListDirectoryDetails(string folderPath = "");
    }
}