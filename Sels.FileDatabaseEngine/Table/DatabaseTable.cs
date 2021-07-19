using Microsoft.Extensions.Logging;
using Sels.Core.Components.Caching;
using Sels.Core.Components.Locking;
using Sels.Core.Components.Serialization;
using Sels.Core.Extensions;

using Sels.Core.Extensions.Linq;
using Sels.Core.Extensions.Logging;
using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.Exceptions;
using Sels.FileDatabaseEngine.Interfaces;
using Sels.FileDatabaseEngine.Table;
using Sels.FileDatabaseEngine.Table.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using static Sels.Core.Delegates;

namespace Sels.FileDatabaseEngine.Table
{
    internal class DatabaseTable<T> : BaseDatabaseTable
    {

        // Fields

        // Properties 
        private readonly ValueCache<List<T>> CachedItems;
        public override Type SourceType => typeof(T);

        // Services
        private IDatabaseTableSource<T> _dataSource;
        private ITypeActivator<IDatabaseTableSource<T>> _typeActivator;

        // Settings
        private readonly Comparator<T> _comparator;

        internal DatabaseTable(string identifier, IDatabaseTableSource<T> dataSource, ITypeActivator<IDatabaseTableSource<T>> typeActivator, DirectoryInfo sourceDirectory, int timeout, Comparator<T> comparator, ILogger logger) : base(sourceDirectory, identifier, timeout, logger)
        {
            comparator.ValidateVariable(nameof(comparator));
            dataSource.ValidateVariable(nameof(dataSource));

            _comparator = comparator;
            _dataSource = dataSource;
            _typeActivator = typeActivator;

            CachedItems = new ValueCache<List<T>>(() => new List<T>(_dataSource.Load()));
        }
   
        #region Queries
        public IEnumerable<T> GetAll(Lock tableLock)
        {
            ValidateLock(tableLock);

            using(_logger.CreateTimedLogger(LogLevel.Debug, $"Getting All items from DatabaseTable({Identifier})<{typeof(T)}>", x => $"Got All items from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    var result = CachedItems.Value;

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Got", result);
                    return _dataSource.Clone(result);
                }
            }

            
        }

        public IEnumerable<T> Query(Lock tableLock, Predicate<T> query)
        {
            ValidateLock(tableLock);
            query.ValidateVariable(nameof(query));

            var results = new List<T>();

            using (_logger.CreateTimedLogger(LogLevel.Debug, $"Querying items from DatabaseTable({Identifier})<{typeof(T)}>", x => $"Queried items from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    var result = CachedItems.Value.WherePredicate(query);
                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Got", result);

                    foreach (var item in result)
                    {
                        if (query(item))
                        {
                            results.Add(_dataSource.Clone(item));
                        }
                    }
                }

                return results;
            }
            
        }

        public T Get(Lock tableLock, Predicate<T> query)
        {
            ValidateLock(tableLock);
            query.ValidateVariable(nameof(query));

            using (_logger.CreateTimedLogger(LogLevel.Debug, $"Querying item from DatabaseTable({Identifier})<{typeof(T)}> using query", x => $"Queried item from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms using query"))
            {
                lock (_threadLock)
                {
                    foreach (var item in CachedItems.Value)
                    {
                        if (query(item))
                        {
                            _logger.LogObject<JsonProvider>(LogLevel.Debug, "Got", item);

                            return _dataSource.Clone(item);
                        }
                    }
                }
            }

            _logger.LogMessage(LogLevel.Debug, $"No matching items found while querying single item from DatabaseTable({Identifier})<{typeof(T)}>");
            throw new NoMatchingObjectsFoundException(Identifier);
        }

        public void Insert(Lock tableLock, T item)
        {
            ValidateLock(tableLock);

            using (_logger.CreateTimedLogger(LogLevel.Debug, $"Inserting item in DatabaseTable({Identifier})<{typeof(T)}>", x => $"Inserted item in DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    InsertItem(item);
                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Inserted", item);
                }
            }     
        }

        public void Insert(Lock tableLock, IEnumerable<T> items)
        {
            ValidateLock(tableLock);
            items.ValidateVariable(nameof(items));

            using (_logger.CreateTimedLogger(LogLevel.Debug, () => $"Inserting {items.Count()} items in DatabaseTable({Identifier})<{typeof(T)}>", x => $"Inserted {items.Count()} items in DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    foreach (var item in items)
                    {
                        InsertItem(item);
                    }

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Inserted", items);
                }
            }        
        }

        public void Update(Lock tableLock, T item)
        {
            ValidateLock(tableLock);

            using (_logger.CreateTimedLogger(LogLevel.Debug, $"Updating item from DatabaseTable({Identifier})<{typeof(T)}>", x => $"Updating item from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    UpdateItem(item);

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Updated", item);
                }
            }         
        }

        public void Update(Lock tableLock, IEnumerable<T> items)
        {
            ValidateLock(tableLock);
            items.ValidateVariable(nameof(items));

            using (_logger.CreateTimedLogger(LogLevel.Debug, () => $"Updating {items.Count()} items from DatabaseTable({Identifier})<{typeof(T)}>", x => $"Updating {items.Count()} items from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    foreach (var item in items)
                    {
                        UpdateItem(item);
                    }

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Updated", items);
                }
            }
        }

        public void Update(Lock tableLock, Predicate<T> query, Action<T> action)
        {
            ValidateLock(tableLock);
            query.ValidateVariable(nameof(query));
            action.ValidateVariable(nameof(action));

            using (_logger.CreateTimedLogger(LogLevel.Debug, $"Updating items from DatabaseTable({Identifier})<{typeof(T)}> using query", x => $"Updating items from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms using query"))
            {
                lock (_threadLock)
                {
                    var matchingItems = CachedItems.Value.WherePredicate(query);

                    if (matchingItems.HasValue())
                    {
                        matchingItems.Execute(action);
                        _hasPendingChanges = true;

                        _logger.LogObject<JsonProvider>(LogLevel.Debug, "Updated", matchingItems);
                    }
                    else
                    {
                        _logger.LogMessage(LogLevel.Debug, "No matching items found while updating via query");
                    }
                }
            }       
        }

        public void Delete(Lock tableLock, T item)
        {
            ValidateLock(tableLock);

            using (_logger.CreateTimedLogger(LogLevel.Debug, $"Deleting item from DatabaseTable({Identifier})<{typeof(T)}>", x => $"Deleted item from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    DeleteItem(item);

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Deleted", item);
                }
            }
            
            
        }

        public void Delete(Lock tableLock, IEnumerable<T> items)
        {
            ValidateLock(tableLock);
            items.ValidateVariable(nameof(items));

            using (_logger.CreateTimedLogger(LogLevel.Debug, () => $"Deleting {items.Count()} items from DatabaseTable({Identifier})<{typeof(T)}>", x => $"Deleted {items.Count()} items from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    foreach (var item in items)
                    {
                        DeleteItem(item);
                    }

                    _logger.LogObject<JsonProvider>(LogLevel.Debug, "Deleted", items);
                }
            }

             
        }

        public void Delete(Lock tableLock, Predicate<T> query)
        {
            ValidateLock(tableLock);
            query.ValidateVariable(nameof(query));

            using (_logger.CreateTimedLogger(LogLevel.Debug, () => $"Deleting items from DatabaseTable({Identifier})<{typeof(T)}> using query", x => $"Deleted items from DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms using query"))
            {
                lock (_threadLock)
                {
                    var result = CachedItems.Value.RemoveAll(query);
                    if (result.HasValue())
                    {
                        _hasPendingChanges = true;

                        _logger.LogMessage(LogLevel.Debug, $"Deleted {result} items from DatabaseTable({Identifier})<{typeof(T)}> using query");
                    }
                    else
                    {
                        _logger.LogMessage(LogLevel.Debug, $"No items deleted from DatabaseTable({Identifier})<{typeof(T)}> using query");
                    }
                }
            }       
        }

        #endregion

        #region InfoFile
        private void CreateInfoFile(string fileName, Type dataProvider)
        {
            _logger.LogMessage(LogLevel.Information, () => $"Creating info file {fileName} for DatabaseTable({Identifier})<{typeof(T)}> with provider {dataProvider.AssemblyQualifiedName}");

            File.WriteAllText(fileName, $"{Identifier}|{typeof(T).AssemblyQualifiedName}|{dataProvider.AssemblyQualifiedName}|{Environment.MachineName}|{DateTime.Now}");
        }

        private void UpdateInfoFile(string fileName, Type dataProvider, string creator, DateTime createdTimestamp)
        {
            _logger.LogMessage(LogLevel.Information, () => $"Updating info file {fileName} for DatabaseTable({Identifier})<{typeof(T)}> with provider {dataProvider.AssemblyQualifiedName} and creator {creator}");

            File.WriteAllText(fileName, $"{Identifier}|{typeof(T).AssemblyQualifiedName}|{dataProvider.AssemblyQualifiedName}|{creator}|{createdTimestamp}");
        }

        private (string Identifier, Type SourceType, Type DataProvider, string Creator, DateTime CreatedTimestamp) ReadInfoFile(string fileName)
        {
            _logger.LogMessage(LogLevel.Information, () => $"Reading info file {fileName} for DatabaseTable({Identifier})<{typeof(T)}>");

            var content = File.ReadAllText(fileName).Split('|');

            return (content[0], Type.GetType(content[1]), Type.GetType(content[2]), content[3], DateTime.Parse(content[4]));
        }
        #endregion

        #region Persistance
        public override void Persist(Lock tableLock)
        {
            ValidateLock(tableLock);
            _logger.LogMessage(LogLevel.Information, $"Checking DatabaseTable({Identifier})<{typeof(T)}> if items need to be persisted");

            lock (_threadLock)
            {
                try
                {
                    if (_hasPendingChanges)
                    {
                        using(var logger = _logger.CreateTimedLogger(LogLevel.Information, $"DatabaseTable({Identifier})<{typeof(T)}> has pending changes. Persisting", x => $"Persisted changes on DatabaseTable({Identifier})<{typeof(T)}> in {x.TotalMilliseconds}ms"))
                        {
                            _dataSource.Persist(CachedItems.Value);
                        }                       
                    }
                    else
                    {
                        _logger.LogMessage(LogLevel.Information, $"DatabaseTable({Identifier})<{typeof(T)}> did not have any pending changes");
                    }
                }
                finally
                {
                    Clear();
                }

            }
        }

        public override void Abort(Lock tableLock)
        {
            ValidateLock(tableLock);
            _logger.LogMessage(LogLevel.Information, $"Aborting changes on DatabaseTable({Identifier})<{typeof(T)}>");

            lock (_threadLock)
            {
                Clear();
            }
        }

        protected override void Clear()
        {
            _logger.LogMessage(LogLevel.Information, $"Clearing remnants of previous connection on DatabaseTable({Identifier})<{typeof(T)}>");
            lock (_threadLock)
            {
                // Only clear cache when we had pending changes. This way sequential reads are faster
                if (_hasPendingChanges)
                {
                    CachedItems.ResetCache();
                }

                _hasPendingChanges = false;
                _isDeadlocked = false;
                _lock.Dispose();
                _lock = null;
            }
        }
        #endregion

        #region State
        protected override void ShutdownAction()
        {
            _logger.LogMessage(LogLevel.Information, $"DatabaseTable({Identifier})<{typeof(T)}> performing shutdown action");

            var timer = new Stopwatch();
            try
            {
                timer.Start();
                while (true)
                {
                    // Try get lock so other threads can't access table
                    if (TryGetAndSetLock())
                    {
                        _logger.LogMessage(LogLevel.Debug, $"DatabaseTable({Identifier})<{typeof(T)}> got lock in graceful manner. Performing graceful shutdown");
                        _dataSource.CreateBackup();
                        Clear();
                        break;
                    }
                    // If we can't get lock we force close down
                    else if (timer.ElapsedMilliseconds < ShutdownTimeout)
                    {
                        _logger.LogMessage(LogLevel.Debug, $"DatabaseTable({Identifier})<{typeof(T)}> could not get lock in a graceful manner. Waited for {ShutdownTimeout}ms. Forcing shutdown");

                        // Releases lock so other processed fail when they have an open connection
                        Clear();

                        // Wait for flush of data source
                        while (!_dataSource.IsFree())
                        {
                            _logger.LogMessage(LogLevel.Debug, $"DatabaseTable({Identifier})<{typeof(T)}> waiting for datasource");
                            Thread.Sleep(100);
                        }
                        _dataSource.CreateBackup();
                        break;
                    };
                }
                
            }
            finally
            {
                timer.Stop();
                
            }
        }

        protected override void StartupAction()
        {
            _logger.LogMessage(LogLevel.Information, $"DatabaseTable({Identifier})<{typeof(T)}> performing startup action");

            lock (_globalThreadLock)
            {
                SourceDirectory.CreateIfNotExistAndValidate(nameof(SourceDirectory));
                _dataSource.Initialize(SourceDirectory);

                var infoFileName = Path.Combine(SourceDirectory.FullName, TableFileName);

                if (File.Exists(infoFileName))
                {
                    var content = ReadInfoFile(infoFileName);

                    // Identifier differs so the directory is probably used by another source
                    if (!content.Identifier.Equals(Identifier))
                    {
                        _logger.LogMessage(LogLevel.Debug, $"DatabaseTable({Identifier})<{typeof(T)}> tried to start in directory {SourceDirectory} but it is already being used by {content.Identifier}");
                        throw new DataTableDirectoryAlreadyUsed(Identifier, SourceDirectory.FullName, content.Identifier);
                    }

                    // Old source is different to current source so we need to migrate
                    if (!content.DataProvider.Equals(_dataSource.GetType()))
                    {
                        _logger.LogMessage(LogLevel.Debug, $"DatabaseTable({Identifier})<{typeof(T)}> current DataSource<{_dataSource.GetType()}> does not match DataSource<{content.DataProvider}>. Performing migration");
                        MigrateSource(content.DataProvider);

                        // Update info file with current provider
                        UpdateInfoFile(infoFileName, _dataSource.GetType(), content.Creator, content.CreatedTimestamp);
                    }
                }
                else
                {
                    CreateInfoFile(infoFileName, _dataSource.GetType());
                }

                _dataSource.CreateBackup();
            }
        }

        private void MigrateSource(Type oldSourceType)
        {
            using(var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Migrating DatabaseTable({Identifier})<{typeof(T)}> from DataSource<{oldSourceType}> to DataSource<{_dataSource.GetType()}>", x => $"Migrated DatabaseTable({Identifier})<{typeof(T)}> from DataSource<{oldSourceType}> to DataSource<{_dataSource.GetType()}> in {x.TotalMilliseconds}ms"))
            {
                IDatabaseTableSource<T> oldSource = ResolveSource(oldSourceType);
                oldSource.Initialize(SourceDirectory);
                _dataSource.MigrateFromSource(oldSource, false);
            }        
        }

        private IDatabaseTableSource<T> ResolveSource(Type type)
        {
            _logger.LogMessage(LogLevel.Information, $"DatabaseTable({Identifier})<{typeof(T)}> trying to resolve DataSource<{type}>");

            if(type == typeof(JsonDatabaseTableSource<T>))
            {
                return DatabaseTableSourceFactory.CreateTableSource<T>(SerializationProvider.Json, _logger);
            }

            if (type == typeof(BsonDatabaseTableSource<T>))
            {
                return DatabaseTableSourceFactory.CreateTableSource<T>(SerializationProvider.Bson, _logger);
            }

            if (type == typeof(XmlDatabaseTableSource<T>))
            {
                return DatabaseTableSourceFactory.CreateTableSource<T>(SerializationProvider.Xml, _logger);
            }

            _typeActivator.ValidateVariable(nameof(_typeActivator));

            return _typeActivator.CreateInstanceFromType(type);
        }
        #endregion

        private void ValidateLock(Lock tableLock)
        {
            _logger.LogObject<JsonProvider>(LogLevel.Trace, "Validating", tableLock);

            lock (_threadLock)
            {
                if (tableLock == null || !tableLock.Equals(_lock) || !tableLock.IsValid)
                {
                    throw new InvalidLockException(Identifier);
                }
            }       
        }

        private void UpdateItem(T item)
        {
            item.ValidateVariable(nameof(item));
            _logger.LogObject<JsonProvider>(LogLevel.Trace, "Updating", item);

            if (CachedItems.Value.UpdateFirst(_comparator, _dataSource.Clone(item)))
            {
                _hasPendingChanges = true;
            }
            else
            {
                throw new TableObjectNotFoundException(item, Identifier);
            }
        }

        private void DeleteItem(T item)
        {
            item.ValidateVariable(nameof(item));
            _logger.LogObject<JsonProvider>(LogLevel.Trace, "Deleting", item);

            if (CachedItems.Value.DeleteFirst(_comparator, item))
            {
                _hasPendingChanges = true;
            }
            else
            {
                throw new TableObjectNotFoundException(item, Identifier);
            }
        }

        private void InsertItem(T item)
        {
            item.ValidateVariable(nameof(item));
            _logger.LogObject<JsonProvider>(LogLevel.Trace, "Inserting", item);

            CachedItems.Value.Add(_dataSource.Clone(item));
            _hasPendingChanges = true;
        }

    }
}
