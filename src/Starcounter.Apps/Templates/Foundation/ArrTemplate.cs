// ***********************************************************************
// <copyright file="ListTemplate.cs" company="Starcounter AB">
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
    /// Class ListTemplate
    /// </summary>
    public abstract class ListTemplate : ParentTemplate
#if IAPP
        , IListTemplate
#endif
    {
    }
}
