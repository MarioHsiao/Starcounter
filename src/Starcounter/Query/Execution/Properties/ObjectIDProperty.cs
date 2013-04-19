﻿using System;
using Starcounter.Binding;

namespace Starcounter.Query.Execution {
    internal class ObjectIDProperty : Property, IStringPathItem {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="extNum">The extent number to which this property belongs.
        /// If it does not belong to any extent number, which is the case for path expressions,
        /// then the number should be -1.</param>
        /// <param name="typeBind">The type binding of the object to which this property belongs.</param>
        internal ObjectIDProperty(Int32 extNum, ITypeBinding typeBind) {
            if (typeBind == null) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
            }
            extentNumber = extNum;
            typeBinding = typeBind;
            propIndex = -1;
        }
        /// <summary>
        /// 
        /// </summary>
        public QueryTypeCode QueryTypeCode {
            get {
                return QueryTypeCode.String;
            }
        }

        /// <summary>
        /// The DbTypeCode of this property.
        /// </summary>
        public override DbTypeCode DbTypeCode {
            get {
                return DbTypeCode.String;
            }
        }

        /// <summary>
        /// Name to be displayed for example as column header in a result grid.
        /// </summary>
        public override String Name {
            get {
                return DbHelper.ObjectIDName;
            }
        }
        
        /// <summary>
        /// Full path name to uniquely identify this property.
        /// </summary>
        public override String FullName {
            get {
                return DbHelper.ObjectIDName;
            }
        }

        /// <summary>
        /// Appends data of this leaf to the provided filter key.
        /// </summary>
        /// <param name="key">Reference to the filter key to which data should be appended.</param>
        /// <param name="obj">Row for which evaluation should be performed.</param>
        public override void AppendToByteArray(ByteArrayBuilder key, IObjectView obj) {
            // Checking if its a property from some previous extent
            // and if yes calculate its data (otherwise do nothing).
            if (propFromPreviousExtent) {
                key.Append(EvaluateToString(obj), false);
            }
        }

        /// <summary>
        /// Calculates the value of this property when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate this property.</param>
        /// <returns>The value of this property when evaluated on the input object.</returns>
        public String EvaluateToString(IObjectView obj) {
            if (obj == null) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj.");
            }
            if (obj is Row) {
                // Control that the type ((obj.TypeBinding as RowTypeBinding).GetTypeBinding(extentNumber)) of the input object
                // is equal to or a subtype (TypeBinding.SubTypeOf(TypeBinding)) of the type (typeBinding) to which this property belongs
                // is not implemented due to that interfaces cannot be handled and computational cost.
                IObjectView partObj = (obj as Row).AccessObject(extentNumber);
                if (partObj == null) {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "No elementary object at extent number: " + extentNumber);
                }
                return DbHelper.GetObjectID(partObj);
            }
            // Control that the type (obj.TypeBinding) of the input object
            // is equal to or a subtype (TypeBinding.SubTypeOf(TypeBinding)) of the type (typeBinding) to which this property belongs
            // is not implemented due to that interfaces cannot be handled and computational cost.
            return DbHelper.GetObjectID(obj);
        }

        /// <summary>
        /// Calculates the value of the path-item when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate the expression.</param>
        /// <param name="startObj">The start object of the current path expression.</param>
        /// <returns>The value of the expression when evaluated on the input object.</returns>
        public String EvaluateToString(IObjectView obj, IObjectView startObj) {
            return EvaluateToString(obj);
        }

        /// <summary>
        /// Examines if the value of the property is null when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate the property.</param>
        /// <returns>True, if the value of the property when evaluated on the input object
        /// is null, otherwise false.</returns>
        public override Boolean EvaluatesToNull(IObjectView obj) {
            return (EvaluateToString(obj) == null);
        }

        /// <summary>
        /// Creates an more instantiated copy of this expression by evaluating it on a Row.
        /// Properties, with extent numbers for which there exist objects attached to the Row,
        /// are evaluated and instantiated to literals, other properties are not changed.
        /// </summary>
        /// <param name="obj">The Row on which to evaluate the expression.</param>
        /// <returns>A more instantiated expression.</returns>
        public IStringExpression Instantiate(Row obj) {
            if (obj != null && extentNumber >= 0 && obj.AccessObject(extentNumber) != null) {
                return new StringLiteral(EvaluateToString(obj));
            }
            return new ObjectIDProperty(extentNumber, typeBinding);
        }

        public override IValueExpression Clone(VariableArray varArray) {
            return CloneToString(varArray);
        }

        public IStringExpression CloneToString(VariableArray varArray) {
            return new ObjectIDProperty(extentNumber, typeBinding);
        }

        /// <summary>
        /// Builds a string presentation of this property using the input string-builder.
        /// </summary>
        /// <param name="stringBuilder">String-builder to use.</param>
        /// <param name="tabs">Number of tab indentations for the presentation.</param>
        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs) {
            stringBuilder.Append(tabs, "ObjectIDProperty(");
            stringBuilder.Append(extentNumber.ToString());
            stringBuilder.Append(", ");
            stringBuilder.Append(DbHelper.ObjectIDName);
            stringBuilder.AppendLine(")");
        }
    }
}
