using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseIdentifierAlreadyUsed : FileDatabaseException
    {
        private const string _messageFormat = "Database with identifier {0} already exists";

        public DatabaseIdentifierAlreadyUsed(string identifier) : base (_messageFormat.FormatString(identifier))
        {

        }
    }
}
