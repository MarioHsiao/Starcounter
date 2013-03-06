/*-------------------------------------------------------------------------
 *
 * postgres.h
 *	  Primary include file for PostgreSQL server .c files
 *
 * This should be the first file included by PostgreSQL backend modules.
 * Client-side code should include postgres_fe.h instead.
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1995, Regents of the University of California
 *
 * src/include/postgres.h
 *
 *-------------------------------------------------------------------------
 */
/*
 *----------------------------------------------------------------
 *	 TABLE OF CONTENTS
 *
 *		When adding stuff to this file, please try to put stuff
 *		into the relevant section, or add new sections as appropriate.
 *
 *	  section	description
 *	  -------	------------------------------------------------
 *		1)		variable-length datatypes (TOAST support)
 *		2)		datum type + support macros
 *		3)		exception handling definitions
 *
 *	 NOTES
 *
 *	In general, this file should contain declarations that are widely needed
 *	in the backend environment, but are of no interest outside the backend.
 *
 *	Simple type definitions live in c.h, where they are shared with
 *	postgres_fe.h.	We do that since those type definitions are needed by
 *	frontend modules that want to deal with binary data transmission to or
 *	from the backend.  Type definitions in this file should be for
 *	representations that never escape the backend, such as Datum or
 *	TOASTed varlena objects.
 *
 *----------------------------------------------------------------
 */
#ifndef POSTGRES_H
#define POSTGRES_H

#include "c.h"
#include "../Utils/palloc.h"

#undef _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES
#define _CRT_SECURE_CPP_OVERLOAD_STANDARD_NAMES 1
/* ----------------------------------------------------------------
 *				Section 3:	exception handling definitions
 *							Assert, Trap, etc macros
 * ----------------------------------------------------------------
 */

extern PGDLLIMPORT bool assert_enabled;

/*
 * USE_ASSERT_CHECKING, if defined, turns on all the assertions.
 * - plai  9/5/90
 *
 * It should _NOT_ be defined in releases or in benchmark copies
 */

/*
 * Trap
 *		Generates an exception if the given condition is true.
 */
#define Trap(condition, errorType) \
	do { \
		if ((assert_enabled) && (condition)) \
			ExceptionalCondition(CppAsString(condition), (errorType), \
								 __FILE__, __LINE__); \
	} while (0)

/*
 *	TrapMacro is the same as Trap but it's intended for use in macros:
 *
 *		#define foo(x) (AssertMacro(x != 0) && bar(x))
 *
 *	Isn't CPP fun?
 */
#define TrapMacro(condition, errorType) \
	((bool) ((! assert_enabled) || ! (condition) || \
			 (ExceptionalCondition(CppAsString(condition), (errorType), \
								   __FILE__, __LINE__))))

#ifndef USE_ASSERT_CHECKING
#define Assert(condition)
#define AssertMacro(condition)	((void)true)
#define AssertArg(condition)
#define AssertState(condition)
#else
#define Assert(condition) \
		Trap(!(condition), "FailedAssertion")

#define AssertMacro(condition) \
		((void) TrapMacro(!(condition), "FailedAssertion"))

#define AssertArg(condition) \
		Trap(!(condition), "BadArgument")

#define AssertState(condition) \
		Trap(!(condition), "BadState")
#endif   /* USE_ASSERT_CHECKING */

extern int ExceptionalCondition(const char *conditionName,
					 const char *errorType,
					 const char *fileName, int lineNumber);


/* From pg_class.h */
#define		  RELPERSISTENCE_PERMANENT	'p'		/* regular table */
#define		  RELPERSISTENCE_UNLOGGED	'u'		/* unlogged permanent table */
#define		  RELPERSISTENCE_TEMP		't'		/* temporary table */

/* From pg_trigger.h */
/* Bits within tgtype */
#define TRIGGER_TYPE_ROW				(1 << 0)
#define TRIGGER_TYPE_BEFORE				(1 << 1)
#define TRIGGER_TYPE_INSERT				(1 << 2)
#define TRIGGER_TYPE_DELETE				(1 << 3)
#define TRIGGER_TYPE_UPDATE				(1 << 4)
#define TRIGGER_TYPE_TRUNCATE			(1 << 5)
#define TRIGGER_TYPE_INSTEAD			(1 << 6)
#define TRIGGER_TYPE_AFTER				0

/* From lock.h*/
/*
 * These are the valid values of type LOCKMODE for all the standard lock
 * methods (both DEFAULT and USER).
 */

/* NoLock is not a lock mode, but a flag value meaning "don't get a lock" */
#define NoLock					0

#define AccessShareLock			1		/* SELECT */
#define RowShareLock			2		/* SELECT FOR UPDATE/FOR SHARE */
#define RowExclusiveLock		3		/* INSERT, UPDATE, DELETE */
#define ShareUpdateExclusiveLock 4		/* VACUUM (non-FULL),ANALYZE, CREATE
										 * INDEX CONCURRENTLY */
#define ShareLock				5		/* CREATE INDEX (WITHOUT CONCURRENTLY) */
#define ShareRowExclusiveLock	6		/* like EXCLUSIVE MODE, but allows ROW
										 * SHARE */
#define ExclusiveLock			7		/* blocks ROW SHARE/SELECT...FOR
										 * UPDATE */
#define AccessExclusiveLock		8		/* ALTER TABLE, DROP TABLE, VACUUM
										 * FULL, and unqualified LOCK TABLE */
/*****
 * For calculating location
 ****/
// Sets the location of n to l divided by 2
#define SET_LOCATION(n, l) n->location = (l>0 ? l / 2: l)

#endif   /* POSTGRES_H */
