using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sels.Core;
using Sels.Core.Components.Caching;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Io;
using Sels.Core.Extensions.Linq;
using Sels.Core.Extensions.Logging;
using Sels.FileDatabaseEngine.Enums;
using Sels.FileDatabaseEngine.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Sels.FileDatabaseEngine
{
    public class DatabaseEngine
    {
        // Constants
        private const string LogCategory = "FileDatabaseEngine";

        // Fields
        private static object _threadLock = new object();
        private static DatabaseEngine _engine;

        private static ILoggerFactory _loggerFactory;

        private List<Database> _databases;
        private ValueCache<ILogger> _logger;

        // Properties
        private static DatabaseEngine Engine { 
            get {
                lock (_threadLock)
                {
                    if(_engine == null)
                    {
                        _engine = new DatabaseEngine();
                    }
                }

                return _engine;
            }        
        }

        private ILogger Logger => _logger.Value;

        internal DatabaseEngine()
        {
            _databases = new List<Database>();
            _loggerFactory = new NullLoggerFactory();
            _logger = new ValueCache<ILogger>(() => _loggerFactory.CreateLogger(LogCategory));

            Helper.App.RegisterApplicationClosingAction(ForceShutdownAll);
        }

        private void AddLogging(ILoggerFactory loggerFactory)
        {
            loggerFactory.ValidateVariable(nameof(loggerFactory));
            _loggerFactory = loggerFactory;
            _logger.ResetCache();
        }

        public static void AddLoggingProvider(ILoggerFactory loggerFactory)
        {
            Engine.AddLogging(loggerFactory);
        }

        public static Database CreateDatabase(string identifier, string databaseSourcePath)
        {
            return CreateDatabase(identifier, new DirectoryInfo(databaseSourcePath));
        }

        public static Database CreateDatabase(string identifier, DirectoryInfo databaseSourceDirectory)
        {
            identifier.ValidateVariable(nameof(identifier));
            databaseSourceDirectory.CreateIfNotExistAndValidate(nameof(databaseSourceDirectory));

            Engine.Logger.LogMessage(LogLevel.Information, $"FileDatabaseEngine creating Database({identifier}) in {databaseSourceDirectory}");

            lock (_threadLock)
            {
                var registeredDatabases = Engine._databases;

                if(registeredDatabases.Where(x => x.Identifier.Equals(identifier)).FirstOrDefault() == null)
                {
                    var database = new Database(identifier, databaseSourceDirectory, _loggerFactory.CreateLogger(LogCategory));

                    registeredDatabases.Add(database);

                    Engine.Logger.LogMessage(LogLevel.Information, $"FileDatabaseEngine created Database({identifier}) in {databaseSourceDirectory}");
                    return database;
                }
                else
                {
                    Engine.Logger.LogMessage(LogLevel.Information, $"FileDatabaseEngine already contains a Database with identifier {identifier}");
                    throw new DatabaseIdentifierAlreadyUsed(identifier);
                }
            }
        }

        public static Database GetDatabase(string identifier)
        {
            Engine.Logger.LogMessage(LogLevel.Information, $"FileDatabaseEngine getting Database({identifier})");
            lock (_threadLock)
            {
                var registeredDatabases = Engine._databases;

                var database = registeredDatabases.Where(x => x.Identifier.Equals(identifier)).FirstOrDefault();

                return database ?? throw new DatabaseNotFoundException(identifier);
            }
        }

        public static T GetPageData<T>(string databaseIdentifier, string pageIdentifier) where T : class
        {
            Engine.Logger.LogMessage(LogLevel.Debug, $"FileDatabaseEngine getting DataPage({pageIdentifier})<{typeof(T)}> from Database({databaseIdentifier})");
            return GetDatabase(databaseIdentifier).GetDataFromPage<T>(pageIdentifier);
        }

        public static void StorePageData<T>(string databaseIdentifier, string pageIdentifier, T pageData) where T : class
        {
            Engine.Logger.LogMessage(LogLevel.Debug, $"FileDatabaseEngine string data in DataPage({pageIdentifier})<{typeof(T)}> from Database({databaseIdentifier})");
            GetDatabase(databaseIdentifier).StoreDataInPage<T>(pageIdentifier, pageData);
        }

        public static void ForceStartAll()
        {
            Engine.Logger.LogMessage(LogLevel.Information, () => $"FileDatabaseEngine attempting to start {Engine._databases.Count()} Databases");
            if (Engine._databases.HasValue())
            {
                Engine._databases.ForceExecute(x => x.Startup(), (database, ex) => Engine.Logger.LogException(LogLevel.Warning, () => $"Error occured while starting Database({database.Identifier})", ex));
            }
        }

        public static void ForceShutdownAll()
        {
            Engine.Logger.LogMessage(LogLevel.Information, () => $"FileDatabaseEngine attempting to shutdown {Engine._databases.Count()} Databases");
            if (Engine._databases.HasValue())
            {
                Engine._databases.ForceExecute(x => x.Shutdown(), (database, ex) => Engine.Logger.LogException(LogLevel.Warning, () => $"Error occured while starting Database({database.Identifier})", ex));
            }
        }

        public static void StartAll()
        {
            Engine.Logger.LogMessage(LogLevel.Information, () => $"FileDatabaseEngine attempting to start {Engine._databases.Count()} Databases");
            if (Engine._databases.HasValue())
            {
                Engine._databases.Execute(x => x.Startup(), (database, ex) => Engine.Logger.LogException(LogLevel.Warning, () => $"Error occured while starting Database({database.Identifier})", ex));
            }
        }

        public static void ShutdownAll()
        {
            Engine.Logger.LogMessage(LogLevel.Information, () => $"FileDatabaseEngine attempting to shutdown {Engine._databases.Count()} Databases");
            if (Engine._databases.HasValue())
            {
                Engine._databases.Execute(x => x.Shutdown(), (database, ex) => Engine.Logger.LogException(LogLevel.Warning, () => $"Error occured while starting Database({database.Identifier})", ex));
            }
        }
    }
}
