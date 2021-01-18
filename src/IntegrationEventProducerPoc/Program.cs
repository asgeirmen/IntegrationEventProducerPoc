using System;
using System.Collections.Generic;
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

            var captureTables = _configuration["CaptureTables"];
            var captureTableList = captureTables.Split(",", StringSplitOptions.RemoveEmptyEntries);

            var publishTasks = new List<Task>();

            foreach (var table in captureTableList)
            {
                publishTasks.Add(PublishChangeEvents(table));
            }

            Task.WaitAll(publishTasks.ToArray());
        }
        static void GetAppSettingsFile()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }
        static async Task PublishChangeEvents(string captureTable)
        {
            var captureSchemaAndTable = captureTable.Trim().Split(".");
            var reader = new ChangeReader(_configuration, captureSchemaAndTable[0], captureSchemaAndTable[1]);
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
                    Console.Write(captureSchemaAndTable[1].Substring(0, 1));
                }

                await Task.Delay(100);
            }
        }
    }
}
