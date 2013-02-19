// ***********************************************************************
// <copyright file="IAppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {


    /// <summary>
    /// Interface IAppFactory
    /// </summary>
    public interface IAppFactory {

        /// <summary>
        /// Creates the app.
        /// </summary>
        /// <returns>IApp.</returns>
        object CreateApp();
        /// <summary>
        /// Creates the app template.
        /// </summary>
        /// <returns>ITApp.</returns>
        object CreateTApp();
        /// <summary>
        /// Creates the string template.
        /// </summary>
        /// <returns>IStringTemplate.</returns>
        object CreateStringTemplate();
        /// <summary>
        /// Creates the double template.
        /// </summary>
        /// <returns>IDoubleTemplate.</returns>
        object CreateDoubleTemplate();
        /// <summary>
        /// Creates the bool template.
        /// </summary>
        /// <returns>IBoolTemplate.</returns>
        object CreateBoolTemplate();

    }

}