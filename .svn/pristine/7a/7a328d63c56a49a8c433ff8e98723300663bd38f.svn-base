using DryIoc;
using SBAST.UniversalIntegrator.Ftp;
using NLog;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Modules
{
    //Тестовый модуль
    [DisallowConcurrentExecution]
    class TestModule : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var container = (Container)context.MergedJobDataMap["container"];

            try
            {
                using (var scope = container.OpenScope())
                {
                 // 
                }
            }
            catch (Exception ex)
            {
                container.Resolve<ILogger>().Error($"Error on downloading Archives {ex}");
            }

            return Task.CompletedTask;

        }
    }
}
