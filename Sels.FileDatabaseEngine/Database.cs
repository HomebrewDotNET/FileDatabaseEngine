using Microsoft.Extensions.Logging;
using Sels.Core.Components.Serialization;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Execution;
using Sels.Core.Extensions.Execution.Linq;
using Sels.Core.Extensions.General.Generic;
using Sels.Core.Extensions.General.Validation;
using Sels.Core.Extensions.Io.FileSystem;
using Sels.Core.Extensions.Logging;
using Sels.Core.Extensions.Object.String;
using Sels.Core.Extensions.Reflection.Types;
using Sels.FileDatabaseEngine.Connection;
using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.Exceptions;
using Sels.FileDatabaseEngine.Extensions;
using Sels.FileDatabaseEngine.Interfaces;
using Sels.FileDatabaseEngine.Page;
using Sels.FileDatabaseEngine.Table;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Sels.FileDatabaseEngine
{
    public class Database
    {
        // Constants
        private const string DatabaseFileName = "Database.info";
        private const string DefaultIdProperty = "Id";

        private const string DatabaseTablePath = "Tables";
        private const string DatabasePagePath = "Pages";

        private const string LogCategory = "FileDatabaseEngine";

        // Fields
        private readonly List<BaseDatabaseTable> _dataTables;
        private readonly List<BaseDataPage> _dataPages;

        private readonly ILogger _logger;

        private object _threadLock = new object();
        private static readonly object _globalThreadLock = new object();

        // Properties
        public string Identifier { get; }
        public DirectoryInfo Source { get; private set; }
        public int DefaultTimeout { get; set; } = 5000;

        public ReadOnlyCollection<BaseDatabaseTable> DataTables { get {
                return new ReadOnlyCollection<BaseDatabaseTable>(_dataTables);
            } 
        }
        public ReadOnlyCollection<BaseDataPage> DataPages { 
            get {
                return new ReadOnlyCollection<BaseDataPage>(_dataPages);
            } 
        }

        // State
        private RunningState _state = RunningState.Shutdown;

        internal Database(string identifier, string filePath, ILogger logger) : this(identifier, new DirectoryInfo(filePath), logger)
        {

        }

        internal Database(string identifier, DirectoryInfo source, ILogger logger)
        {
            identifier.ValidateVariable(nameof(identifier));
            source.ValidateVariable(nameof(source));
            logger.ValidateVariable(nameof(logger));

            Identifier = identifier;
            Source = source;
            _logger = logger;
            _dataTables = new List<BaseDatabaseTable>();
            _dataPages = new List<BaseDataPage>();
        }

        internal void CheckValidForConnection()
        {
            _logger.LogMessage(LogLevel.Information, $"Checking if Database({Identifier}) is valid for connection");

            lock (_threadLock)
            {
                _state.ValidateState(Identifier, RunningState.Running);
            }        
        }

        internal void CheckValidForSetup()
        {
            _logger.LogMessage(LogLevel.Information, $"Checking if Database({Identifier}) is valid for setup");

            lock (_threadLock)
            {
                _state.ValidateState(Identifier, RunningState.Shutdown);
            }
        }

        internal ILogger GetLogger()
        {
            return _logger;
        }

        #region Actions
        public void Shutdown()
        {
            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Shutting down Database({Identifier})", x => $"Shut down Database({Identifier}) in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    _state.ValidateState(Identifier, RunningState.Running);
                    _state = RunningState.ShuttingDown;
                }

                DataTables.Execute(x => x.Shutdown());
                DataPages.Execute(x => x.Shutdown());

                lock (_threadLock)
                {
                    _state = RunningState.Shutdown;
                }
            }        
        }

        public void Startup()
        {
            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, $"Starting up Database({Identifier})", x => $"Started up Database({Identifier}) in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    _state.ValidateState(Identifier, RunningState.Shutdown);
                    _state = RunningState.StartingUp;
                }

                Validate();

                DataTables.Execute(x => x.StartUp());
                DataPages.Execute(x => x.StartUp());

                lock (_threadLock)
                {
                    _state = RunningState.Running;
                }
            }         
        }

        public void MigrateTo(DirectoryInfo newSource, bool overwrite = false, bool keepOldSource = false)
        {
            newSource.EnsureExistsAndValidate(nameof(newSource));

            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, () => $"Migrating Database({Identifier}) to {newSource.FullName}", x => $"Migrated Database({Identifier}) to {newSource.FullName}"))
            {
                lock (_threadLock)
                {
                    _state.ValidateState(Identifier, RunningState.Shutdown);

                    logger.Log(x => $"Copying files to {newSource.FullName} ({x.TotalMilliseconds}ms)");
                    // Copy source files to new source directory
                    Source.CopyContentTo(newSource, overwrite);

                    logger.Log(x => $"Migrating Data Tables ({x.TotalMilliseconds}ms)");
                    // Migrate Tables
                    _dataTables.Execute(x => x.IsInState(RunningState.Shutdown)).Execute(x => x.SourceDirectory = GetTablePath(newSource, x.Identifier));

                    logger.Log(x => $"Migrating Data Pages ({x.TotalMilliseconds}ms)");
                    // Migrate Pages
                    _dataPages.Execute(x => x.IsInState(RunningState.Shutdown)).Execute(x => x.SourceDirectory = GetPagePath(newSource, x.Identifier));

                    if (!keepOldSource)
                    {
                        logger.Log(x => $"Database({Identifier}) deleting old source");
                        Source.Delete(true);
                    }

                    Source = newSource;
                }
            }            
        }

        #endregion

        #region Connection
        public DatabaseConnection OpenConnection()
        {
            CheckValidForConnection();
            _logger.LogMessage(LogLevel.Information, () => $"Opening connection from Database({Identifier})");

            return new DatabaseConnection(this);
        }
        #endregion

        #region Setup Table
        public void RegisterTable<T>() where T : class, new()
        {
            RegisterTable<T>(typeof(T).ToString(), SerializationProvider.Json);
        }

        public void RegisterTable<T>(string tableIdentifier) where T : class, new()
        {
            RegisterTable<T>(tableIdentifier, SerializationProvider.Json);
        }

        public void RegisterTable<T>(string tableIdentifier, SerializationProvider provider) where T : class, new()
        {
            RegisterTable<T>(tableIdentifier, provider, null);
        }

        public void RegisterTable<T>(string tableIdentifier, SerializationProvider provider, ITypeActivator<IDatabaseTableSource<T>> typeActivator) where T : class, new()
        {
            RegisterTable<T>(tableIdentifier, provider, typeActivator, DefaultTableComparator<T>);
        }

        public void RegisterTable<T>(string tableIdentifier, SerializationProvider provider, ITypeActivator<IDatabaseTableSource<T>> typeActivator, Func<T, T, bool> comparator) where T : class, new()
        {
            RegisterTable<T>(tableIdentifier, provider, typeActivator, comparator, DefaultTimeout);
        }

        public void RegisterTable<T>(string tableIdentifier, SerializationProvider provider, ITypeActivator<IDatabaseTableSource<T>> typeActivator, Func<T, T, bool> comparator, int? defaultTimeout) where T : class, new()
        {
            CheckValidForSetup();

            using(var logger = _logger.CreateTimedLogger(LogLevel.Information, () => $"Trying to register DatabaseTable({tableIdentifier})<{typeof(T)}> with Provider<{provider.GetType()}> on Database({Identifier})", x => $"Registered DatabaseTable({tableIdentifier})<{typeof(T)}> with Provider<{provider.GetType()}> on Database({Identifier}) in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    ValidateTableType(typeof(T));
                    tableIdentifier.ValidateVariable(nameof(tableIdentifier));

                    if (_dataTables.Where(x => x.Identifier.Equals(tableIdentifier)).FirstOrDefault() != null)
                    {
                        _logger.LogMessage(LogLevel.Debug, () => $"DatabaseTable({tableIdentifier})<{typeof(T)}> already exists on Database({Identifier})");
                        throw new DatabaseTableIdentifierAlreadyExistsException(tableIdentifier, Identifier);
                    }

                    var tablePath = GetTablePath(Source, tableIdentifier);
                    var tableComparator = comparator ?? DefaultTableComparator<T>;
                    var tableTimeout = defaultTimeout ?? DefaultTimeout;
                    var sourceProvider = DatabaseTableSourceFactory.CreateTableSource<T>(provider, GetLogger());

                    var databaseTable = new DatabaseTable<T>(tableIdentifier, sourceProvider, typeActivator, tablePath, tableTimeout, tableComparator, GetLogger());

                    _dataTables.Add(databaseTable);
                }
            }

            

        }

        private DirectoryInfo GetTablePath(DirectoryInfo source, string tableIdentifier)
        {
            _logger.LogMessage(LogLevel.Debug, () => $"Generating table path for DatabaseTable{tableIdentifier} in {source.FullName}");
            return new DirectoryInfo(Path.Combine(source.FullName, DatabaseTablePath, tableIdentifier).ToValidPath());
        }
        #endregion

        #region Setup Pages
        public void RegisterPage<T>() where T : class, new()
        {
            RegisterPage<T>(typeof(T).ToString());
        }

        public void RegisterPage<T>(string identifier) where T : class, new()
        {
            RegisterPage<T>(identifier, SerializationProvider.Json);
        }

        public void RegisterPage<T>(string identifier, SerializationProvider provider) where T : class, new()
        {
            RegisterPage<T>(identifier, provider, DefaultPageContructor<T>);
        }

        public void RegisterPage<T>(string identifier, SerializationProvider provider, Func<T> pageContructor) where T : class
        {
            RegisterPage<T>(identifier, DataPageSourceFactory.CreatePageSource<T>(provider, GetLogger()), pageContructor);
        }

        public void RegisterPage<T>(string identifier, IDataPageSourceProvider<T> sourceProvider, Func<T> pageContructor) where T : class
        {
            CheckValidForSetup();
            using (var logger = _logger.CreateTimedLogger(LogLevel.Information, () => $"Trying to register DatabaseDataPage({identifier})<{typeof(T)}> with Provider<{sourceProvider.GetType()}> on Database({Identifier})", x => $"Registered DatabaseDataPage({identifier})<{typeof(T)}> with Provider<{sourceProvider.GetType()}> on Database({Identifier}) in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    ValidateTableType(typeof(T));
                    identifier.ValidateVariable(nameof(identifier));
                    sourceProvider.ValidateVariable(nameof(sourceProvider));

                    if (_dataPages.Where(x => x.Identifier.Equals(identifier)).FirstOrDefault() != null)
                    {
                        _logger.LogMessage(LogLevel.Debug, () => $"DatabaseDataPage({identifier})<{typeof(T)}> already exists on Database({Identifier})");
                        throw new DatabaseDataPageIdentifierAlreadyExistsException(identifier, Identifier);
                    }

                    var pagePath = GetPagePath(Source, identifier);

                    _dataPages.Add(new DataPage<T>(identifier, pagePath, sourceProvider, pageContructor, GetLogger()));
                }
            }          
        }

        private DirectoryInfo GetPagePath(DirectoryInfo source, string identifier)
        {
            _logger.LogMessage(LogLevel.Debug, () => $"Generating data page path for DatabaseDataPage{identifier} in {source.FullName}");
            return new DirectoryInfo(Path.Combine(source.FullName, DatabasePagePath, identifier).ToValidPath());
        }
        #endregion

        #region Initialization
        private void Validate()
        {
            using(var logger = _logger.CreateTimedLogger(LogLevel.Information, () => $"Validating Database({Identifier})", x => $"Validated Database({Identifier}) in {x.TotalMilliseconds}ms"))
            {
                lock (_globalThreadLock)
                {
                    Source.EnsureExistsAndValidate(nameof(Source));

                    var infoFileName = Path.Combine(Source.FullName, DatabaseFileName);

                    if (File.Exists(infoFileName))
                    {
                        var content = ReadInfoFile(infoFileName);

                        if (!Identifier.Equals(content.Identifier))
                        {
                            throw new DatabaseDirectoryAlreadyUsedException(Identifier, Source.FullName, content.Identifier);
                        }
                    }
                    else
                    {
                        CreateInfoFile(infoFileName);
                    }
                }
            }       
        }

        private void CreateInfoFile(string fileName)
        {
            _logger.LogMessage(LogLevel.Debug, $"Creating info file {fileName} for Database({Identifier})");
            File.WriteAllText(fileName, $"{Identifier}|{Environment.MachineName}|{DateTime.Now}");
        }

        private (string Identifier, string Creator, DateTime CreatedTimestamp) ReadInfoFile(string fileName)
        {
            _logger.LogMessage(LogLevel.Debug, $"Reading info file {fileName} for Database({Identifier})");
            var content = File.ReadAllText(fileName).Split('|');

            return (content[0], content[1], DateTime.Parse(content[2]));
        }

        #endregion

        #region Pages
        public DataPage<T> GetDataPage<T>(string pageIdentifier) where T : class
        {
            _logger.LogMessage(LogLevel.Information, $"Getting DataPage({pageIdentifier}) from Database({Identifier})");
            Type requestType = typeof(T);

            var dataPage = DataPages.Where(x => x.Identifier.Equals(pageIdentifier) && requestType == x.SourceType).FirstOrDefault();

            return (DataPage<T>)dataPage ?? throw new DatabaseDataPageNotFoundException(pageIdentifier, Identifier);

        }

        public T GetDataFromPage<T>(string pageIdentifier) where T : class
        {
            _logger.LogMessage(LogLevel.Information, $"Getting DataPage({pageIdentifier}) data from Database({Identifier})");
            CheckValidForConnection();
            return GetDataPage<T>(pageIdentifier).DataObject;
        }

        public void StoreDataInPage<T>(string pageIdentifier, T dataObject) where T : class
        {
            _logger.LogMessage(LogLevel.Information, $"Storing data in DataPage({pageIdentifier}) from Database({Identifier})");
            CheckValidForConnection();
            GetDataPage<T>(pageIdentifier).DataObject = dataObject;

            _logger.LogObject<JsonProvider>(LogLevel.Trace, "Stored", dataObject);
        }

        #endregion

        private void ValidateTableType(Type type)
        {
            _logger.LogMessage(LogLevel.Debug, $"Validating if Type {type} is valid for a DatabaseTable");
            if (type.IsPrimitive)
            {
                throw new TypeNotSupportedException(type, "Type cannot be primitive");
            }

            if (type.IsInterface)
            {
                throw new TypeNotSupportedException(type, "Type cannot be an Interface");
            }
        }

        private bool DefaultTableComparator<T>(T sourceObject, T targetObject)
        {
            using(var logger = _logger.CreateTimedLogger(LogLevel.Debug, () => $"Comparing 2 objects of Type {typeof(T)} using default DatabaseTableComparator", x => $"Compared 2 objects of Type {typeof(T)} using default DatabaseTableComparator in {x.TotalMilliseconds}ms"))
            {
                // Use default equals
                if (targetObject.Equals(sourceObject))
                {
                    _logger.LogObject<JsonProvider>(LogLevel.Debug, () => "Objects are equal (Equals Method)", sourceObject, targetObject);
                    return true;
                };

                // Search for property Id and compare
                if (typeof(T).TryFindProperty(DefaultIdProperty, out var idProperty))
                {
                    var sourceValue = idProperty.GetValue(sourceObject);
                    var targetValue = idProperty.GetValue(sourceObject);

                    if (sourceValue.HasValue() && targetValue.HasValue() && sourceValue.Equals(targetValue))
                    {
                        _logger.LogObject<JsonProvider>(LogLevel.Debug, () => "Objects are equal (Id property)", sourceObject, targetObject);
                        return true;
                    }
                }
                else
                {
                    _logger.LogMessage(LogLevel.Debug, () => $"No property Id found on object {typeof(T)}");
                }

                _logger.LogObject<JsonProvider>(LogLevel.Debug, () => "Objects are not equal", sourceObject, targetObject);
                return false;
            }
            
        }

        private T DefaultPageContructor<T>() where T : class, new()
        {
            _logger.LogMessage(LogLevel.Debug, () => $"Creating DataPage<{typeof(T)}> using default page contructor");
            return new T();
        }
    }
}
