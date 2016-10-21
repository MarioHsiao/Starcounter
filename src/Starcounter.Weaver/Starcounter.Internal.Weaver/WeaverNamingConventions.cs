// ***********************************************************************
// <copyright file="WeaverNamingConventions.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using PostSharp.Sdk.CodeModel;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Class WeaverNamingConventions
    /// </summary>
    internal static class WeaverNamingConventions {
        /// <summary>
        /// The implementation details type name
        /// </summary>
        public const string ImplementationDetailsTypeName = "<>Starcounter.ImplementationDetails";
        //public const string LucentObjectsClientAssemblyInitializerName = "LucentObjectsClientAssemblyInitialize";

        /// <summary>
        /// The attribute index variable prefix
        /// </summary>
        public const string AttributeIndexVariablePrefix = "<>0";
        /// <summary>
        /// The attribute index variable suffix
        /// </summary>
        public const string AttributeIndexVariableSuffix = "000";
        /// <summary>
        /// The static type address field name
        /// </summary>
        public const string StaticTypeTableIdFieldName = "__typeTableId";
        /// <summary>
        /// The static type binding field name
        /// </summary>
        public const string StaticTypeBindingFieldName = "__typeBinding";
        /// <summary>
        /// The static type field name
        /// </summary>
        public const string StaticTypeFieldName = "__type";

        /// <summary>
        /// Gets the name of the type address field.
        /// </summary>
        /// <param name="typeDef">The type def.</param>
        /// <returns>System.String.</returns>
        public static string GetTypeTableIdFieldName(TypeDefDeclaration typeDef) {
            StringBuilder builder = new StringBuilder();
            typeDef.WriteReflectionName(builder, ReflectionNameOptions.None);
            builder.Append(StaticTypeTableIdFieldName);
            return builder.ToString();
        }

        /// <summary>
        /// Gets the name of the type address field.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetTypeTableIdFieldName(Type type) {
            return type.ToString() + StaticTypeTableIdFieldName;
        }

        /// <summary>
        /// Gets the name of the type binding field.
        /// </summary>
        /// <param name="typeDef">The type def.</param>
        /// <returns>System.String.</returns>
        public static string GetTypeBindingFieldName(TypeDefDeclaration typeDef) {
            StringBuilder builder = new StringBuilder();
            typeDef.WriteReflectionName(builder, ReflectionNameOptions.None);
            builder.Append(StaticTypeBindingFieldName);
            return builder.ToString();
        }

        /// <summary>
        /// Gets the name of the type binding field.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetTypeBindingFieldName(Type type) {
            return type.ToString() + StaticTypeBindingFieldName;
        }

        /// <summary>
        /// Gets the name of the type field.
        /// </summary>
        /// <param name="typeDef">The type def.</param>
        /// <returns>System.String.</returns>
        public static string GetTypeFieldName(TypeDefDeclaration typeDef) {
            StringBuilder builder = new StringBuilder();
            typeDef.WriteReflectionName(builder, ReflectionNameOptions.None);
            builder.Append(StaticTypeFieldName);
            return builder.ToString();
        }

        /// <summary>
        /// Gets the name of the type field.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetTypeFieldName(Type type) {
            return type.ToString() + StaticTypeFieldName;
        }

        /// <summary>
        /// Makes a decorated name intended to be used to name a variable that is to
        /// hold a persistent field's attribute index.
        /// </summary>
        /// <param name="nakedAttributeName">
        /// The name of the naked attribute, i.e. the name of the field.
        /// </param>
        /// <returns>A name decorated with characters we use to make attribute index
        /// fields unique and recognizable.</returns>
        /// <remarks>
        /// <example>
        /// MakeAttributeIndexVariableName("FirstName") will return the string "&lt;&gt;0FirstName000".
        /// </example>
        /// </remarks>
        public static string MakeAttributeIndexVariableName(string nakedAttributeName) {
            StringBuilder builder;

            builder = new StringBuilder(
                nakedAttributeName.Length +
                AttributeIndexVariablePrefix.Length +
                AttributeIndexVariableSuffix.Length
                );
            builder.Append(AttributeIndexVariablePrefix);
            builder.Append(nakedAttributeName);
            builder.Append(AttributeIndexVariableSuffix);

            return builder.ToString();
        }

        /// <summary>
        /// Tries to extract the naked attribute name of a decorated attribute variable
        /// name. Does the reverse of <see cref="MakeAttributeIndexVariableName"/>.
        /// </summary>
        /// <param name="decoratedName"></param>
        /// <param name="nakedAttributeName"></param>
        /// <returns></returns>
        /// <remarks>
        /// <example>
        /// TryGetNakedAttributeName("&lt;&gt;0FirstName000") will return true and the naked
        /// attribute string "FirstName".
        /// </example>
        /// </remarks>
        public static bool TryGetNakedAttributeName(string decoratedName, out string nakedAttributeName) {
            if (string.IsNullOrEmpty(decoratedName)) {
                nakedAttributeName = null;
                return false;
            }

            if (decoratedName.StartsWith(AttributeIndexVariablePrefix) &&
                decoratedName.EndsWith(AttributeIndexVariableSuffix)) {
                nakedAttributeName = decoratedName.Substring(AttributeIndexVariablePrefix.Length);
                nakedAttributeName = nakedAttributeName.Substring(0, nakedAttributeName.Length - AttributeIndexVariableSuffix.Length);
                return true;
            }

            nakedAttributeName = null;
            return false;
        }
    }
}