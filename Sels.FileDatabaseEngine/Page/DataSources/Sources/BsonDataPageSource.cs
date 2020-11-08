using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Page
{
    internal class BsonDataPageSource<T> : DataPageSource<T> where T : class
    {
        protected override SerializationProvider SerializationProvider => SerializationProvider.Bson;

        public BsonDataPageSource(ILogger logger) : base(logger)
        {

        }
    }
}
