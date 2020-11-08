using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    internal class DatabaseStorageMetaData
    {
        public string ModifiedBy { get; set; }
        public DateTime LastModifed { get; set; }
        public string SourceType { get; set; }
        public int StoredItems { get; set; }

        public DatabaseStorageMetaData(string modifiedBy, string sourceType, DateTime lastModifed, int storedItems)
        {
            ModifiedBy = modifiedBy;
            SourceType = sourceType;
            LastModifed = lastModifed;
            StoredItems = storedItems;
        }

        public DatabaseStorageMetaData()
        {

        }
    }
}
