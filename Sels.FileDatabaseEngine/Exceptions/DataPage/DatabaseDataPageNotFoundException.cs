using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DatabaseDataPageNotFoundException : Exception
    {
        private const string _messageFormat = "Data Page {0} could not be found in database {1}";

        public DatabaseDataPageNotFoundException(string dataPageIdentifier, string databaseIdentifier) : base(_messageFormat.FormatString(dataPageIdentifier, databaseIdentifier))
        {

        }
    }
}
