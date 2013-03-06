/*-------------------------------------------------------------------------
 *
 * c.h
 *	  Fundamental C definitions.  This is included by every .c file in
 *	  PostgreSQL (via either postgres.h or postgres_fe.h, as appropriate).
 *
 *	  Note that the definitions here are not intended to be exposed to clients
 *	  of the frontend interface libraries --- so we don't worry much about
 *	  polluting the namespace with lots of stuff...
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/c.h
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
 *		0)		pg_config.h and standard system headers
 *		1)		hacks to cope with non-ANSI C compilers
 *		2)		bool, true, false, TRUE, FALSE, NULL
 *		3)		standard system types
 *		4)		IsValid macros for system types
 *		5)		offsetof, lengthof, endof, alignment
 *		6)		widely useful macros
 *		7)		random stuff
 *		8)		system-specific hacks
 *
 * NOTE: since this file is included by both frontend and backend modules, it's
 * almost certainly wrong to put an "extern" declaration here.	typedefs and
 * macros are the kind of thing that might go here.
 *
 *----------------------------------------------------------------
 */
#ifndef C_H
#define C_H

/*
 * We have to include stdlib.h here because it defines many of these macros
 * on some platforms, and we only want our definitions used if stdlib.h doesn't
 * have its own.  The same goes for stddef and stdarg if present.
 */

#include "pg_config.h"

/*
 * Object ID is a fundamental type in Postgres.
 */
typedef unsigned int Oid;

#ifdef __cplusplus
#define InvalidOid		(Oid(0))
#else
#define InvalidOid		((Oid) 0)
#endif

#if _MSC_VER >= 1400 || defined(WIN64)
#define errcode __msvc_errcode
#include <crtdefs.h>
#undef errcode
#endif

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <wchar.h>
#include <stddef.h>
#include <stdarg.h>
#ifdef HAVE_STRINGS_H
#include <strings.h>
#endif
#ifdef HAVE_STDINT_H
#include <stdint.h>
#endif
#include <sys/types.h>

#include <errno.h>
#if defined(WIN32) || defined(__CYGWIN__)
#include <fcntl.h>				/* ensure O_BINARY is available */
#endif
#ifdef HAVE_SUPPORTDEFS_H
#include <SupportDefs.h>
#endif

/* Must be before gettext() games below */
#include <locale.h>

#define _(x) gettext(x)

#ifdef ENABLE_NLS
#include <libintl.h>
#else
#define gettext(x) (x)
#define dgettext(d,x) (x)
#define ngettext(s,p,n) ((n) == 1 ? (s) : (p))
#define dngettext(d,s,p,n) ((n) == 1 ? (s) : (p))
#endif


/* ----------------------------------------------------------------
 *				Section 1: hacks to cope with non-ANSI C compilers
 *
 * type prefixes (const, signed, volatile, inline) are handled in pg_config.h.
 * ----------------------------------------------------------------
 */

/*
 * CppAsString
 *		Convert the argument to a string, using the C preprocessor.
 * CppConcat
 *		Concatenate two arguments together, using the C preprocessor.
 *
 * Note: the standard Autoconf macro AC_C_STRINGIZE actually only checks
 * whether #identifier works, but if we have that we likely have ## too.
 */
#if defined(HAVE_STRINGIZE)

#define CppAsString(identifier) #identifier
#define CppConcat(x, y)			x##y
#else							/* !HAVE_STRINGIZE */

#define CppAsString(identifier) "identifier"

/*
 * CppIdentity -- On Reiser based cpp's this is used to concatenate
 *		two tokens.  That is
 *				CppIdentity(A)B ==> AB
 *		We renamed it to _private_CppIdentity because it should not
 *		be referenced outside this file.  On other cpp's it
 *		produces  A  B.
 */
#define _priv_CppIdentity(x)x
#define CppConcat(x, y)			_priv_CppIdentity(x)y
#endif   /* !HAVE_STRINGIZE */

/*
 * dummyret is used to set return values in macros that use ?: to make
 * assignments.  gcc wants these to be void, other compilers like char
 */
#ifdef __GNUC__					/* GNU cc */
#define dummyret	void
#else
#define dummyret	char
#endif

#ifndef __GNUC__
#define __attribute__(_arg_)
#endif

/* ----------------------------------------------------------------
 *				Section 2:	bool, true, false, TRUE, FALSE, NULL
 * ----------------------------------------------------------------
 */

/*
 * bool
 *		Boolean value, either true or false.
 *
 * XXX for C++ compilers, we assume the compiler has a compatible
 * built-in definition of bool.
 */

#ifndef __cplusplus

#ifndef bool
typedef char bool;
#endif

#ifndef true
#define true	((bool) 1)
#endif

#ifndef false
#define false	((bool) 0)
#endif
#endif   /* not C++ */

typedef bool *BoolPtr;

#ifndef TRUE
#define TRUE	1
#endif

#ifndef FALSE
#define FALSE	0
#endif

/*
 * NULL
 *		Null pointer.
 */
#ifndef NULL
#define NULL	((void *) 0)
#endif


/* ----------------------------------------------------------------
 *				Section 3:	standard system types
 * ----------------------------------------------------------------
 */

/*
 * Pointer
 *		Variable holding address of any memory resident object.
 *
 *		XXX Pointer arithmetic is done with this, so it can't be void *
 *		under "true" ANSI compilers.
 */
typedef char *Pointer;

/*
 * intN
 *		Signed integer, EXACTLY N BITS IN SIZE,
 *		used for numerical computations and the
 *		frontend/backend protocol.
 */
#ifndef HAVE_INT8
typedef signed char int8;		/* == 8 bits */
typedef signed short int16;		/* == 16 bits */
typedef signed int int32;		/* == 32 bits */
#endif   /* not HAVE_INT8 */

/*
 * uintN
 *		Unsigned integer, EXACTLY N BITS IN SIZE,
 *		used for numerical computations and the
 *		frontend/backend protocol.
 */
#ifndef HAVE_UINT8
typedef unsigned char uint8;	/* == 8 bits */
typedef unsigned short uint16;	/* == 16 bits */
typedef unsigned int uint32;	/* == 32 bits */
#endif   /* not HAVE_UINT8 */

/*
 * bitsN
 *		Unit of bitwise operation, AT LEAST N BITS IN SIZE.
 */
typedef uint8 bits8;			/* >= 8 bits */
typedef uint16 bits16;			/* >= 16 bits */
typedef uint32 bits32;			/* >= 32 bits */

/*
 * 64-bit integers
 */
#ifdef HAVE_LONG_INT_64
/* Plain "long int" fits, use it */

#ifndef HAVE_INT64
typedef long int int64;
#endif
#ifndef HAVE_UINT64
typedef unsigned long int uint64;
#endif
#elif defined(HAVE_LONG_LONG_INT_64)
/* We have working support for "long long int", use that */

#ifndef HAVE_INT64
typedef long long int int64;
#endif
#ifndef HAVE_UINT64
typedef unsigned long long int uint64;
#endif
#else
/* neither HAVE_LONG_INT_64 nor HAVE_LONG_LONG_INT_64 */
#error must have a working 64-bit integer datatype
#endif

/* Decide if we need to decorate 64-bit constants */
#ifdef HAVE_LL_CONSTANTS
#define INT64CONST(x)  ((int64) x##LL)
#else
#define INT64CONST(x)  ((int64) x)
#endif


/* Select timestamp representation (float8 or int64) */
#ifdef USE_INTEGER_DATETIMES
#define HAVE_INT64_TIMESTAMP
#endif

/*
 * Size
 *		Size of any memory resident object, as returned by sizeof.
 */
typedef size_t Size;

/*
 * Index
 *		Index into any memory resident array.
 *
 * Note:
 *		Indices are non negative.
 */
typedef unsigned int Index;

/*
 * Common Postgres datatype names (as used in the catalogs)
 */
typedef int16 int2;
typedef int32 int4;
typedef float float4;
typedef double float8;

/*
 * Oid, RegProcedure, TransactionId, SubTransactionId, MultiXactId,
 * CommandId
 */

typedef uint32 CommandId;

/* ----------------------------------------------------------------
 *				Section 4:	IsValid macros for system types
 * ----------------------------------------------------------------
 */
/*
 * PointerIsValid
 *		True iff pointer is valid.
 */
#define PointerIsValid(pointer) ((void*)(pointer) != NULL)

/* ----------------------------------------------------------------
 *				Section 5:	offsetof, lengthof, endof, alignment
 * ----------------------------------------------------------------
 */
/*
 * lengthof
 *		Number of elements in an array.
 */
#define lengthof(array) (sizeof (array) / sizeof ((array)[0]))

/* ----------------
 * Alignment macros: align a length or address appropriately for a given type.
 * The fooALIGN() macros round up to a multiple of the required alignment,
 * while the fooALIGN_DOWN() macros round down.  The latter are more useful
 * for problems like "how many X-sized structures will fit in a page?".
 *
 * NOTE: TYPEALIGN[_DOWN] will not work if ALIGNVAL is not a power of 2.
 * That case seems extremely unlikely to be needed in practice, however.
 * ----------------
 */

#define TYPEALIGN(ALIGNVAL,LEN)  \
	(((intptr_t) (LEN) + ((ALIGNVAL) - 1)) & ~((intptr_t) ((ALIGNVAL) - 1)))

#define SHORTALIGN(LEN)			TYPEALIGN(ALIGNOF_SHORT, (LEN))
#define INTALIGN(LEN)			TYPEALIGN(ALIGNOF_INT, (LEN))
#define LONGALIGN(LEN)			TYPEALIGN(ALIGNOF_LONG, (LEN))
#define DOUBLEALIGN(LEN)		TYPEALIGN(ALIGNOF_DOUBLE, (LEN))
#define MAXALIGN(LEN)			TYPEALIGN(MAXIMUM_ALIGNOF, (LEN))

/* ----------------------------------------------------------------
 *				Section 6:	widely useful macros
 * ----------------------------------------------------------------
 */
/*
 * Max
 *		Return the maximum of two numbers.
 */
#define Max(x, y)		((x) > (y) ? (x) : (y))

/*
 * Min
 *		Return the minimum of two numbers.
 */
#define Min(x, y)		((x) < (y) ? (x) : (y))

/*
 * Abs
 *		Return the absolute value of the argument.
 */
#define Abs(x)			((x) >= 0 ? (x) : -(x))

/* Get a bit mask of the bits set in non-long aligned addresses */
#define LONG_ALIGN_MASK (sizeof(long) - 1)

/*
 * MemSet
 *	Exactly the same as standard library function memset(), but considerably
 *	faster for zeroing small word-aligned structures (such as parsetree nodes).
 *	This has to be a macro because the main point is to avoid function-call
 *	overhead.	However, we have also found that the loop is faster than
 *	native libc memset() on some platforms, even those with assembler
 *	memset() functions.  More research needs to be done, perhaps with
 *	MEMSET_LOOP_LIMIT tests in configure.
 */
#define MemSet(start, val, len) \
	do \
	{ \
		/* must be void* because we don't know if it is integer aligned yet */ \
		void   *_vstart = (void *) (start); \
		int		_val = (val); \
		Size	_len = (len); \
\
		if ((((intptr_t) _vstart) & LONG_ALIGN_MASK) == 0 && \
			(_len & LONG_ALIGN_MASK) == 0 && \
			_val == 0 && \
			_len <= MEMSET_LOOP_LIMIT && \
			/* \
			 *	If MEMSET_LOOP_LIMIT == 0, optimizer should find \
			 *	the whole "if" false at compile time. \
			 */ \
			MEMSET_LOOP_LIMIT != 0) \
		{ \
			long *_start = (long *) _vstart; \
			long *_stop = (long *) ((char *) _start + _len); \
			while (_start < _stop) \
				*_start++ = 0; \
		} \
		else \
			memset(_vstart, _val, _len); \
	} while (0)

/*
 * MemSetAligned is the same as MemSet except it omits the test to see if
 * "start" is word-aligned.  This is okay to use if the caller knows a-priori
 * that the pointer is suitably aligned (typically, because he just got it
 * from palloc(), which always delivers a max-aligned pointer).
 */
#define MemSetAligned(start, val, len) \
	do \
	{ \
		long   *_start = (long *) (start); \
		int		_val = (val); \
		Size	_len = (len); \
\
		if ((_len & LONG_ALIGN_MASK) == 0 && \
			_val == 0 && \
			_len <= MEMSET_LOOP_LIMIT && \
			MEMSET_LOOP_LIMIT != 0) \
		{ \
			long *_stop = (long *) ((char *) _start + _len); \
			while (_start < _stop) \
				*_start++ = 0; \
		} \
		else \
			memset(_start, _val, _len); \
	} while (0)


/*
 * MemSetTest/MemSetLoop are a variant version that allow all the tests in
 * MemSet to be done at compile time in cases where "val" and "len" are
 * constants *and* we know the "start" pointer must be word-aligned.
 * If MemSetTest succeeds, then it is okay to use MemSetLoop, otherwise use
 * MemSetAligned.  Beware of multiple evaluations of the arguments when using
 * this approach.
 */
#define MemSetTest(val, len) \
	( ((len) & LONG_ALIGN_MASK) == 0 && \
	(len) <= MEMSET_LOOP_LIMIT && \
	MEMSET_LOOP_LIMIT != 0 && \
	(val) == 0 )

#define MemSetLoop(start, val, len) \
	do \
	{ \
		long * _start = (long *) (start); \
		long * _stop = (long *) ((char *) _start + (Size) (len)); \
	\
		while (_start < _stop) \
			*_start++ = 0; \
	} while (0)


/* ----------------------------------------------------------------
 *				Section 7:	random stuff
 * ----------------------------------------------------------------
 */

/* msb for char */
#define HIGHBIT					(0x80)
#define IS_HIGHBIT_SET(ch)		((unsigned char)(ch) & HIGHBIT)

/* ----------------------------------------------------------------
 *				Section 8: system-specific hacks
 *
 *		This should be limited to things that absolutely have to be
 *		included in every source file.	The port-specific header file
 *		is usually a better place for this sort of thing.
 * ----------------------------------------------------------------
 */

/*
 * Provide prototypes for routines not present in a particular machine's
 * standard C library.
 */

#if !HAVE_DECL_SNPRINTF
extern int
snprintf(char *str, size_t count, const char *fmt,...)
/* This extension allows gcc to check the format string */
__attribute__((format(PG_PRINTF_ATTRIBUTE, 3, 4)));
#endif

#if !HAVE_DECL_VSNPRINTF
extern int	vsnprintf(char *str, size_t count, const char *fmt, va_list args);
#endif

#if !defined(HAVE_MEMMOVE) && !defined(memmove)
#define memmove(d, s, c)		bcopy(s, d, c)
#endif

/* no special DLL markers on most ports */
#ifndef PGDLLIMPORT
#define PGDLLIMPORT
#endif

/* /port compatibility functions */
#include "port.h"

#endif   /* C_H */
