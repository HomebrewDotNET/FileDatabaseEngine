using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class TypeNotSupportedException : FileDatabaseException
    {
        private const string _messageFormat = "Type {0} is not supported. Reason: {1}";

        public TypeNotSupportedException(Type type, string reason) : this(type.ToString(), reason)
        {

        }

        public TypeNotSupportedException(string typeName, string reason) : base(_messageFormat.FormatString(typeName, reason))
        {

        }
    }
}
