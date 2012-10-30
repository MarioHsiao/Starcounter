/*-------------------------------------------------------------------------
 *
 * port.h
 *	  Header for src/port/ compatibility functions.
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/port.h
 *
 *-------------------------------------------------------------------------
 */
#ifndef PG_PORT_H
#define PG_PORT_H

#include <ctype.h>

#ifdef USE_REPL_SNPRINTF

/*
 * Versions of libintl >= 0.13 try to replace printf() and friends with
 * macros to their own versions that understand the %$ format.	We do the
 * same, so disable their macros, if they exist.
 */
#ifdef vsnprintf
#undef vsnprintf
#endif
#ifdef snprintf
#undef snprintf
#endif
#ifdef sprintf
#undef sprintf
#endif
#ifdef vfprintf
#undef vfprintf
#endif
#ifdef fprintf
#undef fprintf
#endif
#ifdef printf
#undef printf
#endif

extern int	pg_vsnprintf(char *str, size_t count, const char *fmt, va_list args);
extern int
pg_snprintf(char *str, size_t count, const char *fmt,...)
/* This extension allows gcc to check the format string */
__attribute__((format(PG_PRINTF_ATTRIBUTE, 3, 4)));
extern int
pg_sprintf(char *str, const char *fmt,...)
/* This extension allows gcc to check the format string */
__attribute__((format(PG_PRINTF_ATTRIBUTE, 2, 3)));
extern int	pg_vfprintf(FILE *stream, const char *fmt, va_list args);
extern int
pg_fprintf(FILE *stream, const char *fmt,...)
/* This extension allows gcc to check the format string */
__attribute__((format(PG_PRINTF_ATTRIBUTE, 2, 3)));
extern int
pg_printf(const char *fmt,...)
/* This extension allows gcc to check the format string */
__attribute__((format(PG_PRINTF_ATTRIBUTE, 1, 2)));

/*
 *	The GCC-specific code below prevents the __attribute__(... 'printf')
 *	above from being replaced, and this is required because gcc doesn't
 *	know anything about pg_printf.
 */
#ifdef __GNUC__
#define vsnprintf(...)	pg_vsnprintf(__VA_ARGS__)
#define snprintf(...)	pg_snprintf(__VA_ARGS__)
#define sprintf(...)	pg_sprintf(__VA_ARGS__)
#define vfprintf(...)	pg_vfprintf(__VA_ARGS__)
#define fprintf(...)	pg_fprintf(__VA_ARGS__)
#define printf(...)		pg_printf(__VA_ARGS__)
#else
#define vsnprintf		pg_vsnprintf
#define snprintf		pg_snprintf
#define sprintf			pg_sprintf
#define vfprintf		pg_vfprintf
#define fprintf			pg_fprintf
#define printf			pg_printf
#endif
#endif   /* USE_REPL_SNPRINTF */

#endif   /* PG_PORT_H */
