using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    internal class DatabaseStorageData<T>
    {
        public IEnumerable<T> DataItems { get; set; }

        public DatabaseStorageData()
        {
                
        }

        public DatabaseStorageData(IEnumerable<T> dataItems)
        {
            DataItems = dataItems;
        }
    }
}
