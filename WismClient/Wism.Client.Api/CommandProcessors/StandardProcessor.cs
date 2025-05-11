using System;
using Wism.Client.Api.CommandPublisher;
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
        private readonly MapSnapshotEmitter? mapEmitter;
        private readonly MapSnapshotBuilder? mapBuilder;

        public StandardProcessor(
            IWismLoggerFactory loggerFactory,
            CommandIpcPublisher? commandPublisher = null,
            MapSnapshotEmitter? mapEmitter = null,
            MapSnapshotBuilder? mapBuilder = null)
        {
            this.logger = loggerFactory.CreateLogger();
            this.commandPublisher = commandPublisher;
            this.mapEmitter = mapEmitter;
            this.mapBuilder = mapBuilder;
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
            EmitMapSnapshot();

            return result;
        }

        private void EmitMapSnapshot()
        {
            try
            {
                if (mapEmitter != null && mapBuilder?.TryBuild(out var snapshot) == true)
                {
                    mapEmitter.Publish(snapshot);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to emit map snapshot: {ex.Message}");
            }
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
