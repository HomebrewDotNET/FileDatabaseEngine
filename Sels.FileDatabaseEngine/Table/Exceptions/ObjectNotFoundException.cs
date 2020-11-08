using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class ObjectNotFoundException : FileDatabaseException
    {
        private const string _messageFormat = "Object of type {0} could not be found in Data Table {1}";

        public object MissingObject { get; }

        public ObjectNotFoundException(object objectNotFound, string tableIdentifier) : base(_messageFormat.FormatString(objectNotFound.GetType(), tableIdentifier))
        {
            MissingObject = objectNotFound;
        }
    }
}
