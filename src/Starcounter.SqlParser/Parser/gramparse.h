/*-------------------------------------------------------------------------
 *
 * gramparse.h
 *		Shared definitions for the "raw" parser (flex and bison phases only)
 *
 * NOTE: this file is only meant to be included in the core parsing files,
 * ie, parser.c, gram.y, scan.l, and keywords.c.  Definitions that are needed
 * outside the core parser should be in parser.h.
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/parser/gramparse.h
 *
 *-------------------------------------------------------------------------
 */

#ifndef GRAMPARSE_H
#define GRAMPARSE_H

#include "../Nodes/parsenodes.h"
#include "scanner.h"

typedef union YYSTYPE
{
	core_YYSTYPE		core_yystype;
	/* these fields must match core_YYSTYPE: */
	__int64				ival;
	wchar_t				*str;
	const wchar_t		*keyword;

	char				chr;
	bool				boolean;
	JoinType			jtype; // In nodes.h
	DropBehavior		dbehavior; // In parsenodes.h
	OnCommitAction		oncommit; // In primnodes.h
	List				*list; // In pg_list.h
	Node				*node; // In nodes.h
	Value				*value; // In value.h
	ObjectType			objtype; // In parsenodes.h
	TypeName			*typnam; // In parsenodes.h
	DefElem				*defelt; // In parsenodes.h
	SortBy				*sortby; // In parsenodes.h
	LimitOffset         *limitoffset; // In parsenodes.h
	WindowDef			*windef; // In parsenodes.h
	JoinExpr			*jexpr; // In primnodes.h
	IndexElem			*ielem; // In parsenodes.h
	Alias				*alias; // In primnodes.h
	RangeVar			*range; // In primnodes.h
	IntoClause			*into; // In primnodes.h
	WithClause			*with; // In parsenodes.h
	A_Indices			*aind; // In parsenodes.h
	ResTarget			*target; // In parsenodes.h
	struct PrivTarget	*privtarget;
	AccessPriv			*accesspriv; // In parsenodes.h
	InsertStmt			*istmt; // In parsenodes.h
	VariableSetStmt		*vsetstmt; // In parsenodes.h
} YYSTYPE;

/*
 * NB: include gram.h only AFTER including scanner.h, because scanner.h
 * is what #defines YYLTYPE.
 */
#include "gram.h"

/*
 * The YY_EXTRA data that a flex scanner allows us to pass around.	Private
 * state needed for raw parsing/lexing goes here.
 */
typedef struct base_yy_extra_type
{
	/*
	 * Fields used by the core scanner.
	 */
	core_yy_extra_type core_yy_extra;

	/*
	 * State variables for base_yylex().
	 */
	bool		have_lookahead; /* is lookahead info valid? */
	int			lookahead_token;	/* one-token lookahead */
	core_YYSTYPE lookahead_yylval;		/* yylval for lookahead token */
	YYLTYPE		lookahead_yylloc;		/* yylloc for lookahead token */

	/*
	 * State variables that belong to the grammar.
	 */
	List	   *parsetree;		/* final parse result is delivered here */
} base_yy_extra_type;

/*
 * In principle we should use yyget_extra() to fetch the yyextra field
 * from a yyscanner struct.  However, flex always puts that field first,
 * and this is sufficiently performance-critical to make it seem worth
 * cheating a bit to use an inline macro.
 */
#define pg_yyget_extra(yyscanner) (*((base_yy_extra_type **) (yyscanner)))


/* from parser.c */
extern int base_yylex(YYSTYPE *lvalp, YYLTYPE *llocp,
		   core_yyscan_t yyscanner);

/* from gram.y */
extern void parser_init(base_yy_extra_type *yyext);
extern int	base_yyparse(core_yyscan_t yyscanner);

#endif   /* GRAMPARSE_H */
