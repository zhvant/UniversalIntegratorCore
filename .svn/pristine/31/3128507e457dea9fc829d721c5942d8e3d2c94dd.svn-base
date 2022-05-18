using DryIoc;
using SBAST.UniversalIntegrator.Services;
using SBAST.UniversalIntegrator.Zip;
using NLog;
using Quartz;
using System;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Modules
{
    [DisallowConcurrentExecution]
    public class DBParserPlanModule : BaseModule
    {
        protected override void RunModule()
        {
            WithMesure<IParserPlanService>(service => service.ProcessFiles());
        }
    }
}
