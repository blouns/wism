using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Agent.Controllers;
using Wism.Client.View;

namespace Wism.Client.View
{
    public class WismAiView : WismViewBase
    {
        private readonly ILogger logger;
        private readonly CommandController commandController;
        private readonly IMapper mapper;

        public WismAiView(ILoggerFactory loggerFactory, CommandController commandController, IMapper mapper) : 
            base(loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger<WismAiView>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        protected override void DoTasks(ref int lastId)
        {
            throw new NotImplementedException();
        }

        protected override void Draw()
        {
            throw new NotImplementedException();
        }

        protected override void HandleInput()
        {
            throw new NotImplementedException();
        }
    }
}
