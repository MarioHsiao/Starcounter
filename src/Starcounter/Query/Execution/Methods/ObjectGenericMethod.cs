// ***********************************************************************
// <copyright file="ObjectGenericMethod.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

#if false
using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
// At the moment only support of GetExtension&ltExtension&gt()-method is implemented.
/// <summary>
/// Class that holds information about a generic method with return type Object (object reference).
/// </summary>
internal class ObjectGenericMethod : IObjectPathItem, IMethod
{
    Int32 extentNumber;
    ITypeBinding typeBinding;
    ExtensionBinding extensionBinding;
    Int32 extIndex;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="extNum">The extent number to which this method belongs.
    /// If it does not belong to any extent number, which is the case for path expressions,
    /// then the number should be -1.</param>
    /// <param name="typeBind">The type rowTypeBind of the object to which this method belongs.</param>
    /// <param name="extBind">The extension rowTypeBind of the requested extension (GetExtension&ltExtension&gt()-method).</param>
    internal ObjectGenericMethod(Int32 extNum, ITypeBinding typeBind, ExtensionBinding extBind)
    : base()
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        if (extBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extBind.");
        }
        extentNumber = extNum;
        typeBinding = typeBind;
        extensionBinding = extBind;
        extIndex = extBind.Index;
    }

    /// <summary>
    /// The extent number of the extent to which this generic method belongs.
    /// </summary>
    public Int32 ExtentNumber
    {
        get
        {
            return extentNumber;
        }
    }

    /// <summary>
    /// Name to be displayed for example as column header in a result grid.
    /// </summary>
    public String Name
    {
        get
        {
            return ""; // TODO: Support method names for example in an IMethodBinding interface.
        }
    }

    /// <summary>
    /// Full name to uniquely identify the path.
    /// </summary>
    public String FullName
    {
        get
        {
            return ""; // TODO: Support method names for example in an IMethodBinding interface.
        }
    }

    /// <summary>
    /// The DbTypeCode of the return value of this method.
    /// </summary>
    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Object;
        }
    }
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Object;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return true;
    }

/// <summary>
    /// The type rowTypeBind of the return value of this method (of type object).
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            return extensionBinding;
        }
    }

    /// <summary>
    /// Calculates the return value of this method when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this method.</param>
    /// <returns>The return value of this method when evaluated on the input object.</returns>
    public IObjectView EvaluateToObject(IObjectView obj)
    {
        if (obj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj.");
        }
        if (obj is Row)
        {
            // Type control removed since type hierarchy and interfaces were not handled.
            // if ((obj.TypeBinding as RowTypeBinding).GetTypeBinding(extentNumber) == typeBinding)
            // {
            IObjectView partObj = (obj as Row).AccessObject(extentNumber);
            if (partObj == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "No elementary object at extent number: " + extentNumber);
            }
            return partObj.GetExtension(extIndex);
            // }
            // else
            //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object of incorrect type: " + obj.TypeBinding);
        }
        // else
        {
            // Type control removed since type hierarchy and interfaces were not handled.
            // if (obj.TypeBinding == typeBinding)
            return obj.GetExtension(extIndex);
            // else
            //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object of incorrect type: " + obj.TypeBinding);
        }
    }

    /// <summary>
    /// Calculates the value of the path-item when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate the expression.</param>
    /// <param name="startObj">The start object of the current path expression.</param>
    /// <returns>The value of the expression when evaluated on the input object.</returns>
    public IObjectView EvaluateToObject(IObjectView obj, IObjectView startObj)
    {
        return EvaluateToObject(obj);
    }

    /// <summary>
    /// Examines if the return value of the method is null when evaluated on an input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this method.</param>
    /// <returns>True, if the return value of this method when evaluated on the input object
    /// is null, otherwise false.</returns>
    public Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToObject(obj) == null);
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Properties, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public IObjectExpression Instantiate(Row obj)
    {
        if (obj != null && obj.AccessObject(extentNumber) != null)
        {
            return new ObjectLiteral(EvaluateToObject(obj));
        }
        else
        {
            return new ObjectGenericMethod(extentNumber, typeBinding, extensionBinding);
        }
    }

    public ITypeExpression Clone(VariableArray varArray)
    {
        return CloneToObject(varArray);
    }

    public IObjectExpression CloneToObject(VariableArray varArray)
    {
        return new ObjectGenericMethod(extentNumber, typeBinding, extensionBinding);
    }

    public void InstantiateExtentSet(ExtentSet extentSet)
    {
        //throw new NotImplementedException("InstantiateExtentSet is not implemented for ObjectGenericMethod");
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ObjectGenericMethod(");
        stringBuilder.AppendLine(tabs + 1, extentNumber.ToString());
        stringBuilder.Append(tabs + 1, "GetExtension<");
        stringBuilder.Append(extensionBinding.Name);
        stringBuilder.AppendLine(">");
        stringBuilder.AppendLine(tabs, ")");
    }

    // No implementation.
    public UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves, CodeGenFilterInstrArray instrArray, Int32 currentExtent, StringBuilder filterText)
    {
        throw new NotImplementedException("AppendToInstrAndLeavesList is not implemented for ObjectGenericMethod");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "BooleanMethod");
    }
}
}
#endif