using Microsoft.Extensions.Logging;
using Sels.Core.Extensions;
using Sels.Core.Extensions;

using Sels.Core.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Sels.FileDatabaseEngine.Page
{
    public class DataPage<T> : BaseDataPage where T : class
    {
        // Constants

        // Fields
        private T _data;

        private Func<T> _constructor;
       
        // Properties
        internal override Type SourceType => typeof(T);

        public T DataObject { 
            get {
                _logger.LogMessage(LogLevel.Debug, $"Getting object from DataPage<{typeof(T)}>");
                lock (_threadLock)
                {   
                    if(_data == null)
                    {
                        T result = null;
                        try
                        {
                            result = _sourceProvider.Load();
                        }
                        catch(Exception ex)
                        {
                            _logger.LogException(LogLevel.Warning, $"Error occured while reading object from DataPage<{typeof(T)}>", ex);
                        }

                        if(result == null)
                        {
                            result = _constructor();
                        }

                        _data = result;
                    }
                }

                return _sourceProvider.Clone(_data);
            } 
            set {
                _logger.LogMessage(LogLevel.Debug, $"Getting object on DataPage<{typeof(T)}>");
                lock (_threadLock)
                {
                    _data = _sourceProvider.Clone(value);
                    _sourceProvider.Store(value);
                }
            } 
        }
       
        // Services
        IDataPageSourceProvider<T> _sourceProvider;

        internal DataPage(string identifier, DirectoryInfo sourceDirectory, IDataPageSourceProvider<T> sourceProvider, Func<T> constructor, ILogger logger) : base(sourceDirectory, identifier, logger)
        {
            constructor.ValidateVariable(nameof(constructor));
            sourceDirectory.ValidateVariable(nameof(sourceProvider));

            _constructor = constructor;
            _sourceProvider = sourceProvider;
        }

        protected override void ShutdownAction()
        {
            _logger.LogMessage(LogLevel.Information, $"DataPage<{typeof(T)}> performing shutdown action");

            while (!_sourceProvider.IsFree())
            {
                Thread.Sleep(100);
            }
        }

        protected override void StartupAction()
        {
            _logger.LogMessage(LogLevel.Information, $"DataPage<{typeof(T)}> performing startup action");

            _sourceProvider.Initialize(SourceDirectory);
        }
    }
}
