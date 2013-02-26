// ***********************************************************************
// <copyright file="ValueMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class ValueMetadata : ObjMetadataBase {

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public ValueMetadata(Obj app, Template template ) : base( app, template ) {
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValueMetadata" /> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled {
            set;
            get;
        }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValueMetadata" /> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public bool Visible {
            set;
            get;
        }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValueMetadata" /> is editable.
        /// </summary>
        /// <value><c>true</c> if editable; otherwise, <c>false</c>.</value>
        public bool Editable {
            set;
            get;
        }
    }
}
