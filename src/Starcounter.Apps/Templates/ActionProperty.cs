// ***********************************************************************
// <copyright file="ActionProperty.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    /// <summary>
    /// Class ActionProperty
    /// </summary>
    public class ActionProperty : Template
#if IAPP
        , IActionTemplate
#endif
    {
        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

        /// <summary>
        /// Gets the type of the json.
        /// </summary>
        /// <value>The type of the json.</value>
        public override string JsonType {
            get { return "function"; }
        }

        /// <summary>
        /// Gets or sets the on run.
        /// </summary>
        /// <value>The on run.</value>
        public string OnRun { get; set; }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance(AppNode parent) {
            return false;
        }
    }
}
