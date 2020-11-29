using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Test
{
    public static class TestUtilities
    {
        public static ILoggerFactory CreateLogFactory()
        {
            var serviceProvider = new ServiceCollection()
                                .AddLogging()
                                .BuildServiceProvider();
            var logFactory = serviceProvider.GetService<ILoggerFactory>();
            return logFactory;
        }
    }
}
