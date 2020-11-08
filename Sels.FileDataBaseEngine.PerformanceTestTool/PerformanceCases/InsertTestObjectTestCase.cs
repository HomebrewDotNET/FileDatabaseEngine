using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDataBaseEngine.PerformanceTestTool.PerformanceCases
{
    public class InsertTestObjectTestCase : BaseTestObjectCase
    {
        public override Func<string> CaseSetup => null;

        public override Action<string> CaseAction => Action;

        public override Action<string> CaseCleanup => Cleanup;

        public InsertTestObjectTestCase(string identifier, int numberOfRuns) : base(identifier, numberOfRuns)
        {

        }

        private string _id;

        protected override string Setup()
        {
            return null;
        }

        protected override void Action(string id)
        {
            
            Console.WriteLine($"Running insert operation on Test Object");
            _id = Create("Create test Object");
        }

        protected override void Cleanup(string id)
        {
            Delete(_id);
        }
    }
}
