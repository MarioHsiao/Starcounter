/*-------------------------------------------------------------------------
 *
 * xml.h
 *	  Declarations for XML data type support.
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * src/include/utils/xml.h
 *
 *-------------------------------------------------------------------------
 */

#ifndef XML_H
#define XML_H

typedef enum
{
	XML_STANDALONE_YES,
	XML_STANDALONE_NO,
	XML_STANDALONE_NO_VALUE,
	XML_STANDALONE_OMITTED
}	XmlStandaloneType;

typedef enum
{
	XMLBINARY_BASE64,
	XMLBINARY_HEX
}	XmlBinaryType;

#endif   /* XML_H */
