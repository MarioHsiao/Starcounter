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
        IApp CreateApp();
        /// <summary>
        /// Creates the app template.
        /// </summary>
        /// <returns>IAppTemplate.</returns>
        IAppTemplate CreateAppTemplate();
        /// <summary>
        /// Creates the string template.
        /// </summary>
        /// <returns>IStringTemplate.</returns>
        IStringTemplate CreateStringTemplate();
        /// <summary>
        /// Creates the double template.
        /// </summary>
        /// <returns>IDoubleTemplate.</returns>
        IDoubleTemplate CreateDoubleTemplate();
        /// <summary>
        /// Creates the bool template.
        /// </summary>
        /// <returns>IBoolTemplate.</returns>
        IBoolTemplate CreateBoolTemplate();

    }

}