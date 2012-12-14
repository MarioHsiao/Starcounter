/*-------------------------------------------------------------------------
 *
 * parser.h
 *		Definitions for the "raw" parser (flex and bison phases only)
 *
 * This is the external API for the raw lexing/parsing functions.
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/parser/parser.h
 *
 *-------------------------------------------------------------------------
 */
#ifndef PARSER_H
#define PARSER_H

#include "../Nodes/parsenodes.h"


typedef enum
{
	BACKSLASH_QUOTE_OFF,
	BACKSLASH_QUOTE_ON,
	BACKSLASH_QUOTE_SAFE_ENCODING
}	BackslashQuoteType;

/* Primary entry point for the raw parsing functions */
extern List *raw_parser(const wchar_t *str);

/* Utility functions exported by gram.y (perhaps these should be elsewhere) */
extern List *SystemFuncName(char *name);
extern TypeName *SystemTypeName(char *name);

#endif   /* PARSER_H */
