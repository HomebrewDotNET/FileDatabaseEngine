using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseNotInitializedException : FileDatabaseException
    {
        private const string _messageFormat = "Database Engine {0} is not yet initialized";

        public DatabaseNotInitializedException(string identifier) : base(_messageFormat.FormatString(identifier))
        {

        }
    }
}
