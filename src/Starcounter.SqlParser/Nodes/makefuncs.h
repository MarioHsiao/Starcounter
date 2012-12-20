/*-------------------------------------------------------------------------
 *
 * makefuncs.h
 *	  prototypes for the creator functions (for primitive nodes)
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/nodes/makefuncs.h
 *
 *-------------------------------------------------------------------------
 */
#ifndef MAKEFUNC_H
#define MAKEFUNC_H

#include "parsenodes.h"


extern A_Expr *makeA_Expr(A_Expr_Kind kind, List *name,
		   Node *lexpr, Node *rexpr, int location);

extern A_Expr *makeSimpleA_Expr(A_Expr_Kind kind, wchar_t *name,
				 Node *lexpr, Node *rexpr, int location);

extern RangeVar *makeRangeVar(List *namespaces, wchar_t *relname, int location);

extern TypeName *makeTypeName(wchar_t *typnam);
extern TypeName *makeTypeNameFromNameList(List *names);
extern TypeName *makeTypeNameFromOid(Oid typeOid, int32 typmod);

extern DefElem *makeDefElem(wchar_t *name, Node *arg);
extern DefElem *makeDefElemExtended(wchar_t *nameSpace, wchar_t *name, Node *arg,
					DefElemAction defaction);

extern DefElem *defWithOids(bool value); // from defrem.h

#endif   /* MAKEFUNC_H */
