using Sels.Core.Components.Console;
using Sels.Core.Components.Performance;
using Sels.Core.Components.Serialization;
using Sels.FileDatabaseEngine;
using Sels.FileDatabaseEngine.Connection;
using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.PerformanceTestTool.TestObjects;
using Sels.FileDataBaseEngine.PerformanceTestTool.Constants;
using Sels.FileDataBaseEngine.PerformanceTestTool.PerformanceCases;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Sels.FileDataBaseEngine.PerformanceTestTool
{
    class Program
    {
        private const string testDatabasePath = "Data";

        static void Main(string[] args)
        {
            ConsoleHelper.Run(Run, (x,y) => Shutdown());
        }

        private static void Run()
        {
            Initialize();

            List<(string Identifier, IEnumerable<PerformanceResult<string>> Results)> resultSets = new List<(string Identifier, IEnumerable<PerformanceResult<string>> Results)>
            {
                RunCrudPerformanceTest(500),
                RunCrudPerformanceTest(1000),
                RunCrudPerformanceTest(10000),
                RunCrudPerformanceTest(100000)
            };

            foreach (var (identifier, results) in resultSets)
            {
                Console.WriteLine($"Results from {identifier}");
                foreach (var result in results)
                {
                    Console.WriteLine(result.ToString());
                }
            }
        }

        private static void Initialize()
        {
            var sourceProvider = SerializationProvider.Json;
            Console.WriteLine("Setting up database");
            var database = DatabaseEngine.CreateDatabase(DatabaseContants.Databases.TestDatabase, testDatabasePath);
            database.RegisterTable<TestObject>(DatabaseContants.Tables.TestTable, sourceProvider);
            database.Startup();
            CleanupDatabaseItems();
        }

        private static (string Identifier, IEnumerable<PerformanceResult<string>> Results) RunCrudPerformanceTest(int initialItems)
        {
            IEnumerable<PerformanceResult<string>> results;
            Console.WriteLine("Setting up performance cases");

            var identifier = $"Test Object CRUD performance profiler ({initialItems})";

            using (var profiler = new PerformanceProfiler<string, string, string>(identifier, () => SetupDatabaseItems(initialItems), CleanupDatabaseItems))
            {
                profiler.AddCase(new GetTestObjectCase("Get", 20));
                profiler.AddCase(new InsertTestObjectTestCase("Insert", 20));
                profiler.AddCase(new UpdateTestObjectCase("Update", 20));
                profiler.AddCase(new DeleteTestObjectCase("Delete", 20));

                Console.WriteLine("Running performance cases");

                results = profiler.RunAllCases();
            }

            return (identifier, results);
        }

        private static void SetupDatabaseItems(int initialItems)
        {
            var items = new List<TestObject>();

            for(int i = 0; i < initialItems; i++)
            {
                items.Add(new TestObject($"Test object {i + 1}"));
            }

            using(var connection = new DatabaseConnection(DatabaseContants.Databases.TestDatabase))
            {
                connection.Insert<TestObject>(DatabaseContants.Tables.TestTable, items);
                connection.Persist();
            }
        }

        private static void CleanupDatabaseItems()
        {
            using (var connection = new DatabaseConnection(DatabaseContants.Databases.TestDatabase))
            {
                connection.Delete<TestObject>(DatabaseContants.Tables.TestTable, x => true);
                connection.Persist();
            }
        }

        private static void Shutdown()
        {
            try
            {
                ConsoleHelper.WriteLine(ConsoleColor.Red, "Application shutting down");

                ConsoleHelper.WriteLine(ConsoleColor.Red, $"Shutting down File Database");

                DatabaseEngine.ForceShutdownAll();

                ConsoleHelper.WriteLine(ConsoleColor.Green, $"Databases have been shutdown");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());

                Console.ReadKey();
            }
            
        }
    }
}
