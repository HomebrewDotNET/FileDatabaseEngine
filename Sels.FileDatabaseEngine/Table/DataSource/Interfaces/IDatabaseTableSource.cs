using Microsoft.Extensions.Logging;
using Sels.Core.Components.Backup;
using Sels.Core.Components.Locking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.FileDatabaseEngine.Table
{
    public interface IDatabaseTableSource<T> : IBackupable
    {
        void Initialize(DirectoryInfo source);

        IEnumerable<T> Load();

        IEnumerable<T> Clone(IEnumerable<T> values);
        T Clone(T value);

        void Persist(IEnumerable<T> values);

        bool IsFree();

        void MigrateFromSource(IDatabaseTableSource<T> oldSource, bool keepOldSource);

        void ClearSource();

    }
}
