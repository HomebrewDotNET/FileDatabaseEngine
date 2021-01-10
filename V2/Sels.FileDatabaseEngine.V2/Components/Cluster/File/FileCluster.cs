using Microsoft.Extensions.Logging;
using Sels.Core.Extensions.General.Validation;
using Sels.Core.Extensions.Io.FileSystem;
using Sels.Core.Extensions.Logging;
using Sels.FileDatabaseEngine.V2.Components.Cluster.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sels.FileDatabaseEngine.V2.Components.Cluster.File
{
    public sealed class FileCluster<TObject>
    {
        // Fields
        private readonly IEnumerable<ILogger> _loggers;

        // Properties
        public FileInfo File { get; }
        public FileClusterSettings<TObject> Settings { get; }

        public FileCluster(FileInfo file, IEnumerable<ILogger> loggers, FileClusterSettings<TObject> settings)
        {
            file.ValidateVariable(nameof(file));
            settings.ValidateVariable(nameof(settings));
            settings.Validate();

            if (file.CreateIfNotExistAndValidate(nameof(file))) {
                loggers.LogMessage(LogLevel.Information, () => $"Created Clustered File <{file.FullName}> for Object <{typeof(TObject)}>");
            }

            File = file;
            Settings = settings;
            _loggers = loggers;
        }
    }
}
