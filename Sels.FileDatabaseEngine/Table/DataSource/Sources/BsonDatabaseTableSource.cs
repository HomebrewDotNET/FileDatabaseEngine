using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    internal class BsonDatabaseTableSource<T> : DatabaseTableSource<T>
    {
        protected override SerializationProvider SerializationProvider => SerializationProvider.Bson;

        public BsonDatabaseTableSource(ILogger logger) :base(logger)
        {

        }
    }
}
