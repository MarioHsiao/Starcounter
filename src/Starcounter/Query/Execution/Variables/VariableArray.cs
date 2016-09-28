// ***********************************************************************
// <copyright file="VariableArray.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Globalization;
using Starcounter.Query.Execution;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
// TODO: Change name of this class to something more general like QueryMetaData.
/// <summary>
/// Class that represents a list of variables used in an SQL statement.
/// Also holds some flags describing the SQL statement.
/// </summary>
internal class VariableArray
{
    UInt64 transactionId = 0; // The id of the transaction to which this ExecutionEnumerator belongs.
    IVariable[] variableArray = null; // Array containing all variables.
    Int32 varsNum = 0; // Number of variables in the array.
    internal QueryFlags QueryFlags; // Flags describing whether the query includes literal, aggregation etc.
    internal String LiteralValue = null;
    internal Boolean FailedToRecreateObject = false; // During index position recreation: True if current object for some extent could not be recreated, otherwise false.
    //internal unsafe Byte* RecreationKeyData = null; // Pointer to recreation key data.

    /// <summary>
    /// Constructor.
    /// </summary>
    internal VariableArray(Int32 varNum)
    {
        varsNum = varNum;

        if (varsNum > 0)
        {
            variableArray = new IVariable[varsNum];
        }

        QueryFlags = QueryFlags.None;
    }

    /// <summary>
    /// Sets the transaction handle value.
    /// </summary>
    public UInt64 TransactionId
    {
        get { return transactionId; }
        set { transactionId = value; }
    }

    /// <summary>
    /// The length of the variable list.
    /// </summary>
    internal Int32 Length
    {
        get
        {
            return varsNum;
        }
    }

    /// <summary>
    /// Prolongs all variable values to given execution enumerator.
    /// </summary>
    public void ProlongValues(IExecutionEnumerator destEnum)
    {
        for (Int32 i = 0; i < varsNum; i++)
            variableArray[i].ProlongValue(destEnum);
    }

    /// <summary>
    /// Creates an empty copy of current variable array.
    /// </summary>
    internal VariableArray CloneEmpty()
    {
        VariableArray varArrClone = new VariableArray(varsNum);

        // Creating individual variables.
        for (Int32 i = 0; i < varsNum; i++)
        {
            IVariable v = variableArray[i];

            if (v is NumericalVariable)
                varArrClone.SetElement(i, new NumericalVariable(i, (v as NumericalVariable).DbTypeCode));
            else if (v is StringVariable)
                varArrClone.SetElement(i, new StringVariable(i));
            else if (v is DateTimeVariable)
                varArrClone.SetElement(i, new DateTimeVariable(i));
            else if (v is ObjectVariable)
                varArrClone.SetElement(i, new ObjectVariable(i, (v as ObjectVariable).TypeBinding));
            else if (v is BooleanVariable)
                varArrClone.SetElement(i, new BooleanVariable(i));
            else if (v is BinaryVariable)
                varArrClone.SetElement(i, new BinaryVariable(i));
        }

        return varArrClone;
    }

    /// <summary>
    /// Resets the variable array shared data.
    /// </summary>
    internal void Reset()
    {
        //unsafe { RecreationKeyData = null; }
        FailedToRecreateObject = false;
    }

    /// <summary>
    /// For debug purposes to control the variable list is correct and completed.
    /// </summary>
    internal Boolean Completed
    {
        get
        {
            for (Int32 i = 0; i < varsNum; i++)
            {
                if (variableArray[i] == null || variableArray[i].Number != i)
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Assigning variable directly.
    /// </summary>
    /// <param name="index">Index of the variable in the array.</param>
    /// <param name="variable">Reference to a replacing variable.</param>
    internal void SetElement(Int32 index, IVariable variable)
    {
        if (index < 0 || index >= varsNum)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index: " + index);
        }
        if (variableArray[index] != null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Variable already placed at index: " + index);
        }
        variableArray[index] = variable;
    }

    /// <summary>
    /// Retrieves variable from certain position in the array.
    /// </summary>
    /// <param name="index">Position in the array.</param>
    /// <returns>Reference to a variable at index.</returns>
    internal IVariable GetElement(Int32 index)
    {
        return variableArray[index];
    }

    /// <summary>
    /// Generates compilable code for handling variables.
    /// </summary>
    internal static void GenerateQueryParamsCode(CodeGenStringGenerator stringGen, VariableArray varArray)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.DECLARATIONS, "INTERNAL_FUNCTION INT32 ProcessQueryParameters(UINT8 *paramsData);");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "INTERNAL_FUNCTION INT32 ProcessQueryParameters(UINT8 *paramsData)");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 4; // Skipping first bytes of total length." + CodeGenStringGenerator.ENDL);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "// Chopping query parameters array into individual global variables.");

        if (varArray.Length > 0)
        {
            stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "// Defining individual query parameters here.");

            // Going throw each variable and generating code for it.
            for (Int32 i = 0; i < varArray.Length; i++)
            {
                stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA BOOL g_IsNullQueryParam" + i + " = false;");

                DbTypeCode type = varArray.GetElement(i).DbTypeCode;
                switch (type)
                {
                    case DbTypeCode.Boolean:
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA BOOL g_QueryParam" + i + " = false;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "if (*paramsData != 0)");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_QueryParam" + i + " = (*(BOOL *) (paramsData + 1));");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 8;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = false;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "else");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = true;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData++;");
                        break;

                    case DbTypeCode.Byte:
                    case DbTypeCode.DateTime:
                    case DbTypeCode.UInt64:
                    case DbTypeCode.UInt32:
                    case DbTypeCode.UInt16:
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA UINT64 g_QueryParam" + i + " = 0;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "if (*paramsData != 0)");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_QueryParam" + i + " = (*(UINT64 *) (paramsData + 1));");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 8;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = false;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "else");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = true;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData++;");
                        break;

                    case DbTypeCode.Int64:
                    case DbTypeCode.Int32:
                    case DbTypeCode.Int16:
                    case DbTypeCode.SByte:
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA INT64 g_QueryParam" + i + " = 0;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "if (*paramsData != 0)");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_QueryParam" + i + " = (*(INT64 *) (paramsData + 1));");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 8;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = false;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "else");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = true;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData++;");
                        break;

                    case DbTypeCode.Single:
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA FLOAT g_QueryParam" + i + " = 0.0f;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "if (*paramsData != 0)");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_QueryParam" + i + " = (*(FLOAT *) (paramsData + 1));");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 8;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = false;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "else");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = true;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData++;");
                        break;

                    case DbTypeCode.Double:
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA DOUBLE g_QueryParam" + i + " = 0.0d;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "if (*paramsData != 0)");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_QueryParam" + i + " = (*(DOUBLE *) (paramsData + 1));");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 8;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = false;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "else");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = true;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData++;");
                        break;

                    case DbTypeCode.String:
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA WCHAR *g_QueryParam" + i + " = 0;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "if (*paramsData != 0)");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "INT32 stringLen = (*(INT32 *) (paramsData + 1));");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 5;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_QueryParam" + i + " = ((WCHAR *) paramsData);");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData += 8;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = false;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "else");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "g_IsNullQueryParam" + i + " = true;");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}");
                        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "paramsData++;");
                        break;

                    case DbTypeCode.Decimal:
                    case DbTypeCode.Object:
                    case DbTypeCode.Binary:
                    //case DbTypeCode.Objects:
                    //    stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA VOID *g_QueryParam" + i + " = 0;");
                    //    break;

                    default:
                        throw new ArgumentException("Incorrect query variable " + i + " with type " + type.ToString() + " for code generation.");
                }

                stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, CodeGenStringGenerator.ENDL);
            }
        }

        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return 0;");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);
    }

    /// <summary>
    /// Used for debug purposes.
    /// </summary>
    /// <returns>A string representation of the variable values.</returns>
    public override String ToString()
    {
        String output = "Variable values: ";

        String type = null;
        String value = null;
        for (Int32 i = 0; i < Length; i++)
        {
            type = variableArray[i].DbTypeCode.ToString();
            value = variableArray[i].ToString();
            output += type + ":" + value + "; ";
        }

        return output;
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    // Note not all properties are compared.
    internal bool AssertEquals(VariableArray other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check if there are not cyclic references
        Debug.Assert(!this.AssertEqualsVisited);
        if (this.AssertEqualsVisited)
            return false;
        Debug.Assert(!other.AssertEqualsVisited);
        if (other.AssertEqualsVisited)
            return false;
        // Check basic types
        Debug.Assert(this.varsNum == other.varsNum);
        if (this.varsNum != other.varsNum)
            return false;
        Debug.Assert(this.QueryFlags == other.QueryFlags);
        if (this.QueryFlags != other.QueryFlags)
            return false;
        // Check cardinalities of collections
        if (this.variableArray == null) {
            Debug.Assert(other.variableArray == null);
            if (other.variableArray != null)
                return false;
        } else {
            Debug.Assert(this.variableArray.Length == other.variableArray.Length);
            if (this.variableArray.Length != other.variableArray.Length)
                return false;
        }
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        // Check collections of objects
        if (this.variableArray != null)
            for (int i = 0; i < this.variableArray.Length && areEquals; i++)
                if (this.variableArray[i] == null) {
                    Debug.Assert(other.variableArray[i] == null);
                    areEquals = other.variableArray[i] == null;
                } else
                    areEquals = this.variableArray[i].AssertEquals(other.variableArray[i]);
        AssertEqualsVisited = false;
        return areEquals;

    }
#endif
}
}
