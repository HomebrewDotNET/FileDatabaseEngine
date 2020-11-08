using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.TestTool.TestObjects
{
    public class Statistics
    {
        public int Fetched { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Deleted { get; set; }

        public override string ToString()
        {
            return $"Fetched {Fetched} items, inserted {Inserted} items, updated {Updated} items and Deleted {Deleted} items";
        }

        public override bool Equals(object obj)
        {
             if(base.Equals(obj)) return true;

             if(obj is Statistics statistics)
            {
                if (Fetched == statistics.Fetched && Inserted == statistics.Inserted && Updated == statistics.Updated && Deleted == statistics.Deleted)
                {
                    return true;
                }
            }

            

                return false;
        }
    }
}
