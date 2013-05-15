// ***********************************************************************
// <copyright file="IDebugInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Templates.Interfaces {

    /// <summary>
    /// Interface IDebugInfo
    /// </summary>
    public interface IDebugInfo {
        /// <summary>
        /// Gets the line no.
        /// </summary>
        /// <value>The line no.</value>
        int LineNo { get; }
        /// <summary>
        /// Gets the col no.
        /// </summary>
        /// <value>The col no.</value>
        int ColNo { get;  }
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        string FileName { get;  }
    }
}


