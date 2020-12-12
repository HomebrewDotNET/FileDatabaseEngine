using Microsoft.Extensions.Logging;
using Sels.Core.Extensions.General.Validation;
using Sels.Core.Extensions.Io;
using Sels.Core.Extensions.Io.FileSystem;
using Sels.Core.Extensions.Logging;
using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.FileDatabaseEngine.Page
{
    public abstract class BaseDataPage
    {
        // Constants

        // Fields
        protected object _threadLock = new object();
        protected readonly ILogger _logger;

        // Properties
        internal string Identifier { get; }

        public DirectoryInfo SourceDirectory { get; internal set; }

        // State
        internal RunningState _state = RunningState.Shutdown;

        public BaseDataPage(DirectoryInfo sourceDirectory, string identifier, ILogger logger)
        {
            identifier.ValidateVariable(nameof(identifier));
            sourceDirectory.CreateIfNotExistAndValidate(nameof(sourceDirectory));
            logger.ValidateVariable(nameof(logger));

            Identifier = identifier;
            SourceDirectory = sourceDirectory;
            _logger = logger;
        }

        internal void Shutdown()
        {
            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Shutting down DataPage {Identifier}", x => $"Shut down DataPage {Identifier} in {x.TotalMilliseconds}ms"))
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
            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Starting up DataPage {Identifier}", x => $"Started up DataPage {Identifier} in {x.TotalMilliseconds}ms"))
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
            _logger.LogMessage(LogLevel.Information, $"Checking if DataPage {Identifier} is in state {expectedState}");
            lock (_threadLock)
            {
                _state.ValidateState(Identifier, expectedState);
            }
        }

        // Abstracts
        internal abstract Type SourceType { get; }

        protected abstract void ShutdownAction();
        protected abstract void StartupAction();

    }
}
