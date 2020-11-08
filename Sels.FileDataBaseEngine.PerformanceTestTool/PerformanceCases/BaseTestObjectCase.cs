using Sels.Core.Components.Performance;
using Sels.FileDatabaseEngine;
using Sels.FileDatabaseEngine.Connection;
using Sels.FileDatabaseEngine.PerformanceTestTool.TestObjects;
using Sels.FileDataBaseEngine.PerformanceTestTool.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDataBaseEngine.PerformanceTestTool.PerformanceCases
{
    public abstract class BaseTestObjectCase : IPerformanceCase<string, string>
    {
        public string Identifier { get; }

        public abstract Func<string> CaseSetup { get; }

        public abstract Action<string> CaseAction { get; }

        public abstract Action<string> CaseCleanup { get; }

        public int NumberOfRuns { get; }

        public BaseTestObjectCase(string identifier, int numberOfRuns)
        {
            Identifier = identifier;
            NumberOfRuns = numberOfRuns;
        }

        protected TestObject Get(string id)
        {
            TestObject testObject = null;

            using (var connection = new DatabaseConnection(DatabaseContants.Databases.TestDatabase))
            {
                connection.Get<TestObject>(DatabaseContants.Tables.TestTable, x => x.Id == id);
            }

            return testObject;
        }

        protected string Create(string name)
        {
            var testObject = new TestObject(name);

            using(var connection = new DatabaseConnection(DatabaseContants.Databases.TestDatabase))
            {
                connection.Insert(DatabaseContants.Tables.TestTable, testObject);
                connection.Persist();
            }

            return testObject.Id;
        }

        protected void Update(string id, Action<TestObject> action)
        {
            using (var connection = new DatabaseConnection(DatabaseContants.Databases.TestDatabase))
            {
                connection.Update<TestObject>(DatabaseContants.Tables.TestTable, x => x.Id == id, action);
                connection.Persist();
            }
        }

        protected void Delete(string id)
        {
            using (var connection = new DatabaseConnection(DatabaseContants.Databases.TestDatabase))
            {
                connection.Delete<TestObject>(DatabaseContants.Tables.TestTable, x => x.Id == id);
                connection.Persist();
            }
        }

        protected abstract string Setup();

        protected abstract void Action(string id);

        protected abstract void Cleanup(string id);
    }
}
