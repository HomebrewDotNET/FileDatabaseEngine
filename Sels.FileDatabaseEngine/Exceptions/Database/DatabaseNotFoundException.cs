using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseNotFoundException : FileDatabaseException
    {
        private const string _messageFormat = "Database {0} could not be found";

        public DatabaseNotFoundException(string databaseIdentifier) : base(_messageFormat.FormatString(databaseIdentifier))
        {

        }
    }
}

