using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Page.DataSources.Sources
{
    internal class XmlDataPageSource<T> : DataPageSource<T> where T : class
    {
        protected override SerializationProvider SerializationProvider => SerializationProvider.Xml;

        public XmlDataPageSource(ILogger logger) :base(logger)
        {

        }
    }
}
