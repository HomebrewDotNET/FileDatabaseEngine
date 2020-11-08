using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    internal class XmlDatabaseTableSource<T> : DatabaseTableSource<T>
    {
        protected override SerializationProvider SerializationProvider => SerializationProvider.Xml;

        public XmlDatabaseTableSource(ILogger logger) : base(logger)
        {

        }
    }
}
