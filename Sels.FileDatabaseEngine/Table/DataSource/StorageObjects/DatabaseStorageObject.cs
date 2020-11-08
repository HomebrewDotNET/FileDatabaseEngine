using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    internal class DatabaseStorageObject<T>
    {
        public DatabaseStorageMetaData MetaData { get; set; }
        public DatabaseStorageData<T> Data { get; set; }

        public DatabaseStorageObject()
        {

        }

        public DatabaseStorageObject(IEnumerable<T> data)
        {
            MetaData = new DatabaseStorageMetaData(Environment.MachineName, typeof(T).ToString(), DateTime.Now, data == null ? 0 : data.Count());
            Data = new DatabaseStorageData<T>(data);
        }
    }
}
 