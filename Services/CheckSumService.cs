using SBAST.UniversalIntegrator.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SBAST.UniversalIntegrator.Services
{
    /// <summary>
    /// Сервис для геренации MD5
    /// </summary>
    public class CheckSumService : ICheckSumService
    {
        /// <summary>
        /// Получаем MD5 для файла по пути файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>MD5</returns>
        public string GetMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
        }
        /// <summary>
        /// Получаем MD5 для файла 
        /// </summary>
        /// <param name="xml">xml</param>
        /// <returns>MD5</returns>
        public string GetMD5Hash(string xml)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(xml));
                return data.Select(d => d.ToString("x2")).StringJoin();
            }
        }
    }
}
