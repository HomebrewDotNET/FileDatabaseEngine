using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using Sels.Core.Components.Serialization.Providers;
using Sels.FileDatabaseEngine.Page.DataSources.Sources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Page
{
    public static class DataPageSourceFactory
    {
        public static IDataPageSourceProvider<T> CreatePageSource<T>(SerializationProvider provider, ILogger logger) where T : class
        {
            switch (provider)
            {
                case SerializationProvider.Json:
                    return new JsonDataPageSource<T>(logger);
                case SerializationProvider.Bson:
                    return new BsonDataPageSource<T>(logger);
                case SerializationProvider.Xml:
                    return new XmlDataPageSource<T>(logger);
            }

            throw new NotSupportedException($"Serialization provider {provider} is not supported");
        }
    }
}
