using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace IntegrationEventProducer
{
    class Program
    {
        private static IConfiguration _configuration;
        static async Task Main(string[] args)
        {
            GetAppSettingsFile();
            await PublishChangeEvents();
        }
        static void GetAppSettingsFile()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }
        static async Task PublishChangeEvents()
        {
            var reader = new ChangeReader(_configuration);
            while (true)
            {
                var events = await reader.GetList();
                if (events.Count > 0)
                {
                    Console.WriteLine(string.Empty);
                    foreach (var ev in events)
                    {
                        Console.WriteLine(ev);
                    }
                }
                else
                {
                    Console.Write('.');
                }

                await Task.Delay(100);
            }
        }
    }
}
