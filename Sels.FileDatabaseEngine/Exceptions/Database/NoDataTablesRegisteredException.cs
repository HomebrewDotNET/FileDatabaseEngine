using Sels.Core.Extensions;
using Sels.Core.Extensions.Object.String;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class NoDataTablesRegisteredException : FileDatabaseException
    {
        private const string _messageFormat = "No data tables have been registered in database {0}";

        public NoDataTablesRegisteredException(string databaseIdentifier) : base(_messageFormat.FormatString(databaseIdentifier))
        {

        }
    }
}
