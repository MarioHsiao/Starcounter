// ***********************************************************************
// <copyright file="ObjMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class ObjMetadata : ObjMetadataBase {

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public ObjMetadata(Obj app, Template template ) : base (app, template ) {}

        /// <summary>
        /// Sets all editable.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        public void SetAllEditable( bool editable = true ) {
        }

        /// <summary>
        /// Sets all visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetAllVisible( bool visible = true ) {
        }

        /// <summary>
        /// Sets all enabled.
        /// </summary>
        /// <param name="enabled">if set to <c>true</c> [enabled].</param>
        public void SetAllEnabled( bool enabled = true ) {
        }

    }
}

