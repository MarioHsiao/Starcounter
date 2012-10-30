/*-------------------------------------------------------------------------
 *
 * pg_wchar.h
 *	  multibyte-character support
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/mb/pg_wchar.h
 *
 *	NOTES
 *		This is used both by the backend and by libpq, but should not be
 *		included by libpq client programs.	In particular, a libpq client
 *		should not assume that the encoding IDs used by the version of libpq
 *		it's linked to match up with the IDs declared here.
 *
 *-------------------------------------------------------------------------
 */
#ifndef PG_WCHAR_H
#define PG_WCHAR_H

/*
 * The pg_wchar type
 */
typedef unsigned int pg_wchar;

/*
 * various definitions for EUC
 */
#define SS2 0x8e				/* single shift 2 (JIS0201) */
#define SS3 0x8f				/* single shift 3 (JIS0212) */

/*
 * SJIS validation macros
 */
#define ISSJISHEAD(c) (((c) >= 0x81 && (c) <= 0x9f) || ((c) >= 0xe0 && (c) <= 0xfc))
#define ISSJISTAIL(c) (((c) >= 0x40 && (c) <= 0x7e) || ((c) >= 0x80 && (c) <= 0xfc))

/*
 * Leading byte types or leading prefix byte for MULE internal code.
 * See http://www.xemacs.org for more details.	(there is a doc titled
 * "XEmacs Internals Manual", "MULE Character Sets and Encodings"
 * section.)
 */
/*
 * Is a leading byte for "official" single byte encodings?
 */
#define IS_LC1(c)	((unsigned char)(c) >= 0x81 && (unsigned char)(c) <= 0x8d)
/*
 * Is a prefix byte for "private" single byte encodings?
 */
#define IS_LCPRV1(c)	((unsigned char)(c) == 0x9a || (unsigned char)(c) == 0x9b)
/*
 * Is a leading byte for "official" multibyte encodings?
 */
#define IS_LC2(c)	((unsigned char)(c) >= 0x90 && (unsigned char)(c) <= 0x99)
/*
 * Is a prefix byte for "private" multibyte encodings?
 */
#define IS_LCPRV2(c)	((unsigned char)(c) == 0x9c || (unsigned char)(c) == 0x9d)


/*
 * PostgreSQL encoding identifiers
 *
 * WARNING: the order of this enum must be same as order of entries
 *			in the pg_enc2name_tbl[] array (in mb/encnames.c), and
 *			in the pg_wchar_table[] array (in mb/wchar.c)!
 *
 *			If you add some encoding don't forget to check
 *			PG_ENCODING_BE_LAST macro.
 *
 * PG_SQL_ASCII is default encoding and must be = 0.
 *
 * XXX	We must avoid renumbering any backend encoding until libpq's major
 * version number is increased beyond 5; it turns out that the backend
 * encoding IDs are effectively part of libpq's ABI as far as 8.2 initdb and
 * psql are concerned.
 */
typedef enum pg_enc
{
	PG_SQL_ASCII = 0,			/* SQL/ASCII */
	PG_EUC_JP,					/* EUC for Japanese */
	PG_EUC_CN,					/* EUC for Chinese */
	PG_EUC_KR,					/* EUC for Korean */
	PG_EUC_TW,					/* EUC for Taiwan */
	PG_EUC_JIS_2004,			/* EUC-JIS-2004 */
	PG_UTF8,					/* Unicode UTF8 */
	PG_MULE_INTERNAL,			/* Mule internal code */
	PG_LATIN1,					/* ISO-8859-1 Latin 1 */
	PG_LATIN2,					/* ISO-8859-2 Latin 2 */
	PG_LATIN3,					/* ISO-8859-3 Latin 3 */
	PG_LATIN4,					/* ISO-8859-4 Latin 4 */
	PG_LATIN5,					/* ISO-8859-9 Latin 5 */
	PG_LATIN6,					/* ISO-8859-10 Latin6 */
	PG_LATIN7,					/* ISO-8859-13 Latin7 */
	PG_LATIN8,					/* ISO-8859-14 Latin8 */
	PG_LATIN9,					/* ISO-8859-15 Latin9 */
	PG_LATIN10,					/* ISO-8859-16 Latin10 */
	PG_WIN1256,					/* windows-1256 */
	PG_WIN1258,					/* Windows-1258 */
	PG_WIN866,					/* (MS-DOS CP866) */
	PG_WIN874,					/* windows-874 */
	PG_KOI8R,					/* KOI8-R */
	PG_WIN1251,					/* windows-1251 */
	PG_WIN1252,					/* windows-1252 */
	PG_ISO_8859_5,				/* ISO-8859-5 */
	PG_ISO_8859_6,				/* ISO-8859-6 */
	PG_ISO_8859_7,				/* ISO-8859-7 */
	PG_ISO_8859_8,				/* ISO-8859-8 */
	PG_WIN1250,					/* windows-1250 */
	PG_WIN1253,					/* windows-1253 */
	PG_WIN1254,					/* windows-1254 */
	PG_WIN1255,					/* windows-1255 */
	PG_WIN1257,					/* windows-1257 */
	PG_KOI8U,					/* KOI8-U */
	/* PG_ENCODING_BE_LAST points to the above entry */

	/* followings are for client encoding only */
	PG_SJIS,					/* Shift JIS (Winindows-932) */
	PG_BIG5,					/* Big5 (Windows-950) */
	PG_GBK,						/* GBK (Windows-936) */
	PG_UHC,						/* UHC (Windows-949) */
	PG_GB18030,					/* GB18030 */
	PG_JOHAB,					/* EUC for Korean JOHAB */
	PG_SHIFT_JIS_2004,			/* Shift-JIS-2004 */
	_PG_LAST_ENCODING_			/* mark only */

} pg_enc;

/*
 * Please use these tests before access to pg_encconv_tbl[]
 * or to other places...
 */
#define PG_VALID_ENCODING(_enc) \
		((_enc) >= 0 && (_enc) < _PG_LAST_ENCODING_)

/*
 * Careful:
 *
 * if (PG_VALID_ENCODING(encoding))
 *		pg_enc2name_tbl[ encoding ];
 */
typedef struct pg_enc2name
{
	char	   *name;
	pg_enc		encoding;
#ifdef WIN32
	unsigned	codepage;		/* codepage for WIN32 */
#endif
} pg_enc2name;

extern pg_enc2name pg_enc2name_tbl[];

/*
 * Encoding names for gettext
 */
typedef struct pg_enc2gettext
{
	pg_enc		encoding;
	const char *name;
} pg_enc2gettext;

/*
 * pg_wchar stuff
 */
typedef int (*mb2wchar_with_len_converter) (const unsigned char *from,
														pg_wchar *to,
														int len);

typedef int (*mblen_converter) (const unsigned char *mbstr);

typedef int (*mbdisplaylen_converter) (const unsigned char *mbstr);

typedef int (*mbverifier) (const unsigned char *mbstr, int len);

typedef struct
{
	mb2wchar_with_len_converter mb2wchar_with_len;		/* convert a multibyte
														 * string to a wchar */
	mblen_converter mblen;		/* get byte length of a char */
	mbdisplaylen_converter dsplen;		/* get display width of a char */
	mbverifier	mbverify;		/* verify multibyte sequence */
	int			maxmblen;		/* max bytes for a char in this encoding */
} pg_wchar_tbl;

extern pg_wchar_tbl pg_wchar_table[];

extern int	pg_mblen(const char *mbstr);
extern int	pg_encoding_max_length(int encoding);
extern int	GetDatabaseEncoding(void);

extern unsigned char *unicode_to_utf8(pg_wchar c, unsigned char *utf8string);
extern pg_wchar utf8_to_unicode(const unsigned char *c);
extern int	pg_utf_mblen(const unsigned char *);

extern bool pg_verifymbstr(const char *mbstr, int len, bool noError);
extern int pg_verify_mbstr_len(int encoding, const char *mbstr, int len,
	bool noError);

extern void report_invalid_encoding(int encoding, const char *mbstr, int len);

extern bool pg_utf8_islegal(const unsigned char *source, int length);

#endif   /* PG_WCHAR_H */
