/*-------------------------------------------------------------------------
 *
 * nodeFuncs.c
 *		Various general-purpose manipulations of Node trees
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 *
 * IDENTIFICATION
 *	  src/backend/nodes/nodeFuncs.c
 *
 *-------------------------------------------------------------------------
 */
#include "../General/postgres.h"

#include "nodeFuncs.h"
#include "parsenodes.h" // from relation.h

static int	leftmostLoc(int loc1, int loc2);



/*
 *	exprLocation -
 *	  returns the parse location of an expression tree, for error reports
 *
 * -1 is returned if the location can't be determined.
 *
 * For expressions larger than a single token, the intent here is to
 * return the location of the expression's leftmost token, not necessarily
 * the topmost Node's location field.  For example, an OpExpr's location
 * field will point at the operator name, but if it is not a prefix operator
 * then we should return the location of the left-hand operand instead.
 * The reason is that we want to reference the entire expression not just
 * that operator, and pointing to its start seems to be the most natural way.
 *
 * The location is not perfect --- for example, since the grammar doesn't
 * explicitly represent parentheses in the parsetree, given something that
 * had been written "(a + b) * c" we are going to point at "a" not "(".
 * But it should be plenty good enough for error reporting purposes.
 *
 * You might think that this code is overly general, for instance why check
 * the operands of a FuncExpr node, when the function name can be expected
 * to be to the left of them?  There are a couple of reasons.  The grammar
 * sometimes builds expressions that aren't quite what the user wrote;
 * for instance x IS NOT BETWEEN ... becomes a NOT-expression whose keyword
 * pointer is to the right of its leftmost argument.  Also, nodes that were
 * inserted implicitly by parse analysis (such as FuncExprs for implicit
 * coercions) will have location -1, and so we can have odd combinations of
 * known and unknown locations in a tree.
 */
int
exprLocation(Node *expr)
{
	int			loc;

	if (expr == NULL)
		return -1;
	switch (nodeTag(expr))
	{
		case T_RangeVar:
			loc = ((RangeVar *) expr)->location;
			break;
		case T_NamedArgExpr:
			{
				NamedArgExpr *na = (NamedArgExpr *) expr;

				/* consider both argument name and value */
				loc = leftmostLoc(na->location,
								  exprLocation((Node *) na->arg));
			}
			break;
		case T_SubLink:
			{
				SubLink    *sublink = (SubLink *) expr;

				/* check the testexpr, if any, and the operator/keyword */
				loc = leftmostLoc(exprLocation(sublink->testexpr),
								  sublink->location);
			}
			break;
		case T_CaseExpr:
			/* CASE keyword should always be the first thing */
			loc = ((CaseExpr *) expr)->location;
			break;
		case T_CaseWhen:
			/* WHEN keyword should always be the first thing */
			loc = ((CaseWhen *) expr)->location;
			break;
		case T_RowExpr:
			/* the location points at ROW or (, which must be leftmost */
			loc = ((RowExpr *) expr)->location;
			break;
		case T_CoalesceExpr:
			/* COALESCE keyword should always be the first thing */
			loc = ((CoalesceExpr *) expr)->location;
			break;
		case T_MinMaxExpr:
			/* GREATEST/LEAST keyword should always be the first thing */
			loc = ((MinMaxExpr *) expr)->location;
			break;
		case T_XmlExpr:
			{
				XmlExpr    *xexpr = (XmlExpr *) expr;

				/* consider both function name and leftmost arg */
				loc = leftmostLoc(xexpr->location,
								  exprLocation((Node *) xexpr->args));
			}
			break;
		case T_NullTest:
			/* just use argument's location */
			loc = exprLocation((Node *) ((NullTest *) expr)->arg);
			break;
		case T_BooleanTest:
			/* just use argument's location */
			loc = exprLocation((Node *) ((BooleanTest *) expr)->arg);
			break;
		case T_SetToDefault:
			loc = ((SetToDefault *) expr)->location;
			break;
		case T_IntoClause:
			/* use the contained RangeVar's location --- close enough */
			loc = exprLocation((Node *) ((IntoClause *) expr)->rel);
			break;
		case T_List:
			{
				/* report location of first list member that has a location */
				ListCell   *lc;

				loc = -1;		/* just to suppress compiler warning */
				foreach(lc, (List *) expr)
				{
					loc = exprLocation((Node *) lfirst(lc));
					if (loc >= 0)
						break;
				}
			}
			break;
		case T_A_Expr:
			{
				A_Expr	   *aexpr = (A_Expr *) expr;

				/* use leftmost of operator or left operand (if any) */
				/* we assume right operand can't be to left of operator */
				loc = leftmostLoc(aexpr->location,
								  exprLocation(aexpr->lexpr));
			}
			break;
		case T_ColumnRef:
			loc = ((ColumnRef *) expr)->location;
			break;
		case T_ParamRef:
			loc = ((ParamRef *) expr)->location;
			break;
		case T_A_Const:
			loc = ((A_Const *) expr)->location;
			break;
		case T_FuncCall:
			{
				FuncCall   *fc = (FuncCall *) expr;

				/* consider both function name and leftmost arg */
				/* (we assume any ORDER BY nodes must be to right of name) */
				loc = leftmostLoc(fc->location,
								  exprLocation((Node *) fc->args));
			}
			break;
		case T_A_ArrayExpr:
			/* the location points at ARRAY or [, which must be leftmost */
			loc = ((A_ArrayExpr *) expr)->location;
			break;
		case T_ResTarget:
			/* we need not examine the contained expression (if any) */
			loc = ((ResTarget *) expr)->location;
			break;
		case T_TypeCast:
			{
				TypeCast   *tc = (TypeCast *) expr;

				/*
				 * This could represent CAST(), ::, or TypeName 'literal', so
				 * any of the components might be leftmost.
				 */
				loc = exprLocation(tc->arg);
				loc = leftmostLoc(loc, tc->typeName->location);
				loc = leftmostLoc(loc, tc->location);
			}
			break;
		case T_CollateClause:
			/* just use argument's location */
			loc = exprLocation(((CollateClause *) expr)->arg);
			break;
		case T_SortBy:
			/* just use argument's location (ignore operator, if any) */
			loc = exprLocation(((SortBy *) expr)->node);
			break;
		case T_WindowDef:
			loc = ((WindowDef *) expr)->location;
			break;
		case T_TypeName:
			loc = ((TypeName *) expr)->location;
			break;
		case T_Constraint:
			loc = ((Constraint *) expr)->location;
			break;
		case T_XmlSerialize:
			/* XMLSERIALIZE keyword should always be the first thing */
			loc = ((XmlSerialize *) expr)->location;
			break;
		case T_WithClause:
			loc = ((WithClause *) expr)->location;
			break;
		case T_CommonTableExpr:
			loc = ((CommonTableExpr *) expr)->location;
			break;
		case T_HintJoinOrder:
			loc = ((HintJoinOrder *) expr)->location;
			break;
		case T_HintIndex:
			loc = ((HintIndex *) expr)->location;
			break;
		default:
			/* for any other node type it's just unknown... */
			loc = -1;
			break;
	}
	return loc;
}

/*
 * leftmostLoc - support for exprLocation
 *
 * Take the minimum of two parse location values, but ignore unknowns
 */
static int
leftmostLoc(int loc1, int loc2)
{
	if (loc1 < 0)
		return loc2;
	else if (loc2 < 0)
		return loc1;
	else
		return Min(loc1, loc2);
}
