// ***********************************************************************
// <copyright file="WeaverUtilities.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter;
using System.Diagnostics;
using PostSharp.Sdk.CodeModel;

namespace Starcounter.Internal.Weaver
{
    /// <summary>
    /// Exposes a set of utility methods relating to weaving.
    /// </summary>
    public static class WeaverUtilities
    {
        /// <summary>
        /// Determines whether a <see cref="Type" /> inherits a parent type, given
        /// the name of the parent type.
        /// </summary>
        /// <param name="child">Child <see cref="Type" />.</param>
        /// <param name="parentName">Name of the parent type.</param>
        /// <returns><b>true</b> if <paramref name="child" /> derives from a type named <paramref name="parentName" />,
        /// otherwise <b>false</b>.</returns>
        internal static bool Inherits(IType child, string parentName, bool onlyDirect = false) {
            TypeDefDeclaration cursor = child.GetTypeDefinition();
            while (true) {
                if (cursor.GetReflectionName() == parentName) {
                    return true;
                }
                if (onlyDirect && cursor != child) {
                    return false;
                }
                if (cursor.BaseType != null) {
                    cursor = cursor.BaseType.GetTypeDefinition();
                } else {
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets a value indicating if the given type directly inherits the
        /// root .NET type <see cref="System.Object"/>.
        /// </summary>
        /// <param name="typeDef">The type to check</param>
        /// <returns>True if it inherits <see cref="System.Object"/> direct;
        /// false otherwise.</returns>
        internal static bool InheritsObject(TypeDefDeclaration typeDef) {
            return WeaverUtilities.Inherits(typeDef, typeof(object).FullName, true);
        }

        /// <summary>
        /// Gets a value indicating if the given type is considered a database
        /// root class.
        /// </summary>
        /// <remarks>
        /// Database classes are classes recognized by Starcounter via some
        /// decoration (currently, a custom attribute). Root database classes
        /// are database classes that does not inherit another database class,
        /// but rather one of the allowed framework classes (currently only
        /// <see cref="System.Object"/>).
        /// </remarks>
        /// <param name="typeDef">The type to evaluate</param>
        /// <returns>True if the type is considered a database root class;
        /// false otherwise</returns>
        internal static bool IsDatabaseRoot(TypeDefDeclaration typeDef) {
            return WeaverUtilities.InheritsObject(typeDef);
        }

        /// <summary>
        /// Converts a weaver message to it's error code.
        /// </summary>
        /// <param name="messageId">
        /// The message ID of the message whose corresponding
        /// error code is requested. Message ID's are defined
        /// in Messages.resx.</param>
        /// <returns>The error code for the given message, or 0 (zero)
        /// if the given ID does not map to an error.</returns>
        public static uint WeaverMessageToErrorCode(string messageId)
        {
            uint errorCode;

            if (ErrorCode.TryParseDecorated(messageId, out errorCode))
                return errorCode;

            switch (messageId)
            {
                case "SCATV01":
                    errorCode = Error.SCERRREFFORBIDDENUSERCODE;
                    break;
                case "SCATV03":
                    errorCode = Error.SCERRILLEGALATTRIBUTEASSIGN;
                    break;
                case "SCDCV01":
                    errorCode = Error.SCERRDBCLASSCANTBEGENERIC;
                    break;
                case "SCDCV02":
                    errorCode = Error.SCERRILLEGALFINALIZER;
                    break;
                case "SCDCV03":
                    errorCode = Error.SCERRILLEGALTYPEREFDECL;
                    break;
                case "SCDCV04":
                    errorCode = Error.SCERRTOCOMPLEXCTOR;
                    break;
                case "SCDCV06":
                    errorCode = Error.SCERRFIELDREDECLARATION;
                    break;
                case "SCDCV07":
                    errorCode = Error.SCERRTYPENAMEDUPLICATE;
                    break;
                case "SCECV01":
                    errorCode = Error.SCERRILLEGALEXTCTOR;
                    break;
                case "SCECV02":
                    errorCode = Error.SCERRILLEGALEXTCREATION;
                    break;
                case "SCECV03":
                    errorCode = Error.SCERRILLEGALEXTCTORBODY;
                    break;
                case "SCECV04":
                    errorCode = Error.SCERREXTNOTSEALED;
                    break;
                case "SCKCV02":
                    errorCode = Error.SCERRKINDWRONGNAME;
                    break;
                case "SCKCV03":
                    errorCode = Error.SCERRKINDMISSINGCONCEPT;
                    break;
                case "SCKCV04":
                    errorCode = Error.SCERRKINDILLEGALPARENT;
                    break;
                case "SCKCV05":
                    errorCode = Error.SCERRKINDMISSINGCTOR;
                    break;
                case "SCKCV06":
                    errorCode = Error.SCERRKINDMISSINGPARENT;
                    break;
                case "SCKCV09":
                    errorCode = Error.SCERRKINDWRONGVISIBILITY;
                    break;
                case "SCPFV02":
                    errorCode = Error.SCERRFIELDCOMPLEXINIT;
                    break;
                case "SCPFV06":
                    errorCode = Error.SCERRSYNNOTARGET;
                    break;
                case "SCPFV07":
                    errorCode = Error.SCERRSYNTYPEMISMATCH;
                    break;
                case "SCPFV08":
                    errorCode = Error.SCERRSYNVISIBILITYMISMATCH;
                    break;
                case "SCPFV09":
                    errorCode = Error.SCERRSYNREADONLYMISMATCH;
                    break;
                case "SCPFV12":
                    errorCode = Error.SCERRSYNTARGETNOTPERSISTENT;
                    break;
                case "SCPFV20":
                    errorCode = Error.SCERRSYNPRIVATETARGET;
                    break;
                case "SCPPV02":
                    errorCode = Error.SCERRPERSPROPNOTARGET;
                    break;
                case "SCPPV03":
                    errorCode = Error.SCERRPERSPROPWRONGCOREREF;
                    break;
                default:
                    errorCode = 0;
                    break;
            }

            return errorCode;
        }

        /// <summary>
        /// Gets a value indicating if the given weaver message
        /// (represented here by its ID) comes from a well-known
        /// Starcounter error code.
        /// </summary>
        /// <param name="messageId">
        /// Identity of the message to validate.
        /// </param>
        /// <returns>True if it matches an error coded message.
        /// False if not.</returns>
        public static bool IsFromErrorCode(string messageId)
        {
            return ErrorCode.IsDecoratedErrorCode(messageId);
        }

        /// <summary>
        /// Constructs a human-readable message indicating the reason why
        /// the assembly represented by <paramref name="cachedAssembly"/>
        /// could not be properly extraced from the weaver cachse.
        /// </summary>
        /// <remarks>
        /// If the extraction was successfull, it is invalid to call this
        /// method, and a <see cref="InvalidOperationException"/> will be
        /// raised. To test if an assembly was properly extracted, consult
        /// the see cref="CachedAssembly.Assembly" property for null.
        /// </remarks>
        /// <returns>A message describing the cause to why the assembly
        /// represented by <paramref name="cachedAssembly"/> was not properly
        /// extracted.
        /// </returns>
        public static string GetExtractionFailureReason(WeaverCache.CachedAssembly cachedAssembly)
        {
            if (cachedAssembly == null)
                throw new ArgumentNullException("cachedAssembly");

            if (cachedAssembly.Assembly != null)
                throw new InvalidOperationException("The assembly was successfully extracted.");

            if (cachedAssembly.Cache.Disabled)
                return "The cache was disabled.";

            if (cachedAssembly.NotFound)
                return "The assembly metadata was not found in the cache.";

            if (cachedAssembly.DeserializationException != null)
                return string.Format(
                    "Deserializing the cached schema failed: {0}.", 
                    cachedAssembly.DeserializationException.Message
                    );

            if (cachedAssembly.MissingDependency != null)
                return string.Format(
                    "The dependent assembly \"{0}\" was not found.",
                    cachedAssembly.MissingDependency
                    );

            if (cachedAssembly.BrokenDependency != null)
                return string.Format(
                    "The dependent assembly \"{0}\" had changed.",
                    cachedAssembly.BrokenDependency
                    );

            if (cachedAssembly.TransformationNotFound)
                return "The weaved assembly result was not found in the cache.";

            if (cachedAssembly.TransformationOutdated)
                return "The weaved assembly result in the cache was outdated with respect to the metadata.";

            Trace.Fail("The cached assembly representation was corrupt.");
            return null;
        }
    }
}
