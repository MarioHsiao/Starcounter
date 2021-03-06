﻿// ***********************************************************************
// <copyright file="IHttpRestServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Advanced {
    /// <summary>
    /// To serve Web pages and App state, you can implement a REST handler. A REST handler can process a Method on a
    /// resource located using a URI.
    /// </summary>
    /// <remarks>The most commonly known use of Representational state transfer (REST) is the world wide web (WWW)
    /// that uses the HTTP protocol (the most commonly used REST protocol). The most commonly used REST Methods
    /// are GET and POST, often used by web browser to get REST resources in the HTML format.
    /// The term REST was introduced by Roy Fielding, one of the authors of the original HTTP protocol.</remarks>
    public interface IRestServer {

        /// <summary>
        /// The starcounter .EXE modules will provide a path where static file resources such as .html files or images
        /// are kept. This allows the web server to serve content from all modules without having to copy or deploy files to
        /// a single location.
        /// </summary>
        void UserAddedLocalFileDirectoryWithStaticContent(String appName, UInt16 port, String path);

        /// <summary>
        /// Get a list with all folders where static file resources such as .html files or images are kept.
        /// </summary>
        /// <returns></returns>
        List<string> GetWorkingDirectories(UInt16 port);

        /// <summary>
        /// Housekeeps this instance.
        /// </summary>
        void Housekeep();
    }
}
