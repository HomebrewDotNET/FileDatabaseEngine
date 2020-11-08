using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseAlreadyInitializedException : FileDatabaseException
    {
        private const string _messageFormat = "Database Engine {0} is already initialized";

        public DatabaseAlreadyInitializedException(string identifier) : base(_messageFormat.FormatString(identifier))
        {

        }
    }
}
