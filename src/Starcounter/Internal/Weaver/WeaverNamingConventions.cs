
using System;
using System.Text;
using PostSharp.Sdk.CodeModel;

namespace Starcounter.Internal.Weaver {
    internal static class WeaverNamingConventions {
        public const string ImplementationDetailsTypeName = "<>Starcounter.ImplementationDetails";
        //public const string LucentObjectsClientAssemblyInitializerName = "LucentObjectsClientAssemblyInitialize";

        public const string AttributeIndexVariablePrefix = "<>0";
        public const string AttributeIndexVariableSuffix = "000";

        public const string StaticTypeAddressFieldName = "__typeAddress";
        public const string StaticTypeBindingFieldName = "__typeBinding";
        public const string StaticTypeFieldName = "__type";

        public static string GetTypeAddressFieldName(TypeDefDeclaration typeDef) {
            StringBuilder builder = new StringBuilder();
            typeDef.WriteReflectionName(builder, ReflectionNameOptions.None);
            builder.Append(StaticTypeAddressFieldName);
            return builder.ToString();
        }

        public static string GetTypeAddressFieldName(Type type) {
            return type.ToString() + StaticTypeAddressFieldName;
        }

        public static string GetTypeBindingFieldName(TypeDefDeclaration typeDef) {
            StringBuilder builder = new StringBuilder();
            typeDef.WriteReflectionName(builder, ReflectionNameOptions.None);
            builder.Append(StaticTypeBindingFieldName);
            return builder.ToString();
        }

        public static string GetTypeBindingFieldName(Type type) {
            return type.ToString() + StaticTypeBindingFieldName;
        }

        public static string GetTypeFieldName(TypeDefDeclaration typeDef) {
            StringBuilder builder = new StringBuilder();
            typeDef.WriteReflectionName(builder, ReflectionNameOptions.None);
            builder.Append(StaticTypeFieldName);
            return builder.ToString();
        }

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
        /// MakeAttributeIndexVariableName("FirstName") will return the string "<>0FirstName000".
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
        /// TryGetNakedAttributeName("<>0FirstName000") will return true and the naked
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