using System;
using IBMMQResilientClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace ClientSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            IQueueReader reader = null;
            IQueueWriter writer = null;
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                writer = serviceProvider.GetService<IQueueWriter>();
                reader = serviceProvider.GetService<IQueueReader>();
            }
            var putMsg = new QueueMessage()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Data = "This is a message",
                QueueName = "DEV.QUEUE.1"
            };
            writer.Enqueue(putMsg);

            var getMsg = reader.Dequque();
            
            Console.WriteLine(getMsg.Data);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true)
                .Build();
            services.AddLogging(configure => configure.AddConsole())
                .AddSingleton(config)
                .AddIbmMQ();
        }
    }
}
