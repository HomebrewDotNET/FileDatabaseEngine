using Sels.Core.Components.Console;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Serialization;
using Sels.FileDatabaseEngine.Connection;
using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.TestTool.TestObjects;
using System;
using System.Linq;
using System.Threading;
using Sels.Core.Components.Serialization;

namespace Sels.FileDatabaseEngine.TestTool
{
    class Program
    {
        private const string testDatabase = "TestDatabase";
        private const string testDatabasePath = "Data";
        private const string testDatabaseMigrationPath = "DataMigration";
        private const string testTable = "TestTable";
        private const string statisticPage = "Statistics";

        public Statistics Statistics { get; set; }

        static void Main(string[] args)
        {
            ConsoleHelper.Run(Run, (x,y) => Shutdown());
        }

        private static void Run()
        {
            Console.WriteLine("Setting up databases");
            SetupDatabases();

            var statistics = DatabaseEngine.GetPageData<Statistics>(testDatabase, statisticPage);

            Console.WriteLine($"Database statistics: {statistics}");

            Console.WriteLine("Checking previously saved items");
            using (var connection = new DatabaseConnection(testDatabase))
            {
                var itemsSaved = connection.GetAll<TestObject>(testTable).Count();
                statistics.Fetched += itemsSaved;
                Console.WriteLine($"{itemsSaved} items were previously saved");
            }

            Console.WriteLine("Inserting object");
            string id = Guid.NewGuid().ToString();
            var testObject = new TestObject(id, "I'm a test object");
            using (var connection = new DatabaseConnection(testDatabase))
            {
                connection.Insert(testTable, testObject);
                statistics.Inserted++;
                connection.Persist();
            }

            Console.WriteLine($"Inserted: {Environment.NewLine}{testObject.SerializeAsJson()}");

            Console.WriteLine("Getting and updating object");
            using (var connection = new DatabaseConnection(testDatabase))
            {
                testObject = connection.Get<TestObject>(testTable, x => x.Id == id);
                statistics.Fetched++;
                testObject.Name = "I'm an updated test object";
                connection.Update(testTable, testObject);
                statistics.Updated++;
                connection.Persist();
            }

            Console.WriteLine($"Updated: {Environment.NewLine}{testObject.SerializeAsJson()}");

            Console.WriteLine("Deleting object");
            using (var connection = new DatabaseConnection(testDatabase))
            {
                testObject = connection.Get<TestObject>(testTable, x => x.Id == id);
                connection.Delete(testTable, testObject);
                statistics.Deleted++;
                connection.Persist();
            }

            Console.WriteLine($"Deleted: {Environment.NewLine}{testObject.SerializeAsJson()}");

            Console.WriteLine("Adding multiple items");
          
            using (var connection = new DatabaseConnection(testDatabase))
            {
                for (int i = 0; i < 100000; i++)
                {
                    var newObject = new TestObject($"I'm object {i + 1}");
                    Console.WriteLine($"Inserting: {Environment.NewLine}{newObject.SerializeAsJson()}");
                    connection.Insert(testTable, newObject);
                    statistics.Inserted++;
                }
                connection.Persist();
            }

            Console.WriteLine("Getting multiple items");
            var totalItems = 0;
            using (var connection = new DatabaseConnection(testDatabase))
            {
                totalItems = connection.GetAll<TestObject>(testTable).Execute(x => Console.WriteLine($"Got: {Environment.NewLine}{x.SerializeAsJson()}")).Count();

                statistics.Fetched+= totalItems;
            }


            if(totalItems > 500000)
            {
                Console.WriteLine("Deleting all items");
                using (var connection = new DatabaseConnection(testDatabase))
                {
                    connection.Delete<TestObject>(testTable, x => true);
                    connection.Persist();
                    statistics.Deleted += totalItems;
                }
            }

            using (var connection = new DatabaseConnection(testDatabase))
            {
                totalItems = connection.GetAll<TestObject>(testTable).Count();
            }



            Console.WriteLine($"Database statistics: {statistics}");
            DatabaseEngine.StorePageData(testDatabase, statisticPage, statistics);

            Console.WriteLine("Testing migration");
            var database = DatabaseEngine.GetDatabase(testDatabase);
            Console.WriteLine($"Shutting down database {testDatabase}");
            database.Shutdown();
            Console.WriteLine($"Migrating from {testDatabasePath} to {testDatabaseMigrationPath}");
            database.MigrateTo(new System.IO.DirectoryInfo(testDatabaseMigrationPath), true, true);
            Console.WriteLine("Migration complete. Starting database.");
            database.Startup();
            Console.WriteLine("Doing health check");
            var healthCheckOk = false;
            using (var connection = database.OpenConnection())
            {
                var totalCount = connection.GetAll<TestObject>(testTable).Count();

                healthCheckOk = totalCount == totalItems;
            }

            var migratedStatistics = DatabaseEngine.GetPageData<Statistics>(testDatabase, statisticPage);

            Console.WriteLine($"Health check status: {(healthCheckOk && statistics.Equals(migratedStatistics) ? "OK" : "NOK")}");
        }

        private static void SetupDatabases()
        {
            var sourceProvider = SerializationProvider.Json;
            var database = DatabaseEngine.CreateDatabase(testDatabase, testDatabasePath);
            database.RegisterTable<TestObject>(testTable, sourceProvider);
            database.RegisterPage<Statistics>(statisticPage);
            database.Startup();
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                Console.ReadKey();
            }

        }
    }
}
