using System;
using System.Diagnostics;
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;

namespace Starcounter.Query.RawParserAnalyzer
{
    internal partial class ParserAnalyzer
    {
        /// <summary>
        /// Analyze select statement and creates structures for optimizer.
        /// It replaces part of OptimizeAndCreateEnumerator, CreateNodeTree and
        /// others from _Creator.cs
        /// </summary>
        /// <param name="stmt"></param>
        internal unsafe void AnalyzeSelectStmt(SelectStmt* stmt)
        {
            // Read and assert the input tree
            Debug.Assert(JoinTree == null && WhereCondition == null && VarArray == null, "The variables for the result of analyzer should be reset.");
            // Let's go through FROM clause first
            List* fromClause = stmt->fromClause;
            // Assume only one relation in from clause
            SQLParserAssert(fromClause->length == 1, "Assuming one relation in from clause only");
            SQLParserAssert(((Node*)fromClause->head->data.ptr_value)->type == NodeTag.T_RangeVar, "Expected T_RangeVar, but got " + ((Node*)fromClause->head->data.ptr_value)->type.ToString());
            RangeVar* extent = (RangeVar*)fromClause->head->data.ptr_value;
            List* selectClause = stmt->targetList;
            SQLParserAssert(selectClause->length == 1, "Assuming projection of an alias");
            SQLParserAssert(((Node*)selectClause->head->data.ptr_value)->type == NodeTag.T_ResTarget, "Expected T_ResTarget, but got " + ((Node*)selectClause->head->data.ptr_value)->type.ToString());
            ResTarget* target = (ResTarget*)selectClause->head->data.ptr_value;
            SQLParserAssert(target->name == null);
            SQLParserAssert(target->val->type == NodeTag.T_List, "Expected T_List, but got " + target->val->type.ToString());
            SQLParserAssert(((List*)target->val)->length == 1, "Expected list with one element - alias access");
            SQLParserAssert(((Node*)((List*)target->val)->head->data.ptr_value)->type == NodeTag.T_ColumnRef, "Expected T_ColumnRef, but got " + 
                ((Node*)((List*)target->val)->head->data.ptr_value)->type.ToString());
            ColumnRef* col = (ColumnRef*)((List*)target->val)->head->data.ptr_value;
            SQLParserAssert(col->name != null, "Assuming alias name");
            //SQLParserAssert(val->type == NodeTag.T_String, "Expected T_String, but got " + val->type.ToString());
            SQLParserAssert(extent->alias != null, "Assuming that alias is given after the extent name");
            SQLParserAssert(new String(extent->alias->aliasname) == new String(col->name), "Assuming that aliases are equivalent");
            SQLParserAssert(stmt->sortClause == null, "Assuming no order by");
            SQLParserAssert(stmt->whereClause == null, "Assuming no where clause");
            SQLParserAssert(stmt->optionClause == null, "Assuming no option clause with optimizer hints");
            // Creating output structures
            RowTypeBinding typeBindings = new RowTypeBinding();
            Int32 extNum = 0;
            TypeBinding extType = GetTypeBindingFor(extent);
            typeBindings.AddTypeBinding(extType);
            // Add projection to typebinding
            ITypeExpression propExpr = new ObjectThis(extNum, extType);
            typeBindings.AddPropertyMapping(extNum.ToString(), propExpr);
            VarArray = new VariableArray(0);
            if ((typeBindings.PropertyCount == 1) && (typeBindings.GetPropertyBinding(0).TypeCode == DbTypeCode.Object))
                VarArray.QueryFlags = VarArray.QueryFlags | QueryFlags.SingletonProjection;
            JoinTree = new ExtentNode(typeBindings, 0, VarArray, Query);
            WhereCondition = new ConditionDictionary();
            ILogicalExpression whereCond = new LogicalLiteral(TruthValue.TRUE);
            WhereCondition.AddCondition(whereCond);
            HintSpec = new HintSpecification();
        }
    }
}
