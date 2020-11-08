using Sels.FileDatabaseEngine.PerformanceTestTool.TestObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDataBaseEngine.PerformanceTestTool.PerformanceCases
{
    public class UpdateTestObjectCase : BaseTestObjectCase
    {
        public override Func<string> CaseSetup => Setup;

        public override Action<string> CaseAction => Action;

        public override Action<string> CaseCleanup => Cleanup;

        public UpdateTestObjectCase(string identifier, int numberOfRuns) : base(identifier, numberOfRuns)
        {

        }

        protected override string Setup()
        {
            return Create("Create Test Object");
        }

        protected override void Action(string id)
        {
            Console.WriteLine($"Running update operation on Test Object {id}");
            Update(id, x => x.Name = "Update test Object");
        }

        protected override void Cleanup(string id)
        {
            Delete(id);
        }
    }
}
