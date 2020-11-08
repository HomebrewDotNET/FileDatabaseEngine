using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.PerformanceTestTool.TestObjects
{
    public class TestObject
    {
        public TestObject(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
        }

        public TestObject(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public TestObject()
        {
                
        }

        public string Id { get; set; }

        public string Name { get; set; }
    }
}
