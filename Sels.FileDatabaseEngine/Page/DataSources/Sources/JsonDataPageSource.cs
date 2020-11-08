using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Page
{
    internal class JsonDataPageSource<T> : DataPageSource<T> where T : class
    {
        protected override SerializationProvider SerializationProvider => SerializationProvider.Json;

        public JsonDataPageSource(ILogger logger) :base(logger)
        {

        }
    }
}
