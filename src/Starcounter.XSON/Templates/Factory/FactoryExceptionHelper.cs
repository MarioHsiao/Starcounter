using System;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.Templates.Factory {
    /// <summary>
    ///
    /// </summary>
    internal static class FactoryExceptionHelper {
        private const string ALREADY_EXISTS = "{0} already contains a definition for '{1}'";
        private const string WRONG_VALUE = "Wrong value for the property '{0}'. Expected a {1} but found a {2}";
        private const string INVALID = "Property '{0}' is not valid on this field.";
        private const string INVALID_CHARS = "Property '{0}' contains unsupported characters and is not valid ({1}).";
        private const string INVALID_TYPE = "Invalid field for Type property. Valid fields are string, int, decimal, double and boolean.";
        private const string UNKNOWN_PROPERTY = "Unknown property '{0}'.";
        private const string UNKNOWN_TYPE = "Unknown type '{0}'.";

        internal static void RaisePropertyExistsError(string propertyName, ISourceInfo sourceInfo) {
            throw new TemplateFactoryException(
                string.Format(ALREADY_EXISTS, sourceInfo.Filename, propertyName),  
                sourceInfo
            );
        }

        /// <summary>
        /// Raises the wrong value for property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="foundType">Type of the found.</param>
        /// <param name="sourceInfo">The debug info.</param>
        internal static void RaiseWrongValueForPropertyError(String propertyName,
                                                             String expectedType,
                                                             String foundType,
                                                             ISourceInfo sourceInfo) {
            throw new TemplateFactoryException(
                string.Format(WRONG_VALUE, propertyName, expectedType, foundType),
                sourceInfo
            );
        }

        /// <summary>
        /// Raises the invalid property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="sourceInfo">The debug info.</param>
        internal static void RaiseInvalidPropertyError(String propertyName, ISourceInfo sourceInfo) {
            throw new TemplateFactoryException(
                string.Format(INVALID, propertyName),
                sourceInfo
            );
        }

        /// <summary>
        /// Raises the invalid property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseInvalidPropertyCharactersError(String propertyName, 
                                                                 String invalidChars, 
                                                                 ISourceInfo sourceInfo) {
            throw new TemplateFactoryException(
                string.Format(INVALID_CHARS, propertyName, invalidChars),
                sourceInfo
            );
        }

        /// <summary>
        /// Raises the invalid type conversion error.
        /// </summary>
        /// <param name="sourceInfo">The debug info.</param>
        internal static void RaiseInvalidTypeConversionError(ISourceInfo sourceInfo) {
            throw new TemplateFactoryException(
                INVALID_TYPE,
                sourceInfo
            );
        }
        
        /// <summary>
        /// Raises the unknown property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="sourceInfo">The debug info.</param>
        internal static void RaiseUnknownPropertyError(String propertyName, ISourceInfo sourceInfo) {
            throw new TemplateFactoryException(
                string.Format(UNKNOWN_PROPERTY, propertyName),
                sourceInfo
            );
        }

        /// <summary>
        /// Raises the unknown property type error.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="sourceInfo">The debug info.</param>
        internal static void RaiseUnknownPropertyTypeError(String typeName, ISourceInfo sourceInfo) {
            throw new TemplateFactoryException(
                string.Format(UNKNOWN_TYPE, typeName),
                sourceInfo
            );
        }

        ///// <summary>
        ///// Raises the not implemented exception.
        ///// </summary>
        ///// <param name="name">The name.</param>
        ///// <param name="debugInfo">The debug info.</param>
        //internal static void RaiseNotImplementedException(String name, ISourceInfo debugInfo) {
        //    Error.CompileError.Raise<Object>(
        //        "The property '" + name + "' is not implemented yet.",
        //        new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
        //        debugInfo.FileName
        //    );
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name">The name.</param>
        ///// <param name="debugInfo">The debug info.</param>
        //internal static void RaiseInvalidEditableFlagForMetadata(String name, ISourceInfo debugInfo) {
        //    Error.CompileError.Raise<Object>(
        //        "Cannot set metadata property " + name + " as editable. The ending '$' should be removed.",
        //        new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
        //        debugInfo.FileName
        //    );
        //}
    }
}
