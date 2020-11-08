using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDataBaseEngine.PerformanceTestTool.PerformanceCases
{
    public class GetTestObjectCase : BaseTestObjectCase
    {
        public override Func<string> CaseSetup => Setup;

        public override Action<string> CaseAction => Action;

        public override Action<string> CaseCleanup => Cleanup;

        public GetTestObjectCase(string identifier, int numberOfRuns) : base(identifier, numberOfRuns)
        {

        }

        protected override string Setup()
        {
            return Create("Create Test Object");
        }

        protected override void Action(string id)
        {
            Console.WriteLine($"Running get operation on Test Object {id}");
            Get(id);
        }

        protected override void Cleanup(string id)
        {
            Delete(id);
        }
    }
}
