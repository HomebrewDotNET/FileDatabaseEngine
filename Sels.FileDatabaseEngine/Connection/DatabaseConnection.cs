using Microsoft.Extensions.Logging;
using Sels.Core.Components.Locking;
using Sels.Core.Components.Serialization;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Linq;
using Sels.Core.Extensions.Logging;
using Sels.FileDatabaseEngine.Connection;
using Sels.FileDatabaseEngine.Exceptions;
using Sels.FileDatabaseEngine.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Schema;

namespace Sels.FileDatabaseEngine.Connection
{
    public class DatabaseConnection : IDatabaseConnection
    {
        // Fields
        private object _threadLock = new object();

        private readonly Database _database;

        private readonly ILogger _logger;

        // State
        private bool _isDisposed;
        private bool _isCommitted;

        private Dictionary<BaseDatabaseTable, Lock> _tableConnections;

        internal DatabaseConnection(Database database)
        {
            _database = database;
            _database.CheckValidForConnection();
            _isDisposed = false;
            _tableConnections = new Dictionary<BaseDatabaseTable, Lock>();
            _logger = _database.GetLogger();

            _logger.LogMessage(LogLevel.Information, $"Opening connection for Database {_database.Identifier}");
        }

        public DatabaseConnection(string databaseIdentifier) : this(DatabaseEngine.GetDatabase(databaseIdentifier))
        {

        }

        #region Queries
        public IEnumerable<T> GetAll<T>(string tableIdentifier)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using(var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Querying Table {tableIdentifier} (GetAll)", x => $"Finished querying Table {tableIdentifier} (GetAll) in {x.TotalMilliseconds}ms"))
            {
                var result = connection.QueryableTable.GetAll(connection.TableLock);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, $"GetAll on Table {tableIdentifier} returned", result);

                return result;
            }        
        }

        public IEnumerable<T> Query<T>(string tableIdentifier, Predicate<T> query)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Querying Table {tableIdentifier} (Query)", x => $"Finished querying Table {tableIdentifier} (Query) in {x.TotalMilliseconds}ms"))
            {
                var result = connection.QueryableTable.Query(connection.TableLock, query);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, $"Query on Table {tableIdentifier} returned", result);

                return result;
            }
        }

        public T Get<T>(string tableIdentifier, Predicate<T> query)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Querying Table {tableIdentifier} (Get)", x => $"Finished querying Table {tableIdentifier} (Get) in {x.TotalMilliseconds}ms"))
            {
                var result = connection.QueryableTable.Get(connection.TableLock, query); ;

                _logger.LogObject<JsonProvider>(LogLevel.Debug, $"Get on Table {tableIdentifier} returned", result);

                return result;
            }
        }

        public void Insert<T>(string tableIdentifier, T item)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Inserting into Table {tableIdentifier}", x => $"Inserted into Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Insert(connection.TableLock, item);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, "Inserted", item);
            }            
        }

        public void Insert<T>(string tableIdentifier, IEnumerable<T> items)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Inserting multiple into Table {tableIdentifier}", x => $"Inserted multiple into Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Insert(connection.TableLock, items);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, "Inserted", items);
            }            
        }

        public void Update<T>(string tableIdentifier, T item)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Updating item in Table {tableIdentifier}", x => $"Updated item in Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Update(connection.TableLock, item);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, "Updated", item);
            }        
        }

        public void Update<T>(string tableIdentifier, IEnumerable<T> items)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Updating multiple items in Table {tableIdentifier}", x => $"Updated multiple items in Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Update(connection.TableLock, items);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, "Updated", items);
            }
        }

        public void Update<T>(string tableIdentifier, Predicate<T> query, Action<T> action)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Updating multiple items using query in Table {tableIdentifier}", x => $"Updated multiple items using query in Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Update(connection.TableLock, query, action);
            }         
        }

        public void Delete<T>(string tableIdentifier, T item)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Deleting item in Table {tableIdentifier}", x => $"Deleted item in Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Delete(connection.TableLock, item);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, "Deleted", item);
            }            
        }

        public void Delete<T>(string tableIdentifier, IEnumerable<T> items)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Deleting multiple items in Table {tableIdentifier}", x => $"Deleted multiple items in Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Delete(connection.TableLock, items);

                _logger.LogObject<JsonProvider>(LogLevel.Debug, "Deleted", items);
            }         
        }

        public void Delete<T>(string tableIdentifier, Predicate<T> query)
        {
            var connection = TryOpenConnection<T>(tableIdentifier);

            using (var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Deleting multiple items using query in Table {tableIdentifier}", x => $"Deleted multiple items using query in Table {tableIdentifier} in {x.TotalMilliseconds}ms"))
            {
                connection.QueryableTable.Delete(connection.TableLock, query);
            }         
        }
        #endregion

        public void Persist()
        {
            _logger.LogMessage(LogLevel.Information, $"Persisting connection for Database {_database.Identifier}");
            lock (_threadLock)
            {
                if (!_isCommitted && _tableConnections.HasValue())
                {
                    _tableConnections.Execute(x => x.Key.Persist(x.Value));
                    _isCommitted = true;
                }
            }
        }

        public void Abort()
        {
            _logger.LogMessage(LogLevel.Information, $"Aborting connection for Database {_database.Identifier}");
            lock (_threadLock)
            {
                if (!_isCommitted && _tableConnections.HasValue())
                {
                    foreach( var connection in _tableConnections)
                    {
                        try
                        {
                            connection.Key.Abort(connection.Value);
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            using(var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Disposing connection for Database {_database.Identifier}", x => $"Disposing connection for Database {_database.Identifier} finished in {x.TotalMilliseconds}ms"))
            {
                lock (_threadLock)
                {
                    if (!_isDisposed)
                    {
                        Abort();

                        if (_tableConnections.HasValue())
                        {
                            _tableConnections.ForceExecute(x => x.Value.Dispose());
                        }

                        _isDisposed = true;
                    }
                }
            }           
        }

        private (DatabaseTable<T> QueryableTable, Lock TableLock) TryOpenConnection<T>(string tableIdentifier)
        {
            using(var logger = _logger.CreateTimedLogger(LogLevel.Debug, $"Trying to open connection for Table {tableIdentifier} in Database {_database.Identifier}", x => $"Finished opening connection for Table {tableIdentifier} in Database {_database.Identifier} in {x.TotalMilliseconds}ms"))
            {
                tableIdentifier.ValidateVariable(nameof(tableIdentifier));

                var tableConnection = _tableConnections.Where(x => x.Key.Identifier.Equals(tableIdentifier)).FirstOrDefault();
                BaseDatabaseTable table = null;
                Lock tableLock = null;

                if (tableConnection.HasValue())
                {
                    table = tableConnection.Key;
                    tableLock = tableConnection.Value;
                    ValidateConnection<T>(table);
                }
                else
                {
                    table = FindTable<T>(tableIdentifier);
                    ValidateConnection<T>(table);
                    tableLock = table.TryGetLock();
                    _tableConnections.Add(table, tableLock);
                }

                return ((DatabaseTable<T>)table, tableLock);
            }       
        }

        private DatabaseTable<T> FindTable<T>(string tableIdentifier)
        {
            _logger.LogMessage(LogLevel.Debug, $"Trying to find Table {tableIdentifier} in Database {_database.Identifier}");
            var table = _database.DataTables.Where(x => x.Identifier.Equals(tableIdentifier)).FirstOrDefault();

            return (DatabaseTable<T>)table ?? throw new DatabaseTableNotFoundException(tableIdentifier, _database.Identifier);
        }

        private void ValidateConnection<T>(BaseDatabaseTable table)
        {
            _logger.LogMessage(LogLevel.Debug, $"Validating {table.Identifier} for Type {typeof(T)}");
            if (table.SourceType != typeof(T))
            {
                throw new DatabaseTableInvalidTypeException(table.Identifier, typeof(T));
            }
        }

    }
}
