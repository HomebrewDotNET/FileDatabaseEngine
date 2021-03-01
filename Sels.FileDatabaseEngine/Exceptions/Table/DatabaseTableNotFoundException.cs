using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseTableNotFoundException : FileDatabaseException
    {
        private const string _messageFormat = "Data Table {0} could not be found in database {1}";

        public DatabaseTableNotFoundException(string tableIdentifier, string databaseIdentifier) : base(_messageFormat.FormatString(tableIdentifier, databaseIdentifier))
        {

        }
    }
}
