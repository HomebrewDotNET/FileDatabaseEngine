using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using Sels.FileDatabaseEngine.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Table.Exceptions
{
    public class NoMatchingObjectsFoundException : FileDatabaseException
    {
        private const string _messageFormat = "No matching objects found while querying Data Table {0}";

        public NoMatchingObjectsFoundException(string tableIdentifier) : base(_messageFormat.FormatString(tableIdentifier))
        {

        }
    }
}
