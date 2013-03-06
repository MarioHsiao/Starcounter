%{
/*-------------------------------------------------------------------------
 *
 * scan.l
 *	  lexical scanner for PostgreSQL
 *
 * NOTE NOTE NOTE:
 *
 * The rules in this file must be kept in sync with psql's lexer!!!
 *
 * The rules are designed so that the scanner never has to backtrack,
 * in the sense that there is always a rule that can match the input
 * consumed so far (the rule action may internally throw back some input
 * with yyless(), however).  As explained in the flex manual, this makes
 * for a useful speed increase --- about a third faster than a plain -CF
 * lexer, in simple testing.  The extra complexity is mostly in the rules
 * for handling float numbers and continued string literals.  If you change
 * the lexical rules, verify that you haven't broken the no-backtrack
 * property by running flex with the "-b" option and checking that the
 * resulting "lex.backup" file says that no backing up is needed.
 *
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 * IDENTIFICATION
 *	  src/backend/parser/scan.l
 *
 *-------------------------------------------------------------------------
 */
#include "../General/postgres.h"

#include <ctype.h>
#include <unistd.h>

#include "parser.h"				/* only needed for GUC variables */
#include "scanner.h"
#include "scansup.h"
#include "../Utils/pg_wchar.h"


#ifdef _MSC_VER
# pragma warning(push)
#pragma warning(disable: 4244)
#pragma warning(disable: 4267)
#endif // _MSC_VER

/* Avoid exit() on fatal scanner errors (a bit ugly -- see yy_fatal_error) */
#undef fprintf
#define fprintf(file, fmt, msg)  ThrowExceptionCode(SCERRUNEXPSQLSCANNER,msg)

/*
 * GUC variables.  This is a DIRECT violation of the warning given at the
 * head of gram.y, ie flex/bison code must not depend on any GUC variables;
 * as such, changing their values can induce very unintuitive behavior.
 * But we shall have to live with it as a short-term thing until the switch
 * to SQL-standard string syntax is complete.
 */
const int				backslash_quote = BACKSLASH_QUOTE_SAFE_ENCODING;
const bool			escape_string_warning = true;

/*
 * Set the type of YYSTYPE.
 */
#define YYSTYPE core_YYSTYPE

/*
 * Set the type of yyextra.  All state variables used by the scanner should
 * be in yyextra, *not* statically allocated.
 */
#define YY_EXTRA_TYPE core_yy_extra_type *

/*
 * Each call to yylex must set yylloc to the location of the found token
 * (expressed as a byte offset from the start of the input text).
 * When we parse a token that requires multiple lexer rules to process,
 * this should be done in the first such rule, else yylloc will point
 * into the middle of the token.
 */
#define SET_YYLLOC()  (*(yylloc) = yytext - yyextra->scanbuf)

/*
 * Advance yylloc by the given number of bytes.
 */
#define ADVANCE_YYLLOC(delta)  ( *(yylloc) += (delta) )

#define startlit()  ( yyextra->literallen = 0 )
static void addlit(char *ytext, int yleng, core_yyscan_t yyscanner);
static void addlitchar(unsigned char ychar, core_yyscan_t yyscanner);
static wchar_t *litbufdup(core_yyscan_t yyscanner);
static int	process_integer_literal(const wchar_t *token, YYSTYPE *lval);

#define yyerror(msg)  scanner_yyerror(msg, yyscanner)

/*
 * Work around a bug in flex 2.5.35: it emits a couple of functions that
 * it forgets to emit declarations for.  Since we use -Wmissing-prototypes,
 * this would cause warnings.  Providing our own declarations should be
 * harmless even when the bug gets fixed.
 */
extern int	core_yyget_column(yyscan_t yyscanner);
extern void core_yyset_column(int column_no, yyscan_t yyscanner);

%}

%option reentrant
%option bison-bridge
%option bison-locations
%option 8bit
%option never-interactive
%option nodefault
%option noinput
%option nounput
%option noyywrap
%option noyyalloc
%option noyyrealloc
%option noyyfree
%option warn
%option prefix="core_yy"

/*
 * OK, here is a short description of lex/flex rules behavior.
 * The longest pattern which matches an input string is always chosen.
 * For equal-length patterns, the first occurring in the rules list is chosen.
 * INITIAL is the starting state, to which all non-conditional rules apply.
 * Exclusive states change parsing rules while the state is active.  When in
 * an exclusive state, only those rules defined for that state apply.
 *
 * We use exclusive states for quoted strings, extended comments,
 * and to eliminate parsing troubles for numeric strings.
 * Exclusive states:
 *  <xb> bit string literal
 *  <xc> extended C-style comments
 *  <xd> delimited identifiers (double-quoted identifiers)
 *  <xh> hexadecimal numeric string
 *  <xq> standard quoted strings
 *  <xe> extended quoted strings (support backslash escape sequences)
 *  <xui> quoted identifier with Unicode escapes
 *  <xus> quoted string with Unicode escapes
 *  <xeu> Unicode surrogate pair in extended quoted string
 */

%x xb
%x xc
%x xd
%x xh
%x xq

/*
 * In order to make the world safe for Windows and Mac clients as well as
 * Unix ones, we accept either \n or \r as a newline.  A DOS-style \r\n
 * sequence will be seen as two successive newlines, but that doesn't cause
 * any problems.  Comments that start with -- and extend to the next
 * newline are treated as equivalent to a single whitespace character.
 *
 * NOTE a fine point: if there is no newline following --, we will absorb
 * everything to the end of the input as a comment.  This is correct.  Older
 * versions of Postgres failed to recognize -- as a comment if the input
 * did not end with a newline.
 *
 * XXX perhaps \f (formfeed) should be treated as a newline as well?
 *
 * XXX if you change the set of whitespace characters, fix scanner_isspace()
 * to agree, and see also the plpgsql lexer.
 */

space			[ \t\n\r\f]\x00
horiz_space		[ \t\f]\x00
newline			[\n\r]\x00
non_newline		[^\n\r]\x00

comment			("-"\x00"-"\x00{non_newline}*)

whitespace		({space}+|{comment})

cyrillic		[\x00-\xFF]\x04
qcharletter		[^']\x00
qletter			{qcharletter}|{cyrillic}
dqcharletter	[^"]\x00
dqletter		{dqcharletter}|{cyrillic}

/*
 * SQL requires at least one newline in the whitespace separating
 * string literals that are to be concatenated.  Silly, but who are we
 * to argue?  Note that {whitespace_with_newline} should not have * after
 * it, whereas {whitespace} should generally have a * after it...
 */

special_whitespace		({space}+|{comment}{newline})
horiz_whitespace		({horiz_space}|{comment})
whitespace_with_newline	({horiz_whitespace}*{newline}{special_whitespace}*)

/*
 * To ensure that {quotecontinue} can be scanned without having to back up
 * if the full pattern isn't matched, we include trailing whitespace in
 * {quotestop}.  This matches all cases where {quotecontinue} fails to match,
 * except for {quote} followed by whitespace and just one "-" (not two,
 * which would start a {comment}).  To cover that we have {quotefail}.
 * The actions for {quotestop} and {quotefail} must throw back characters
 * beyond the quote proper.
 */
quote			'\x00
quotestop		{quote}{whitespace}*
quotecontinue	{quote}{whitespace_with_newline}{quote}
quotefail		{quote}{whitespace}*"-"\x00

/* Bit string
 * It is tempting to scan the string for only those characters
 * which are allowed. However, this leads to silently swallowed
 * characters if illegal characters are included in the string.
 * For example, if xbinside is [01] then B'ABCD' is interpreted
 * as a zero-length string, and the ABCD' is lost!
 * Better to pass the string forward and let the input routines
 * validate the contents.
 */
xbstart			[bB]\x00{quote}
xbinside		{qletter}*

/* Hexadecimal number */
xhstart			[xX]\x00{quote}
xhinside		{qletter}*

/* National character */
xnstart			[nN]\x00{quote}

/* Extended quote
 * xqdouble implements embedded quote, ''''
 */
xqstart			{quote}
xqdouble		{quote}{quote}
xqinside		{qletter}+

/* Double quote
 * Allows embedded spaces and other special characters into identifiers.
 */
dquote			\"\x00
xdstart			{dquote}
xdstop			{dquote}
xddouble		{dquote}{dquote}
xdinside		{dqletter}+

/* C-style comments
 *
 * The "extended comment" syntax closely resembles allowable operator syntax.
 * The tricky part here is to get lex to recognize a string starting with
 * slash-star as a comment, when interpreting it as an operator would produce
 * a longer match --- remember lex will prefer a longer match!  Also, if we
 * have something like plus-slash-star, lex will think this is a 3-character
 * operator whereas we want to see it as a + operator and a comment start.
 * The solution is two-fold:
 * 1. append {op_chars}* to xcstart so that it matches as much text as
 *    {operator} would. Then the tie-breaker (first matching rule of same
 *    length) ensures xcstart wins.  We put back the extra stuff with yyless()
 *    in case it contains a star-slash that should terminate the comment.
 * 2. In the operator rule, check for slash-star within the operator, and
 *    if found throw it back with yyless().  This handles the plus-slash-star
 *    problem.
 * Dash-dash comments have similar interactions with the operator rule.
 */
xcstart			\/\x00\*\x00{op_chars}*
xcstop			(\*\x00)+\/\x00
xcinside		([^*/]\x00)+

digit			[0-9]\x00
ident_start		[A-Za-z\200-\377_]\x00|{cyrillic}
ident_cont		[A-Za-z\200-\377_0-9\$]\x00|{cyrillic}

identifier		{ident_start}{ident_cont}*

dot_dot			\.\x00\.\x00
colon_equals	":"\x00"="\x00

/*
 * "self" is the set of chars that should be returned as single-character
 * tokens.  "op_chars" is the set of chars that can make up "Op" tokens,
 * which can be one or more characters long (but if a single-char token
 * appears in the "self" set, it is not to be returned as an Op).  Note
 * that the sets overlap, but each has some chars that are not in the other.
 *
 * If you change either set, adjust the character lists appearing in the
 * rule for "operator"!
 */
self			[,()\[\].;\:\?\+\-\*\/\%\^\<\>\=]\x00
op_chars		[\~\!\@\#\^\&\|\`\+\-\*\/\%\<\>\=]\x00
operator		{op_chars}{op_chars}

/* we no longer allow unary minus in numbers.
 * instead we pass it separately to parser. there it gets
 * coerced via doNegate() -- Leon aug 20 1999
 *
* {decimalfail} is used because we would like "1..10" to lex as 1, dot_dot, 10.
*
 * {realfail1} and {realfail2} are added to prevent the need for scanner
 * backup when the {real} rule fails to match completely.
 */

integer			{digit}+
decimal			(({digit}*\.\x00{digit}+)|({digit}+\.\x00{digit}*))
decimalfail		{digit}+\.\x00\.\x00
real			({integer}|{decimal})[Ee]\00([-+]\x00)?{digit}+
realfail1		({integer}|{decimal})[Ee]\x00
realfail2		({integer}|{decimal})[Ee]\x00[-+]\x00

param			\$\x00{integer}|\?\x00

other			.

/*
 * Dollar quoted strings are totally opaque, and no escaping is done on them.
 * Other quoted strings must allow some special characters such as single-quote
 *  and newline.
 * Embedded single-quotes are implemented both in the SQL standard
 *  style of two adjacent single quotes "''" and in the Postgres/Java style
 *  of escaped-quote "\'".
 * Other embedded escaped characters are matched explicitly and the leading
 *  backslash is dropped from the string.
 * Note that xcstart must appear before operator, as explained above!
 *  Also whitespace (comment) must appear before operator.
 */

%%

{whitespace}	{
					/* ignore */
				}

{xcstart}		{
					/* Set location in case of syntax error in comment */
					SET_YYLLOC();
					yyextra->xcdepth = 0;
					BEGIN(xc);
					/* Put back any characters past slash-star; see above */
					yyless(2);
				}

<xc>{xcstart}	{
					(yyextra->xcdepth)++;
					/* Put back any characters past slash-star; see above */
					yyless(2);
				}

<xc>{xcstop}	{
					if (yyextra->xcdepth <= 0)
						BEGIN(INITIAL);
					else
						(yyextra->xcdepth)--;
				}

<xc>{xcinside}	{
					/* ignore */
				}

<xc>{op_chars}	{
					/* ignore */
				}

<xc>\*+			{
					/* ignore */
				}

<xc><<EOF>>		{ yyerror("Unterminated /* comment."); }

{xbstart}		{
					/* Binary bit type.
					 * At some point we should simply pass the string
					 * forward to the parser and label it there.
					 * In the meantime, place a leading "b" on the string
					 * to mark it for the input routine as a binary string.
					 */
					SET_YYLLOC();
					BEGIN(xb);
					startlit();
					addlitchar('b', yyscanner);
				}
<xb>{quotestop}	|
<xb>{quotefail} {
					yyless(sizeof(wchar_t));
					BEGIN(INITIAL);
					yylval->str = litbufdup(yyscanner);
					return BCONST;
				}
<xh>{xhinside}	|
<xb>{xbinside}	{
					addlit(yytext, yyleng, yyscanner);
				}
<xh>{quotecontinue}	|
<xb>{quotecontinue}	{
					/* ignore */
				}
<xb><<EOF>>		{ yyerror("Unterminated bit string literal."); }

{xhstart}		{
					/* Hexadecimal bit type.
					 * At some point we should simply pass the string
					 * forward to the parser and label it there.
					 * In the meantime, place a leading "x" on the string
					 * to mark it for the input routine as a hex string.
					 */
					SET_YYLLOC();
					BEGIN(xh);
					startlit();
					addlitchar('x', yyscanner);
				}
<xh>{quotestop}	|
<xh>{quotefail} {
					yyless(sizeof(wchar_t));
					BEGIN(INITIAL);
					yylval->str = litbufdup(yyscanner);
					return XCONST;
				}
<xh><<EOF>>		{ yyerror("Unterminated hexadecimal string literal."); }

{xnstart}		{
					/* National character.
					 * We will pass this along as a normal character string,
					 * but preceded with an internally-generated "NCHAR".
					 */
					const ScanKeyword *keyword;

					SET_YYLLOC();
					yyless(sizeof(wchar_t));				/* eat only 'n' this time */

					keyword = ScanKeywordLookup(L"nchar",
												yyextra->keywords,
												yyextra->num_keywords);
					if (keyword != NULL)
					{
						yylval->keyword = keyword->name;
						return keyword->value;
					}
					else
					{
						/* If NCHAR isn't a keyword, just return "n" */
						yylval->str = wpstrdup(L"n");
						return IDENT;
					}
				}

{xqstart}		{
					SET_YYLLOC();
					BEGIN(xq);
					startlit();
				}
<xq>{quotestop}	|
<xq>{quotefail} {
					yyless(sizeof(wchar_t));
					BEGIN(INITIAL);
					yylval->str = litbufdup(yyscanner);
					return SCONST;
				}
<xq>{xqdouble} {
					addlitchar('\'', yyscanner);
				}
<xq>{xqinside}  {
					addlit(yytext, yyleng, yyscanner);
				}
<xq>{quotecontinue} {
					/* ignore */
				}
<xq><<EOF>>		{ yyerror("Unterminated quoted string."); }


{xdstart}		{
					SET_YYLLOC();
					BEGIN(xd);
					startlit();
				}
<xd>{xdstop}	{
					wchar_t		   *ident;

					BEGIN(INITIAL);
					if (yyextra->literallen == 0)
						yyerror("Zero-length delimited identifier.");
					ident = litbufdup(yyscanner);
					yylval->str = ident;
					return IDENT;
				}
<xd>{xddouble}	{
					addlitchar('"', yyscanner);
				}
<xd>{xdinside}	{
					addlit(yytext, yyleng, yyscanner);
				}

{dot_dot}		{
					SET_YYLLOC();
					return DOT_DOT;
				}

{colon_equals}	{
					SET_YYLLOC();
					return COLON_EQUALS;
				}

{self}			{
					SET_YYLLOC();
					return yytext[0];
				}

{operator}		{
					/*
					 * Note that slash-star or dash-dash at the first
					 * character will match a prior rule, not this one.
					 */

					SET_YYLLOC();

					/*
					 * For SQL compatibility, '+' and '-' cannot be the
					 * last char of a multi-char operator unless the operator
					 * contains chars that are not in SQL operators.
					 * The idea is to lex '=-' as two operators, but not
					 * to forbid operator names like '?-' that could not be
					 * sequences of SQL operators.
					 */
					if (strchr("+-", yytext[2]) && !strchr("~!@#^&|`?%", yytext[0]))
					{
						yyless(2);
						return yytext[0];
					}

					/* Convert "!=" operator to "<>" for compatibility */
					if (wcscmp((wchar_t*)yytext, L"!=") == 0)
						yylval->str = wpstrdup(L"<>");
					else
						yylval->str = wpstrdup((wchar_t*)yytext);
					return Op;
				}

{param}			{
					SET_YYLLOC();
					yylval->ival = atol(yytext + 1);
					return PARAM;
				}

{integer}		{
					SET_YYLLOC();
					return process_integer_literal((wchar_t*)yytext, yylval);
				}
{decimal}		{
					SET_YYLLOC();
					yylval->str = wpstrdup((wchar_t*)yytext);
					return FCONST;
				}
{decimalfail}	{
					/* throw back the .., and treat as integer */
					yyless(yyleng-2);
					SET_YYLLOC();
					return process_integer_literal((wchar_t*)yytext, yylval);
				}
{real}			{
					SET_YYLLOC();
					yylval->str = wpstrdup((wchar_t*)yytext);
					return FCONST;
				}
{realfail1}		{
					/*
					 * throw back the [Ee], and treat as {decimal}.  Note
					 * that it is possible the input is actually {integer},
					 * but since this case will almost certainly lead to a
					 * syntax error anyway, we don't bother to distinguish.
					 */
					yyless(yyleng-1);
					SET_YYLLOC();
					yylval->str = wpstrdup((wchar_t*)yytext);
					return FCONST;
				}
{realfail2}		{
					/* throw back the [Ee][+-], and proceed as above */
					yyless(yyleng-2);
					SET_YYLLOC();
					yylval->str = wpstrdup((wchar_t*)yytext);
					return FCONST;
				}


{identifier}	{
					const ScanKeyword *keyword;
					//char		   *ident;

					SET_YYLLOC();

					/* Is it a keyword? */
					keyword = ScanKeywordLookup((wchar_t*)yytext,
												yyextra->keywords,
												yyextra->num_keywords);
					if (keyword != NULL)
					{
						yylval->keyword = keyword->name;
						return keyword->value;
					}

					/*
					 * No.  Convert the identifier to lower case, and truncate
					 * if necessary.
					 */
					//ident = downcase_identifier(yytext, yyleng, true);
					yylval->str = wpstrdup((wchar_t*)yytext);
					return IDENT;
				}

{other}			{
					SET_YYLLOC();
					return yytext[0];
				}

<<EOF>>			{
					SET_YYLLOC();
					yyterminate();
				}

%%

/*
 * Arrange access to yyextra for subroutines of the main yylex() function.
 * We expect each subroutine to have a yyscanner parameter.  Rather than
 * use the yyget_xxx functions, which might or might not get inlined by the
 * compiler, we cheat just a bit and cast yyscanner to the right type.
 */
#undef yyextra
#define yyextra  (((struct yyguts_t *) yyscanner)->yyextra_r)

/* Likewise for a couple of other things we need. */
#undef yylloc
#define yylloc  (((struct yyguts_t *) yyscanner)->yylloc_r)
#undef yyleng
#define yyleng  (((struct yyguts_t *) yyscanner)->yyleng_r)


/*
 * scanner_yyerror
 *		Report a lexer or grammar error.
 *
 * The message's cursor position is whatever YYLLOC was last set to,
 * ie, the start of the current token if called within yylex(), or the
 * most recently lexed token if called from the grammar.
 * This is OK for syntax error messages from the Bison parser, because Bison
 * parsers report error as soon as the first unparsable token is reached.
 * Beware of using yyerror for other purposes, as the cursor position might
 * be misleading!
 */
void
scanner_yyerror(const char *message, core_yyscan_t yyscanner)
{
	char *loc = yyextra->scanbuf + *yylloc;

	if (*loc == YY_END_OF_BUFFER_CHAR)
	{
		errprint("\nERROR SYNTAX_ERROR: %s at end of input, position %d",_(message), *yylloc);
		ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, *yylloc, NULL, ScErrMessageStr("%s", message));
	}
	else
	{
		errprint("\nERROR SYNTAX_ERROR: %s at or near \"%s\"",_(message), loc);
		ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, *yylloc, (wchar_t*)loc, ScErrMessageStr("%s",message));
	}
}


/*
 * Called before any actual parsing is done
 */
core_yyscan_t
scanner_init(const wchar_t *str,
			 core_yy_extra_type *yyext,
			 const ScanKeyword *keywords,
			 int num_keywords)
{
	Size		slen = wcslen(str)*sizeof(wchar_t);
	yyscan_t	scanner;

	if (yylex_init(&scanner) != 0)
	{
		errprint("\nERROR: yylex_init() failed: %m");
		ScEreport(SCERRUNEXPSQLPARSER, -1, NULL, ScErrMessage("yylex_init() failed."));
		return NULL;
	}

	core_yyset_extra(yyext, scanner);

	yyext->keywords = keywords;
	yyext->num_keywords = num_keywords;

	/*
	 * Make a scan buffer with special termination needed by flex.
	 */
	yyext->scanbuf = (char *) palloc(slen + 2);
	yyext->scanbuflen = slen;
	memcpy(yyext->scanbuf, (char*)str, slen);
	yyext->scanbuf[slen] = yyext->scanbuf[slen + 1] = YY_END_OF_BUFFER_CHAR;
	yy_scan_buffer(yyext->scanbuf, slen + 2, scanner);

	/* initialize literal buffer to a reasonable but expansible size */
	yyext->literalalloc = 1024;
	yyext->literalbuf = (char *) palloc(yyext->literalalloc);
	yyext->literallen = 0;

	return scanner;
}


/*
 * Called after parsing is done to clean up after scanner_init()
 */
void
scanner_finish(core_yyscan_t yyscanner)
{
	/*
	 * We don't bother to call yylex_destroy(), because all it would do
	 * is pfree a small amount of control storage.  It's cheaper to leak
	 * the storage until the parsing context is destroyed.  The amount of
	 * space involved is usually negligible compared to the output parse
	 * tree anyway.
	 *
	 * We do bother to pfree the scanbuf and literal buffer, but only if they
	 * represent a nontrivial amount of space.  The 8K cutoff is arbitrary.
	 */
	if (yyextra->scanbuflen >= 8192)
		pfree(yyextra->scanbuf);
	if (yyextra->literalalloc >= 8192)
		pfree(yyextra->literalbuf);
}

#include "scanfuncs.h"

/*
 * Interface functions to make flex use palloc() instead of malloc().
 * It'd be better to make these static, but flex insists otherwise.
 */

void *
core_yyalloc(yy_size_t bytes, core_yyscan_t yyscanner)
{
	return palloc(bytes);
}

void *
core_yyrealloc(void *ptr, yy_size_t bytes, core_yyscan_t yyscanner)
{
	if (ptr)
		return repalloc(ptr, bytes);
	else
		return palloc(bytes);
}

void
core_yyfree(void *ptr, core_yyscan_t yyscanner)
{
	if (ptr)
		pfree(ptr);
}

#ifdef _MSC_VER
# pragma warning(pop)
#endif // _MSC_VER
