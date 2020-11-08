using Microsoft.Extensions.Logging;
using Sels.Core.Components.Locking;
using Sels.Core.Extensions;
using Sels.Core.Extensions.General.Validation;
using Sels.Core.Extensions.Io.FileSystem;
using Sels.Core.Extensions.Logging;
using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.Exceptions;
using Sels.FileDatabaseEngine.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Sels.FileDatabaseEngine.Table
{
    public abstract class BaseDatabaseTable
    {
        // Constants
        protected const string TableFileName = "Table.info";
        protected const int ThreadSleep = 100;
        protected const int ShutdownTimeout = 5000;

        // Fields
        protected readonly object _threadLock = new object();
        protected readonly static object _globalThreadLock = new object();
        protected ILogger _logger;

        // Properties
        public virtual string Identifier { get; }

        public virtual Type SourceType { get; }

        public DirectoryInfo SourceDirectory { get; internal set; }

        // State
        protected Lock _lock;
        protected bool _hasPendingChanges = false;
        protected bool _isDeadlocked = false;

        internal RunningState _state = RunningState.Shutdown;

        // Settings
        protected readonly int _lockTimeout;

        public BaseDatabaseTable(DirectoryInfo sourceDirectoy, string identifier, int timeout, ILogger logger)
        {
            sourceDirectoy.EnsureExistsAndValidate(nameof(sourceDirectoy));
            identifier.ValidateVariable(nameof(identifier));
            timeout.ValidateVariable(nameof(timeout));
            logger.ValidateVariable(nameof(logger));

            Identifier = identifier;
            _lockTimeout = timeout;
            SourceDirectory = sourceDirectoy;
            _logger = logger;
        }

        internal void Shutdown()
        {
            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Shutting down DatabaseTable({Identifier})<{SourceType}>", x => $"Shut down DatabaseTable({Identifier})<{SourceType}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    _state.ValidateState(Identifier, RunningState.Running);
                    _state = RunningState.ShuttingDown;
                }

                ShutdownAction();

                lock (_threadLock)
                {
                    _state = RunningState.Shutdown;
                }
            }         
        }

        internal void StartUp()
        {
            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Starting up DatabaseTable({Identifier})<{SourceType}>", x => $"Started up DatabaseTable({Identifier})<{SourceType}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    _state.ValidateState(Identifier, RunningState.Shutdown);
                    _state = RunningState.StartingUp;
                }

                StartupAction();

                lock (_threadLock)
                {
                    _state = RunningState.Running;
                }
            }        
        }

        internal void IsInState(RunningState expectedState)
        {
            _logger.LogMessage(LogLevel.Information, $"Checking if DatabaseTable({Identifier})<{SourceType}> is in state {expectedState}");
            lock (_threadLock)
            {
                _state.ValidateState(Identifier, expectedState);
            }
        }

        #region Lock
        protected void CanGetLock()
        {
            _logger.LogMessage(LogLevel.Information, $"Checking if DatabaseTable({Identifier})<{SourceType}> can be locked");
            lock (_threadLock)
            {
                if (_isDeadlocked) {
                    _logger.LogMessage(LogLevel.Debug, $"DatabaseTable({Identifier})<{SourceType}> is Deadlocked. Chose deadlock victim.");

                    try
                    {
                        throw new DeadLockedException(Identifier);
                    }
                    finally
                    {
                        _isDeadlocked = false;
                    }                    
                   };
                _state.ValidateState(Identifier, RunningState.Running);
            }          
        }
        protected bool TryGetAndSetLock()
        {
            _logger.LogMessage(LogLevel.Information, $"Trying to get lock on DatabaseTable({Identifier})<{SourceType}>");
            try
            {
                if (Monitor.TryEnter(_threadLock))
                {
                    if (_lock == null)
                    {
                        _lock = new Lock(this);
                        return true;
                    };
                };
                return false;
            }
            finally
            {
                Monitor.Exit(_threadLock);
            }

        }

        public virtual Lock TryGetLock()
        {
            return TryGetLock(_lockTimeout);
        }

        public virtual Lock TryGetLock(int maxWaitTime)
        {
            _logger.LogMessage(LogLevel.Information, $"Trying to get lock on DatabaseTable({Identifier})<{SourceType}> using a max wait time of {maxWaitTime}ms");
            maxWaitTime.ValidateVariable((x) => x > 1, () => $"{nameof(maxWaitTime)} must be higher than 1");

            var timeOutTimer = new Stopwatch();

            try
            {
                timeOutTimer.Start();
                while (true)
                {
                    CanGetLock();

                    if(TryGetAndSetLock())
                    {                       
                        return _lock;
                    }

                    _logger.LogMessage(LogLevel.Debug, $"Could not get lock on DatabaseTable({Identifier})<{SourceType}>. Sleeping Thread for {ThreadSleep}ms");
                    Thread.Sleep(ThreadSleep);

                    if (timeOutTimer.ElapsedMilliseconds > maxWaitTime)
                    {
                        _logger.LogMessage(LogLevel.Debug, $"Could not get lock on DatabaseTable({Identifier})<{SourceType}> within {maxWaitTime}ms. Tagging DatabaseTable as Deadlocked");
                        lock (_threadLock)
                        {
                            if (_isDeadlocked == false)
                            {
                                _isDeadlocked = true;
                            }
                        }
                    }
                }
            }
            finally
            {
                timeOutTimer.Stop();
            }
        }

        public abstract void Persist(Lock tableLock);

        public abstract void Abort(Lock tableLock);
        #endregion

        protected abstract void Clear();
        protected abstract void StartupAction();
        protected abstract void ShutdownAction();
    }
}
