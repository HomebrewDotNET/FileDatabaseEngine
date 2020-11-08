using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Extensions
{
    internal static class StateExtensions
    {
        internal static void ValidateState(this RunningState currentState, string identifier, RunningState expectedState)
        {
            if (!currentState.Equals(expectedState)) throw new WrongStateException(identifier, expectedState, currentState);
        }
    }
}
