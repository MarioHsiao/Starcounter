// ***********************************************************************
// <copyright file="ObjMetadataBase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class ObjMetadataBase  {

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjMetadataBase" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public ObjMetadataBase(Obj app, Template template) {
            _App = app;
            _Template = template;
        }

        private Obj _App;
        private Template _Template;

        /// <summary>
        /// Gets the app.
        /// </summary>
        /// <value>The app.</value>
        public Obj App { get { return _App; } }

        /// <summary>
        /// Gets the template.
        /// </summary>
        /// <value>The template.</value>
        public Template Template { get { return _Template; } }

    }
}

