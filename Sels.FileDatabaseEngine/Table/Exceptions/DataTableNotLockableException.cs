using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DataTableNotLockableException : FileDatabaseException
    {
        private const string _messageFormat = "Data Table {0} can not be locked";

        public DataTableNotLockableException(string identifier) : base (_messageFormat.FormatString(identifier))
        {

        }
    }
}
