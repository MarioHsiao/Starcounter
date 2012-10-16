
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Starcounter.VisualStudio.Projects {

    /// <summary>
    /// Represents metadata about a project property whose value is
    /// possible to get/set from <see cref="StarcounterProjectConfiguration"/>
    /// implementations.
    /// </summary>
    internal sealed class ProjectPropertySettings {
        /// <summary>
        /// Gets or sets the project property storage type.
        /// </summary>
        internal readonly _PersistStorageType StorageType;

        /// <summary>
        /// Gets or sets a value indicating of the property is configuration
        /// dependent.
        /// </summary>
        internal readonly bool IsConfigurationDependent;
        
        /// <summary>
        /// Gets or sets the default value of the project property.
        /// </summary>
        internal string DefaultValue;

        /// <summary>
        /// Initializes a <see cref="ProjectPropertySettings"/>.
        /// </summary>
        /// <param name="storageType"></param>
        /// <param name="configurationDependent"></param>
        /// <param name="defaultValue"></param>
        internal ProjectPropertySettings(_PersistStorageType storageType, bool configurationDependent) : 
            this(storageType, configurationDependent, string.Empty) {
        }

        /// <summary>
        /// Initializes a <see cref="ProjectPropertySettings"/>.
        /// </summary>
        /// <param name="storageType"></param>
        /// <param name="configurationDependent"></param>
        /// <param name="defaultValue"></param>
        internal ProjectPropertySettings(_PersistStorageType storageType, bool configurationDependent, string defaultValue) {
            this.StorageType = storageType;
            this.IsConfigurationDependent = configurationDependent;
            this.DefaultValue = defaultValue;
        }
    }
}