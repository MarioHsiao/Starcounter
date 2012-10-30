/*-------------------------------------------------------------------------
 *
 * keywords.c
 *	  lexical token lookup for key words in PostgreSQL
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 *
 * IDENTIFICATION
 *	  src/backend/parser/keywords.c
 *
 *-------------------------------------------------------------------------
 */
#include "../General/postgres.h"

#include "gramparse.h"

#define PG_KEYWORD(a,b,c) {a,b,c},


const ScanKeyword ScanKeywords[] = {
#include "kwlist.h"
};

const int	NumScanKeywords = lengthof(ScanKeywords);
