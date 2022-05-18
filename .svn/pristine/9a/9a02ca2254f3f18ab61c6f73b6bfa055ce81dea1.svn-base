using NLog;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Helpers
{
    public class ModuleExecutionListener : ITriggerListener
    {
        private readonly ILogger _logger;

        public string Name => "ModuleExecutionListener";

        public ModuleExecutionListener(ILogger logger)
        {
            _logger = logger;
        }

        public Task TriggerComplete(ITrigger trigger, IJobExecutionContext context, SchedulerInstruction triggerInstructionCode, CancellationToken cancellationToken = default)
        {
            return new TaskFactory(cancellationToken).StartNew(() => {
                _logger.Debug("Trigger " + trigger.Key.Name + " complete.");
            });
        }

        public Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return new TaskFactory(cancellationToken).StartNew(() => {
                _logger.Debug("Trigger " + trigger.Key.Name + " fired.");
            });
        }

        public Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            return new TaskFactory(cancellationToken).StartNew(() => {
                _logger.Error("Trigger " + trigger.Key.Name + " misfired.");
            });
        }

        public Task<bool> VetoJobExecution(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return new TaskFactory<bool>(cancellationToken).StartNew(() => { return false; });
        }
    }
}
