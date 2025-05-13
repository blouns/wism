using System;
using Wism.Client.Api.Telemetry;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;

namespace Wism.Client.CommandProcessors
{
    public class StandardProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly CommandIpcPublisher? commandPublisher;

        public StandardProcessor(
            IWismLoggerFactory loggerFactory,
            CommandIpcPublisher? commandPublisher = null)
        {
            this.logger = loggerFactory.CreateLogger();
            this.commandPublisher = commandPublisher;
        }

        public bool CanExecute(ICommandAction command)
        {
            return true;
        }

        public ActionState Execute(ICommandAction command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var result = command.Execute();

            EmitCommandEvent(command, result);

            return result;
        }

        private void EmitCommandEvent(ICommandAction command, ActionState result)
        {
            if (commandPublisher == null)
            {
                return;
            }

            try
            {
                CommandExecutedEvent evt;

                if (command is IReplayableCommand replayable)
                {
                    evt = replayable.ToExecutedEvent(result);
                }
                else
                {
                    evt = new CommandExecutedEvent
                    {
                        CommandType = command.GetType().Name,
                        Result = result.ToString(),
                        Timestamp = DateTime.UtcNow
                    };
                }

                commandPublisher.Publish(evt);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to emit command to companion: {ex.Message}");
            }
        }
    }
}
