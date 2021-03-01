using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseShuttingDownException : FileDatabaseException
    {
        private const string _messageFromat = "Database {0} is shutting down";

        public DatabaseShuttingDownException(string identifier) : base(_messageFromat.FormatString(identifier))
        {

        }
    }
}
