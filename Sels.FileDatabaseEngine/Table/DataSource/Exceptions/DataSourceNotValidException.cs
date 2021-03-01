using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class DataSourceNotValidException : Exception
    {
        private const string _messageFormat = "Data source {0} is not in a valid state or could not be found";

        public DataSourceNotValidException(string sourcePath) : base(_messageFormat.FormatString(sourcePath))
        {

        }
    }
}
