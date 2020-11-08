using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    internal class DeadLockedException : FileDatabaseException
    {
        private const string _messageFormat = "Could not acquire lock on data table {0} during allowed time";

        internal DeadLockedException(string dataTableIdentifier) : base(_messageFormat.FormatString(dataTableIdentifier))
        {

        }
    }
}
