using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class TableObjectNotFoundException : FileDatabaseException
    {
        private const string _messageFormat = "Object of type {0} could not be found in Data Table {1}";

        public object MissingObject { get; }

        public TableObjectNotFoundException(object objectNotFound, string tableIdentifier) : base(_messageFormat.FormatString(objectNotFound.GetType(), tableIdentifier))
        {
            MissingObject = objectNotFound;
        }
    }
}
