using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest.Demo.Models;

namespace Nest.Demo
{
    class Program
    {
        const string computer_monitor_index = "computer_monitor";
        static void Main(string[] args)
        {
            var esClient = CreateEsClient();
            Random rnd = new();
            var sys = new SystemInfo()
            {
                UUID = Guid.NewGuid().ToString(),
                System = "Windows",
                Version = "10.201"
            };
            var plugs = new List<Plugin>(){
                new Plugin(){
                    Type = "USB1",
                    CommPort = 50050
                },
                new Plugin(){
                    Type = "USB2",
                    CommPort = 50051
                }
            };

            Task.Run(async() =>
            {
                while (true)
                {
                    var memory = 0.0;
                    using (Process proc = Process.GetCurrentProcess())
                    {
                        // The proc.PrivateMemorySize64 will returns the private memory usage in byte.
                        // Would like to Convert it to Megabyte? divide it by 2^20
                        memory = proc.PrivateMemorySize64 / (1024 * 1024);
                    }

                    var result = await BulkInsert(new List<ComputerMonitor>(){
                    new ComputerMonitor(){
                        LogId = DateTime.Now.Ticks.ToString(),
                        CpuUsage = (float)Math.Round(rnd.NextDouble(), 2),
                        Memory = (float)memory,
                        Time = DateTime.Now,
                        SystemInfo = sys,
                        Plugins = plugs
                    }}, esClient)
                    .ConfigureAwait(false);

                    System.Console.WriteLine($"Insert: {result}");
                    Thread.Sleep(1000);
                }
            });


            Task.Run(async () =>
            {
                while (true)
                {
                    var result = await GetAsync(esClient);
                    System.Console.WriteLine("LogId: " + result.FirstOrDefault()?.LogId + ", Time: " + result.FirstOrDefault()?.Time);
                    Thread.Sleep(1000);
                }
            });

            Console.ReadKey();
        }

        static ElasticClient CreateEsClient()
        {
            var connectionPool = new StaticConnectionPool(new List<Uri>() { new Uri("http://localhost:9200") });
            var connectionSetting = new ConnectionSettings(connectionPool)
                .DisableAutomaticProxyDetection()
                .DisableDirectStreaming();
            var client = new ElasticClient(connectionSetting);


            var temp = client.Indices.Exists(computer_monitor_index).Exists;
            if (client.Indices.Exists(computer_monitor_index).Exists.Equals(false))
            {
                client.Indices.Create(computer_monitor_index, i => i
                .Map<ComputerMonitor>(m => m
                .AutoMap()).Settings(
                    index => index.Sorting<ComputerMonitor>(
                        sort => sort.Fields(f => f.Field(y => y.Time)).Order(IndexSortOrder.Descending)
                    )));
            }

            return client;
        }

        static async Task<bool> BulkInsert(IEnumerable<ComputerMonitor> items, ElasticClient esClient)
        {
            var bulkDescriptor = new BulkDescriptor();

            foreach (var model in items)
            {
                bulkDescriptor.Index<ComputerMonitor>
                (
                    d => d.Index(computer_monitor_index)
                        .Id(model.LogId)
                        .Document(model)
                );
            }

            var response = await esClient.BulkAsync(bulkDescriptor);

            if (response.IsValid.Equals(false))
            {
                throw response.OriginalException;
            }

            return response.IsValid;
        }


        static async Task<IEnumerable<ComputerMonitor>> GetAsync(ElasticClient esClient)
        {
            var results = new List<ComputerMonitor>();

            var queryContainer = new List<QueryContainer>();
            var query = new BoolQuery { Filter = queryContainer };

            var searchRequest = new SearchRequest<ComputerMonitor>(index: computer_monitor_index)
            {
                Source = new SourceFilter
                {
                    Includes = "*"
                },
                Query = query,
                Sort = (List<ISort>)new List<ISort>(){ new FieldSort(){ Field = "time", Order = SortOrder.Descending}},
                From = 0,
                Size = 1,
                Scroll = "1m"
            };

            var searchResponse = await esClient.SearchAsync<ComputerMonitor>(s => searchRequest);

            //超過5000筆才需要用Scroll 一般用上面的就可以
            while (searchResponse.Documents.Any())
            {
                results.AddRange(searchResponse.Documents);
                searchResponse = await esClient.ScrollAsync<ComputerMonitor>("1m", searchResponse.ScrollId);
            }

            await esClient.ClearScrollAsync(new ClearScrollRequest(searchResponse.ScrollId));

            return results;
        }

    }

}
