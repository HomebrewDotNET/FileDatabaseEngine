using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseTableIdentifierAlreadyExistsException : FileDatabaseException
    {
        private const string _messageFormat = "Data page with identifier {0} already exists in database {1}";

        public DatabaseTableIdentifierAlreadyExistsException(string pageIdentifier, string databaseIdentifier) : base(_messageFormat.FormatString(pageIdentifier, databaseIdentifier))
        {

        }
    }
}
