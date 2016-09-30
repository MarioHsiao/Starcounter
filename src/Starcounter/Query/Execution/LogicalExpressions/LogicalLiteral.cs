// ***********************************************************************
// <copyright file="LogicalLiteral.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;
using System.Collections;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
    /// <summary>
    /// Class that holds information about a literal of type logical (TruthValue).
    /// </summary>
    internal class LogicalLiteral : Literal, ILogicalExpression
    {
        TruthValue value;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The value of this literal.</param>
        internal LogicalLiteral(TruthValue value)
        {
            this.value = value;
        }

        /// <summary>
        /// Calculates the truth value of this literal.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The truth value of this literal.</returns>
        public TruthValue Evaluate(IObjectView obj)
        {
            return value;
        }

        /// <summary>
        /// Calculates the Boolean value of this literal.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The Boolean value of this literal.</returns>
        public Boolean Filtrate(IObjectView obj)
        {
            return Evaluate(obj) == TruthValue.TRUE;
        }

        /// <summary>
        /// Creates a copy of this literal.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>A copy of this literal.</returns>
        public ILogicalExpression Instantiate(Row obj)
        {
            return new LogicalLiteral(value);
        }

        public ILogicalExpression Clone(VariableArray var_array)
        {
            return this;
        }

        public ExtentSet GetOutsideJoinExtentSet()
        {
            return null;
        }

        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        {
            stringBuilder.Append(tabs, "LogicalValue(");
            stringBuilder.Append(value.ToString());
            stringBuilder.AppendLine(")");
        }

        // String representation of this instruction.
        protected override String CodeAsString()
        {
            return "LDV_UINT_LIT";
        }

        // Instruction code value.
        protected override UInt32 InstrCode()
        {
            return CodeGenFilterInstrCodes.LDV_UINT;
        }

        /// <summary>
        /// Appends data of this leaf to the provided filter key.
        /// </summary>
        /// <param name="key">Reference to the filter key to which data should be appended.</param>
        /// <param name="obj">Row for which evaluation should be performed.</param>
        public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
        {
            if (value == TruthValue.FALSE)
            {
                key.Append((UInt64)0);
            }
            else if (value == TruthValue.TRUE)
            {
                key.Append((UInt64)1);
            }
            else // Unknown value (should be ((DWORD)-1) according to CodeGen spec).
            {
                key.Append(unchecked((UInt64)(-1)));
            }
        }

        /// <summary>
        /// The DbTypeCode of the value of the expression or the property.
        /// </summary>
        public override DbTypeCode DbTypeCode
        {
            get { throw new NotImplementedException("DbTypeCode is not implemented for LogicalLiteral"); }
        }

        /// <summary>
        /// Examines if the value of the expression is null when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate the expression.</param>
        /// <returns>True, if the value of the expression when evaluated on the input object
        /// is null, otherwise false.</returns>
        public override Boolean EvaluatesToNull(IObjectView obj)
        {
            throw new NotImplementedException("EvaluatesToNull is not implemented for LogicalLiteral");
        }

        /// <summary>
        /// Generates compilable code representation of this data structure.
        /// </summary>
        public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
        {
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, value.ToString());
        }

#if DEBUG
        public bool AssertEquals(ILogicalExpression other) {
            LogicalLiteral otherNode = other as LogicalLiteral;
            Debug.Assert(otherNode != null);
            return this.AssertEquals(otherNode);
        }
        internal bool AssertEquals(LogicalLiteral other) {
            Debug.Assert(other != null);
            if (other == null)
                return false;
            // Check basic types
            Debug.Assert(this.value == other.value);
            if (this.value != other.value)
                return false;
            return true;
        }
#endif
    }
}
