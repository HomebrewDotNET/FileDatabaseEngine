using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    internal class JsonDatabaseTableSource<T> : DatabaseTableSource<T>
    {
        protected override SerializationProvider SerializationProvider => SerializationProvider.Json;

        public JsonDatabaseTableSource(ILogger logger) :base(logger)
        {

        }
    }
}
