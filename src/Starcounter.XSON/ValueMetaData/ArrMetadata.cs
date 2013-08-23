// ***********************************************************************
// <copyright file="ArrMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="AppType"></typeparam>
    /// <typeparam name="TemplateType"></typeparam>
    public class ArrMetadata<AppType,JsonType> : ValueMetadata<JsonType,ArrSchema<AppType>>
                where AppType : Json<object>, new()
                where JsonType : Json<object>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="prop"></param>
        public ArrMetadata(JsonType app, ArrSchema<AppType> prop) : base(app, prop) { }

    }
}