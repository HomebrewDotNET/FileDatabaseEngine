using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sels.FileDatabaseEngine.Connection
{
    public class AsyncDatabaseConnection : DatabaseConnection, IAsyncDisposable
    {
        public AsyncDatabaseConnection(string databaseIdentifier) : base(databaseIdentifier)
        {

        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }
    }
}
