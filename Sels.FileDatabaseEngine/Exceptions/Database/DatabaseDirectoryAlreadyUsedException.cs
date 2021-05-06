using System;
using System.Collections.Generic;
using System.Text;
using Sels.Core.Extensions;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseDirectoryAlreadyUsedException : FileDatabaseException
    {
        private const string _messageFormat = "Could not initialize Database {0}. Directory {1} is already being used by Database {2}";

        public DatabaseDirectoryAlreadyUsedException(string databaseIdentifier, string directory, string sourceDatabaseIdentifier) : base(_messageFormat.FormatString(databaseIdentifier, directory, sourceDatabaseIdentifier))
        {

        }
    }
}
