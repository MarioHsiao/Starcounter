/*-------------------------------------------------------------------------
 *
 * primnodes.h
 *	  Definitions for "primitive" node types, those that are used in more
 *	  than one of the parse/plan/execute stages of the query pipeline.
 *	  Currently, these are mostly nodes for executable expressions
 *	  and join trees.
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/nodes/primnodes.h
 *
 *-------------------------------------------------------------------------
 */
#ifndef PRIMNODES_H
#define PRIMNODES_H

#include "../General/postgres.h"
#include "pg_list.h"


/* ----------------------------------------------------------------
 *						node definitions
 * ----------------------------------------------------------------
 */

/*
 * Alias -
 *	  specifies an alias for a range variable; the alias might also
 *	  specify renaming of columns within the table.
 *
 * Note: colnames is a list of Value nodes (always strings).  In Alias structs
 * associated with RTEs, there may be entries corresponding to dropped
 * columns; these are normally empty strings ("").	See parsenodes.h for info.
 */
typedef struct Alias
{
	NodeTag		type;
	wchar_t	   *aliasname;		/* aliased rel name (never qualified) */
	List	   *colnames;		/* optional list of column aliases */
} Alias;

typedef enum InhOption InhOption;

/* What to do at commit time for temporary relations */
typedef enum OnCommitAction
{
	ONCOMMIT_NOOP,				/* No ON COMMIT clause (do nothing) */
	ONCOMMIT_PRESERVE_ROWS,		/* ON COMMIT PRESERVE ROWS (do nothing) */
	ONCOMMIT_DELETE_ROWS,		/* ON COMMIT DELETE ROWS */
	ONCOMMIT_DROP				/* ON COMMIT DROP */
} OnCommitAction;

/*
 * RangeVar - range variable, used in FROM clauses
 *
 * Also used to represent table names in utility statements; there, the alias
 * field is not used, and inhOpt shows whether to apply the operation
 * recursively to child tables.  In some contexts it is also useful to carry
 * a TEMP table indication here.
 */
typedef struct RangeVar
{
	NodeTag		type;
	List       *path;     /* Names of namespaces in order from most outer to most inner */
	wchar_t	   *relname;		/* the relation/sequence name */
	InhOption	inhOpt;			/* expand rel by inheritance? recursively act
								 * on children? */
	char		relpersistence; /* see RELPERSISTENCE_* in pg_class.h -> postgres.h */
	Alias	   *alias;			/* table alias & optional column aliases */
	int			location;		/* token location, or -1 if unknown */
} RangeVar;

/*
 * IntoClause - target information for SELECT INTO and CREATE TABLE AS
 */
typedef struct IntoClause
{
	NodeTag		type;

	RangeVar   *rel;			/* target relation name */
	List	   *colNames;		/* column names to assign, or NIL */
	List	   *options;		/* options from WITH clause */
	OnCommitAction onCommit;	/* what do we do at COMMIT? */
	wchar_t	   *tableSpaceName; /* table space to use, or NULL */
} IntoClause;


/* ----------------------------------------------------------------
 *					node types for executable expressions
 * ----------------------------------------------------------------
 */

/*
 * Expr - generic superclass for executable-expression nodes
 *
 * All node types that are used in executable expression trees should derive
 * from Expr (that is, have Expr as their first field).  Since Expr only
 * contains NodeTag, this is a formality, but it is an easy form of
 * documentation.  See also the ExprState node types in execnodes.h.
 */
typedef struct Expr
{
	NodeTag		type;
} Expr;

/*
 * CoercionForm - information showing how to display a function-call node
 */
typedef enum CoercionForm
{
	COERCE_EXPLICIT_CALL,		/* display as a function call */
	COERCE_EXPLICIT_CAST,		/* display as an explicit cast */
	COERCE_IMPLICIT_CAST,		/* implicit cast, so hide it */
	COERCE_DONTCARE				/* special case for planner */
} CoercionForm;

/*
 * NamedArgExpr - a named argument of a function
 *
 * This node type can only appear in the args list of a FuncCall or FuncExpr
 * node.  We support pure positional call notation (no named arguments),
 * named notation (all arguments are named), and mixed notation (unnamed
 * arguments followed by named ones).
 *
 * Parse analysis sets argnumber to the positional index of the argument,
 * but doesn't rearrange the argument list.
 *
 * The planner will convert argument lists to pure positional notation
 * during expression preprocessing, so execution never sees a NamedArgExpr.
 */
typedef struct NamedArgExpr
{
	Expr		xpr;
	Expr	   *arg;			/* the argument expression */
	wchar_t	   *name;			/* the name */
	int			argnumber;		/* argument's number in positional notation */
	int			location;		/* argument name location, or -1 if unknown */
} NamedArgExpr;

/*
 * SubLink
 *
 * A SubLink represents a subselect appearing in an expression, and in some
 * cases also the combining operator(s) just above it.	The subLinkType
 * indicates the form of the expression represented:
 *	EXISTS_SUBLINK		EXISTS(SELECT ...)
 *	ALL_SUBLINK			(lefthand) op ALL (SELECT ...)
 *	ANY_SUBLINK			(lefthand) op ANY (SELECT ...)
 *	ROWCOMPARE_SUBLINK	(lefthand) op (SELECT ...)
 *	EXPR_SUBLINK		(SELECT with single targetlist item ...)
 *	ARRAY_SUBLINK		ARRAY(SELECT with single targetlist item ...)
 *	CTE_SUBLINK			WITH query (never actually part of an expression)
 * For ALL, ANY, and ROWCOMPARE, the lefthand is a list of expressions of the
 * same length as the subselect's targetlist.  ROWCOMPARE will *always* have
 * a list with more than one entry; if the subselect has just one target
 * then the parser will create an EXPR_SUBLINK instead (and any operator
 * above the subselect will be represented separately).  Note that both
 * ROWCOMPARE and EXPR require the subselect to deliver only one row.
 * ALL, ANY, and ROWCOMPARE require the combining operators to deliver boolean
 * results.  ALL and ANY combine the per-row results using AND and OR
 * semantics respectively.
 * ARRAY requires just one target column, and creates an array of the target
 * column's type using any number of rows resulting from the subselect.
 *
 * SubLink is classed as an Expr node, but it is not actually executable;
 * it must be replaced in the expression tree by a SubPlan node during
 * planning.
 *
 * NOTE: in the raw output of gram.y, testexpr contains just the raw form
 * of the lefthand expression (if any), and operName is the String name of
 * the combining operator.	Also, subselect is a raw parsetree.  During parse
 * analysis, the parser transforms testexpr into a complete boolean expression
 * that compares the lefthand value(s) to PARAM_SUBLINK nodes representing the
 * output columns of the subselect.  And subselect is transformed to a Query.
 * This is the representation seen in saved rules and in the rewriter.
 *
 * In EXISTS, EXPR, and ARRAY SubLinks, testexpr and operName are unused and
 * are always null.
 *
 * The CTE_SUBLINK case never occurs in actual SubLink nodes, but it is used
 * in SubPlans generated for WITH subqueries.
 */
typedef enum SubLinkType
{
	EXISTS_SUBLINK,
	ALL_SUBLINK,
	ANY_SUBLINK,
	ROWCOMPARE_SUBLINK,
	EXPR_SUBLINK,
	ARRAY_SUBLINK,
	CTE_SUBLINK					/* for SubPlans only */
} SubLinkType;


typedef struct SubLink
{
	Expr		xpr;
	SubLinkType subLinkType;	/* see above */
	Node	   *testexpr;		/* outer-query test for ALL/ANY/ROWCOMPARE */
	List	   *operName;		/* originally specified operator name */
	Node	   *subselect;		/* subselect as Query* or parsetree */
	int			location;		/* token location, or -1 if unknown */
} SubLink;

/*----------
 * CaseExpr - a CASE expression
 *
 * We support two distinct forms of CASE expression:
 *		CASE WHEN boolexpr THEN expr [ WHEN boolexpr THEN expr ... ]
 *		CASE testexpr WHEN compexpr THEN expr [ WHEN compexpr THEN expr ... ]
 * These are distinguishable by the "arg" field being NULL in the first case
 * and the testexpr in the second case.
 *
 * In the raw grammar output for the second form, the condition expressions
 * of the WHEN clauses are just the comparison values.	Parse analysis
 * converts these to valid boolean expressions of the form
 *		CaseTestExpr '=' compexpr
 * where the CaseTestExpr node is a placeholder that emits the correct
 * value at runtime.  This structure is used so that the testexpr need be
 * evaluated only once.  Note that after parse analysis, the condition
 * expressions always yield boolean.
 *
 * Note: we can test whether a CaseExpr has been through parse analysis
 * yet by checking whether casetype is InvalidOid or not.
 *----------
 */
typedef struct CaseExpr
{
	Expr		xpr;
	Oid			casetype;		/* type of expression result */
	Oid			casecollid;		/* OID of collation, or InvalidOid if none */
	Expr	   *arg;			/* implicit equality comparison argument */
	List	   *args;			/* the arguments (list of WHEN clauses) */
	Expr	   *defresult;		/* the default result (ELSE clause) */
	int			location;		/* token location, or -1 if unknown */
} CaseExpr;

/*
 * CaseWhen - one arm of a CASE expression
 */
typedef struct CaseWhen
{
	Expr		xpr;
	Expr	   *expr;			/* condition expression */
	Expr	   *result;			/* substitution result */
	int			location;		/* token location, or -1 if unknown */
} CaseWhen;

/*
 * RowExpr - a ROW() expression
 *
 * Note: the list of fields must have a one-for-one correspondence with
 * physical fields of the associated rowtype, although it is okay for it
 * to be shorter than the rowtype.	That is, the N'th list element must
 * match up with the N'th physical field.  When the N'th physical field
 * is a dropped column (attisdropped) then the N'th list element can just
 * be a NULL constant.	(This case can only occur for named composite types,
 * not RECORD types, since those are built from the RowExpr itself rather
 * than vice versa.)  It is important not to assume that length(args) is
 * the same as the number of columns logically present in the rowtype.
 *
 * colnames is NIL in a RowExpr built from an ordinary ROW() expression.
 * It is provided in cases where we expand a whole-row Var into a RowExpr,
 * to retain the column alias names of the RTE that the Var referenced
 * (which would otherwise be very difficult to extract from the parsetree).
 * Like the args list, it is one-for-one with physical fields of the rowtype.
 */
typedef struct RowExpr
{
	Expr		xpr;
	List	   *args;			/* the fields */
	Oid			row_typeid;		/* RECORDOID or a composite type's ID */

	/*
	 * Note: we deliberately do NOT store a typmod.  Although a typmod will be
	 * associated with specific RECORD types at runtime, it will differ for
	 * different backends, and so cannot safely be stored in stored
	 * parsetrees.	We must assume typmod -1 for a RowExpr node.
	 *
	 * We don't need to store a collation either.  The result type is
	 * necessarily composite, and composite types never have a collation.
	 */
	CoercionForm row_format;	/* how to display this node */
	List	   *colnames;		/* list of String, or NIL */
	int			location;		/* token location, or -1 if unknown */
} RowExpr;

/*
 * CoalesceExpr - a COALESCE expression
 */
typedef struct CoalesceExpr
{
	Expr		xpr;
	Oid			coalescetype;	/* type of expression result */
	Oid			coalescecollid; /* OID of collation, or InvalidOid if none */
	List	   *args;			/* the arguments */
	int			location;		/* token location, or -1 if unknown */
} CoalesceExpr;

/*
 * MinMaxExpr - a GREATEST or LEAST function
 */
typedef enum MinMaxOp
{
	IS_GREATEST,
	IS_LEAST
} MinMaxOp;

typedef struct MinMaxExpr
{
	Expr		xpr;
	Oid			minmaxtype;		/* common type of arguments and result */
	Oid			minmaxcollid;	/* OID of collation of result */
	Oid			inputcollid;	/* OID of collation that function should use */
	MinMaxOp	op;				/* function to execute */
	List	   *args;			/* the arguments */
	int			location;		/* token location, or -1 if unknown */
} MinMaxExpr;

/*
 * XmlExpr - various SQL/XML functions requiring special grammar productions
 *
 * 'name' carries the "NAME foo" argument (already XML-escaped).
 * 'named_args' and 'arg_names' represent an xml_attribute list.
 * 'args' carries all other arguments.
 *
 * Note: result type/typmod/collation are not stored, but can be deduced
 * from the XmlExprOp.	The type/typmod fields are just used for display
 * purposes, and are NOT the true result type of the node.
 */
typedef enum XmlExprOp
{
	IS_XMLCONCAT,				/* XMLCONCAT(args) */
	IS_XMLELEMENT,				/* XMLELEMENT(name, xml_attributes, args) */
	IS_XMLFOREST,				/* XMLFOREST(xml_attributes) */
	IS_XMLPARSE,				/* XMLPARSE(text, is_doc, preserve_ws) */
	IS_XMLPI,					/* XMLPI(name [, args]) */
	IS_XMLROOT,					/* XMLROOT(xml, version, standalone) */
	IS_XMLSERIALIZE			/* XMLSERIALIZE(is_document, xmlval) */
} XmlExprOp;

typedef enum
{
	XMLOPTION_DOCUMENT,
	XMLOPTION_CONTENT
} XmlOptionType;

typedef struct XmlExpr
{
	Expr		xpr;
	XmlExprOp	op;				/* xml function ID */
	wchar_t	   *name;			/* name in xml(NAME foo ...) syntaxes */
	List	   *named_args;		/* non-XML expressions for xml_attributes */
	List	   *arg_names;		/* parallel list of Value strings */
	List	   *args;			/* list of expressions */
	XmlOptionType xmloption;	/* DOCUMENT or CONTENT */
	Oid			type;			/* target type/typmod for XMLSERIALIZE */
	int32		typmod;
	int			location;		/* token location, or -1 if unknown */
} XmlExpr;

/* ----------------
 * NullTest
 *
 * NullTest represents the operation of testing a value for NULLness.
 * The appropriate test is performed and returned as a boolean Datum.
 *
 * NOTE: the semantics of this for rowtype inputs are noticeably different
 * from the scalar case.  We provide an "argisrow" flag to reflect that.
 * ----------------
 */

typedef enum NullTestType
{
	IS_NULL, IS_NOT_NULL
} NullTestType;

typedef struct NullTest
{
	Expr		xpr;
	Expr	   *arg;			/* input expression */
	NullTestType nulltesttype;	/* IS NULL, IS NOT NULL */
	bool		argisrow;		/* T if input is of a composite type */
} NullTest;

/*
 * BooleanTest
 *
 * BooleanTest represents the operation of determining whether a boolean
 * is TRUE, FALSE.  All six meaningful combinations
 * are supported.  Note that a NULL input does *not* cause a NULL result.
 * The appropriate test is performed and returned as a boolean Datum.
 */

typedef enum BoolTestType
{
	IS_TRUE, IS_NOT_TRUE, IS_FALSE, IS_NOT_FALSE
} BoolTestType;

typedef struct BooleanTest
{
	Expr		xpr;
	Expr	   *arg;			/* input expression */
	BoolTestType booltesttype;	/* test type */
} BooleanTest;

/*
 * Placeholder node for a DEFAULT marker in an INSERT or UPDATE command.
 *
 * This is not an executable expression: it must be replaced by the actual
 * column default expression during rewriting.	But it is convenient to
 * treat it as an expression node during parsing and rewriting.
 */
typedef struct SetToDefault
{
	Expr		xpr;
	Oid			typeId;			/* type for substituted value */
	int32		typeMod;		/* typemod for substituted value */
	Oid			collation;		/* collation for the substituted value */
	int			location;		/* token location, or -1 if unknown */
} SetToDefault;

/* ----------------------------------------------------------------
 *					node types for join trees
 *
 * The leaves of a join tree structure are RangeTblRef nodes.  Above
 * these, JoinExpr nodes can appear to denote a specific kind of join
 * or qualified join.  Also, FromExpr nodes can appear to denote an
 * ordinary cross-product join ("FROM foo, bar, baz WHERE ...").
 * FromExpr is like a JoinExpr of jointype JOIN_INNER, except that it
 * may have any number of child nodes, not just two.
 *
 * NOTE: the top level of a Query's jointree is always a FromExpr.
 * Even if the jointree contains no rels, there will be a FromExpr.
 *
 * NOTE: the qualification expressions present in JoinExpr nodes are
 * *in addition to* the query's main WHERE clause, which appears as the
 * qual of the top-level FromExpr.	The reason for associating quals with
 * specific nodes in the jointree is that the position of a qual is critical
 * when outer joins are present.  (If we enforce a qual too soon or too late,
 * that may cause the outer join to produce the wrong set of NULL-extended
 * rows.)  If all joins are inner joins then all the qual positions are
 * semantically interchangeable.
 *
 * NOTE: in the raw output of gram.y, a join tree contains RangeVar,
 * RangeSubselect, and RangeFunction nodes, which are all replaced by
 * RangeTblRef nodes during the parse analysis phase.  Also, the top-level
 * FromExpr is added during parse analysis; the grammar regards FROM and
 * WHERE as separate.
 * ----------------------------------------------------------------
 */

/*----------
 * JoinExpr - for SQL JOIN expressions
 *
 * isNatural, usingClause, and quals are interdependent.  The user can write
 * only one of NATURAL, USING(), or ON() (this is enforced by the grammar).
 * If he writes NATURAL then parse analysis generates the equivalent USING()
 * list, and from that fills in "quals" with the right equality comparisons.
 * If he writes USING() then "quals" is filled with equality comparisons.
 * If he writes ON() then only "quals" is set.	Note that NATURAL/USING
 * are not equivalent to ON() since they also affect the output column list.
 *
 * alias is an Alias node representing the AS alias-clause attached to the
 * join expression, or NULL if no clause.  NB: presence or absence of the
 * alias has a critical impact on semantics, because a join with an alias
 * restricts visibility of the tables/columns inside it.
 *
 * During parse analysis, an RTE is created for the Join, and its index
 * is filled into rtindex.	This RTE is present mainly so that Vars can
 * be created that refer to the outputs of the join.  The planner sometimes
 * generates JoinExprs internally; these can have rtindex = 0 if there are
 * no join alias variables referencing such joins.
 *----------
 */
typedef struct JoinExpr
{
	NodeTag		type;
	JoinType	jointype;		/* type of join */
	bool		isNatural;		/* Natural join? Will need to shape table */
	Node	   *larg;			/* left subtree */
	Node	   *rarg;			/* right subtree */
	List	   *usingClause;	/* USING clause, if any (list of String) */
	Node	   *quals;			/* qualifiers on join, if any */
	Alias	   *alias;			/* user-written alias clause, if any */
	int			rtindex;		/* RT index assigned for join, or 0 */
} JoinExpr;

#endif   /* PRIMNODES_H */
