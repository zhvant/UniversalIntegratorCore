using SBAST.UniversalIntegrator.Zip;
using Quartz;

namespace SBAST.UniversalIntegrator.Modules
{
    /// <summary>
    /// Модуль для разархивирования файлов и сохранения в бд
    /// </summary>
    [DisallowConcurrentExecution]
    public class UnArchiverModule : BaseModule
    {
        protected override void RunModule()
        {
            WithMesure<IZipService>(service => service.ProcessArchives());
        }
    }
}
