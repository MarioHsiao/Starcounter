// ***********************************************************************
// <copyright file="AppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

#if CLIENT
using Starcounter.Client.Template;
#else
using Starcounter.Templates;
#endif

using Starcounter.Templates.Interfaces;
namespace Starcounter.Client {
    /// <summary>
    /// Class AppFactory
    /// </summary>
    public class AppFactory : IAppFactory {

        /// <summary>
        /// Creates the app.
        /// </summary>
        /// <returns>IApp.</returns>
        public IApp CreateApp() {
            return new App();
        }

        /// <summary>
        /// Creates the app template.
        /// </summary>
        /// <returns>IAppTemplate.</returns>
        public IAppTemplate CreateAppTemplate() {
            return new AppTemplate();
        }

        /// <summary>
        /// Creates the string template.
        /// </summary>
        /// <returns>IStringTemplate.</returns>
        public IStringTemplate CreateStringTemplate() {
            return new StringProperty();
        }

        /// <summary>
        /// Creates the double template.
        /// </summary>
        /// <returns>IDoubleTemplate.</returns>
        public IDoubleTemplate CreateDoubleTemplate() {
            return new DoubleProperty();
        }

        /// <summary>
        /// Creates the bool template.
        /// </summary>
        /// <returns>IBoolTemplate.</returns>
        public IBoolTemplate CreateBoolTemplate() {
            return new BoolProperty();
        }
    }
}
