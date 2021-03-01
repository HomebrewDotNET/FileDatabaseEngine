using Microsoft.Extensions.Logging;
using Sels.Core.Components.Backup;
using Sels.Core.Components.Caching;
using Sels.Core.Components.Locking;
using Sels.Core.Components.Serialization;
using Sels.Core.Extensions;
using Sels.Core.Extensions;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Io;
using Sels.Core.Extensions.Io;
using Sels.Core.Extensions.Logging;
using Sels.Core.Extensions.Serialization;
using Sels.FileDatabaseEngine.Exceptions;
using Sels.FileDatabaseEngine.Exceptions.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sels.FileDatabaseEngine.Table
{
    internal abstract class DatabaseTableSource<T> : IDatabaseTableSource<T> 
    {
        // Constants
        private const string _backUpDirectory = "BackUps";

        // Fields
        private readonly object _threadLock = new object();
        private DirectoryInfo _source;
        private FileBackupManager _backupManager;
        private ILogger _logger;

        // Property provider
        private readonly ValueCache<string> _sourceFileProvider;

        // Properties

        private string SourceFile => _sourceFileProvider.Value;

        protected abstract SerializationProvider SerializationProvider { get; }

        public DatabaseTableSource(ILogger logger)
        {
            logger.ValidateVariable(nameof(logger));

            _logger = logger;

            _sourceFileProvider = new ValueCache<string>(() => Path.Combine(_source.FullName, $"Data.{SerializationProvider}"));
        }

        #region Initialization And Validation
        public void Initialize(DirectoryInfo source)
        {
            source.CreateIfNotExistAndValidate(nameof(source));

            _source = source;

            _backupManager = new FileBackupManager(new FileInfo(SourceFile), new DirectoryInfo(Path.Combine(_source.FullName, _backUpDirectory)), BackupRetentionMode.Amount, 5);
            Initialize();
        }
        protected virtual void Initialize()
        {
            using(var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Initializing DatabaseTableSource<{typeof(T)}>", x => $"Initialized DatabaseTableSource<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                _backupManager.ValidateVariable(nameof(_backupManager));

                lock (_threadLock)
                {
                    if (File.Exists(SourceFile))
                    {
                        ValidateSourceAndTryRestoreIfFailed();
                    }
                    else
                    {
                        InitializeSource();
                    }
                }
            }       
        }

        protected virtual void InitializeSource()
        {
            _logger.LogMessage(LogLevel.Information, $"Creating source for DatabaseTableSource<{typeof(T)}>");
            lock (_threadLock)
            {
                var storageObject = new DatabaseStorageObject<T>(new List<T>());
                File.WriteAllText(SourceFile, Convert(storageObject));
            }
        }

        protected virtual void ValidateSource()
        {
            _logger.LogMessage(LogLevel.Information, $"Validating source file {SourceFile} for DatabaseTableSource<{typeof(T)}>");
            lock (_threadLock)
            {
                if (!File.Exists(SourceFile))
                {
                    throw new DataSourceNotValidException(SourceFile);
                }
                else
                {
                    var storageObject = Convert(File.ReadAllText(SourceFile));

                    if (storageObject == null || (storageObject.Data.DataItems == null ? 0 : storageObject.Data.DataItems.Count()) != storageObject.MetaData.StoredItems)
                    {
                        throw new DataSourceNotValidException(SourceFile);
                    }
                }         
            }
        }

        protected virtual void ValidateSourceAndTryRestoreIfFailed()
        {
            _logger.LogMessage(LogLevel.Information, $"Starting validation of DatabaseTableSource<{typeof(T)}> and will try restore backup if failed");
            lock (_threadLock)
            {
                try
                {
                    ValidateSource();
                }
                catch(Exception ex)
                {
                    _logger.LogException(LogLevel.Warning, $"Could not validate source for DatabaseTableSource<{typeof(T)}>", ex);
                    if (!TryRestoreBackupUntilValidSource()) {
                        _logger.LogException(LogLevel.Error, $"Could not restore any backups for DatabaseTableSource<{typeof(T)}>", ex);
                        throw new DatabaseTableSourceCouldNotRestoreBackupException(typeof(T), _backupManager?.Backups.Count ?? 0);
                    };
                }
               

            }
        }
        #endregion

        #region Provider
        public IEnumerable<T> Load()
        {
            _logger.LogMessage(LogLevel.Information, $"Loading data objects from DatabaseTableSource<{typeof(T)}>");
            lock (_threadLock)
            {
                var storageObject = Convert(File.ReadAllText(SourceFile));

                var result = storageObject.Data.DataItems ?? new List<T>();

                _logger.LogMessage(LogLevel.Debug, () => $"Loaded {result.Count()} data objects from DatabaseTableSource<{typeof(T)}>");

                return result;
            }
        }

        public void Persist(IEnumerable<T> values)
        {
            _logger.LogMessage(LogLevel.Information, $"Persisting data objects to DatabaseTableSource<{typeof(T)}>");

            lock (_threadLock)
            {
                var storageObject = new DatabaseStorageObject<T>(values);
                File.WriteAllText(SourceFile, Convert(storageObject));

                _logger.LogMessage(LogLevel.Debug, () => $"Persisted {values.Count()} data objects to DatabaseTableSource<{typeof(T)}>");
            }
        }

        public bool IsFree()
        {
            _logger.LogMessage(LogLevel.Information, $"Checking if DatabaseTableSource<{typeof(T)}> is free");
            lock (_threadLock)
            {
                return !new FileInfo(SourceFile).IsLocked();
            }
        }
        #endregion

        #region Migration
        public void MigrateFromSource(IDatabaseTableSource<T> oldSource, bool keepOldSource = false)
        {
            _logger.LogMessage(LogLevel.Information, $"DatabaseTableSource<{typeof(T)}> is migrating data objects from other data source");

            oldSource.ValidateVariable(nameof(oldSource));
            
            // Load in items from old source
            var items = oldSource.Load();

            // Perist to new source
            if (items.HasValue())
            {
                Persist(items);
            }

            // Delete old source
            if(!keepOldSource) oldSource.ClearSource();
        }

        public void ClearSource()
        {
            _logger.LogMessage(LogLevel.Information, $"Clearing DatabaseTableSource<{typeof(T)}>");

            if (File.Exists(SourceFile))
            {
                File.Delete(SourceFile);
            }

            if (_backupManager.HasValue())
            {
                _backupManager.DeleteAll();
            }
        }
        #endregion

        #region Backup
        private bool TryRestoreBackupUntilValidSource()
        {
            _logger.LogMessage(LogLevel.Information, $"Trying to restore backups for DatabaseTableSource<{typeof(T)}>");

            _backupManager.ValidateVariable(nameof(_backupManager));          

            if (_backupManager.Backups.HasValue())
            {
                foreach (var backup in _backupManager.Backups)
                {
                    if (backup.TryRestoreBackup(_source))
                    {
                        try
                        {
                            ValidateSource();
                            _logger.LogMessage(LogLevel.Information, $"Backup {backup.BackedupFile.FullName} successfully restored for DatabaseTableSource<{typeof(T)}>");
                            return true;
                        }
                        catch (Exception ex){
                            _logger.LogException(LogLevel.Warning, $"Could not restore backup {backup.BackedupFile.FullName} for DatabaseTableSource<{typeof(T)}>", ex);
                        }

                    }
                }
            }

            return false;
        }

        public void CreateBackup()
        {
            _logger.LogMessage(LogLevel.Information, $"Creating backup for DatabaseTableSource<{typeof(T)}>");

            _backupManager.ValidateVariable(nameof(_backupManager));

            var backup = _backupManager.CreateBackup();

            _logger.LogMessage(LogLevel.Information, $"Created backup in {backup.SourceDirectory} for DatabaseTableSource <{typeof(T)}>");
        }

        public IEnumerable<Backup> GetBackups()
        {
            _logger.LogMessage(LogLevel.Information, $"Getting all backups for DatabaseTableSource<{typeof(T)}>");
            _backupManager.ValidateVariable(nameof(_backupManager));

            return _backupManager.Backups;
        }

        public void RestoreLatestBackup()
        {
            _logger.LogMessage(LogLevel.Information, $"Restoring latest backup for DatabaseTableSource<{typeof(T)}>");
            _backupManager.ValidateVariable(nameof(_backupManager));

            _backupManager.RestoreLatestBackup(_source);
        }

        public void RestoreEarliestBackup()
        {
            _logger.LogMessage(LogLevel.Information, $"Restoring latest backup for DatabaseTableSource<{typeof(T)}>");
            _backupManager.ValidateVariable(nameof(_backupManager));

            _backupManager.RestoreEarliestBackup(_source);
        }

        public void RestoreBackup(Backup backup)
        {
            _logger.LogMessage(LogLevel.Information, $"Restoring backup for DatabaseTableSource<{typeof(T)}>");

            _backupManager.ValidateVariable(nameof(_backupManager));
            backup.ValidateVariable(nameof(backup));

            backup.RestoreBackup(_source);
        }
        #endregion

        #region Serialization
        public DatabaseStorageObject<T> Convert(string source)
        {
            _logger.LogMessage(LogLevel.Information, $"Converting string to StorageObject in DatabaseTableSource<{typeof(T)}>");
            return source.Deserialize<DatabaseStorageObject<T>>(SerializationProvider);
        }
        public string Convert(DatabaseStorageObject<T> storageObject)
        {
            _logger.LogMessage(LogLevel.Information, $"Converting StorageObject to string in DatabaseTableSource<{typeof(T)}>");

            return storageObject.Serialize(SerializationProvider);
        }

        public IEnumerable<T> Clone(IEnumerable<T> values)
        {
            _logger.LogMessage(LogLevel.Information, () => $"Cloning {values.Count()} objects in DatabaseTableSource<{typeof(T)}>");

            return values.Serialize(SerializationProvider).Deserialize<IEnumerable<T>>(SerializationProvider);
        }
        public T Clone(T value)
        {
            _logger.LogMessage(LogLevel.Information, () => $"Cloning object in DatabaseTableSource<{typeof(T)}>");

            return value.Serialize(SerializationProvider).Deserialize<T>(SerializationProvider);
        }
        #endregion
    }

}
