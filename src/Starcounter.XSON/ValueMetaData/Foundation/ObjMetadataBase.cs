// ***********************************************************************
// <copyright file="ObjMetadataBase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class ObjMetadataBase<JsonType,TemplateType>
                where JsonType : Json<object> 
                where TemplateType : Template {

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjMetadataBase" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public ObjMetadataBase(JsonType app, TemplateType template) {
            _App = app;
            _Template = template;
        }

        private JsonType _App;
        private TemplateType _Template;

        /// <summary>
        /// Gets the app.
        /// </summary>
        /// <value>The app.</value>
        public JsonType App { get { return _App; } }

        /// <summary>
        /// Gets the template.
        /// </summary>
        /// <value>The template.</value>
        public TemplateType Template { get { return _Template; } }

    }
}

