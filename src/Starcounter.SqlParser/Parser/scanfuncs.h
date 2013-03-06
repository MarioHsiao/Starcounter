static void
addlit(char *ytext, int yleng, core_yyscan_t yyscanner)
{
	/* enlarge buffer if needed */
	if ((yyextra->literallen + yleng) >= yyextra->literalalloc)
	{
		do {
			yyextra->literalalloc *= 2;
		} while ((yyextra->literallen + yleng) >= yyextra->literalalloc);
		yyextra->literalbuf = (char *) repalloc(yyextra->literalbuf,
												yyextra->literalalloc);
	}
	/* append new data */
	memcpy(yyextra->literalbuf + yyextra->literallen, ytext, yleng);
	yyextra->literallen += yleng;
}


static void
addlitchar(unsigned char ychar, core_yyscan_t yyscanner)
{
	/* enlarge buffer if needed */
	if ((yyextra->literallen + sizeof(wchar_t)) >= yyextra->literalalloc)
	{
		yyextra->literalalloc *= 2;
		yyextra->literalbuf = (char *) repalloc(yyextra->literalbuf,
												yyextra->literalalloc);
	}
	/* append new data */
	yyextra->literalbuf[yyextra->literallen] = ychar;
	yyextra->literalbuf[yyextra->literallen+1] = '\x0';
	yyextra->literallen += sizeof(wchar_t);
}


/*
 * Create a palloc'd copy of literalbuf, adding a trailing null.
 */
static wchar_t *
litbufdup(core_yyscan_t yyscanner)
{
	int			llen = yyextra->literallen;
	char	   *new;

	new = palloc(llen + 2);
	memcpy(new, yyextra->literalbuf, llen);
	new[llen] = new[llen+1] = '\0';
	return (wchar_t*)new;
}

static int
process_integer_literal(const wchar_t *token, YYSTYPE *lval)
{
	__int64		val;
	wchar_t	   *endptr;

	errno = 0;
	val = _wcstoi64(token, &endptr, 10);
	if (*endptr != L'\0' || errno == ERANGE)
	{
		/* integer too large, treat it as a float */
		lval->str = wpstrdup(token);
		return FCONST;
	}
	lval->ival = val;
	return ICONST;
}

static unsigned int
hexval(unsigned char c)
{
	if (c >= '0' && c <= '9')
		return c - '0';
	if (c >= 'a' && c <= 'f')
		return c - 'a' + 0xA;
	if (c >= 'A' && c <= 'F')
		return c - 'A' + 0xA;
	errprint("\nERROR: invalid hexadecimal digit");
	ThrowExceptionCode(SCERRSQLINCORRECTSYNTAX, ScErrMessage("Scanner error: invalid hexadecimal digit"));
	return 0; /* not reached */
}
