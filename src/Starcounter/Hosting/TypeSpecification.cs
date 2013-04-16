using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Binding;
using System.Reflection;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents the interface to a database type specification, as
    /// defined <a href="http://www.starcounter.com/internal/wiki/W3">
    /// here</a>.
    /// </summary>
    /// <remarks>
    /// The current implementation is backed by reflection and static
    /// constructs. By having this functionality encapsulated by this
    /// class, we can swap the implementation later if we find a need
    /// to improve it.
    /// </remarks>
    public sealed class TypeSpecification {
        Type typeSpecificationType;
        FieldInfo tableHandle;
        FieldInfo typeBinding;

        /// <summary>
        /// Provides the type specification class name.
        /// </summary>
        public const string Name = "__starcounterTypeSpecification";

        /// <summary>
        /// Provides the name of the type specification table handle
        /// field.
        /// </summary>
        public const string TableHandleName = "tableHandle";

        /// <summary>
        /// Provides the name of the type specification type binding
        /// field.
        /// </summary>
        public const string TypeBindingName = "typeBinding";

        /// <summary>
        /// Provides the name of the "this handle" field, part of
        /// the database class itself.
        /// </summary>
        public const string ThisHandleName = "__sc__this_handle__";

        /// <summary>
        /// Provides the name of the "this identity" field, part of
        /// the database class itself.
        /// </summary>
        public const string ThisIdName = "__sc__this_id__";

        /// <summary>
        /// Provides the name of the "this binding" field, part of
        /// the database class itself.
        /// </summary>
        public const string ThisBindingName = "__sc__this_binding__";

        /// <summary>
        /// Converts a field name to its corresponding column handle
        /// name.
        /// </summary>
        /// <param name="fieldName">The field name to convert.</param>
        /// <returns>A column handle name for the given field.</returns>
        public static string FieldNameToColumnHandleName(string fieldName) {
            return string.Concat("columnHandle_", fieldName);
        }

        /// <summary>
        /// Converts a field name to its corresponding column handle
        /// name.
        /// </summary>
        /// <param name="fieldName">The field name to convert.</param>
        /// <returns>A column handle name for the given field.</returns>
        public static string ColumnHandleNameToFieldName(string columnHandleName) {
            return columnHandleName.Substring("columnHandle_".Length);
        }

        /// <summary>
        /// Initializes a <see cref="TypeSpecification"/> with the
        /// given <paramref name="typeSpecType"/>.
        /// </summary>
        /// <param name="typeSpecType">The <see cref="Type"/> of the
        /// type specification the current instance represent.</param>
        /// <param name="omitVerifyType">Instruts this method not to
        /// verify the type-level construct. Used internally by emitted
        /// code, when it's certain that the construct is correct.</param>
        internal TypeSpecification(Type typeSpecType, bool omitVerifyType = false) {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            typeSpecificationType = typeSpecType;
            tableHandle = typeSpecType.GetField(TypeSpecification.TableHandleName, flags);
            typeBinding = typeSpecType.GetField(TypeSpecification.TypeBindingName, flags);
            Validate(omitVerifyType);
        }

        /// <summary>
        /// Gets or sets the underlying table handle.
        /// </summary>
        public ushort TableHandle {
            get { return (ushort)tableHandle.GetValue(null); }
            set { tableHandle.SetValue(null, value); }
        }

        /// <summary>
        /// Gets or sets the underlying type binding reference.
        /// </summary>
        public ITypeBinding TypeBinding {
            get { return (ITypeBinding) typeBinding.GetValue(null); }
            set { typeBinding.SetValue(null, value); }
        }

        /// <summary>
        /// Assigns the column index of a column with a specified name.
        /// </summary>
        /// <param name="columnName">The name of the column whos handle
        /// should be assigned.</param>
        /// <param name="index">The column index to assign the handle.
        /// </param>
        public void SetColumnIndex(string columnName, int index) {
            var handleName = TypeSpecification.FieldNameToColumnHandleName(columnName);
            var field = typeSpecificationType.GetField(handleName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) {
                var msg = string.Format("Missing column handle: {0}/{1}", columnName, handleName);
                throw ErrorCode.ToException(Error.SCERRTYPESPECILLEGALCONSTRUCT, msg);
            }
            field.SetValue(null, index);
        }

        void Validate(bool omitVerifyType) {
            var typeSpecType = typeSpecificationType;

            if (!omitVerifyType) {
                if (!typeSpecType.Name.Equals(TypeSpecification.Name)) {
                    var specName = string.Format("Specification type: {0} (required name: {1})", typeSpecType.Name, TypeSpecification.Name);
                    throw ErrorCode.ToException(Error.SCERRNOTATYPESPECIFICATIONTYPE, specName);

                } else if (typeSpecType.DeclaringType == null || !typeSpecType.DeclaringType.IsClass) {
                    var msg = string.Format("Specification type: {0} (must nest in its database class)", typeSpecType.Name);
                    throw ErrorCode.ToException(Error.SCERRNOTATYPESPECIFICATIONTYPE, msg);
                }
            }

            string missingConstruct = "Missing {0} ({1})";
            string wrongType = "The {0} has the wrong type {1}, expected {2}";
            if (tableHandle == null) {
                missingConstruct = string.Format(missingConstruct, "table handle", TypeSpecification.TableHandleName);
                throw ErrorCode.ToException(Error.SCERRTYPESPECILLEGALCONSTRUCT, missingConstruct);
            } else if (!tableHandle.FieldType.Equals(typeof(ushort))) {
                wrongType = string.Format(wrongType, "table handle", tableHandle.FieldType.Name, typeof(ushort).Name);
                throw ErrorCode.ToException(Error.SCERRTYPESPECILLEGALCONSTRUCT, wrongType);
            }
            if (typeBinding == null) {
                missingConstruct = string.Format(missingConstruct, "type binding reference", TypeSpecification.TypeBindingName);
                throw ErrorCode.ToException(Error.SCERRTYPESPECILLEGALCONSTRUCT, missingConstruct);

            } else if (!typeBinding.FieldType.Equals(typeof(TypeBinding))) {
                wrongType = string.Format(wrongType, "type binding", typeBinding.FieldType.Name, typeof(TypeBinding).Name);
                throw ErrorCode.ToException(Error.SCERRTYPESPECILLEGALCONSTRUCT, wrongType);
            }
        }
    }
}
