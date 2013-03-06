/*-------------------------------------------------------------------------
 *
 * datetime.h
 *	  Definitions for the date/time and other date/time support code.
 *	  The support code is shared with other date data types,
 *	   including abstime, reltime, date, and time.
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/utils/datetime.h
 *
 *-------------------------------------------------------------------------
 */
#ifndef DATETIME_H
#define DATETIME_H

#include <limits.h>
#include <math.h>

/* Sc: From timestamp.h */
#define INTERVAL_MASK(b) (1 << (b))
/* Macros to handle packing and unpacking the typmod field for intervals */
#define INTERVAL_FULL_RANGE (0x7FFF)
/* SC: Back to datetime.h */
/*
 * Fields for time decoding.
 *
 * Can't have more of these than there are bits in an unsigned int
 * since these are turned into bit masks during parsing and decoding.
 *
 * Furthermore, the values for YEAR, MONTH, DAY, HOUR, MINUTE, SECOND
 * must be in the range 0..14 so that the associated bitmasks can fit
 * into the left half of an INTERVAL's typmod value.  Since those bits
 * are stored in typmods, you can't change them without initdb!
 */

#define RESERV	0
#define MONTH	1
#define YEAR	2
#define DAY		3
#define JULIAN	4
#define TZ		5
#define DTZ		6
#define DTZMOD	7
#define IGNORE_DTF	8
#define AMPM	9
#define HOUR	10
#define MINUTE	11
#define SECOND	12
#define MILLISECOND 13
#define MICROSECOND 14
#define DOY		15
#define DOW		16
#define UNITS	17
#define ADBC	18
/* these are only for relative dates */
#define AGO		19
#define ABS_BEFORE		20
#define ABS_AFTER		21
/* generic fields to help with parsing */
#define ISODATE 22
#define ISOTIME 23
/* these are only for parsing intervals */
#define WEEK		24
#define DECADE		25
#define CENTURY		26
#define MILLENNIUM	27
/* reserved for unrecognized string values */
#define UNKNOWN_FIELD	31


#endif   /* DATETIME_H */
