using Sels.Core.Extensions;
using Sels.Core.Extensions.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sels.FileDatabaseEngine.V2.Components.Cluster.Settings
{
    public class FileClusterSettings<TObject>
    {
        // Constants
        protected const string DefaultIdProperty = "Id";

        public FileClusterSettings()
        {
            // Defaults to 50Mb
            MaxFileSize = (long) 50 * 1024 * 1024 * 1024;

            // Default Comparator
            Comparator = DefaultComparator;
        }

        /// <summary>
        /// Delegate that deserializes objects
        /// </summary>
        public Func<string, IEnumerable<TObject>> Deserializer { get; set; }
        /// <summary>
        /// Delegate that serializes objects
        /// </summary>
        public Func<IEnumerable<TObject>, string> Serializer { get; set; }
        /// <summary>
        /// Delegate that dictates that 2 objects are equal
        /// </summary>
        public Func<TObject, TObject, bool> Comparator { get; set; }

        /// <summary>
        /// Max file size of a clustered file
        /// </summary>
        public long MaxFileSize { get; set; }

        public virtual void Validate()
        {
            Deserializer.ValidateVariable(nameof(Deserializer));
            Serializer.ValidateVariable(nameof(Serializer));
            Comparator.ValidateVariable(nameof(Comparator));

            MaxFileSize.ValidateVariable(x => x > 0, () => $"{nameof(MaxFileSize)} must be larger than 0. Was <{MaxFileSize}>");
        }

        private bool DefaultComparator(TObject sourceObject, TObject targetObject)
        {
            // Use default equals
            if (targetObject.Equals(sourceObject))
            {
                return true;
            };

            // Return false if any of the 2 objects are null
            if(!sourceObject.HasValue() && !targetObject.HasValue())
            {
                return false;
            }

            // Search for property Id and compare
            if (typeof(TObject).TryFindProperty(DefaultIdProperty, out var idProperty))
            {
                var sourceValue = idProperty.GetValue(sourceObject);
                var targetValue = idProperty.GetValue(sourceObject);

                if (sourceValue.HasValue() && targetValue.HasValue() && sourceValue.Equals(targetValue))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
