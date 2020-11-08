using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Connection
{
    public interface IDatabaseConnection : IDisposable
    {
        IEnumerable<T> GetAll<T>(string tableIdentifier);

        IEnumerable<T> Query<T>(string tableIdentifier, Predicate<T> query);

        T Get<T>(string tableIdentifier, Predicate<T> query);

        void Insert<T>(string tableIdentifier, T item);

        void Insert<T>(string tableIdentifier, IEnumerable<T> items);

        void Update<T>(string tableIdentifier, T item);

        void Update<T>(string tableIdentifier, IEnumerable<T> items);

        void Update<T>(string tableIdentifier, Predicate<T> query, Action<T> action);

        void Delete<T>(string tableIdentifier, T item);

        void Delete<T>(string tableIdentifier, IEnumerable<T> items);

        void Delete<T>(string tableIdentifier, Predicate<T> query);

        void Persist();

        void Abort();
    }
}
