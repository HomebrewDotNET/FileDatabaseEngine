using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseDataPageIdentifierAlreadyExistsException : FileDatabaseException
    {
        private const string _messageFormat = "Table with identifier {0} already exists in database {1}";

        public DatabaseDataPageIdentifierAlreadyExistsException(string tableIdentifier, string databaseIdentifier) : base(_messageFormat.FormatString(tableIdentifier, databaseIdentifier))
        {

        }
    }
}
