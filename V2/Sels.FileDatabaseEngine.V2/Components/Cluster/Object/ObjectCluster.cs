using Microsoft.Extensions.Logging;
using Sels.Core.Extensions.General.Validation;
using Sels.Core.Extensions.Io.FileSystem;
using Sels.Core.Extensions.Logging;
using Sels.Core.Extensions.Object.Time;
using Sels.FileDatabaseEngine.V2.Components.Cluster.File;
using Sels.FileDatabaseEngine.V2.Components.Cluster.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Sels.FileDatabaseEngine.V2.Components.Cluster.Object
{
    public class ObjectCluster<TObject>
    {
        // Constants
        private const string ClusterFileNameFormat = "{0}.{1}.data";

        // Fields
        private readonly IEnumerable<ILogger> _loggers;
        private readonly List<FileCluster<TObject>> _clusters = new List<FileCluster<TObject>>();

        // Properties
        public string PartitionKey { get; }
        public DirectoryInfo Directory { get; }
        public ObjectClusterSettings<TObject> Settings { get; }
        public ReadOnlyCollection<FileCluster<TObject>> Clusters => new ReadOnlyCollection<FileCluster<TObject>>(_clusters);

        public ObjectCluster(string partitionKey, DirectoryInfo directory, IEnumerable<ILogger> loggers, ObjectClusterSettings<TObject> settings)
        {
            partitionKey.ValidateVariable(nameof(partitionKey));
            settings.ValidateVariable(nameof(settings));
            settings.Validate();

            if (directory.CreateIfNotExistAndValidate(nameof(directory)))
            {
                loggers.LogMessage(LogLevel.Information, () => $"Created Clustered Directory <{directory.FullName}> for Object <{typeof(TObject)}>");
            }
            else
            {
                DiscoverClusters(directory, loggers, settings);
            }

            PartitionKey = partitionKey;
            Directory = directory;
            Settings = settings;
            _loggers = loggers;
        }




        private void DiscoverClusters(DirectoryInfo directory, IEnumerable<ILogger> loggers, FileClusterSettings<TObject> settings)
        {
            using (var timedLogger = loggers.CreateTimedLogger(LogLevel.Information, () => $"Discovering Clusters in <{directory?.FullName}>", x => $"Finished discovering Clusters in <{directory?.FullName}> in {x.PrintTotalMs()}"))
            {
                directory.ValidateVariable(nameof(directory));

                var files = directory.GetFiles();

                foreach (var file in files)
                {
                    if (file.Name.StartsWith(PartitionKey))
                    {
                        timedLogger.Log((x,y) => y.LogMessage(LogLevel.Trace, () => $"Found Cluster {file.FullName} for PartitionKey {PartitionKey} ({x.PrintTotalMs()})"));

                        _clusters.Add(new FileCluster<TObject>(file, loggers, settings));
                    }
                    else
                    {
                        timedLogger.Log((x,y) => y.LogMessage(LogLevel.Debug, () => $"Found {file.FullName} while searching for Clusters for PartitionKey {PartitionKey} but file name did not match ({x.PrintTotalMs()})"));
                    }
                }
            }     
        }
    }
}
