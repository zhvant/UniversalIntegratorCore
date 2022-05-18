using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace SBAST.UniversalIntegrator.Ftp
{
    /// <summary>
    /// Специальный потокобезопасный класс дл яокнтроля одновременного доступа к одному и тому же ftp
    /// </summary>
    public class FtpClientSingleton : IFtpClient
    {
        private static Dictionary<string, FtpClient> _clientList = new Dictionary<string, FtpClient>();
        private FtpClient _client;
        private TimeSpan _requestDelay = TimeSpan.FromSeconds(1);

        readonly string _ftpAddress;

        public DateTime LastFtpAccess
        {
            get
            {
                return (_client.LastFtpAccess);
            }
            set
            {
                _client.LastFtpAccess = value;
            }
        }

        public FtpClientSingleton(string ftpAddress, string ftpLogin, string ftpPassword, int requestDelay)
        {
            _ftpAddress = ftpAddress;
            _requestDelay = TimeSpan.FromMilliseconds(requestDelay);

            if (_clientList.ContainsKey(ftpAddress) == false)
            {
                _client = new FtpClient(ftpAddress, ftpLogin, ftpPassword);
            }
            else
            {
                _client = _clientList[ftpAddress];
            }
        }

        protected void Access()
        {
            if ((DateTime.Now - _client.LastFtpAccess) < _requestDelay)
            {
                Thread.Sleep(DateTime.Now - _client.LastFtpAccess);
            }
            LastFtpAccess = DateTime.Now;
        }

        public string DownloadFile(string source, string dest)
        {
            lock(_client)
            {
                Access();
                return(_client.DownloadFile(source, dest));
            }
        }

        public long GetFileSize(string fileName)
        {
            lock (_client)
            {
                Access();
                return (_client.GetFileSize(fileName));
            }
        }
        public DateTime GetFileLastModified(string fileName)
        {
            lock (_client)
            {
                Access();
                return (_client.GetFileLastModified(fileName));
            }
        }

        public string[] ListDirectory(string folderPath = "")
        {
            lock (_client)
            {
                Access();
                return (_client.ListDirectory(folderPath));
            }
        }
        public string[] ListDirectoryDetails(string folderPath = "")
        {
            lock (_client)
            {
                Access();
                return (_client.ListDirectoryDetails(folderPath));
            }
        }
    }
}
