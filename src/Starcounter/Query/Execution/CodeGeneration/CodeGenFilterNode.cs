// ***********************************************************************
// <copyright file="CodeGenFilterNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
    // Default implementation is for intermediate nodes.
    internal abstract class CodeGenFilterNode
    {
        /// <summary>
        /// The DbTypeCode of the value of the expression or the property.
        /// </summary>
        public virtual DbTypeCode DbTypeCode
        {
            get
            {
                throw new NotImplementedException("DbTypeCode is not implemented for CodeGenFilterNode");
            }
        }

        /// <summary>
        /// Gets if code gen can be applied to the node. For example, code properties and conditions involving them cannot be code gen.
        /// </summary>
        public abstract Boolean CanCodeGen { get; }
        
        /// <summary>
        /// Examines if the value of the expression is null when evaluated on an input object.
        /// </summary>
        /// <param name="obj">The object on which to evaluate the expression.</param>
        /// <returns>True, if the value of the expression when evaluated on the input object
        /// is null, otherwise false.</returns>
        public virtual Boolean EvaluatesToNull(IObjectView obj)
        {
            throw new NotImplementedException("EvaluatesToNull is not implemented for CodeGenFilterNode");
        }

        /// <summary>
        /// Updates the set of extents with all extents referenced in the current expression.
        /// </summary>
        /// <param name="extentSet">The set of extents to be updated.</param>
        public virtual void InstantiateExtentSet(ExtentSet extentSet)
        {
            //throw new NotImplementedException("InstantiateExtentSet is not implemented for CodeGenFilterNode");
        }

        /// <summary>
        /// Builds a string presentation of the expression using the input string-builder.
        /// </summary>
        /// <param name="stringBuilder">String-builder to use.</param>
        /// <param name="tabs">Number of tab indentations for the presentation.</param>
        public virtual void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        {
            throw new NotImplementedException("BuildString is not implemented for CodeGenFilterNode");
        }

        // Append this node to filter instructions and leaves.
        // Called statically so no need to worry about performance.
        // Need to redefine for leaves (this implementation is for intermediate nodes).
        public virtual UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                         CodeGenFilterInstrArray instrArray,
                                                         Int32 currentExtent,
                                                         StringBuilder filterText)
        {
            if (filterText != null)
            {
                filterText.Append(InstrCode() + ": " + CodeAsString() + "\n");
            }

            UInt32 newInstrCode = InstrCode();
            if (instrArray != null)
            {
                instrArray.Add(newInstrCode);
            }
            
            return StackChange();
        }

        // The following interface is used by binary nodes (comparison, logical).
        // Called statically so no need to worry about performance.
        public virtual UInt32 AppendToInstrAndLeavesList(IConditionTreeNode exprLeft,
                                                         IConditionTreeNode exprRight,
                                                         List<CodeGenFilterNode> dataLeaves,
                                                         CodeGenFilterInstrArray instrArray,
                                                         Int32 currentExtent,
                                                         StringBuilder filterText)
        {
            UInt32 stackChangeLeft = exprLeft.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText),
                   stackChangeRight = exprRight.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);

            if (filterText != null)
            {
                filterText.Append(InstrCode() + ": " + CodeAsString() + "\n");
            }
            
            UInt32 newInstrCode = InstrCode();
            if (instrArray != null)
            {
                instrArray.Add(newInstrCode);
            }

            // Returning total stack change.
            return stackChangeLeft + stackChangeRight + StackChange();
        }

        // Appends data of this leaf (if its a leaf) to the provided byte array.
        // Second parameter is a context object (or null if there is no).
        // By default does nothing.
        public virtual void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
        {
            throw new NotImplementedException("AppendToByteArray is not implemented for CodeGenFilterNode");
        }

        // String representation of this instruction.
        protected virtual String CodeAsString()
        {
            throw new NotImplementedException("CodeAsString is not implemented for CodeGenFilterNode");
        }

        // Instruction code value.
        protected virtual UInt32 InstrCode()
        {
            throw new NotImplementedException("InstrCode is not implemented for CodeGenFilterNode");
        }

        // Indicates stack changes caused by this instruction.
        // The following implementation is default for intermediate nodes.
        protected virtual UInt32 StackChange()
        {
            return 0;
        }

        // String representation of this comparison instruction.
        protected virtual String CodeAsStringGeneric(ComparisonOperator compOperator,
                                                     String comparisonType,
                                                     String comparisonPostfix)
        {
            switch (compOperator)
            {
                case ComparisonOperator.Equal:
                    return "EQ_" + comparisonPostfix;
                case ComparisonOperator.NotEqual:
                    return "NEQ_" + comparisonPostfix;
                case ComparisonOperator.LessThan:
                    return "LS_" + comparisonPostfix;
                case ComparisonOperator.LessThanOrEqual:
                    return "LSE_" + comparisonPostfix;
                case ComparisonOperator.GreaterThan:
                    return "GR_" + comparisonPostfix;
                case ComparisonOperator.GreaterThanOrEqual:
                    return "GRE_" + comparisonPostfix;
                case ComparisonOperator.IS:
                    return "IS_" + comparisonPostfix;
                case ComparisonOperator.ISNOT:
                    return "ISN_" + comparisonPostfix;
                case ComparisonOperator.LIKEstatic:
                    return "LIKEstatic_" + comparisonPostfix;
                case ComparisonOperator.LIKEdynamic:
                    return "LIKEdynamic_" + comparisonPostfix;
                default:
                    throw new NotImplementedException("CodeAsString is not implemented for " + comparisonType);
            }
        }

        // Instruction code of this comparison value.
        protected virtual UInt32 InstrCodeGeneric(ComparisonOperator compOperator,
                                                  UInt32 instrDataType,
                                                  String comparisonType)
        {
            switch (compOperator)
            {
                case ComparisonOperator.Equal:
                    return CodeGenFilterInstrCodes.EQ_BASE + instrDataType;
                case ComparisonOperator.NotEqual:
                    return CodeGenFilterInstrCodes.NEQ_BASE + instrDataType;
                case ComparisonOperator.LessThan:
                    return CodeGenFilterInstrCodes.LS_BASE + instrDataType;
                case ComparisonOperator.LessThanOrEqual:
                    return CodeGenFilterInstrCodes.LSE_BASE + instrDataType;
                case ComparisonOperator.GreaterThan:
                    return CodeGenFilterInstrCodes.GR_BASE + instrDataType;
                case ComparisonOperator.GreaterThanOrEqual:
                    return CodeGenFilterInstrCodes.GRE_BASE + instrDataType;
                case ComparisonOperator.IS:
                    throw new NotImplementedException("InstrCode()->IS is not implemented for " + comparisonType);
                case ComparisonOperator.ISNOT:
                    throw new NotImplementedException("InstrCode()->ISNOT is not implemented for " + comparisonType);
                case ComparisonOperator.LIKEstatic:
                    throw new NotImplementedException("InstrCode()->LIKEstatic is not implemented for " + comparisonType);
                case ComparisonOperator.LIKEdynamic:
                    throw new NotImplementedException("InstrCode()->LIKEdynamic is not implemented for " + comparisonType);
                default:
                    throw new NotImplementedException("InstrCode()->Default is not implemented for " + comparisonType);
            }
        }

        // Low level representation of the holding value (if any).
        protected Byte[] byteData = null;

        /// <summary>
        /// By default appends unsupported type to the node type list.
        /// </summary>
        /// <param name="nodeTypeList">List with condition nodes types.</param>
        public virtual void AddNodeTypeToList(List<ConditionNodeType> nodeTypeList)
        {
            nodeTypeList.Add(ConditionNodeType.Unsupported);
        }

        /// <summary>
        /// Adds operation node type to the node type list.
        /// </summary>
        /// <param name="compOperator">Comparison operator.</param>
        /// <param name="expr1">Left branch of the expression.</param>
        /// <param name="expr2">Right branch of the expression.</param>
        /// <param name="nodeTypeList">Node type list.</param>
        protected void AddNodeCompTypeToList(ComparisonOperator compOperator,
                                             IValueExpression expr1,
                                             IValueExpression expr2,
                                             List<ConditionNodeType> nodeTypeList)
        {
            // Checking sub-nodes if they are supported.
            if ((expr1 is CodeGenFilterNode) &&
                (expr2 is CodeGenFilterNode))
            {
                // Processing sub-nodes first.
                (expr1 as CodeGenFilterNode).AddNodeTypeToList(nodeTypeList);
                (expr2 as CodeGenFilterNode).AddNodeTypeToList(nodeTypeList);
            }
            else
            {
                // There is at least one unsupported sub-nodes.
                nodeTypeList.Add(ConditionNodeType.Unsupported);
                return;
            }

            // Adding this comparison node.
            switch (compOperator)
            {
                case ComparisonOperator.Equal:
                {
                    nodeTypeList.Add(ConditionNodeType.CompOpEqual);
                    return;
                }
                case ComparisonOperator.NotEqual:
                {
                    nodeTypeList.Add(ConditionNodeType.CompOpNotEqual);
                    return;
                }
                case ComparisonOperator.LessThan:
                {
                    nodeTypeList.Add(ConditionNodeType.CompOpLess);
                    return;
                }
                case ComparisonOperator.LessThanOrEqual:
                {
                    nodeTypeList.Add(ConditionNodeType.CompOpLessOrEqual);
                    return;
                }
                case ComparisonOperator.GreaterThan:
                {
                    nodeTypeList.Add(ConditionNodeType.CompOpGreater);
                    return;
                }
                case ComparisonOperator.GreaterThanOrEqual:
                {
                    nodeTypeList.Add(ConditionNodeType.CompOpGreaterOrEqual);
                    return;
                }
                default:
                {
                    nodeTypeList.Add(ConditionNodeType.Unsupported);
                    return;
                }
            }
        }
    }
}