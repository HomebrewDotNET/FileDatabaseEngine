using Sels.Core.Extensions;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.Exceptions.Table
{
    public class DatabaseTableSourceCouldNotRestoreBackupException : FileDatabaseException
    {
        private const string _messageFormat = "Database Table Source of Type {0} could not be restored using any of the {1} backups present";

        public DatabaseTableSourceCouldNotRestoreBackupException(Type sourceType, int backupCount) : base(_messageFormat.FormatString(sourceType, backupCount))
        {

        }
    }
}
