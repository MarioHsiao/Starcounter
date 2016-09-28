// ***********************************************************************
// <copyright file="NumericalProperty.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
    /// <summary>
    /// Class that holds information about a property of type integer.
    /// </summary>
    internal class NumericalProperty : Property, INumericalExpression
    {
        // Exact type of this property.
        DbTypeCode dbTypeCode;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="extNum">The extent number to which this property belongs.
        /// If it does not belong to any extent number, which is the case for path expressions,
        /// then the number should be -1.</param>
        /// <param name="typeBind">The type binding of the object to which this property belongs.</param>
        /// <param name="propBind">The property binding of this property.</param>
        internal NumericalProperty(Int32 extNum, ITypeBinding typeBind, IPropertyBinding propBind)
        {
            if (typeBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
            }

            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect propBind.");
            }

            // Checking if provided type is correct.
            if (!IsNumericalType(propBind.TypeCode))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Property of incorrect type.");
            }

            // Obtaining the type of the property.
            dbTypeCode = propBind.TypeCode;
            extentNumber = extNum;
            typeBinding = typeBind;
            propBinding = propBind;
            propIndex = propBind.Index;
        }

        // Check if provided type is numerical.
        internal static Boolean IsNumericalType(DbTypeCode origTypeCode)
        {
            switch (origTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:

                case DbTypeCode.Double:
                case DbTypeCode.Decimal:
                case DbTypeCode.Single:
                    return true;

                default:
                    return false;
            }
        }

        // Remove?
        public QueryTypeCode QueryTypeCode
        {
            get
            {
                switch (dbTypeCode)
                {
                    case DbTypeCode.SByte:
                    case DbTypeCode.Int16:
                    case DbTypeCode.Int32:
                    case DbTypeCode.Int64:
                        return QueryTypeCode.Integer;

                    case DbTypeCode.Byte:
                    case DbTypeCode.UInt16:
                    case DbTypeCode.UInt32:
                    case DbTypeCode.UInt64:
                        return QueryTypeCode.UInteger;

                    case DbTypeCode.Decimal:
                        return QueryTypeCode.Decimal;

                    case DbTypeCode.Double:
                    case DbTypeCode.Single:
                        return QueryTypeCode.Double;

                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
                }
            }
        }

        // String representation of this instruction.
        protected override String CodeAsString()
        {
            // Checking if its a property from some previous extent.
            // Returning code string as for a data or a property.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                    {
                        if (propFromPreviousExtent)
                        {
                            return "LDV_SINT";
                        }
                        return "LDA_SINT " + DataIndex;
                    }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                    {
                        if (propFromPreviousExtent)
                        {
                            return "LDV_UINT";
                        }
                        return "LDA_UINT " + DataIndex;
                    }

                case DbTypeCode.Decimal:
                    {
                        if (propFromPreviousExtent)
                        {
                            return "LDV_DEC";
                        }
                        return "LDA_DEC " + DataIndex;
                    }

                case DbTypeCode.Double:
                    {
                        if (propFromPreviousExtent)
                        {
                            return "LDV_FLT8";
                        }
                        return "LDA_FLT8 " + DataIndex;
                    }

                case DbTypeCode.Single:
                    {
                        if (propFromPreviousExtent)
                        {
                            return "LDV_FLT4";
                        }
                        return "LDA_FLT4 " + DataIndex;
                    }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        // Instruction code value.
        protected override UInt32 InstrCode()
        {
            // Checking if its a property from some previous extent.
            // Returning code as for a data or a property.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    if (propFromPreviousExtent)
                    {
                        return CodeGenFilterInstrCodes.LDV_SINT;
                    }
                    return CodeGenFilterInstrCodes.LDA_SINT | ((UInt32) DataIndex << 8);
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    if (propFromPreviousExtent)
                    {
                        return CodeGenFilterInstrCodes.LDV_UINT;
                    }
                    return CodeGenFilterInstrCodes.LDA_UINT | ((UInt32) DataIndex << 8);
                }

                case DbTypeCode.Decimal:
                {
                    if (propFromPreviousExtent)
                    {
                        return CodeGenFilterInstrCodes.LDV_DEC;
                    }
                    return CodeGenFilterInstrCodes.LDA_DEC | ((UInt32) DataIndex << 8);
                }

                case DbTypeCode.Double:
                {
                    if (propFromPreviousExtent)
                    {
                        return CodeGenFilterInstrCodes.LDV_FLT8;
                    }
                    return CodeGenFilterInstrCodes.LDA_FLT8 | ((UInt32) DataIndex << 8);
                }

                case DbTypeCode.Single:
                {
                    if (propFromPreviousExtent)
                    {
                        return CodeGenFilterInstrCodes.LDV_FLT4;
                    }
                    return CodeGenFilterInstrCodes.LDA_FLT4 | ((UInt32) DataIndex << 8);
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Appends data of this leaf to the provided filter key.
        /// </summary>
        /// <param name="key">Reference to the filter key to which data should be appended.</param>
        /// <param name="obj">Row for which evaluation should be performed.</param>
        public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
        {
            // Checking if its a property from some previous extent
            // and if yes calculate its data (otherwise do nothing).
            if (propFromPreviousExtent)
            {
                switch (dbTypeCode)
                {
                    case DbTypeCode.SByte:
                    case DbTypeCode.Int16:
                    case DbTypeCode.Int32:
                    case DbTypeCode.Int64:
                        key.Append(EvaluateToInteger(obj));
                    break;

                    case DbTypeCode.Byte:
                    case DbTypeCode.UInt16:
                    case DbTypeCode.UInt32:
                    case DbTypeCode.UInt64:
                        key.Append(EvaluateToUInteger(obj));
                    break;

                    case DbTypeCode.Decimal:
                        key.Append(EvaluateToDecimal(obj));
                    break;

                    case DbTypeCode.Double:
                    case DbTypeCode.Single:
                        key.Append(EvaluateToDouble(obj));
                    break;

                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
                }
            }
        }

        /// <summary>
        /// Examines if the value of the property is null when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate the property.</param>
        /// <returns>True, if the value of the property when evaluated on the input object
        /// is null, otherwise false.</returns>
        public override Boolean EvaluatesToNull(IObjectView obj)
        {
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                    return (EvaluateToInteger(obj) == null);

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                    return (EvaluateToUInteger(obj) == null);

                case DbTypeCode.Decimal:
                    return (EvaluateToDecimal(obj) == null);

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                    return (EvaluateToDouble(obj) == null);

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }
        
        internal IObjectView GetActualObject(IObjectView contextObj)
        {
            if (contextObj == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Context object value is not set.");
            }

            if (contextObj is Row)
            {
                // Accessing object of the needed extent.
                IObjectView subObj = (contextObj as Row).AccessObject(extentNumber);

                // Checking if object from a certain extent exists.
                if (subObj == null)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "No elementary object at extent number: " + extentNumber);
                }

                return subObj;
            }

            return contextObj;
        }

        /// <summary>
        /// Calculates the value of this property when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate this property.</param>
        /// <returns>The value of this property when evaluated on the input object.</returns>
        public Nullable<Int64> EvaluateToInteger(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    return actualObj.GetInt64(propIndex);
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    Nullable<UInt64> value = actualObj.GetUInt64(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value > Int64.MaxValue)
                    {
                        return null;
                    }
                    return (Int64) value.Value;
                }

                case DbTypeCode.Decimal:
                {
                    Nullable<Decimal> value = actualObj.GetDecimal(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    Decimal roundedValue = Math.Round(value.Value);
                    if (roundedValue < (Decimal) Int64.MinValue)
                    {
                        return null;
                    }
                    if (roundedValue > (Decimal) Int64.MaxValue)
                    {
                        return null;
                    }
                    return (Int64) roundedValue;
                }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                {
                    Nullable<Double> value = actualObj.GetDouble(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    Double roundedValue = Math.Round(value.Value);
                    if (roundedValue < (Double) Int64.MinValue)
                    {
                        return null;
                    }
                    if (roundedValue > (Double) Int64.MaxValue)
                    {
                        return null;
                    }
                    return (Int64) roundedValue;
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }
        
        /// <summary>
        /// Calculates the value of this property when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate this property.</param>
        /// <returns>The value of this property when evaluated on the input object.</returns>
        public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    Nullable<Int64> value = actualObj.GetInt64(propIndex);
                    if (value == null)
                    {
                        return null;
                    }

                    if (value.Value < (Decimal) UInt64.MinValue)
                    {
                        return null;
                    }
                    return (UInt64) value.Value;
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    return actualObj.GetUInt64(propIndex);
                }

                case DbTypeCode.Decimal:
                {
                    Nullable<Decimal> value = actualObj.GetDecimal(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    Decimal roundedValue = Math.Round(value.Value);
                    if (roundedValue < (Decimal) UInt64.MinValue)
                    {
                        return null;
                    }
                    if (roundedValue > (Decimal) UInt64.MaxValue)
                    {
                        return null;
                    }
                    return (UInt64) roundedValue;
                }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                {
                    Nullable<Double> value = actualObj.GetDouble(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    Double roundedValue = Math.Round(value.Value);
                    if (roundedValue < (Double) UInt64.MinValue)
                    {
                        return null;
                    }
                    if (roundedValue > (Double) UInt64.MaxValue)
                    {
                        return null;
                    }
                    return (UInt64) roundedValue;
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Calculates the value of this property as a nullable Decimal.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Decimal> EvaluateToDecimal(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    return actualObj.GetInt64(propIndex);
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    return actualObj.GetUInt64(propIndex);
                }

                case DbTypeCode.Decimal:
                {
                    return actualObj.GetDecimal(propIndex);
                }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                {
                    return (Nullable<Decimal>) actualObj.GetDouble(propIndex);
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Calculates the value of this property as a nullable Double.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Double> EvaluateToDouble(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    return actualObj.GetInt64(propIndex);
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    return actualObj.GetUInt64(propIndex);
                }

                case DbTypeCode.Decimal:
                {
                    return (Nullable<Double>)actualObj.GetDecimal(propIndex);
                }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                {
                    return actualObj.GetDouble(propIndex);
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Calculates the value of this property as a ceiling (round up) nullable Int64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    return actualObj.GetInt64(propIndex);
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    Nullable<UInt64> value = actualObj.GetUInt64(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value > Int64.MaxValue)
                    {
                        return null;
                    }
                    return (Int64) value.Value;
                }

                case DbTypeCode.Decimal:
                {
                    Nullable<Decimal> value = actualObj.GetDecimal(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value < (Decimal) Int64.MinValue)
                    {
                        return Int64.MinValue;
                    }
                    if (value.Value > (Decimal) Int64.MaxValue)
                    {
                        return null;
                    }
                    return (Int64) Math.Ceiling(value.Value);
                }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                {
                    Nullable<Double> value = actualObj.GetDouble(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value < (Double) Int64.MinValue)
                    {
                        return Int64.MinValue;
                    }
                    if (value.Value > (Double) Int64.MaxValue)
                    {
                        return null;
                    }
                    return (Int64) Math.Ceiling(value.Value);
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Calculates the value of this property as a floor (round down) nullable Int64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    return actualObj.GetInt64(propIndex);
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    Nullable<UInt64> value = actualObj.GetUInt64(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value > Int64.MaxValue)
                    {
                        return Int64.MaxValue;
                    }
                    return (Int64) value.Value;
                }

                case DbTypeCode.Decimal:
                {
                    Nullable<Decimal> value = actualObj.GetDecimal(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value < (Decimal) Int64.MinValue)
                    {
                        return null;
                    }
                    if (value.Value > (Decimal) Int64.MaxValue)
                    {
                        return Int64.MaxValue;
                    }
                    return (Int64) Math.Floor(value.Value);
                }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                {
                    Nullable<Double> value = actualObj.GetDouble(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value < (Double) Int64.MinValue)
                    {
                        return null;
                    }
                    if (value.Value > (Double) Int64.MaxValue)
                    {
                        return Int64.MaxValue;
                    }
                    return (Int64) Math.Floor(value.Value);
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Calculates the value of this property as a ceiling (round up) nullable UInt64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    Nullable<Int64> value = actualObj.GetInt64(propIndex);
                    if (value == null)
                    {
                        return null;
                    }

                    if (value.Value < (Decimal) UInt64.MinValue)
                    {
                        return UInt64.MinValue;
                    }
                    return (UInt64) value.Value;
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    return actualObj.GetUInt64(propIndex);
                }

                case DbTypeCode.Decimal:
                {
                    Nullable<Decimal> value = actualObj.GetDecimal(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value < (Decimal) UInt64.MinValue)
                    {
                        return UInt64.MinValue;
                    }
                    if (value.Value > (Decimal) UInt64.MaxValue)
                    {
                        return null;
                    }
                    return (UInt64) Math.Ceiling(value.Value);
                }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                {
                    Nullable<Double> value = actualObj.GetDouble(propIndex);
                    if (value == null)
                    {
                        return null;
                    }
                    if (value.Value < (Double) UInt64.MinValue)
                    {
                        return UInt64.MinValue;
                    }
                    if (value.Value > (Double) UInt64.MaxValue)
                    {
                        return null;
                    }
                    return (UInt64) Math.Ceiling(value.Value);
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Calculates the value of this property as a floor (round down) nullable UInt64.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this property.</returns>
        public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
        {
            IObjectView actualObj = GetActualObject(obj);

            // Accessing certain data field of the object.
            switch (dbTypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                {
                    Nullable<Int64> value = actualObj.GetInt64(propIndex);
                    if (value == null)
                    {
                        return null;
                    }

                    if (value.Value < (Decimal) UInt64.MaxValue)
                    {
                        return null;
                    }
                    return (UInt64) value.Value;
                }

                case DbTypeCode.Byte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                {
                    return actualObj.GetUInt64(propIndex);
                }

                case DbTypeCode.Decimal:
                    {
                        Nullable<Decimal> value = actualObj.GetDecimal(propIndex);
                        if (value == null)
                        {
                            return null;
                        }
                        if (value.Value < (Decimal) UInt64.MinValue)
                        {
                            return null;
                        }
                        if (value.Value > (Decimal) UInt64.MaxValue)
                        {
                            return UInt64.MaxValue;
                        }
                        return (UInt64) Math.Floor(value.Value);
                    }

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                    {
                        Nullable<Double> value = actualObj.GetDouble(propIndex);
                        if (value == null)
                        {
                            return null;
                        }
                        if (value.Value < (Double) UInt64.MinValue)
                        {
                            return null;
                        }
                        if (value.Value > (Double) UInt64.MaxValue)
                        {
                            return Int64.MaxValue;
                        }
                        return (UInt64) Math.Floor(value.Value);
                    }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
            }
        }

        /// <summary>
        /// Creates an more instantiated copy of this expression by evaluating it on a Row.
        /// Properties, with extent numbers for which there exist objects attached to the Row,
        /// are evaluated and instantiated to literals, other properties are not changed.
        /// </summary>
        /// <param name="obj">The Row on which to evaluate the expression.</param>
        /// <returns>A more instantiated expression.</returns>
        public INumericalExpression Instantiate(Row obj)
        {
            if ((obj != null) && (extentNumber >= 0) && (obj.AccessObject(extentNumber) != null))
            {
                // a_m-TODO: Resolve.
                return new IntegerLiteral(EvaluateToInteger(obj));
            }

            return new NumericalProperty(extentNumber, typeBinding, propBinding);
        }

        public override IValueExpression Clone(VariableArray varArray)
        {
            return CloneToNumerical(varArray);
        }

        public INumericalExpression CloneToNumerical(VariableArray varArray)
        {
            return new NumericalProperty(extentNumber, typeBinding, propBinding);
        }

        /// <summary>
        /// Builds a string presentation of this property using the input string-builder.
        /// </summary>
        /// <param name="stringBuilder">String-builder to use.</param>
        /// <param name="tabs">Number of tab indentations for the presentation.</param>
        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        {
            stringBuilder.Append(tabs, "NumericalProperty(");
            stringBuilder.Append(dbTypeCode.ToString());
            stringBuilder.Append(", ");
            stringBuilder.Append(extentNumber.ToString());
            stringBuilder.Append(", ");
            stringBuilder.Append(propBinding.Name);
            stringBuilder.AppendLine(")");
        }

        /// <summary>
        /// Generates compilable code representation of this data structure.
        /// </summary>
        public override void GenerateCompilableCode(CodeGenStringGenerator stringGen)
        {
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetNumericalProperty();");
        }
    }
}

