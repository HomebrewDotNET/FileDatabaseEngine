using Sels.Core.Extensions;
using Sels.Core.Extensions;
using Sels.FileDatabaseEngine.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions
{
    public class WrongStateException : FileDatabaseException
    {
        private const string _messageFormat = "Expected {0} to be in state {1} but was {2}";

        internal WrongStateException(string identifier, RunningState expectedState, RunningState state) : base(_messageFormat.FormatString(identifier, expectedState, state))
        {

        }
    }
}
