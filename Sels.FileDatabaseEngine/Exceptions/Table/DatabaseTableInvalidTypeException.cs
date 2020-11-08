using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseTableInvalidTypeException : FileDatabaseException
    {
        private const string _messageFormat = "Data Table {0} does not support type {1}";

        public DatabaseTableInvalidTypeException(string dataTableIdentifier, Type requestedType) : base(_messageFormat.FormatString(dataTableIdentifier, requestedType.ToString()))
        {

        }
    }
}
