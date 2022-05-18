using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Models
{
    /// <summary>
    /// Структура папок при скачивании с FTP
    /// </summary>
    public enum FolderStructureEnum
    {
        /// <summary>
        /// По умолчанию: Single
        /// </summary>
        Default = 0,

        /// <summary>
        /// Все файлы в одну папку
        /// </summary>
        Single = 1,

        /// <summary>
        /// Сохранение структуры папок FTP
        /// </summary>
        Copy = 2
    }
}
