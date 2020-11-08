using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public abstract class FileDatabaseException : Exception
    {
        public FileDatabaseException(string message) : base(message)
        {

        }
    }
}
