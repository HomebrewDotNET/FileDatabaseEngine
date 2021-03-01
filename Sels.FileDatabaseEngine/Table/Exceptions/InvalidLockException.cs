using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    internal class InvalidLockException : FileDatabaseException
    {
        private const string _messageFormat = "Invalid lock was passed when trying to access Data Table {0}";

        public InvalidLockException(string dataTableIdentifier) : base (_messageFormat.FormatString(dataTableIdentifier))
        {

        }
    }
}
