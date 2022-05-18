using DryIoc;
using SBAST.UniversalIntegrator.Services;
using SBAST.UniversalIntegrator.Zip;
using NLog;
using Quartz;
using System;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Modules
{
    /// <summary>
    /// Модуль для парсинга файлов
    /// </summary>
    [DisallowConcurrentExecution]
    public class DBParserModule : BaseModule
    {
        protected override void RunModule()
        {
            WithMesure<IParserService>(service => service.ProcessFiles());
        }
    }
}
