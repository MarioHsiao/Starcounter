// ***********************************************************************
// <copyright file="ObjMetadataBase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class ObjMetadataBase<TJson, TTemplate>
        where TJson : Json 
        where TTemplate : Template {

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjMetadataBase" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public ObjMetadataBase(TJson app, TTemplate template) {
            this.app = app;
            this.template = template;
        }

        private TJson app;
        private TTemplate template;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public TJson App { get { return app; } }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public TTemplate Template { get { return template; } }

    }
}

