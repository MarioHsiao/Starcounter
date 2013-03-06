#if __LINE__
#pragma once
#define internal
#endif

internal 
enum NodeTag
{
	T_Invalid = 0,

	/*
	 * TAGS FOR PRIMITIVE NODES (primnodes.h)
	 */
	T_Alias = 300,
	T_RangeVar,
	T_NamedArgExpr,
	T_SubLink,
	T_CaseExpr,
	T_CaseWhen,
	T_RowExpr,
	T_CoalesceExpr,
	T_MinMaxExpr,
	T_XmlExpr,
	T_NullTest,
	T_BooleanTest,
	T_SetToDefault,
	T_JoinExpr,
	T_IntoClause,
    T_LimitOffset,
    T_HintJoinOrder,
    T_HintIndex,

	/*
	 * TAGS FOR MEMORY NODES (memnodes.h)
	 */
	T_AllocSetContext = 600,

	/*
	 * TAGS FOR VALUE NODES (value.h)
	 */
	T_Value = 650,
	T_Integer,
	T_Float,
	T_String,
    T_Binary,
    T_Object,
	T_BitString,
	T_Null,

	/*
	 * TAGS FOR LIST NODES (pg_list.h)
	 */
	T_List,
	T_IntList,
	T_OidList,

	/*
	 * TAGS FOR STATEMENT NODES (mostly in parsenodes.h)
	 */
	T_InsertStmt = 700,
	T_DeleteStmt,
	T_UpdateStmt,
	T_SelectStmt,
	T_AlterTableStmt,
	T_AlterTableCmd,
	T_AlterDomainStmt,
	T_SetOperationStmt,
	T_GrantStmt,
	T_GrantRoleStmt,
	T_CreateStmt,
	T_DefineStmt,
	T_DropStmt,
    T_DropIndexStmt,
	T_TruncateStmt,
	T_IndexStmt,
	T_TransactionStmt,
	T_ViewStmt,
	T_CreateDomainStmt,
	T_CreatedbStmt,
	T_DropdbStmt,
	T_ExplainStmt,
	T_CreateSeqStmt,
	T_AlterSeqStmt,
	T_VariableSetStmt,
	T_VariableShowStmt,
	T_DiscardStmt,
	T_CreateTrigStmt,
	T_DropPropertyStmt,
	T_CreateRoleStmt,
	T_DropRoleStmt,
	T_LockStmt,
	T_ConstraintsSetStmt,
	T_ReindexStmt,
	T_CheckPointStmt,
	T_AlterDatabaseStmt,
	T_AlterDatabaseSetStmt,
	T_PrepareStmt,
	T_ExecuteStmt,
	T_DeallocateStmt,
	T_DropOwnedStmt,
	T_ReassignOwnedStmt,
	T_SecLabelStmt,

	/*
	 * TAGS FOR PARSE TREE NODES (parsenodes.h)
	 */
	T_A_Expr = 900,
	T_ColumnRef,
	T_ParamRef,
	T_A_Const,
	T_FuncCall,
	T_A_Star,
	T_A_Indices,
	T_A_Indirection,
	T_A_ArrayExpr,
	T_ResTarget,
	T_TypeCast,
	T_CollateClause,
	T_SortBy,
	T_WindowDef,
	T_RangeSubselect,
	T_RangeFunction,
	T_TypeName,
	T_ColumnDef,
	T_IndexElem,
	T_Constraint,
	T_DefElem,
	T_PrivGrantee,
	T_AccessPriv,
	T_InhRelation,
	T_LockingClause,
	T_XmlSerialize,
	T_WithClause,
	T_CommonTableExpr,
};

internal 
 enum SetOperation
{
    SETOP_NONE = 0,
    SETOP_UNION,
    SETOP_INTERSECT,
    SETOP_EXCEPT
};

internal
 enum InhOption
{
    INH_NO,						/* Do NOT scan child tables */
    INH_YES,					/* DO scan child tables */
    INH_DEFAULT					/* Use current SQL_inheritance option */
};

internal
enum A_Expr_Kind
{
	AEXPR_OP,					/* normal operator */
	AEXPR_AND,					/* booleans - name field is unused */
	AEXPR_OR,
	AEXPR_NOT,
	AEXPR_OP_ANY,				/* scalar op ANY (array) */
	AEXPR_OP_ALL,				/* scalar op ALL (array) */
	AEXPR_DISTINCT,				/* IS DISTINCT FROM - name must be "=" */
	AEXPR_NULLIF,				/* NULLIF - name must be "=" */
	AEXPR_OF,					/* IS [NOT] OF - name must be "=" or "<>" */
	AEXPR_IN					/* [NOT] IN - name must be "=" or "<>" */
};

internal
enum SortByDir
{
	SORTBY_DEFAULT,
	SORTBY_ASC,
	SORTBY_DESC,
	SORTBY_USING				/* not allowed in CREATE INDEX ... */
};

internal
enum SortByNulls
{
	SORTBY_NULLS_DEFAULT,
	SORTBY_NULLS_FIRST,
	SORTBY_NULLS_LAST
};

#if __LINE__
#undef internal
#endif
