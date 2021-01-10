using Sels.Core.Extensions.General.Validation;
using Sels.Core.Extensions.Reflection.Types;
using Sels.FileDatabaseEngine.V2.Components.Cluster.Settings.Options;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sels.FileDatabaseEngine.V2.Components.Cluster.Settings
{
    public class ObjectClusterSettings<TObject> : FileClusterSettings<TObject>
    {
        public ObjectClusterSettings()
        {
            DefaultClusterSize = 1;
            DistributionMethod = ClusterDistributionMethod.Smallest;
        }

        /// <summary>
        /// How mant clusters are created initially. If all initial clusters are full, additional will be made
        /// </summary>
        public int DefaultClusterSize { get; set; }
        /// <summary>
        /// How to distribute objects among clusters. Smallest will choose the smallest cluster to persist objects while Largest will select the largest cluster
        /// </summary>
        public ClusterDistributionMethod DistributionMethod { get; set; }

        /// <summary>
        /// Delegate responsible for generating a partition key for TObject
        /// </summary>
        public Func<TObject, string> PartitionKeyGenerator { get; set; }

        public override void Validate()
        {
            base.Validate();

            DefaultClusterSize.ValidateVariable(x => x > 0, () => $"{nameof(DefaultClusterSize)} must be larger than 0. Was <{DefaultClusterSize}>");
        }
        
        private string DefaultPartitionKeyGenerator(TObject sourceObject)
        {
            sourceObject.ValidateVariable(nameof(sourceObject));

            var sourceValue = sourceObject.GetHashCode().ToString();

            // Use Id property if present
            if (typeof(TObject).TryFindProperty(DefaultIdProperty, out var idProperty))
            {
                var idValue = idProperty.GetValue(sourceObject);
                
                if(idValue != null)
                {
                    sourceValue = idValue.ToString();
                }
            }

            return string.Empty;
        }
    }
}
