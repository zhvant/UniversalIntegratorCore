using DryIoc;
using NLog;
using Quartz;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Modules
{
    /// <summary>
    /// Базовый модуль. Для упрощения создания другоих модулей путем наследования
    /// Стартует абстрактный метод, логирует начало, конец, ошибки и замеряет время выполнения
    /// </summary>
    public abstract class BaseModule : IJob
    {
        protected Container Container { get; set; }
        protected ILogger Logger { get; set; }
        public Task Execute(IJobExecutionContext context)
        {
            Container = (Container)context.MergedJobDataMap["container"];
            Logger = Container.Resolve<ILogger>();
            RunModule();

            return Task.CompletedTask;
        }

        protected abstract void RunModule();

        protected void WithMesure<TService>(Action<TService> action)
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                Logger.Info("Start module");
                try
                {
                    using (var scope = Container.OpenScope())
                    {
                        action(scope.Resolve<TService>());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in Module {ex}");
                }

                Logger.Info($"End module in {stopWatch.Elapsed}ms");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in Module level 2");
                Logger.Error(ex.Message);
            }
        }

    }
}
