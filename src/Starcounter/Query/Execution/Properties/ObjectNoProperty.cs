﻿using System;
using Starcounter.Binding;

namespace Starcounter.Query.Execution {
    internal class ObjectNoProperty : Property, IUIntegerPathItem {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="extNum">The extent number to which this property belongs.
        /// If it does not belong to any extent number, which is the case for path expressions,
        /// then the number should be -1.</param>
        /// <param name="typeBind">The type binding of the object to which this property belongs.</param>
        internal ObjectNoProperty(Int32 extNum, ITypeBinding typeBind) {
            if (typeBind == null) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
            }
            extentNumber = extNum;
            typeBinding = typeBind;
        }

        public QueryTypeCode QueryTypeCode {
            get {
                return QueryTypeCode.UInteger;
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
                key.Append(EvaluateToUInteger(obj));
            }
        }

        /// <summary>
        /// Calculates the value of this property when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate this property.</param>
        /// <returns>The value of this property when evaluated on the input object.</returns>
        public Nullable<UInt64> EvaluateToUInteger(IObjectView obj) {
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
                return DbHelper.GetObjectNo(partObj);
            }
            // Control that the type (obj.TypeBinding) of the input object
            // is equal to or a subtype (TypeBinding.SubTypeOf(TypeBinding)) of the type (typeBinding) to which this property belongs
            // is not implemented due to that interfaces cannot be handled and computational cost.
            return DbHelper.GetObjectNo(obj);
        }

        /// <summary>
        /// Calculates the value of this property as a nullable Decimal.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Decimal> EvaluateToDecimal(IObjectView obj) {
            return EvaluateToUInteger(obj);
        }

        /// <summary>
        /// Calculates the value of this property as a nullable Double.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Double> EvaluateToDouble(IObjectView obj) {
            return EvaluateToUInteger(obj);
        }

        /// <summary>
        /// Calculates the value of this property as a nullable Int64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Int64> EvaluateToInteger(IObjectView obj) {
            Nullable<UInt64> value = EvaluateToUInteger(obj);
            if (value == null) {
                return null;
            }
            if (value.Value > Int64.MaxValue) {
                return null;
            }
            return (Int64)value.Value;
        }

        /// <summary>
        /// Calculates the value of this property as a ceiling (round up) nullable Int64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj) {
            Nullable<UInt64> value = EvaluateToUInteger(obj);
            if (value == null) {
                return null;
            }
            if (value.Value > Int64.MaxValue) {
                return null;
            }
            return (Int64)value.Value;
        }

        /// <summary>
        /// Calculates the value of this property as a floor (round down) nullable Int64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj) {
            Nullable<UInt64> value = EvaluateToUInteger(obj);
            if (value == null) {
                return null;
            }
            if (value.Value > Int64.MaxValue) {
                return Int64.MaxValue;
            }
            return (Int64)value.Value;
        }

        /// <summary>
        /// Calculates the value of this property as a ceiling (round up) nullable UInt64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj) {
            return EvaluateToUInteger(obj);
        }

        /// <summary>
        /// Calculates the value of this property as a floor (round down) nullable UInt64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj) {
            return EvaluateToUInteger(obj);
        }

        /// <summary>
        /// Calculates the value of the path-item when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate the expression.</param>
        /// <param name="startObj">The start object of the current path expression.</param>
        /// <returns>The value of the expression when evaluated on the input object.</returns>
        public Nullable<UInt64> EvaluateToUInteger(IObjectView obj, IObjectView startObj) {
            return EvaluateToUInteger(obj);
        }

        /// <summary>
        /// Examines if the value of the property is null when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate the property.</param>
        /// <returns>True, if the value of the property when evaluated on the input object
        /// is null, otherwise false.</returns>
        public override Boolean EvaluatesToNull(IObjectView obj) {
            return (EvaluateToUInteger(obj) == null);
        }


        /// <summary>
        /// Creates an more instantiated copy of this expression by evaluating it on a Row.
        /// Properties, with extent numbers for which there exist objects attached to the Row,
        /// are evaluated and instantiated to literals, other properties are not changed.
        /// </summary>
        /// <param name="obj">The Row on which to evaluate the expression.</param>
        /// <returns>A more instantiated expression.</returns>
        public INumericalExpression Instantiate(Row obj) {
            if (obj != null && extentNumber >= 0 && obj.AccessObject(extentNumber) != null) {
                return new UIntegerLiteral(EvaluateToUInteger(obj));
            }
            return new ObjectNoProperty(extentNumber, typeBinding);
        }

        public override IValueExpression Clone(VariableArray varArray) {
            return CloneToUInteger(varArray);
        }

        public IUIntegerExpression CloneToUInteger(VariableArray varArray) {
            return new ObjectNoProperty(extentNumber, typeBinding);
        }

        public INumericalExpression CloneToNumerical(VariableArray varArray) {
            return new ObjectNoProperty(extentNumber, typeBinding);
        }

        /// <summary>
        /// Builds a string presentation of this property using the input string-builder.
        /// </summary>
        /// <param name="stringBuilder">String-builder to use.</param>
        /// <param name="tabs">Number of tab indentations for the presentation.</param>
        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs) {
            stringBuilder.Append(tabs, "ObjectNoProperty(");
            stringBuilder.Append(extentNumber.ToString());
            stringBuilder.Append(", ");
            stringBuilder.Append(DbHelper.ObjectNoName);
            stringBuilder.AppendLine(")");
        }
    }
}