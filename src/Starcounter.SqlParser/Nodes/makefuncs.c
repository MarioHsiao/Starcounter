/*-------------------------------------------------------------------------
 *
 * makefuncs.c
 *	  creator functions for primitive nodes. The functions here are for
 *	  the most frequently created nodes.
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 *
 * IDENTIFICATION
 *	  src/backend/nodes/makefuncs.c
 *
 *-------------------------------------------------------------------------
 */
#include "../General/postgres.h"

#include "makefuncs.h"
#include "nodeFuncs.h"

/*
 * makeA_Expr -
 *		makes an A_Expr node
 */
A_Expr *
makeA_Expr(A_Expr_Kind kind, List *name,
		   Node *lexpr, Node *rexpr, int location)
{
	A_Expr	   *a = makeNode(A_Expr);

	a->kind = kind;
	a->name = name;
	a->lexpr = lexpr;
	a->rexpr = rexpr;
	SET_LOCATION(a, location);
	return a;
}

/*
 * makeSimpleA_Expr -
 *		As above, given a simple (unqualified) operator name
 */
A_Expr *
makeSimpleA_Expr(A_Expr_Kind kind, wchar_t *name,
				 Node *lexpr, Node *rexpr, int location)
{
	A_Expr	   *a = makeNode(A_Expr);

	a->kind = kind;
	a->name = list_make1(makeString(name));
	a->lexpr = lexpr;
	a->rexpr = rexpr;
	SET_LOCATION(a, location);
	return a;
}

/*
 * makeRangeVar -
 *	  creates a RangeVar node (rather oversimplified case)
 */
RangeVar *
makeRangeVar(List *namespaces, wchar_t *relname, int location)
{
	RangeVar   *r = makeNode(RangeVar);

	r->path = namespaces;
	r->relname = relname;
	r->inhOpt = INH_DEFAULT;
	r->relpersistence = RELPERSISTENCE_PERMANENT;
	r->alias = NULL;
	SET_LOCATION(r, location);

	return r;
}

/*
 * makeTypeName -
 *	build a TypeName node for an unqualified name.
 *
 * typmod is defaulted, but can be changed later by caller.
 */
TypeName *
makeTypeName(wchar_t *typnam)
{
	return makeTypeNameFromNameList(list_make1(makeString(typnam)));
}

/*
 * makeTypeNameFromNameList -
 *	build a TypeName node for a String list representing a qualified name.
 *
 * typmod is defaulted, but can be changed later by caller.
 */
TypeName *
makeTypeNameFromNameList(List *names)
{
	TypeName   *n = makeNode(TypeName);

	n->names = names;
	n->typmods = NIL;
	n->typemod = -1;
	n->generics = NIL;
	SET_LOCATION(n, -1);
	return n;
}

/*
 * makeTypeNameFromOid -
 *	build a TypeName node to represent a type already known by OID/typmod.
 */
TypeName *
makeTypeNameFromOid(Oid typeOid, int32 typmod)
{
	TypeName   *n = makeNode(TypeName);

	n->typeOid = typeOid;
	n->typemod = typmod;
	SET_LOCATION(n, -1);
	return n;
}

/*
 * makeDefElem -
 *	build a DefElem node
 *
 * This is sufficient for the "typical" case with an unqualified option name
 * and no special action.
 */
DefElem *
makeDefElem(wchar_t *name, Node *arg)
{
	DefElem    *res = makeNode(DefElem);

	res->defnamespace = NULL;
	res->defname = name;
	res->arg = arg;
	res->defaction = DEFELEM_UNSPEC;

	return res;
}

/*
 * makeDefElemExtended -
 *	build a DefElem node with all fields available to be specified
 */
DefElem *
makeDefElemExtended(wchar_t *nameSpace, wchar_t *name, Node *arg,
					DefElemAction defaction)
{
	DefElem    *res = makeNode(DefElem);

	res->defnamespace = nameSpace;
	res->defname = name;
	res->arg = arg;
	res->defaction = defaction;

	return res;
}

/*
 * Create a DefElem setting "oids" to the specified value.
 * From define.c
 */
DefElem *
defWithOids(bool value)
{
	return makeDefElem(L"oids", (Node *) makeInteger(value));
}
