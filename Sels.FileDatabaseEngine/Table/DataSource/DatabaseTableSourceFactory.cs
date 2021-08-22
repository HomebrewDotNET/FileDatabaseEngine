using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using Sels.Core.Components.Serialization.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    public static class DatabaseTableSourceFactory
    {
        public static IDatabaseTableSource<T> CreateTableSource<T>(SerializationProvider provider, ILogger logger)
        {
            switch (provider)
            {
                case SerializationProvider.Json:
                    return new JsonDatabaseTableSource<T>(logger);
                case SerializationProvider.Bson:
                    return new BsonDatabaseTableSource<T>(logger);
                case SerializationProvider.Xml:
                    return new XmlDatabaseTableSource<T>(logger);
            }

            throw new NotSupportedException($"Serialization provider {provider} is not supported");
        }
    }
}
