using Microsoft.Extensions.Logging;
using Sels.Core.Components.Caching;
using Sels.Core.Components.Serialization;
using Sels.Core.Extensions;

using Sels.Core.Extensions.Logging;
using Sels.Core.Extensions.Conversion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Sels.Core.Components.Serialization.Providers;

namespace Sels.FileDatabaseEngine.Page
{
    internal abstract class DataPageSource<T> : IDataPageSourceProvider<T> where T : class
    {
        // Fields
        private object _threadLock = new object();
        private DirectoryInfo _baseDirectory;
        private readonly ILogger _logger;

        internal DataPageSource(ILogger logger)
        {
            logger.ValidateVariable(nameof(logger));

            _logger = logger;
        }

        // Properties
        protected abstract SerializationProvider SerializationProvider { get; }
        private FileInfo _dataFile;

        public void Initialize(DirectoryInfo source)
        {
            _logger.LogMessage(LogLevel.Information, $"Initializing DataPageSource<{typeof(T)}>({SerializationProvider})");

            source.ValidateVariable(nameof(source));
            _baseDirectory = source;

            _dataFile = new FileInfo(Path.Combine(_baseDirectory.FullName, $"Content.{SerializationProvider}"));
        }

        public T Load()
        {
            _logger.LogMessage(LogLevel.Information, $"Loading object from DataPageSource<{typeof(T)}>({SerializationProvider})");
            _dataFile.ValidateVariable(nameof(_dataFile));

            lock (_threadLock)
            {
                using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Reading object from DataPageSource<{typeof(T)}>({SerializationProvider}) {{{_dataFile.FullName}}}", x => $"Read object from DataPageSource<{typeof(T)}>({SerializationProvider}) {{{_dataFile.FullName}}}) in {x.TotalMilliseconds}ms"))
                {
                    var fileContent = _dataFile.Read();

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Read", fileContent);

                    T result = null;

                    if (fileContent.HasValue())
                    {
                        return fileContent.Deserialize<T>(SerializationProvider);
                    }

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Loaded", result);

                    return result;
                }                               
            }
        }

        public void Store(T dataObject)
        {
            _logger.LogMessage(LogLevel.Information, $"Storing object to DataPageSource<{typeof(T)}>({SerializationProvider})");

            _dataFile.ValidateVariable(nameof(_dataFile));

            lock (_threadLock)
            {
                using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Writing object to DataPageSource<{typeof(T)}>({SerializationProvider}) {{{_dataFile.FullName}}}", x => $"Wrote object to DataPageSource<{typeof(T)}>({SerializationProvider}) {{{_dataFile.FullName}}} in {x.TotalMilliseconds}ms"))
                {
                    _dataFile.Write(dataObject.Serialize(SerializationProvider));

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Wrote", dataObject);
                }              
            }
        }

        public void Clear()
        {
            _logger.LogMessage(LogLevel.Information, $"Clearing DataPageSource<{typeof(T)}>({SerializationProvider})");
            _dataFile.ValidateVariable(nameof(_dataFile));

            lock (_threadLock)
            {
                if (_dataFile.Exists)
                {
                    _dataFile.Clear();
                }
            }
        }

        public T Clone(T dataObject)
        {
            _logger.LogMessage(LogLevel.Information, $"Cloning object in DataPageSource<{typeof(T)}>({SerializationProvider})");

            var result =  dataObject.Serialize(SerializationProvider).Deserialize<T>(SerializationProvider);

            _logger.LogObject<JsonProvider>(LogLevel.Debug, "Cloned", dataObject);

            return result;
        }

        public bool IsFree()
        {
            _logger.LogMessage(LogLevel.Information, $"Checking if DataPageSource<{typeof(T)}>({SerializationProvider}) is free");
            _dataFile.ValidateVariable(nameof(_dataFile));

            lock (_threadLock)
            {
                return !_dataFile.IsLocked();
            }
        }
    }
}
