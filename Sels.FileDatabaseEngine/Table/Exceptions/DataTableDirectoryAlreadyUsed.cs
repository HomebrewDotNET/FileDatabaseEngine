using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DataTableDirectoryAlreadyUsed : FileDatabaseException
    {
        private const string _messageFormat = "Could not initialize Data Table {0}. Directory {1} is already being used by Data Table {2}";

        public DataTableDirectoryAlreadyUsed(string tableIdentifier, string directory, string sourceTableIdentifier) : base(_messageFormat.FormatString(tableIdentifier, directory, sourceTableIdentifier))
        {

        }
    }
}
