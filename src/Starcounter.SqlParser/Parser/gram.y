%{

/*#define YYDEBUG 1*/
/*-------------------------------------------------------------------------
 *
 * gram.y
 *	  POSTGRESQL BISON rules/actions
 *
 * Portions Copyright (c) 1996-2011, PostgreSQL Global Development Group
 * Portions Copyright (c) 1994, Regents of the University of California
 *
 *
 * IDENTIFICATION
 *	  src/backend/parser/gram.y
 *
 * HISTORY
 *	  AUTHOR			DATE			MAJOR EVENT
 *	  Andrew Yu			Sept, 1994		POSTQUEL to SQL conversion
 *	  Andrew Yu			Oct, 1994		lispy code conversion
 *
 * NOTES
 *	  CAPITALS are used to represent terminal symbols.
 *	  non-capitals are used to represent non-terminals.
 *	  SQL92-specific syntax is separated from plain SQL/Postgres syntax
 *	  to help isolate the non-extensible portions of the parser.
 *
 *	  In general, nothing in this file should initiate database accesses
 *	  nor depend on changeable state (such as SET variables).  If you do
 *	  database accesses, your code will fail when we have aborted the
 *	  current transaction and are just parsing commands to find the next
 *	  ROLLBACK or COMMIT.  If you make use of SET variables, then you
 *	  will do the wrong thing in multi-query strings like this:
 *			SET SQL_inheritance TO off; SELECT * FROM foo;
 *	  because the entire string is parsed by gram.y before the SET gets
 *	  executed.  Anything that depends on the database or changeable state
 *	  should be handled during parse analysis so that it happens at the
 *	  right time not the wrong time.  The handling of SQL_inheritance is
 *	  a good example.
 *
 * WARNINGS
 *	  If you use a list, make sure the datum is a node so that the printing
 *	  routines work.
 *
 *	  Sometimes we assign constants to makeStrings. Make sure we don't free
 *	  those.
 *
 *-------------------------------------------------------------------------
 */
#include "../General/postgres.h"

#include <ctype.h>
#include <limits.h>

#include "../Nodes/parsenodes.h"
#include "../Nodes/makefuncs.h"
#include "../Nodes/nodeFuncs.h"
#include "gramparse.h"
#include "parser.h"
#include "../Utils/datetime.h"
#include "../Utils/xml.h"


#ifdef _MSC_VER
# pragma warning(push)
#pragma warning(disable: 4024)
#pragma warning(disable: 4047)
#pragma warning(disable: 4090)
#pragma warning(disable: 4133)
#pragma warning(disable: 4244)
#pragma warning(disable: 4996)
#endif // _MSC_VER

/* Location tracking support --- simpler than bison's default */
#define YYLLOC_DEFAULT(Current, Rhs, N)				\
    do									\
      if (YYID (N))                                                    \
	{								\
	  (Current)   = YYRHSLOC (Rhs, 1);	\
	}								\
      else								\
	{								\
	  (Current)   = YYRHSLOC (Rhs, 0);				\
	}								\
    while (YYID (0))

/*
 * Bison doesn't allocate anything that needs to live across parser calls,
 * so we can easily have it use palloc instead of malloc.  This prevents
 * memory leaks if we error out during parsing.  Note this only works with
 * bison >= 2.0.  However, in bison 1.875 the default is to use alloca()
 * if possible, so there's not really much problem anyhow, at least if
 * you're building with gcc.
 */
#define YYMALLOC palloc
#define YYFREE   pfree

/* Private struct for the result of privilege_target production */
typedef struct PrivTarget
{
	GrantTargetType targtype;
	GrantObjectType objtype;
	List	   *objs;
} PrivTarget;

/* ConstraintAttributeSpec yields an integer bitmask of these flags: */
#define CAS_NOT_DEFERRABLE			0x01
#define CAS_DEFERRABLE				0x02
#define CAS_INITIALLY_IMMEDIATE		0x04
#define CAS_INITIALLY_DEFERRED		0x08
#define CAS_NOT_VALID				0x10


#define parser_yyerror(msg)  scanner_yyerror(msg, yyscanner)

static void base_yyerror(YYLTYPE *locp, core_yyscan_t yyscanner,
						 const char *msg);
static Node *makeColumnRef(char *colname, List *indirection,
						   YYLTYPE location, core_yyscan_t yyscanner);
static Node *makeTypeCast(Node *arg, TypeName *typename, YYLTYPE location);
static Node *makeStringConst(wchar_t *str, YYLTYPE location);
static Node *makeStringConstCast(wchar_t *str, YYLTYPE location, TypeName *typename);
static Node *makeBinaryConst(char *str, YYLTYPE location);
static Node *makeObjectConst(int val, YYLTYPE location);
static Node *makeIntConst(__int64 val, YYLTYPE location);
static Node *makeFloatConst(wchar_t *str, YYLTYPE location);
static Node *makeBitStringConst(wchar_t *str, YYLTYPE location);
static Node *makeNullAConst(YYLTYPE location);
static Node *makeAConst(Value *v, YYLTYPE location);
static Node *makeBoolAConst(bool state, YYLTYPE location);
static FuncCall *makeOverlaps(List *largs, List *rargs,
							  YYLTYPE location, core_yyscan_t yyscanner);
static void check_qualified_name(List *names, core_yyscan_t yyscanner);
static List *check_func_name(List *names, core_yyscan_t yyscanner);
static List *check_indirection(List *indirection, core_yyscan_t yyscanner);
static SelectStmt *findLeftmostSelect(SelectStmt *node);
static void insertSelectOptions(SelectStmt *stmt,
								List *sortClause, List *lockingClause,
								Node *limitOffset, WithClause *withClause,
								List *optionClause, core_yyscan_t yyscanner);
static Node *makeSetOp(SetOperation op, bool all, Node *larg, Node *rarg);
static Node *doNegate(Node *n, YYLTYPE location);
static void doNegateFloat(Value *v);
static Node *makeAArrayExpr(List *elements, YYLTYPE location);
static Node *makeXmlExpr(XmlExprOp op, wchar_t *name, List *named_args,
						 List *args, YYLTYPE location);
static RangeVar *makeRangeVarFromAnyName(List *names, YYLTYPE position, core_yyscan_t yyscanner);
static void SplitColQualList(List *qualList,
							 List **constraintList, CollateClause **collClause,
							 core_yyscan_t yyscanner);
static void processCASbits(int cas_bits, YYLTYPE location, const char *constrType,
			   bool *deferrable, bool *initdeferred, bool *not_valid,
			   core_yyscan_t yyscanner);

%}

%glr-parser
%define api.pure
%expect 3
%expect-rr 0
%name-prefix="base_yy"
%locations
//%debug
//%verbose

%parse-param {core_yyscan_t yyscanner}
%lex-param   {core_yyscan_t yyscanner}

%type <node>	stmt
		AlterDomainStmt
		AlterSeqStmt AlterTableStmt
		ConstraintsSetStmt CreateAsStmt
		CreateDomainStmt
		CreateSeqStmt CreateStmt
		CreateTrigStmt
		CreateUserStmt CreateRoleStmt
		CreatedbStmt DefineStmt DeleteStmt DiscardStmt
		DropIndexStmt DropStmt
		DropTrigStmt DropRoleStmt
		DropUserStmt DropdbStmt
		ExplainStmt
		GrantStmt GrantRoleStmt IndexStmt InsertStmt
		LockStmt ExplainableStmt PreparableStmt
		ReindexStmt
		RevokeStmt RevokeRoleStmt
		SelectStmt TransactionStmt TruncateStmt
		UpdateStmt
		VariableResetStmt VariableSetStmt VariableShowStmt
		ViewStmt CheckPointStmt
		DeallocateStmt PrepareStmt ExecuteStmt
		DropOwnedStmt ReassignOwnedStmt

%type <node>	select_no_parens select_with_parens select_clause
				simple_select values_clause

%type <node>	alter_column_default alter_using
%type <ival>	opt_asc_desc opt_nulls_order

%type <node>	alter_table_cmd opt_collate_clause
%type <list>	alter_table_cmds

%type <dbehavior>	opt_drop_behavior

%type <list>	createdb_opt_list
				transaction_mode_list
%type <defelt>	createdb_opt_item
				transaction_mode_item

%type <ival>	opt_lock lock_type
%type <boolean>	opt_force
				opt_grant_grant_option opt_grant_admin_option
				opt_nowait opt_with_data

%type <list>	OptRoleList
%type <defelt>	CreateOptRoleElem AlterOptRoleElem

%type <boolean> TriggerForSpec TriggerForType
%type <ival>	TriggerActionTime
%type <list>	TriggerEvents TriggerOneEvent
%type <value>	TriggerFuncArg
%type <node>	TriggerWhen

%type <str>		database_name access_method_clause access_method attr_name
				name
				index_name opt_index_name

%type <list>	func_name qual_Op qual_all_Op subquery_Op
				opt_class
				opt_collate

%type <range>	qualified_name OptConstrFromTable

%type <str>		all_Op MathOp

%type <str>		iso_level opt_encoding
%type <node>	grantee
%type <list>	grantee_list
%type <accesspriv> privilege
%type <list>	privileges privilege_list
%type <privtarget> privilege_target

%type <list>	stmtblock stmtmulti
				OptTableElementList TableElementList OptInherit definition
				OptTypedTableElementList TypedTableElementList
				reloptions opt_reloptions
				OptWith opt_distinct opt_definition
				opt_column_list columnList opt_name_list
				sort_clause opt_sort_clause sortby_list index_params
				name_list from_clause from_list opt_array_bounds
				qualified_name_list any_name any_name_list
				any_operator expr_list attrs
				target_list insert_column_list set_target_list
				set_clause_list set_clause multiple_set_clause
				ctext_expr_list ctext_row def_list indirection opt_indirection
				reloption_list group_clause TriggerFuncArgs 
				opt_select_limit
				transaction_mode_list_or_empty
				prep_type_clause
				execute_param_clause using_clause returning_clause
				alter_generic_options
				relation_expr_list

%type <node>	member_access_el member_access_indices standard_func_call
%type <list>	member_access_seq member_func_expr
%type <node>	index_spec
%type <list>	index_spec_list

%type <range>	OptTempTableName
%type <into>	into_clause create_as_target

%type <typnam>	func_type

%type <boolean>  opt_restart_seqs
%type <ival>	 OptTemp
%type <oncommit> OnCommitOption

%type <node>	for_locking_item
%type <list>	for_locking_clause opt_for_locking_clause for_locking_items
%type <list>	locked_rels_list
%type <boolean>	opt_all

%type <node>	join_outer join_qual
%type <jtype>	join_type

%type <list>	extract_list overlay_list position_list
%type <list>	substr_list trim_list
%type <list>	opt_interval interval_second
%type <node>	overlay_placing substr_from substr_for

%type <boolean> opt_unique opt_concurrently opt_verbose

%type <ival>	opt_column opt_set_data
%type <objtype>	reindex_type drop_type

%type <node>	limit_clause select_limit_value
				offset_clause offsetkey_clause
				select_offset_value select_offsetkey_value
				select_offset_value2 opt_select_fetch_first_value
%type <ival>	row_or_rows first_or_next

%type <list>	option_hint_clause opt_option_hint_clause option_hint_lists
				extent_alias_sequence
%type <node>	option_hint

%type <list>	OptSeqOptList SeqOptList
%type <defelt>	SeqOptElem

%type <istmt>	insert_rest

%type <vsetstmt> set_rest

%type <node>	TableElement TypedTableElement ConstraintElem
%type <node>	columnDef columnOptions
%type <defelt>	def_elem reloption_elem
%type <node>	def_arg columnElem where_clause where_or_current_clause
				a_expr b_expr c_expr AexprConst indirection_el
				in_expr having_clause array_expr
				ExclusionWhereClause
%type <list>	ExclusionConstraintList ExclusionConstraintElem
%type <list>	func_arg_list
%type <node>	func_arg_expr
%type <list>	row type_list array_expr_list OptGenerics
%type <node>	case_expr case_arg when_clause case_default
%type <list>	when_clause_list
%type <ival>	sub_type
%type <list>	OptCreateAs CreateAsList
%type <node>	CreateAsElement ctext_expr
%type <value>	NumericOnly
%type <list>	NumericOnly_list
%type <alias>	alias_clause
%type <sortby>	sortby
%type <limitoffset> select_limit
%type <ielem>	index_elem
%type <node>	table_ref
%type <jexpr>	joined_table
%type <range>	relation_expr
%type <range>	relation_expr_opt_alias
%type <target>	target_el single_set_clause set_target insert_column_item

%type <str>		generic_option_name
%type <node>	generic_option_arg
%type <defelt>	generic_option_elem alter_generic_option_elem
%type <list>	alter_generic_option_list
%type <str>		explain_option_name
%type <node>	explain_option_arg
%type <defelt>	explain_option_elem
%type <list>	explain_option_list

%type <typnam>	Typename SimpleTypename ConstTypename
				GenericType Numeric opt_float
				Character ConstCharacter
				CharacterWithLength CharacterWithoutLength
				ConstDatetime ConstInterval
				Bit ConstBit BitWithLength BitWithoutLength
%type <str>		character
%type <str>		extract_arg
%type <str>		opt_charset
%type <boolean> opt_varying opt_timezone

%type <ival>	Iconst SignedIconst
%type <str>		Sconst
%type <str>		RoleId opt_granted_by opt_boolean_or_string ColId_or_Sconst
%type <list>	var_list
%type <str>		ColId ColLabel var_name type_function_name param_name
%type <node>	var_value zone_value

%type <keyword> unreserved_keyword type_func_name_keyword
%type <keyword> col_name_keyword reserved_keyword

%type <node>	TableConstraint TableLikeClause
%type <ival>	TableLikeOptionList TableLikeOption
%type <list>	ColQualList
%type <node>	ColConstraint ColConstraintElem ConstraintAttr
%type <ival>	key_actions key_delete key_match key_update key_action
%type <ival>	ConstraintAttributeSpec ConstraintAttributeElem
%type <str>		ExistingIndex

%type <list>	constraints_set_list
%type <boolean> constraints_set_mode
%type <str>		OptTableSpace OptConsTableSpace
%type <list>	opt_check_option

%type <target>	xml_attribute_el
%type <list>	xml_attribute_list xml_attributes
%type <node>	xml_root_version opt_xml_root_standalone
%type <node>	xmlexists_argument
%type <ival>	document_or_content
%type <boolean> xml_whitespace_option

%type <node> 	common_table_expr
%type <with> 	with_clause opt_with_clause
%type <list>	cte_list

%type <list>	window_clause window_definition_list opt_partition_clause
%type <windef>	window_definition over_clause window_specification
				opt_frame_clause frame_extent frame_bound
%type <str>		opt_existing_window_name


/*
 * Non-keyword token types.  These are hard-wired into the "flex" lexer.
 * They must be listed first so that their numeric codes do not depend on
 * the set of keywords.  PL/pgsql depends on this so that it can share the
 * same lexer.  If you add/change tokens here, fix PL/pgsql to match!
 *
 * DOT_DOT is unused in the core SQL grammar, and so will always provoke
 * parse errors.  It is needed by PL/pgsql.
 */
%token <str>	IDENT FCONST SCONST BCONST XCONST Op
%token <ival>	ICONST PARAM
%token			DOT_DOT COLON_EQUALS

/*
 * If you want to make any keyword changes, update the keyword table in
 * src/include/parser/kwlist.h and add new keywords to the appropriate one
 * of the reserved-or-not-so-reserved keyword lists, below; search
 * this file for "Keyword category lists".
 */

/* ordinary key words in alphabetical order */
%token <keyword> ABORT_P ACCESS ACTION ADD_P ADMIN AFTER
	ALL ALTER ALWAYS ANALYSE ANALYZE AND ANY ARRAY AS ASC
	ASYMMETRIC AT AUTHORIZATION

	BEFORE BEGIN_P BETWEEN BIGINT BINARY BIT
	BOOLEAN_P BOTH BY

	CACHE CASCADE CASCADED CASE CAST CATALOG_P CHAR_P
	CHARACTER CHARACTERISTICS CHECK CHECKPOINT
	COALESCE COLLATE COLLATION COLUMN COMMENTS COMMIT
	COMMITTED CONCURRENTLY CONNECTION CONSTRAINT CONSTRAINTS
	CONTENT_P CONTINUE_P CREATE
	CROSS CSV CURRENT_P
	CURSOR CYCLE

	DATA_P DATABASE DAY_P DEALLOCATE DEC DECIMAL_P DEFAULT DEFAULTS
	DEFERRABLE DEFERRED DELETE_P DESC
	DISABLE_P DISCARD DISTINCT DO DOCUMENT_P DOMAIN_P DOUBLE_P DROP

	EACH ELSE ENABLE_P ENCODING ENCRYPTED END_P ESCAPE EXCEPT
	EXCLUDE EXCLUDING EXCLUSIVE EXECUTE EXISTS EXPLAIN
	EXTRACT

	FALSE_P FETCH FIRST_P FLOAT_P FOLLOWING FOR FORCE FOREIGN
	FROM FULL

	GLOBAL GRANT GRANTED GREATEST GROUP_P

	HAVING HOLD HOUR_P

	IDENTITY_P IF_P ILIKE IMMEDIATE IN_P
	INCLUDING INCREMENT INDEX INDEXES INHERIT INHERITS INITIALLY
	INNER_P INSERT INSTEAD INT_P INTEGER
	INTERSECT INTERVAL INTO IS ISNULL ISOLATION

	JOIN

	KEY

	LARGE_P LAST_P LC_COLLATE_P LC_CTYPE_P LEADING
	LEAST LEFT LEVEL LIKE LIMIT LOCAL
	LOCK_P

	MATCH MAXVALUE MINUTE_P MINVALUE MODE MONTH_P

	NAME_P NAMES NATIONAL NATURAL NCHAR NEXT NO
	NOT NOTNULL NOWAIT NULL_P NULLIF
	NULLS_P NUMERIC

	OBJECT_P OF OFFSET OFFSETKEY OIDS ON ONLY OPERATOR OPTION OPTIONS OR
	ORDER OUTER_P OVER OVERLAPS OVERLAY OWNED OWNER

	PARTIAL PARTITION PASSING PASSWORD PLACING PLANS POSITION
	PRECEDING PRECISION PRESERVE PREPARE PREPARED PRIMARY
	PRIVILEGES PROCEDURE

	RANGE READ REAL REASSIGN RECURSIVE REF REFERENCES REINDEX
	RELEASE REPEATABLE REPLACE REPLICA
	RESET RESTART RESTRICT RETURNING REVOKE RIGHT ROLE ROLLBACK
	ROW ROWS

	SAVEPOINT SCHEMA SECOND_P SELECT SEQUENCE
	SERIALIZABLE SESSION SET SETOF SHARE
	SHOW SIMILAR SIMPLE SMALLINT SOME STANDALONE_P START STARTS 
	STATEMENT STORAGE STRIP_P SUBSTRING
	SYMMETRIC SYSID SYSTEM_P

	TABLE TABLESPACE TEMP TEMPLATE TEMPORARY THEN TIME TIMESTAMP
	TO TRAILING TRANSACTION TREAT TRIGGER TRIM TRUE_P
	TRUNCATE TYPE_P TYPEOF

	UNBOUNDED UNCOMMITTED UNENCRYPTED UNION UNIQUE UNLOGGED
	UNTIL UPDATE USER USING

	VALID VALIDATE VALUE_P VALUES VARCHAR VARIADIC VARYING
	VERBOSE VERSION_P VIEW

	WHEN WHERE WHITESPACE_P WINDOW WITH WITHOUT WORK WRITE

	XML_P XMLATTRIBUTES XMLCONCAT XMLELEMENT XMLEXISTS XMLFOREST XMLPARSE
	XMLPI XMLROOT XMLSERIALIZE

	YEAR_P YES_P

	ZONE

/*
 * The grammar thinks these are keywords, but they are not in the kwlist.h
 * list and so can never be entered directly.  The filter in parser.c
 * creates these tokens when required.
 */
%token			NULLS_FIRST NULLS_LAST WITH_TIME


/* Precedence: lowest to highest */
%nonassoc	SET				/* see relation_expr_opt_alias */
%left		UNION EXCEPT
%left		INTERSECT
%left		OR
%left		AND
%right		NOT
%right		'='
%nonassoc	'<' '>'
%nonassoc	LIKE ILIKE SIMILAR STARTS
%nonassoc	ESCAPE
%nonassoc	OVERLAPS
%nonassoc	BETWEEN
%nonassoc	IN_P
%left		POSTFIXOP		/* dummy for postfix Op rules */
/*
 * To support target_el without AS, we must give IDENT an explicit priority
 * between POSTFIXOP and Op.  We can safely assign the same priority to
 * various unreserved keywords as needed to resolve ambiguities (this can't
 * have any bad effects since obviously the keywords will still behave the
 * same as if they weren't keywords).  We need to do this for PARTITION,
 * RANGE, ROWS to support opt_existing_window_name; and for RANGE, ROWS
 * so that they can follow a_expr without creating postfix-operator problems;
 * and for NULL so that it can follow b_expr in ColQualList without creating
 * postfix-operator problems.
 *
 * The frame_bound productions UNBOUNDED PRECEDING and UNBOUNDED FOLLOWING
 * are even messier: since UNBOUNDED is an unreserved keyword (per spec!),
 * there is no principled way to distinguish these from the productions
 * a_expr PRECEDING/FOLLOWING.  We hack this up by giving UNBOUNDED slightly
 * lower precedence than PRECEDING and FOLLOWING.  At present this doesn't
 * appear to cause UNBOUNDED to be treated differently from other unreserved
 * keywords anywhere else in the grammar, but it's definitely risky.  We can
 * blame any funny behavior of UNBOUNDED on the SQL standard, though.
 */
%nonassoc	UNBOUNDED		/* ideally should have same precedence as IDENT */
%nonassoc	IDENT NULL_P PARTITION RANGE ROWS PRECEDING FOLLOWING
%left		Op OPERATOR		/* multi-character ops and user-defined operators */
%nonassoc	NOTNULL
%nonassoc	ISNULL
%nonassoc	IS				/* sets precedence for IS NULL, etc */
%left		'+' '-'
%left		'*' '/' '%'
%left		'^'
/* Unary Operators */
%left		AT				/* sets precedence for AT TIME ZONE */
%left		COLLATE
%right		UMINUS
%left		ANGLE
%left		'[' ']'
%left		'(' ')'
%left		'.'
/*
 * These might seem to be low-precedence, but actually they are not part
 * of the arithmetic hierarchy at all in their use as JOIN operators.
 * We make them high-precedence to support their use as function names.
 * They wouldn't be given a precedence at all, were it not that we need
 * left-associativity among the JOIN rules themselves.
 */
%left		JOIN CROSS LEFT FULL RIGHT INNER_P NATURAL
/* kluge to keep xml_whitespace_option from causing shift/reduce conflicts */
%right		PRESERVE STRIP_P

%%

/*
 *	The target production for the whole parse.
 */
stmtblock:	stmtmulti
			{
				pg_yyget_extra(yyscanner)->parsetree = $1;
			}
		;

/* the thrashing around here is to discard "empty" statements... */
stmtmulti:	stmtmulti ';' stmt
				{
					if ($3 != NULL)
						$$ = lappend($1, $3);
					else
						$$ = $1;
				}
			| stmt
				{
					if ($1 != NULL)
						$$ = list_make1($1);
					else
						$$ = NIL;
				}
		;

stmt :
			AlterDomainStmt
			| AlterSeqStmt
			| AlterTableStmt
			| CheckPointStmt
			| ConstraintsSetStmt
			| CreateAsStmt
			| CreateDomainStmt
			| CreateSeqStmt
			| CreateStmt
			| CreateTrigStmt
			| CreateRoleStmt
			| CreateUserStmt
			| CreatedbStmt
			| DeallocateStmt
			| DefineStmt
			| DeleteStmt
			| DiscardStmt
			| DropIndexStmt
			| DropOwnedStmt
			| DropStmt
			| DropTrigStmt
			| DropRoleStmt
			| DropUserStmt
			| DropdbStmt
			| ExecuteStmt
			| ExplainStmt
			| GrantStmt
			| GrantRoleStmt
			| IndexStmt
			| InsertStmt
			| LockStmt
			| PrepareStmt
			| ReassignOwnedStmt
			| ReindexStmt
			| RevokeStmt
			| RevokeRoleStmt
			| SelectStmt
			| TransactionStmt
			| TruncateStmt
			| UpdateStmt
			| VariableResetStmt
			| VariableSetStmt
			| VariableShowStmt
			| ViewStmt
			| /*EMPTY*/
				{ $$ = NULL; }
		;

/*****************************************************************************
 *
 * Create a new Postgres DBMS role
 *
 *****************************************************************************/

CreateRoleStmt:
			CREATE ROLE RoleId opt_with OptRoleList
				{
					CreateRoleStmt *n = makeNode(CreateRoleStmt);
					n->stmt_type = ROLESTMT_ROLE;
					n->role = $3;
					n->options = $5;
					$$ = (Node *)n;
				}
		;


opt_with:	WITH									{}
			| /*EMPTY*/								{}
		;

/*
 * Options for CREATE ROLE and ALTER ROLE (also used by CREATE/ALTER USER
 * for backwards compatibility).  Note: the only option required by SQL99
 * is "WITH ADMIN name".
 */
OptRoleList:
			OptRoleList CreateOptRoleElem			{ $$ = lappend($1, $2); }
			| /* EMPTY */							{ $$ = NIL; }
		;

AlterOptRoleElem:
			PASSWORD Sconst
				{
					$$ = makeDefElem(L"password",
									 (Node *)makeString($2));
				}
			| PASSWORD NULL_P
				{
					$$ = makeDefElem(L"password", NULL);
				}
			| ENCRYPTED PASSWORD Sconst
				{
					$$ = makeDefElem(L"encryptedPassword",
									 (Node *)makeString($3));
				}
			| UNENCRYPTED PASSWORD Sconst
				{
					$$ = makeDefElem(L"unencryptedPassword",
									 (Node *)makeString($3));
				}
			| INHERIT
				{
					$$ = makeDefElem(L"inherit", (Node *)makeInteger(TRUE));
				}
			| CONNECTION LIMIT SignedIconst
				{
					$$ = makeDefElem(L"connectionlimit", (Node *)makeInteger($3));
				}
			| VALID UNTIL Sconst
				{
					$$ = makeDefElem(L"validUntil", (Node *)makeString($3));
				}
		/*	Supported but not documented for roles, for use by ALTER GROUP. */
			| USER name_list
				{
					$$ = makeDefElem(L"rolemembers", (Node *)$2);
				}
			| IDENT
				{
					/*
					 * We handle identifiers that aren't parser keywords with
					 * the following special-case codes, to avoid bloating the
					 * size of the main parser.
					 */
					if (wcscmp($1, L"superuser") == 0)
						$$ = makeDefElem(L"superuser", (Node *)makeInteger(TRUE));
					else if (wcscmp($1, L"nosuperuser") == 0)
						$$ = makeDefElem(L"superuser", (Node *)makeInteger(FALSE));
					else if (strcmp($1, "createuser") == 0)
					{
						/* For backwards compatibility, synonym for SUPERUSER */
						$$ = makeDefElem(L"superuser", (Node *)makeInteger(TRUE));
					}
					else if (wcscmp($1, L"nocreateuser") == 0)
					{
						/* For backwards compatibility, synonym for SUPERUSER */
						$$ = makeDefElem(L"superuser", (Node *)makeInteger(FALSE));
					}
					else if (wcscmp($1, L"createrole") == 0)
						$$ = makeDefElem(L"createrole", (Node *)makeInteger(TRUE));
					else if (wcscmp($1, L"nocreaterole") == 0)
						$$ = makeDefElem(L"createrole", (Node *)makeInteger(FALSE));
					else if (wcscmp($1, L"replication") == 0)
						$$ = makeDefElem(L"isreplication", (Node *)makeInteger(TRUE));
					else if (wcscmp($1, L"noreplication") == 0)
						$$ = makeDefElem(L"isreplication", (Node *)makeInteger(FALSE));
					else if (wcscmp($1, L"createdb") == 0)
						$$ = makeDefElem(L"createdb", (Node *)makeInteger(TRUE));
					else if (wcscmp($1, L"nocreatedb") == 0)
						$$ = makeDefElem(L"createdb", (Node *)makeInteger(FALSE));
					else if (wcscmp($1, L"login") == 0)
						$$ = makeDefElem(L"canlogin", (Node *)makeInteger(TRUE));
					else if (wcscmp($1, L"nologin") == 0)
						$$ = makeDefElem(L"canlogin", (Node *)makeInteger(FALSE));
					else if (wcscmp($1, L"noinherit") == 0)
					{
						/*
						 * Note that INHERIT is a keyword, so it's handled by main parser, but
						 * NOINHERIT is handled here.
						 */
						$$ = makeDefElem(L"inherit", (Node *)makeInteger(FALSE));
					}
					else
					{
						errprint("\nERROR SYNTAX_ERROR: unrecognized role option \"%s\"", $1);
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $1, ScErrMessage("Unrecognized role option"));
					}
				}
		;

CreateOptRoleElem:
			AlterOptRoleElem			{ $$ = $1; }
			/* The following are not supported by ALTER ROLE/USER/GROUP */
			| SYSID Iconst
				{
					$$ = makeDefElem(L"sysid", (Node *)makeInteger($2));
				}
			| ADMIN name_list
				{
					$$ = makeDefElem(L"adminmembers", (Node *)$2);
				}
			| ROLE name_list
				{
					$$ = makeDefElem(L"rolemembers", (Node *)$2);
				}
			| IN_P ROLE name_list
				{
					$$ = makeDefElem(L"addroleto", (Node *)$3);
				}
			| IN_P GROUP_P name_list
				{
					$$ = makeDefElem(L"addroleto", (Node *)$3);
				}
		;


/*****************************************************************************
 *
 * Create a new Postgres DBMS user (role with implied login ability)
 *
 *****************************************************************************/

CreateUserStmt:
			CREATE USER RoleId opt_with OptRoleList
				{
					CreateRoleStmt *n = makeNode(CreateRoleStmt);
					n->stmt_type = ROLESTMT_USER;
					n->role = $3;
					n->options = $5;
					$$ = (Node *)n;
				}
		;


/*****************************************************************************
 *
 * Drop a postgresql DBMS role
 *
 * XXX Ideally this would have CASCADE/RESTRICT options, but since a role
 * might own objects in multiple databases, there is presently no way to
 * implement either cascading or restricting.  Caveat DBA.
 *****************************************************************************/

DropRoleStmt:
			DROP ROLE name_list
				{
					DropRoleStmt *n = makeNode(DropRoleStmt);
					n->missing_ok = FALSE;
					n->roles = $3;
					$$ = (Node *)n;
				}
			| DROP ROLE IF_P EXISTS name_list
				{
					DropRoleStmt *n = makeNode(DropRoleStmt);
					n->missing_ok = TRUE;
					n->roles = $5;
					$$ = (Node *)n;
				}
			;

/*****************************************************************************
 *
 * Drop a postgresql DBMS user
 *
 * XXX Ideally this would have CASCADE/RESTRICT options, but since a user
 * might own objects in multiple databases, there is presently no way to
 * implement either cascading or restricting.  Caveat DBA.
 *****************************************************************************/

DropUserStmt:
			DROP USER name_list
				{
					DropRoleStmt *n = makeNode(DropRoleStmt);
					n->missing_ok = FALSE;
					n->roles = $3;
					$$ = (Node *)n;
				}
			| DROP USER IF_P EXISTS name_list
				{
					DropRoleStmt *n = makeNode(DropRoleStmt);
					n->roles = $5;
					n->missing_ok = TRUE;
					$$ = (Node *)n;
				}
			;


/*****************************************************************************
 *
 * Set PG internal variable
 *	  SET name TO 'var_value'
 * Include SQL92 syntax (thomas 1997-10-22):
 *	  SET TIME ZONE 'var_value'
 *
 *****************************************************************************/

VariableSetStmt:
			SET set_rest
				{
					VariableSetStmt *n = $2;
					n->is_local = false;
					$$ = (Node *) n;
				}
			| SET LOCAL set_rest
				{
					VariableSetStmt *n = $3;
					n->is_local = true;
					$$ = (Node *) n;
				}
			| SET SESSION set_rest
				{
					VariableSetStmt *n = $3;
					n->is_local = false;
					$$ = (Node *) n;
				}
		;

set_rest:	/* Generic SET syntaxes: */
			var_name TO var_list
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = $1;
					n->args = $3;
					$$ = n;
				}
			| var_name '=' var_list
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = $1;
					n->args = $3;
					$$ = n;
				}
			| var_name TO DEFAULT
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_DEFAULT;
					n->name = $1;
					$$ = n;
				}
			| var_name '=' DEFAULT
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_DEFAULT;
					n->name = $1;
					$$ = n;
				}
			| var_name FROM CURRENT_P
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_CURRENT;
					n->name = $1;
					$$ = n;
				}
			/* Special syntaxes mandated by SQL standard: */
			| TIME ZONE zone_value
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = "timezone";
					if ($3 != NULL)
						n->args = list_make1($3);
					else
						n->kind = VAR_SET_DEFAULT;
					$$ = n;
				}
			| TRANSACTION transaction_mode_list
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_MULTI;
					n->name = "TRANSACTION";
					n->args = $2;
					$$ = n;
				}
			| SESSION CHARACTERISTICS AS TRANSACTION transaction_mode_list
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_MULTI;
					n->name = "SESSION CHARACTERISTICS";
					n->args = $5;
					$$ = n;
				}
			| CATALOG_P Sconst
				{
					errprint("\nERROR FEATURE_NOT_SUPPORTED: current database cannot be changed");
					ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @2, $1, ScErrMessage("Current database cannot be changed"));
					$$ = NULL; /*not reached*/
				}
			| SCHEMA Sconst
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = "search_path";
					n->args = list_make1(makeStringConst($2, @2));
					$$ = n;
				}
			| NAMES opt_encoding
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = "client_encoding";
					if ($2 != NULL)
						n->args = list_make1(makeStringConst($2, @2));
					else
						n->kind = VAR_SET_DEFAULT;
					$$ = n;
				}
			| ROLE ColId_or_Sconst
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = "role";
					n->args = list_make1(makeStringConst($2, @2));
					$$ = n;
				}
			| SESSION AUTHORIZATION ColId_or_Sconst
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = "session_authorization";
					n->args = list_make1(makeStringConst($3, @3));
					$$ = n;
				}
			| SESSION AUTHORIZATION DEFAULT
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_DEFAULT;
					n->name = "session_authorization";
					$$ = n;
				}
			| XML_P OPTION document_or_content
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_SET_VALUE;
					n->name = "xmloption";
					n->args = list_make1(makeStringConst($3 == XMLOPTION_DOCUMENT ? "DOCUMENT" : "CONTENT", @3));
					$$ = n;
				}
		;

var_name:	ColId								{ $$ = $1; }
			| var_name '.' ColId
				{
					Size new_len = wcslen($1) + wcslen($3) + 2;
					$$ = palloc(new_len * sizeof(wchar_t));
					swprintf($$, new_len, L"%s.%s", $1, $3);
				}
		;

var_list:	var_value								{ $$ = list_make1($1); }
			| var_list ',' var_value				{ $$ = lappend($1, $3); }
		;

var_value:	opt_boolean_or_string
				{ $$ = makeStringConst($1, @1); }
			| NumericOnly
				{ $$ = makeAConst($1, @1); }
		;

iso_level:	READ UNCOMMITTED						{ $$ = L"read uncommitted"; }
			| READ COMMITTED						{ $$ = L"read committed"; }
			| REPEATABLE READ						{ $$ = L"repeatable read"; }
			| SERIALIZABLE							{ $$ = L"serializable"; }
		;

opt_boolean_or_string:
			TRUE_P									{ $$ = L"true"; }
			| FALSE_P								{ $$ = L"false"; }
			| ON									{ $$ = L"on"; }
			/*
			 * OFF is also accepted as a boolean value, but is handled
			 * by the ColId rule below. The action for booleans and strings
			 * is the same, so we don't need to distinguish them here.
			 */
			| ColId_or_Sconst						{ $$ = $1; }
		;

/* Timezone values can be:
 * - a string such as 'pst8pdt'
 * - an identifier such as "pst8pdt"
 * - an integer or floating point number
 * - a time interval per SQL99
 * ColId gives reduce/reduce errors against ConstInterval and LOCAL,
 * so use IDENT (meaning we reject anything that is a key word).
 */
zone_value:
			Sconst
				{
					$$ = makeStringConst($1, @1);
				}
			| IDENT
				{
					$$ = makeStringConst($1, @1);
				}
			| ConstInterval Sconst opt_interval
				{
					TypeName *t = $1;
					if ($3 != NIL)
					{
						A_Const *n = (A_Const *) linitial($3);
						if ((n->val.val.ival & ~(INTERVAL_MASK(HOUR) | INTERVAL_MASK(MINUTE))) != 0)
						{
							errprint("\nERROR SYNTAX_ERROR: time zone interval must be HOUR or HOUR TO MINUTE");
							ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @3, $3, ScErrMessage("time zone interval must be HOUR or HOUR TO MINUTE"));
						}
					}
					t->typmods = $3;
					$$ = makeStringConstCast($2, @2, t);
				}
			| ConstInterval '(' Iconst ')' Sconst opt_interval
				{
					TypeName *t = $1;
					if ($6 != NIL)
					{
						A_Const *n = (A_Const *) linitial($6);
						if ((n->val.val.ival & ~(INTERVAL_MASK(HOUR) | INTERVAL_MASK(MINUTE))) != 0)
						{
							errprint("\nERROR SYNTAX_ERROR: time zone interval must be HOUR or HOUR TO MINUTE");
							ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @6, $6, ScErrMessage("time zone interval must be HOUR or HOUR TO MINUTE"));
						}
						if (list_length($6) != 1)
						{
							errprint("\nERROR SYNTAX_ERROR: interval precision specified twice");
							ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $6, ScErrMessage("interval precision specified twice"));
						}
						t->typmods = lappend($6, makeIntConst($3, @3));
					}
					else
						t->typmods = list_make2(makeIntConst(INTERVAL_FULL_RANGE, -1),
												makeIntConst($3, @3));
					$$ = makeStringConstCast($5, @5, t);
				}
			| NumericOnly							{ $$ = makeAConst($1, @1); }
			| DEFAULT								{ $$ = NULL; }
			| LOCAL									{ $$ = NULL; }
		;

opt_encoding:
			Sconst									{ $$ = $1; }
			| DEFAULT								{ $$ = NULL; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

ColId_or_Sconst:
			ColId									{ $$ = $1; }
			| Sconst								{ $$ = $1; }
		;

VariableResetStmt:
			RESET var_name
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_RESET;
					n->name = $2;
					$$ = (Node *) n;
				}
			| RESET TIME ZONE
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_RESET;
					n->name = "timezone";
					$$ = (Node *) n;
				}
			| RESET TRANSACTION ISOLATION LEVEL
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_RESET;
					n->name = "transaction_isolation";
					$$ = (Node *) n;
				}
			| RESET SESSION AUTHORIZATION
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_RESET;
					n->name = "session_authorization";
					$$ = (Node *) n;
				}
			| RESET ALL
				{
					VariableSetStmt *n = makeNode(VariableSetStmt);
					n->kind = VAR_RESET_ALL;
					$$ = (Node *) n;
				}
		;

VariableShowStmt:
			SHOW var_name
				{
					VariableShowStmt *n = makeNode(VariableShowStmt);
					n->name = $2;
					$$ = (Node *) n;
				}
			| SHOW TIME ZONE
				{
					VariableShowStmt *n = makeNode(VariableShowStmt);
					n->name = "timezone";
					$$ = (Node *) n;
				}
			| SHOW TRANSACTION ISOLATION LEVEL
				{
					VariableShowStmt *n = makeNode(VariableShowStmt);
					n->name = "transaction_isolation";
					$$ = (Node *) n;
				}
			| SHOW SESSION AUTHORIZATION
				{
					VariableShowStmt *n = makeNode(VariableShowStmt);
					n->name = "session_authorization";
					$$ = (Node *) n;
				}
			| SHOW ALL
				{
					VariableShowStmt *n = makeNode(VariableShowStmt);
					n->name = "all";
					$$ = (Node *) n;
				}
		;


ConstraintsSetStmt:
			SET CONSTRAINTS constraints_set_list constraints_set_mode
				{
					ConstraintsSetStmt *n = makeNode(ConstraintsSetStmt);
					n->constraints = $3;
					n->deferred    = $4;
					$$ = (Node *) n;
				}
		;

constraints_set_list:
			ALL										{ $$ = NIL; }
			| qualified_name_list					{ $$ = $1; }
		;

constraints_set_mode:
			DEFERRED								{ $$ = TRUE; }
			| IMMEDIATE								{ $$ = FALSE; }
		;


/*
 * Checkpoint statement
 */
CheckPointStmt:
			CHECKPOINT
				{
					CheckPointStmt *n = makeNode(CheckPointStmt);
					$$ = (Node *)n;
				}
		;


/*****************************************************************************
 *
 * DISCARD { ALL | TEMP | PLANS }
 *
 *****************************************************************************/

DiscardStmt:
			DISCARD ALL
				{
					DiscardStmt *n = makeNode(DiscardStmt);
					n->target = DISCARD_ALL;
					$$ = (Node *) n;
				}
			| DISCARD TEMP
				{
					DiscardStmt *n = makeNode(DiscardStmt);
					n->target = DISCARD_TEMP;
					$$ = (Node *) n;
				}
			| DISCARD TEMPORARY
				{
					DiscardStmt *n = makeNode(DiscardStmt);
					n->target = DISCARD_TEMP;
					$$ = (Node *) n;
				}
			| DISCARD PLANS
				{
					DiscardStmt *n = makeNode(DiscardStmt);
					n->target = DISCARD_PLANS;
					$$ = (Node *) n;
				}
		;


/*****************************************************************************
 *
 *	ALTER [ TABLE | INDEX | SEQUENCE | VIEW ] variations
 *
 * Note: we accept all subcommands for each of the four variants, and sort
 * out what's really legal at execution time.
 *****************************************************************************/

AlterTableStmt:
			ALTER TABLE relation_expr alter_table_cmds
				{
					AlterTableStmt *n = makeNode(AlterTableStmt);
					n->relation = $3;
					n->cmds = $4;
					n->relkind = OBJECT_TABLE;
					$$ = (Node *)n;
				}
		|	ALTER INDEX qualified_name alter_table_cmds
				{
					AlterTableStmt *n = makeNode(AlterTableStmt);
					n->relation = $3;
					n->cmds = $4;
					n->relkind = OBJECT_INDEX;
					$$ = (Node *)n;
				}
		|	ALTER SEQUENCE qualified_name alter_table_cmds
				{
					AlterTableStmt *n = makeNode(AlterTableStmt);
					n->relation = $3;
					n->cmds = $4;
					n->relkind = OBJECT_SEQUENCE;
					$$ = (Node *)n;
				}
		|	ALTER VIEW qualified_name alter_table_cmds
				{
					AlterTableStmt *n = makeNode(AlterTableStmt);
					n->relation = $3;
					n->cmds = $4;
					n->relkind = OBJECT_VIEW;
					$$ = (Node *)n;
				}
		;

alter_table_cmds:
			alter_table_cmd							{ $$ = list_make1($1); }
			| alter_table_cmds ',' alter_table_cmd	{ $$ = lappend($1, $3); }
		;

alter_table_cmd:
			/* ALTER TABLE <name> ADD <coldef> */
			ADD_P columnDef
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_AddColumn;
					n->def = $2;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ADD COLUMN <coldef> */
			| ADD_P COLUMN columnDef
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_AddColumn;
					n->def = $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ALTER [COLUMN] <colname> {SET DEFAULT <expr>|DROP DEFAULT} */
			| ALTER opt_column ColId alter_column_default
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_ColumnDefault;
					n->name = $3;
					n->def = $4;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ALTER [COLUMN] <colname> DROP NOT NULL */
			| ALTER opt_column ColId DROP NOT NULL_P
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DropNotNull;
					n->name = $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ALTER [COLUMN] <colname> SET NOT NULL */
			| ALTER opt_column ColId SET NOT NULL_P
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_SetNotNull;
					n->name = $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ALTER [COLUMN] <colname> SET ( column_parameter = value [, ... ] ) */
			| ALTER opt_column ColId SET reloptions
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_SetOptions;
					n->name = $3;
					n->def = (Node *) $5;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ALTER [COLUMN] <colname> SET ( column_parameter = value [, ... ] ) */
			| ALTER opt_column ColId RESET reloptions
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_ResetOptions;
					n->name = $3;
					n->def = (Node *) $5;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ALTER [COLUMN] <colname> SET STORAGE <storagemode> */
			| ALTER opt_column ColId SET STORAGE ColId
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_SetStorage;
					n->name = $3;
					n->def = (Node *) makeString($6);
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> DROP [COLUMN] IF EXISTS <colname> [RESTRICT|CASCADE] */
			| DROP opt_column IF_P EXISTS ColId opt_drop_behavior
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DropColumn;
					n->name = $5;
					n->behavior = $6;
					n->missing_ok = TRUE;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> DROP [COLUMN] <colname> [RESTRICT|CASCADE] */
			| DROP opt_column ColId opt_drop_behavior
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DropColumn;
					n->name = $3;
					n->behavior = $4;
					n->missing_ok = FALSE;
					$$ = (Node *)n;
				}
			/*
			 * ALTER TABLE <name> ALTER [COLUMN] <colname> [SET DATA] TYPE <typename>
			 *		[ USING <expression> ]
			 */
			| ALTER opt_column ColId opt_set_data TYPE_P Typename opt_collate_clause alter_using
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					ColumnDef *def = makeNode(ColumnDef);
					n->subtype = AT_AlterColumnType;
					n->name = $3;
					n->def = (Node *) def;
					/* We only use these three fields of the ColumnDef node */
					def->typeName = $6;
					def->collClause = (CollateClause *) $7;
					def->raw_default = $8;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ADD CONSTRAINT ... */
			| ADD_P TableConstraint
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_AddConstraint;
					n->def = $2;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> VALIDATE CONSTRAINT ... */
			| VALIDATE CONSTRAINT name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_ValidateConstraint;
					n->name = $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> DROP CONSTRAINT IF EXISTS <name> [RESTRICT|CASCADE] */
			| DROP CONSTRAINT IF_P EXISTS name opt_drop_behavior
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DropConstraint;
					n->name = $5;
					n->behavior = $6;
					n->missing_ok = TRUE;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> DROP CONSTRAINT <name> [RESTRICT|CASCADE] */
			| DROP CONSTRAINT name opt_drop_behavior
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DropConstraint;
					n->name = $3;
					n->behavior = $4;
					n->missing_ok = FALSE;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ENABLE TRIGGER <trig> */
			| ENABLE_P TRIGGER name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_EnableTrig;
					n->name = $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ENABLE ALWAYS TRIGGER <trig> */
			| ENABLE_P ALWAYS TRIGGER name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_EnableAlwaysTrig;
					n->name = $4;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ENABLE REPLICA TRIGGER <trig> */
			| ENABLE_P REPLICA TRIGGER name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_EnableReplicaTrig;
					n->name = $4;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ENABLE TRIGGER ALL */
			| ENABLE_P TRIGGER ALL
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_EnableTrigAll;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> ENABLE TRIGGER USER */
			| ENABLE_P TRIGGER USER
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_EnableTrigUser;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> DISABLE TRIGGER <trig> */
			| DISABLE_P TRIGGER name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DisableTrig;
					n->name = $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> DISABLE TRIGGER ALL */
			| DISABLE_P TRIGGER ALL
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DisableTrigAll;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> DISABLE TRIGGER USER */
			| DISABLE_P TRIGGER USER
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DisableTrigUser;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> INHERIT <parent> */
			| INHERIT qualified_name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_AddInherit;
					n->def = (Node *) $2;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> NO INHERIT <parent> */
			| NO INHERIT qualified_name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DropInherit;
					n->def = (Node *) $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> OF <type_name> */
			| OF any_name
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					TypeName *def = makeTypeNameFromNameList($2);
					SET_LOCATION(def, @2);
					n->subtype = AT_AddOf;
					n->def = (Node *) def;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> NOT OF */
			| NOT OF
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_DropOf;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> OWNER TO RoleId */
			| OWNER TO RoleId
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_ChangeOwner;
					n->name = $3;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> SET (...) */
			| SET reloptions
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_SetRelOptions;
					n->def = (Node *)$2;
					$$ = (Node *)n;
				}
			/* ALTER TABLE <name> RESET (...) */
			| RESET reloptions
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_ResetRelOptions;
					n->def = (Node *)$2;
					$$ = (Node *)n;
				}
			| alter_generic_options
				{
					AlterTableCmd *n = makeNode(AlterTableCmd);
					n->subtype = AT_GenericOptions;
					n->def = (Node *)$1;
					$$ = (Node *) n;
				}
		;

alter_column_default:
			SET DEFAULT a_expr			{ $$ = $3; }
			| DROP DEFAULT				{ $$ = NULL; }
		;

opt_drop_behavior:
			CASCADE						{ $$ = DROP_CASCADE; }
			| RESTRICT					{ $$ = DROP_RESTRICT; }
			| /* EMPTY */				{ $$ = DROP_RESTRICT; /* default */ }
		;

opt_collate_clause:
			COLLATE any_name
				{
					CollateClause *n = makeNode(CollateClause);
					n->arg = NULL;
					n->collname = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *) n;
				}
			| /* EMPTY */				{ $$ = NULL; }
		;

alter_using:
			USING a_expr				{ $$ = $2; }
			| /* EMPTY */				{ $$ = NULL; }
		;

reloptions:
		  	'(' reloption_list ')'					{ $$ = $2; }
		;

opt_reloptions:		WITH reloptions					{ $$ = $2; }
			 |		/* EMPTY */						{ $$ = NIL; }
		;

reloption_list:
			reloption_elem							{ $$ = list_make1($1); }
			| reloption_list ',' reloption_elem		{ $$ = lappend($1, $3); }
		;

/* This should match def_elem and also allow qualified names */
reloption_elem:
			ColLabel '=' def_arg
				{
					$$ = makeDefElem($1, (Node *) $3);
				}
			| ColLabel
				{
					$$ = makeDefElem($1, NULL);
				}
			| ColLabel '.' ColLabel '=' def_arg
				{
					$$ = makeDefElemExtended($1, $3, (Node *) $5,
											 DEFELEM_UNSPEC);
				}
			| ColLabel '.' ColLabel
				{
					$$ = makeDefElemExtended($1, $3, NULL, DEFELEM_UNSPEC);
				}
		;


/*****************************************************************************
 *
 *		QUERY :
 *				CREATE TABLE relname
 *
 *****************************************************************************/

CreateStmt:	CREATE OptTemp TABLE qualified_name '(' OptTableElementList ')'
			OptInherit OptWith OnCommitOption OptTableSpace
				{
					CreateStmt *n = makeNode(CreateStmt);
					$4->relpersistence = $2;
					n->relation = $4;
					n->tableElts = $6;
					n->inhRelations = $8;
					n->constraints = NIL;
					n->options = $9;
					n->oncommit = $10;
					n->tablespacename = $11;
					n->if_not_exists = false;
					$$ = (Node *)n;
				}
		| CREATE OptTemp TABLE IF_P NOT EXISTS qualified_name '('
			OptTableElementList ')' OptInherit OptWith OnCommitOption
			OptTableSpace
				{
					CreateStmt *n = makeNode(CreateStmt);
					$7->relpersistence = $2;
					n->relation = $7;
					n->tableElts = $9;
					n->inhRelations = $11;
					n->constraints = NIL;
					n->options = $12;
					n->oncommit = $13;
					n->tablespacename = $14;
					n->if_not_exists = true;
					$$ = (Node *)n;
				}
		| CREATE OptTemp TABLE qualified_name OF any_name
			OptTypedTableElementList OptWith OnCommitOption OptTableSpace
				{
					CreateStmt *n = makeNode(CreateStmt);
					$4->relpersistence = $2;
					n->relation = $4;
					n->tableElts = $7;
					n->ofTypename = makeTypeNameFromNameList($6);
					SET_LOCATION(n->ofTypename, @6);
					n->constraints = NIL;
					n->options = $8;
					n->oncommit = $9;
					n->tablespacename = $10;
					n->if_not_exists = false;
					$$ = (Node *)n;
				}
		| CREATE OptTemp TABLE IF_P NOT EXISTS qualified_name OF any_name
			OptTypedTableElementList OptWith OnCommitOption OptTableSpace
				{
					CreateStmt *n = makeNode(CreateStmt);
					$7->relpersistence = $2;
					n->relation = $7;
					n->tableElts = $10;
					n->ofTypename = makeTypeNameFromNameList($9);
					SET_LOCATION(n->ofTypename, @9);
					n->constraints = NIL;
					n->options = $11;
					n->oncommit = $12;
					n->tablespacename = $13;
					n->if_not_exists = true;
					$$ = (Node *)n;
				}
		;

/*
 * Redundancy here is needed to avoid shift/reduce conflicts,
 * since TEMP is not a reserved word.  See also OptTempTableName.
 *
 * NOTE: we accept both GLOBAL and LOCAL options; since we have no modules
 * the LOCAL keyword is really meaningless.
 */
OptTemp:	TEMPORARY					{ $$ = RELPERSISTENCE_TEMP; }
			| TEMP						{ $$ = RELPERSISTENCE_TEMP; }
			| LOCAL TEMPORARY			{ $$ = RELPERSISTENCE_TEMP; }
			| LOCAL TEMP				{ $$ = RELPERSISTENCE_TEMP; }
			| GLOBAL TEMPORARY			{ $$ = RELPERSISTENCE_TEMP; }
			| GLOBAL TEMP				{ $$ = RELPERSISTENCE_TEMP; }
			| UNLOGGED					{ $$ = RELPERSISTENCE_UNLOGGED; }
			| /*EMPTY*/					{ $$ = RELPERSISTENCE_PERMANENT; }
		;

OptTableElementList:
			TableElementList					{ $$ = $1; }
			| /*EMPTY*/							{ $$ = NIL; }
		;

OptTypedTableElementList:
			'(' TypedTableElementList ')'		{ $$ = $2; }
			| /*EMPTY*/							{ $$ = NIL; }
		;

TableElementList:
			TableElement
				{
					$$ = list_make1($1);
				}
			| TableElementList ',' TableElement
				{
					$$ = lappend($1, $3);
				}
		;

TypedTableElementList:
			TypedTableElement
				{
					$$ = list_make1($1);
				}
			| TypedTableElementList ',' TypedTableElement
				{
					$$ = lappend($1, $3);
				}
		;

TableElement:
			columnDef							{ $$ = $1; }
			| TableLikeClause					{ $$ = $1; }
			| TableConstraint					{ $$ = $1; }
		;

TypedTableElement:
			columnOptions						{ $$ = $1; }
			| TableConstraint					{ $$ = $1; }
		;

columnDef:	ColId Typename ColQualList
				{
					ColumnDef *n = makeNode(ColumnDef);
					n->colname = $1;
					n->typeName = $2;
					n->inhcount = 0;
					n->is_local = true;
					n->is_not_null = false;
					n->is_from_type = false;
					n->storage = 0;
					n->raw_default = NULL;
					n->cooked_default = NULL;
					n->collOid = InvalidOid;
					SplitColQualList($3, &n->constraints, &n->collClause,
									 yyscanner);
					$$ = (Node *)n;
				}
		;

columnOptions:	ColId WITH OPTIONS ColQualList
				{
					ColumnDef *n = makeNode(ColumnDef);
					n->colname = $1;
					n->typeName = NULL;
					n->inhcount = 0;
					n->is_local = true;
					n->is_not_null = false;
					n->is_from_type = false;
					n->storage = 0;
					n->raw_default = NULL;
					n->cooked_default = NULL;
					n->collOid = InvalidOid;
					SplitColQualList($4, &n->constraints, &n->collClause,
									 yyscanner);
					$$ = (Node *)n;
				}
		;

ColQualList:
			ColQualList ColConstraint				{ $$ = lappend($1, $2); }
			| /*EMPTY*/								{ $$ = NIL; }
		;

ColConstraint:
			CONSTRAINT name ColConstraintElem
				{
					Constraint *n = (Constraint *) $3;
					Assert(IsA(n, Constraint));
					n->conname = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *) n;
				}
			| ColConstraintElem						{ $$ = $1; }
			| ConstraintAttr						{ $$ = $1; }
			| COLLATE any_name
				{
					/*
					 * Note: the CollateClause is momentarily included in
					 * the list built by ColQualList, but we split it out
					 * again in SplitColQualList.
					 */
					CollateClause *n = makeNode(CollateClause);
					n->arg = NULL;
					n->collname = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *) n;
				}
		;

/* DEFAULT NULL is already the default for Postgres.
 * But define it here and carry it forward into the system
 * to make it explicit.
 * - thomas 1998-09-13
 *
 * WITH NULL and NULL are not SQL92-standard syntax elements,
 * so leave them out. Use DEFAULT NULL to explicitly indicate
 * that a column may have that value. WITH NULL leads to
 * shift/reduce conflicts with WITH TIME ZONE anyway.
 * - thomas 1999-01-08
 *
 * DEFAULT expression must be b_expr not a_expr to prevent shift/reduce
 * conflict on NOT (since NOT might start a subsequent NOT NULL constraint,
 * or be part of a_expr NOT LIKE or similar constructs).
 */
ColConstraintElem:
			NOT NULL_P
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_NOTNULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| NULL_P
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| UNIQUE opt_definition OptConsTableSpace
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_UNIQUE;
					SET_LOCATION(n, @1);
					n->keys = NULL;
					n->options = $2;
					n->indexname = NULL;
					n->indexspace = $3;
					$$ = (Node *)n;
				}
			| PRIMARY KEY opt_definition OptConsTableSpace
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_PRIMARY;
					SET_LOCATION(n, @1);
					n->keys = NULL;
					n->options = $3;
					n->indexname = NULL;
					n->indexspace = $4;
					$$ = (Node *)n;
				}
			| CHECK '(' a_expr ')'
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_CHECK;
					SET_LOCATION(n, @1);
					n->raw_expr = $3;
					n->cooked_expr = NULL;
					$$ = (Node *)n;
				}
			| DEFAULT b_expr
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_DEFAULT;
					SET_LOCATION(n, @1);
					n->raw_expr = $2;
					n->cooked_expr = NULL;
					$$ = (Node *)n;
				}
			| REFERENCES qualified_name opt_column_list key_match key_actions
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_FOREIGN;
					SET_LOCATION(n, @1);
					n->pktable			= $2;
					n->fk_attrs			= NIL;
					n->pk_attrs			= $3;
					n->fk_matchtype		= $4;
					n->fk_upd_action	= (char) ($5 >> 8);
					n->fk_del_action	= (char) ($5 & 0xFF);
					n->skip_validation  = false;
					n->initially_valid  = true;
					$$ = (Node *)n;
				}
		;

/*
 * ConstraintAttr represents constraint attributes, which we parse as if
 * they were independent constraint clauses, in order to avoid shift/reduce
 * conflicts (since NOT might start either an independent NOT NULL clause
 * or an attribute).  parse_utilcmd.c is responsible for attaching the
 * attribute information to the preceding "real" constraint node, and for
 * complaining if attribute clauses appear in the wrong place or wrong
 * combinations.
 *
 * See also ConstraintAttributeSpec, which can be used in places where
 * there is no parsing conflict.  (Note: currently, NOT VALID is an allowed
 * clause in ConstraintAttributeSpec, but not here.  Someday we might need
 * to allow it here too, but for the moment it doesn't seem useful in the
 * statements that use ConstraintAttr.)
 */
ConstraintAttr:
			DEFERRABLE
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_ATTR_DEFERRABLE;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| NOT DEFERRABLE
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_ATTR_NOT_DEFERRABLE;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| INITIALLY DEFERRED
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_ATTR_DEFERRED;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| INITIALLY IMMEDIATE
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_ATTR_IMMEDIATE;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
		;


/*
 * SQL99 supports wholesale borrowing of a table definition via the LIKE clause.
 * This seems to be a poor man's inheritance capability, with the resulting
 * tables completely decoupled except for the original commonality in definitions.
 *
 * This is very similar to CREATE TABLE AS except for the INCLUDING DEFAULTS extension
 * which is a part of SQL:2003.
 */
TableLikeClause:
			LIKE qualified_name TableLikeOptionList
				{
					InhRelation *n = makeNode(InhRelation);
					n->relation = $2;
					n->options = $3;
					$$ = (Node *)n;
				}
		;

TableLikeOptionList:
				TableLikeOptionList INCLUDING TableLikeOption	{ $$ = $1 | $3; }
				| TableLikeOptionList EXCLUDING TableLikeOption	{ $$ = $1 & ~$3; }
				| /* EMPTY */						{ $$ = 0; }
		;

TableLikeOption:
				DEFAULTS			{ $$ = CREATE_TABLE_LIKE_DEFAULTS; }
				| CONSTRAINTS		{ $$ = CREATE_TABLE_LIKE_CONSTRAINTS; }
				| INDEXES			{ $$ = CREATE_TABLE_LIKE_INDEXES; }
				| STORAGE			{ $$ = CREATE_TABLE_LIKE_STORAGE; }
				| COMMENTS			{ $$ = CREATE_TABLE_LIKE_COMMENTS; }
				| ALL				{ $$ = CREATE_TABLE_LIKE_ALL; }
		;


/* ConstraintElem specifies constraint syntax which is not embedded into
 *	a column definition. ColConstraintElem specifies the embedded form.
 * - thomas 1997-12-03
 */
TableConstraint:
			CONSTRAINT name ConstraintElem
				{
					Constraint *n = (Constraint *) $3;
					Assert(IsA(n, Constraint));
					n->conname = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *) n;
				}
			| ConstraintElem						{ $$ = $1; }
		;

ConstraintElem:
			CHECK '(' a_expr ')' ConstraintAttributeSpec
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_CHECK;
					SET_LOCATION(n, @1);
					n->raw_expr = $3;
					n->cooked_expr = NULL;
					processCASbits($5, @5, "CHECK",
								   NULL, NULL, NULL,
								   yyscanner);
					$$ = (Node *)n;
				}
			| UNIQUE '(' columnList ')' opt_definition OptConsTableSpace
				ConstraintAttributeSpec
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_UNIQUE;
					SET_LOCATION(n, @1);
					n->keys = $3;
					n->options = $5;
					n->indexname = NULL;
					n->indexspace = $6;
					processCASbits($7, @7, "UNIQUE",
								   &n->deferrable, &n->initdeferred, NULL,
								   yyscanner);
					$$ = (Node *)n;
				}
			| UNIQUE ExistingIndex ConstraintAttributeSpec
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_UNIQUE;
					SET_LOCATION(n, @1);
					n->keys = NIL;
					n->options = NIL;
					n->indexname = $2;
					n->indexspace = NULL;
					processCASbits($3, @3, "UNIQUE",
								   &n->deferrable, &n->initdeferred, NULL,
								   yyscanner);
					$$ = (Node *)n;
				}
			| PRIMARY KEY '(' columnList ')' opt_definition OptConsTableSpace
				ConstraintAttributeSpec
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_PRIMARY;
					SET_LOCATION(n, @1);
					n->keys = $4;
					n->options = $6;
					n->indexname = NULL;
					n->indexspace = $7;
					processCASbits($8, @8, "PRIMARY KEY",
								   &n->deferrable, &n->initdeferred, NULL,
								   yyscanner);
					$$ = (Node *)n;
				}
			| PRIMARY KEY ExistingIndex ConstraintAttributeSpec
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_PRIMARY;
					SET_LOCATION(n, @1);
					n->keys = NIL;
					n->options = NIL;
					n->indexname = $3;
					n->indexspace = NULL;
					processCASbits($4, @4, "PRIMARY KEY",
								   &n->deferrable, &n->initdeferred, NULL,
								   yyscanner);
					$$ = (Node *)n;
				}
			| EXCLUDE access_method_clause '(' ExclusionConstraintList ')'
				opt_definition OptConsTableSpace ExclusionWhereClause
				ConstraintAttributeSpec
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_EXCLUSION;
					SET_LOCATION(n, @1);
					n->access_method	= $2;
					n->exclusions		= $4;
					n->options			= $6;
					n->indexname		= NULL;
					n->indexspace		= $7;
					n->where_clause		= $8;
					processCASbits($9, @9, "EXCLUDE",
								   &n->deferrable, &n->initdeferred, NULL,
								   yyscanner);
					$$ = (Node *)n;
				}
			| FOREIGN KEY '(' columnList ')' REFERENCES qualified_name
				opt_column_list key_match key_actions ConstraintAttributeSpec
				{
					Constraint *n = makeNode(Constraint);
					n->contype = CONSTR_FOREIGN;
					SET_LOCATION(n, @1);
					n->pktable			= $7;
					n->fk_attrs			= $4;
					n->pk_attrs			= $8;
					n->fk_matchtype		= $9;
					n->fk_upd_action	= (char) ($10 >> 8);
					n->fk_del_action	= (char) ($10 & 0xFF);
					processCASbits($11, @11, "FOREIGN KEY",
								   &n->deferrable, &n->initdeferred,
								   &n->skip_validation,
								   yyscanner);
					n->initially_valid = !n->skip_validation;
					$$ = (Node *)n;
				}
		;

opt_column_list:
			'(' columnList ')'						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

columnList:
			columnElem								{ $$ = list_make1($1); }
			| columnList ',' columnElem				{ $$ = lappend($1, $3); }
		;

columnElem: ColId
				{
					$$ = (Node *) makeString($1);
				}
		;

key_match:  MATCH FULL
			{
				$$ = FKCONSTR_MATCH_FULL;
			}
		| MATCH PARTIAL
			{
				errprint("\nERROR FEATURE_NOT_SUPPORTED: MATCH PARTIAL not yet implemented");
				ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @1, $2, ScErrMessage("MATCH PARTIAL not supported"));
				$$ = FKCONSTR_MATCH_PARTIAL;
			}
		| MATCH SIMPLE
			{
				$$ = FKCONSTR_MATCH_UNSPECIFIED;
			}
		| /*EMPTY*/
			{
				$$ = FKCONSTR_MATCH_UNSPECIFIED;
			}
		;

ExclusionConstraintList:
			ExclusionConstraintElem					{ $$ = list_make1($1); }
			| ExclusionConstraintList ',' ExclusionConstraintElem
													{ $$ = lappend($1, $3); }
		;

ExclusionConstraintElem: index_elem WITH any_operator
			{
				$$ = list_make2($1, $3);
			}
			/* allow OPERATOR() decoration for the benefit of ruleutils.c */
			| index_elem WITH OPERATOR '(' any_operator ')'
			{
				$$ = list_make2($1, $5);
			}
		;

ExclusionWhereClause:
			WHERE '(' a_expr ')'					{ $$ = $3; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

/*
 * We combine the update and delete actions into one value temporarily
 * for simplicity of parsing, and then break them down again in the
 * calling production.  update is in the left 8 bits, delete in the right.
 * Note that NOACTION is the default.
 */
key_actions:
			key_update
				{ $$ = ($1 << 8) | (FKCONSTR_ACTION_NOACTION & 0xFF); }
			| key_delete
				{ $$ = (FKCONSTR_ACTION_NOACTION << 8) | ($1 & 0xFF); }
			| key_update key_delete
				{ $$ = ($1 << 8) | ($2 & 0xFF); }
			| key_delete key_update
				{ $$ = ($2 << 8) | ($1 & 0xFF); }
			| /*EMPTY*/
				{ $$ = (FKCONSTR_ACTION_NOACTION << 8) | (FKCONSTR_ACTION_NOACTION & 0xFF); }
		;

key_update: ON UPDATE key_action		{ $$ = $3; }
		;

key_delete: ON DELETE_P key_action		{ $$ = $3; }
		;

key_action:
			NO ACTION					{ $$ = FKCONSTR_ACTION_NOACTION; }
			| RESTRICT					{ $$ = FKCONSTR_ACTION_RESTRICT; }
			| CASCADE					{ $$ = FKCONSTR_ACTION_CASCADE; }
			| SET NULL_P				{ $$ = FKCONSTR_ACTION_SETNULL; }
			| SET DEFAULT				{ $$ = FKCONSTR_ACTION_SETDEFAULT; }
		;

OptInherit: INHERITS '(' qualified_name_list ')'	{ $$ = $3; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

/* WITH (options) is preferred, WITH OIDS and WITHOUT OIDS are legacy forms */
OptWith:
			WITH reloptions				{ $$ = $2; }
			| WITH OIDS					{ $$ = list_make1(defWithOids(true)); }
			| WITHOUT OIDS				{ $$ = list_make1(defWithOids(false)); }
			| /*EMPTY*/					{ $$ = NIL; }
		;

OnCommitOption:  ON COMMIT DROP				{ $$ = ONCOMMIT_DROP; }
			| ON COMMIT DELETE_P ROWS		{ $$ = ONCOMMIT_DELETE_ROWS; }
			| ON COMMIT PRESERVE ROWS		{ $$ = ONCOMMIT_PRESERVE_ROWS; }
			| /*EMPTY*/						{ $$ = ONCOMMIT_NOOP; }
		;

OptTableSpace:   TABLESPACE name					{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

OptConsTableSpace:   USING INDEX TABLESPACE name	{ $$ = $4; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

ExistingIndex:   USING INDEX index_name				{ $$ = $3; }
		;


/*
 * Note: CREATE TABLE ... AS SELECT ... is just another spelling for
 * SELECT ... INTO.
 */

CreateAsStmt:
		CREATE OptTemp TABLE create_as_target AS SelectStmt opt_with_data
				{
					/*
					 * When the SelectStmt is a set-operation tree, we must
					 * stuff the INTO information into the leftmost component
					 * Select, because that's where analyze.c will expect
					 * to find it.	Similarly, the output column names must
					 * be attached to that Select's target list.
					 */
					SelectStmt *n = findLeftmostSelect((SelectStmt *) $6);
					if (n->intoClause != NULL)
					{
						errprint("\nERROR SYNTAX_ERROR: CREATE TABLE AS cannot specify INTO");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, exprLocation((Node *) n->intoClause), $6, ScErrMessage("CREATE TABLE AS cannot specify INTO"));
						}
					$4->rel->relpersistence = $2;
					n->intoClause = $4;
					/* Implement WITH NO DATA by forcing top-level LIMIT 0 */
//					if (!$7)
//						((SelectStmt *) $6)->limitCount = makeIntConst(0, -1);
					$$ = $6;
				}
		;

create_as_target:
			qualified_name OptCreateAs OptWith OnCommitOption OptTableSpace
				{
					$$ = makeNode(IntoClause);
					$$->rel = $1;
					$$->colNames = $2;
					$$->options = $3;
					$$->onCommit = $4;
					$$->tableSpaceName = $5;
				}
		;

OptCreateAs:
			'(' CreateAsList ')'					{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

CreateAsList:
			CreateAsElement							{ $$ = list_make1($1); }
			| CreateAsList ',' CreateAsElement		{ $$ = lappend($1, $3); }
		;

CreateAsElement:
			ColId
				{
					ColumnDef *n = makeNode(ColumnDef);
					n->colname = $1;
					n->typeName = NULL;
					n->inhcount = 0;
					n->is_local = true;
					n->is_not_null = false;
					n->is_from_type = false;
					n->storage = 0;
					n->raw_default = NULL;
					n->cooked_default = NULL;
					n->collClause = NULL;
					n->collOid = InvalidOid;
					n->constraints = NIL;
					$$ = (Node *)n;
				}
		;

opt_with_data:
			WITH DATA_P								{ $$ = TRUE; }
			| WITH NO DATA_P						{ $$ = FALSE; }
			| /*EMPTY*/								{ $$ = TRUE; }
		;


/*****************************************************************************
 *
 *		QUERY :
 *				CREATE SEQUENCE seqname
 *				ALTER SEQUENCE seqname
 *
 *****************************************************************************/

CreateSeqStmt:
			CREATE OptTemp SEQUENCE qualified_name OptSeqOptList
				{
					CreateSeqStmt *n = makeNode(CreateSeqStmt);
					$4->relpersistence = $2;
					n->sequence = $4;
					n->options = $5;
					n->ownerId = InvalidOid;
					$$ = (Node *)n;
				}
		;

AlterSeqStmt:
			ALTER SEQUENCE qualified_name SeqOptList
				{
					AlterSeqStmt *n = makeNode(AlterSeqStmt);
					n->sequence = $3;
					n->options = $4;
					$$ = (Node *)n;
				}
		;

OptSeqOptList: SeqOptList							{ $$ = $1; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

SeqOptList: SeqOptElem								{ $$ = list_make1($1); }
			| SeqOptList SeqOptElem					{ $$ = lappend($1, $2); }
		;

SeqOptElem: CACHE NumericOnly
				{
					$$ = makeDefElem(L"cache", (Node *)$2);
				}
			| CYCLE
				{
					$$ = makeDefElem(L"cycle", (Node *)makeInteger(TRUE));
				}
			| NO CYCLE
				{
					$$ = makeDefElem(L"cycle", (Node *)makeInteger(FALSE));
				}
			| INCREMENT opt_by NumericOnly
				{
					$$ = makeDefElem(L"increment", (Node *)$3);
				}
			| MAXVALUE NumericOnly
				{
					$$ = makeDefElem(L"maxvalue", (Node *)$2);
				}
			| MINVALUE NumericOnly
				{
					$$ = makeDefElem(L"minvalue", (Node *)$2);
				}
			| NO MAXVALUE
				{
					$$ = makeDefElem(L"maxvalue", NULL);
				}
			| NO MINVALUE
				{
					$$ = makeDefElem(L"minvalue", NULL);
				}
			| OWNED BY any_name
				{
					$$ = makeDefElem(L"owned_by", (Node *)$3);
				}
			| START opt_with NumericOnly
				{
					$$ = makeDefElem(L"start", (Node *)$3);
				}
			| RESTART
				{
					$$ = makeDefElem(L"restart", NULL);
				}
			| RESTART opt_with NumericOnly
				{
					$$ = makeDefElem(L"restart", (Node *)$3);
				}
		;

opt_by:		BY				{}
			| /* empty */	{}
	  ;

NumericOnly:
			FCONST								{ $$ = makeFloat($1); }
			| '-' FCONST
				{
					$$ = makeFloat($2);
					doNegateFloat($$);
				}
			| SignedIconst						{ $$ = makeInteger($1); }
		;

NumericOnly_list:	NumericOnly						{ $$ = list_make1($1); }
				| NumericOnly_list ',' NumericOnly	{ $$ = lappend($1, $3); }
		;

/*****************************************************************************
 *
 * 		QUERY :
 *				ALTER FOREIGN DATA WRAPPER name options - sc: removed
 *
 ****************************************************************************/

/* Options definition for ALTER FDW, SERVER and USER MAPPING */
alter_generic_options:
			OPTIONS	'(' alter_generic_option_list ')'		{ $$ = $3; }
		;

alter_generic_option_list:
			alter_generic_option_elem
				{
					$$ = list_make1($1);
				}
			| alter_generic_option_list ',' alter_generic_option_elem
				{
					$$ = lappend($1, $3);
				}
		;

alter_generic_option_elem:
			generic_option_elem
				{
					$$ = $1;
				}
			| SET generic_option_elem
				{
					$$ = $2;
					$$->defaction = DEFELEM_SET;
				}
			| ADD_P generic_option_elem
				{
					$$ = $2;
					$$->defaction = DEFELEM_ADD;
				}
			| DROP generic_option_name
				{
					$$ = makeDefElemExtended(NULL, $2, NULL, DEFELEM_DROP);
				}
		;

generic_option_elem:
			generic_option_name generic_option_arg
				{
					$$ = makeDefElem($1, $2);
				}
		;

generic_option_name:
				ColLabel			{ $$ = $1; }
		;

/* We could use def_arg here, but the spec only requires string literals */
generic_option_arg:
				Sconst				{ $$ = (Node *) makeString($1); }
		;

/*****************************************************************************
 *
 *		QUERIES :
 *				CREATE TRIGGER ...
 *				DROP TRIGGER ...
 *
 *****************************************************************************/

CreateTrigStmt:
			CREATE TRIGGER name TriggerActionTime TriggerEvents ON
			qualified_name TriggerForSpec TriggerWhen
			EXECUTE PROCEDURE func_name '(' TriggerFuncArgs ')'
				{
					CreateTrigStmt *n = makeNode(CreateTrigStmt);
					n->trigname = $3;
					n->relation = $7;
					n->funcname = $12;
					n->args = $14;
					n->row = $8;
					n->timing = $4;
					n->events = intVal(linitial($5));
					n->columns = (List *) lsecond($5);
					n->whenClause = $9;
					n->isconstraint  = FALSE;
					n->deferrable	 = FALSE;
					n->initdeferred  = FALSE;
					n->constrrel = NULL;
					$$ = (Node *)n;
				}
			| CREATE CONSTRAINT TRIGGER name AFTER TriggerEvents ON
			qualified_name OptConstrFromTable ConstraintAttributeSpec
			FOR EACH ROW TriggerWhen
			EXECUTE PROCEDURE func_name '(' TriggerFuncArgs ')'
				{
					CreateTrigStmt *n = makeNode(CreateTrigStmt);
					n->trigname = $4;
					n->relation = $8;
					n->funcname = $17;
					n->args = $19;
					n->row = TRUE;
					n->timing = TRIGGER_TYPE_AFTER;
					n->events = intVal(linitial($6));
					n->columns = (List *) lsecond($6);
					n->whenClause = $14;
					n->isconstraint  = TRUE;
					processCASbits($10, @10, "TRIGGER",
								   &n->deferrable, &n->initdeferred, NULL,
								   yyscanner);
					n->constrrel = $9;
					$$ = (Node *)n;
				}
		;

TriggerActionTime:
			BEFORE								{ $$ = TRIGGER_TYPE_BEFORE; }
			| AFTER								{ $$ = TRIGGER_TYPE_AFTER; }
			| INSTEAD OF						{ $$ = TRIGGER_TYPE_INSTEAD; }
		;

TriggerEvents:
			TriggerOneEvent
				{ $$ = $1; }
			| TriggerEvents OR TriggerOneEvent
				{
					int		events1 = intVal(linitial($1));
					int		events2 = intVal(linitial($3));
					List   *columns1 = (List *) lsecond($1);
					List   *columns2 = (List *) lsecond($3);

					if (events1 & events2)
						parser_yyerror("duplicate trigger events specified");
					/*
					 * concat'ing the columns lists loses information about
					 * which columns went with which event, but so long as
					 * only UPDATE carries columns and we disallow multiple
					 * UPDATE items, it doesn't matter.  Command execution
					 * should just ignore the columns for non-UPDATE events.
					 */
					$$ = list_make2(makeInteger(events1 | events2),
									list_concat(columns1, columns2));
				}
		;

TriggerOneEvent:
			INSERT
				{ $$ = list_make2(makeInteger(TRIGGER_TYPE_INSERT), NIL); }
			| DELETE_P
				{ $$ = list_make2(makeInteger(TRIGGER_TYPE_DELETE), NIL); }
			| UPDATE
				{ $$ = list_make2(makeInteger(TRIGGER_TYPE_UPDATE), NIL); }
			| UPDATE OF columnList
				{ $$ = list_make2(makeInteger(TRIGGER_TYPE_UPDATE), $3); }
			| TRUNCATE
				{ $$ = list_make2(makeInteger(TRIGGER_TYPE_TRUNCATE), NIL); }
		;

TriggerForSpec:
			FOR TriggerForOptEach TriggerForType
				{
					$$ = $3;
				}
			| /* EMPTY */
				{
					/*
					 * If ROW/STATEMENT not specified, default to
					 * STATEMENT, per SQL
					 */
					$$ = FALSE;
				}
		;

TriggerForOptEach:
			EACH									{}
			| /*EMPTY*/								{}
		;

TriggerForType:
			ROW										{ $$ = TRUE; }
			| STATEMENT								{ $$ = FALSE; }
		;

TriggerWhen:
			WHEN '(' a_expr ')'						{ $$ = $3; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

TriggerFuncArgs:
			TriggerFuncArg							{ $$ = list_make1($1); }	%dprec 5
			| TriggerFuncArgs ',' TriggerFuncArg	{ $$ = lappend($1, $3); }	%dprec 1
			| /*EMPTY*/								{ $$ = NIL; }
		;

TriggerFuncArg:
			Iconst
				{
					wchar_t buf[64];
					_snwprintf(buf, sizeof(buf), L"%d", $1);
					$$ = makeString(wpstrdup(buf));
				}
			| FCONST								{ $$ = makeString($1); }
			| Sconst								{ $$ = makeString($1); }
			| ColLabel								{ $$ = makeString($1); }
		;

OptConstrFromTable:
			FROM qualified_name						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

ConstraintAttributeSpec:
			/*EMPTY*/
				{ $$ = 0; }
			| ConstraintAttributeSpec ConstraintAttributeElem
				{
					/*
					 * We must complain about conflicting options.
					 * We could, but choose not to, complain about redundant
					 * options (ie, where $2's bit is already set in $1).
					 */
					int		newspec = $1 | $2;

					/* special message for this case */
					if ((newspec & (CAS_NOT_DEFERRABLE | CAS_INITIALLY_DEFERRED)) == (CAS_NOT_DEFERRABLE | CAS_INITIALLY_DEFERRED))
					{
						errprint("\nERROR SYNTAX_ERROR: constraint declared INITIALLY DEFERRED must be DEFERRABLE");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @2, $1, ScErrMessage("constraint declared INITIALLY DEFERRED must be DEFERRABLE"));
					}
					/* generic message for other conflicts */
					if ((newspec & (CAS_NOT_DEFERRABLE | CAS_DEFERRABLE)) == (CAS_NOT_DEFERRABLE | CAS_DEFERRABLE) ||
						(newspec & (CAS_INITIALLY_IMMEDIATE | CAS_INITIALLY_DEFERRED)) == (CAS_INITIALLY_IMMEDIATE | CAS_INITIALLY_DEFERRED))
					{
						errprint("\nERROR SYNTAX_ERROR: conflicting constraint properties");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @2, $1, ScErrMessage("conflicting constraint properties"));
					}
					$$ = newspec;
				}
		;

ConstraintAttributeElem:
			NOT DEFERRABLE					{ $$ = CAS_NOT_DEFERRABLE; }
			| DEFERRABLE					{ $$ = CAS_DEFERRABLE; }
			| INITIALLY IMMEDIATE			{ $$ = CAS_INITIALLY_IMMEDIATE; }
			| INITIALLY DEFERRED			{ $$ = CAS_INITIALLY_DEFERRED; }
			| NOT VALID						{ $$ = CAS_NOT_VALID; }
		;


DropTrigStmt:
			DROP TRIGGER name ON qualified_name opt_drop_behavior
				{
					DropPropertyStmt *n = makeNode(DropPropertyStmt);
					n->relation = $5;
					n->property = $3;
					n->behavior = $6;
					n->removeType = OBJECT_TRIGGER;
					n->missing_ok = false;
					$$ = (Node *) n;
				}
			| DROP TRIGGER IF_P EXISTS name ON qualified_name opt_drop_behavior
				{
					DropPropertyStmt *n = makeNode(DropPropertyStmt);
					n->relation = $7;
					n->property = $5;
					n->behavior = $8;
					n->removeType = OBJECT_TRIGGER;
					n->missing_ok = true;
					$$ = (Node *) n;
				}
		;


/*****************************************************************************
 *
 *		QUERY :
 *				define (aggregate,operator,type)
 *
 *****************************************************************************/

DefineStmt:
			CREATE COLLATION any_name definition
				{
					DefineStmt *n = makeNode(DefineStmt);
					n->kind = OBJECT_COLLATION;
					n->args = NIL;
					n->defnames = $3;
					n->definition = $4;
					$$ = (Node *)n;
				}
			| CREATE COLLATION any_name FROM any_name
				{
					DefineStmt *n = makeNode(DefineStmt);
					n->kind = OBJECT_COLLATION;
					n->args = NIL;
					n->defnames = $3;
					n->definition = list_make1(makeDefElem(L"from", (Node *) $5));
					$$ = (Node *)n;
				}
		;

definition: '(' def_list ')'						{ $$ = $2; }
		;

def_list:  	def_elem								{ $$ = list_make1($1); }
			| def_list ',' def_elem					{ $$ = lappend($1, $3); }
		;

def_elem:	ColLabel '=' def_arg
				{
					$$ = makeDefElem($1, (Node *) $3);
				}
			| ColLabel
				{
					$$ = makeDefElem($1, NULL);
				}
		;

/* Note: any simple identifier will be returned as a type name! */
def_arg:	func_type						{ $$ = (Node *)$1; }
			| reserved_keyword				{ $$ = (Node *)makeString(wpstrdup($1)); }
			| qual_all_Op					{ $$ = (Node *)$1; }
			| NumericOnly					{ $$ = (Node *)$1; }
			| Sconst						{ $$ = (Node *)makeString($1); }
		;


/*****************************************************************************
 *
 *		QUERY:
 *
 *		DROP OWNED BY username [, username ...] [ RESTRICT | CASCADE ]
 *		REASSIGN OWNED BY username [, username ...] TO username
 *
 *****************************************************************************/
DropOwnedStmt:
			DROP OWNED BY name_list opt_drop_behavior
			 	{
					DropOwnedStmt *n = makeNode(DropOwnedStmt);
					n->roles = $4;
					n->behavior = $5;
					$$ = (Node *)n;
				}
		;

ReassignOwnedStmt:
			REASSIGN OWNED BY name_list TO name
				{
					ReassignOwnedStmt *n = makeNode(ReassignOwnedStmt);
					n->roles = $4;
					n->newrole = $6;
					$$ = (Node *)n;
				}
		;

/*****************************************************************************
 *
 *		QUERY:
 *
 *		DROP itemtype [ IF EXISTS ] itemname [, itemname ...]
 *           [ RESTRICT | CASCADE ]
 *
 *****************************************************************************/

DropStmt:	DROP drop_type IF_P EXISTS any_name_list opt_drop_behavior
				{
					DropStmt *n = makeNode(DropStmt);
					n->removeType = $2;
					n->missing_ok = TRUE;
					n->objects = $5;
					n->behavior = $6;
					$$ = (Node *)n;
				}
			| DROP drop_type any_name_list opt_drop_behavior
				{
					DropStmt *n = makeNode(DropStmt);
					n->removeType = $2;
					n->missing_ok = FALSE;
					n->objects = $3;
					n->behavior = $4;
					$$ = (Node *)n;
				}
		;


drop_type:	TABLE									{ $$ = OBJECT_TABLE; }
			| VIEW									{ $$ = OBJECT_VIEW; }
			| DOMAIN_P								{ $$ = OBJECT_DOMAIN; }
			| COLLATION								{ $$ = OBJECT_COLLATION; }
		;

any_name_list:
			any_name								{ $$ = list_make1($1); }
			| any_name_list ',' any_name			{ $$ = lappend($1, $3); }
		;

any_name:	ColId						{ $$ = list_make1(makeString($1)); }
			| ColId attrs				{ $$ = lcons(makeString($1), $2); }
		;

attrs:		'.' attr_name
					{ $$ = list_make1(makeString($2)); }
			| attrs '.' attr_name
					{ $$ = lappend($1, makeString($3)); }
		;


/*****************************************************************************
 *
 *		QUERY:
 *
 *		DROP INDEX [ IF EXISTS ] itemname ON classname [, itemname ON classname ...]
 *           [ RESTRICT | CASCADE ]
 *
 *****************************************************************************/

	DropIndexStmt:	
			DROP INDEX index_spec_list		{ $$ = (Node*)$3; }
		;

		index_spec_list:
			index_spec								{ $$ = list_make1($1); }
			| index_spec_list ',' index_spec		{ $$ = lappend($1, $3); }
		;

		index_spec:
			any_name ON GenericType opt_drop_behavior
				{
					DropIndexStmt *n = makeNode(DropIndexStmt);
					n->name = $1;
					n->typeName = $3;
					n->missing_ok = FALSE;
					n->behavior = $4;
					$$ = (Node *)n;
				}
			| IF_P EXISTS any_name ON GenericType opt_drop_behavior
				{
					DropIndexStmt *n = makeNode(DropIndexStmt);
					n->name = $3;
					n->typeName = $5;
					n->missing_ok = TRUE;
					n->behavior = $6;
					$$ = (Node *)n;
				}
			;
		
/*****************************************************************************
 *
 *		QUERY:
 *				truncate table relname1, relname2, ...
 *
 *****************************************************************************/

TruncateStmt:
			TRUNCATE opt_table relation_expr_list opt_restart_seqs opt_drop_behavior
				{
					TruncateStmt *n = makeNode(TruncateStmt);
					n->relations = $3;
					n->restart_seqs = $4;
					n->behavior = $5;
					$$ = (Node *)n;
				}
		;

opt_restart_seqs:
			CONTINUE_P IDENTITY_P		{ $$ = false; }
			| RESTART IDENTITY_P		{ $$ = true; }
			| /* EMPTY */				{ $$ = false; }
		;

/*****************************************************************************
 *
 * GRANT and REVOKE statements
 *
 *****************************************************************************/

GrantStmt:	GRANT privileges ON privilege_target TO grantee_list
			opt_grant_grant_option
				{
					GrantStmt *n = makeNode(GrantStmt);
					n->is_grant = true;
					n->privileges = $2;
					n->targtype = ($4)->targtype;
					n->objtype = ($4)->objtype;
					n->objects = ($4)->objs;
					n->grantees = $6;
					n->grant_option = $7;
					$$ = (Node*)n;
				}
		;

RevokeStmt:
			REVOKE privileges ON privilege_target
			FROM grantee_list opt_drop_behavior
				{
					GrantStmt *n = makeNode(GrantStmt);
					n->is_grant = false;
					n->grant_option = false;
					n->privileges = $2;
					n->targtype = ($4)->targtype;
					n->objtype = ($4)->objtype;
					n->objects = ($4)->objs;
					n->grantees = $6;
					n->behavior = $7;
					$$ = (Node *)n;
				}
			| REVOKE GRANT OPTION FOR privileges ON privilege_target
			FROM grantee_list opt_drop_behavior
				{
					GrantStmt *n = makeNode(GrantStmt);
					n->is_grant = false;
					n->grant_option = true;
					n->privileges = $5;
					n->targtype = ($7)->targtype;
					n->objtype = ($7)->objtype;
					n->objects = ($7)->objs;
					n->grantees = $9;
					n->behavior = $10;
					$$ = (Node *)n;
				}
		;


/*
 * Privilege names are represented as strings; the validity of the privilege
 * names gets checked at execution.  This is a bit annoying but we have little
 * choice because of the syntactic conflict with lists of role names in
 * GRANT/REVOKE.  What's more, we have to call out in the "privilege"
 * production any reserved keywords that need to be usable as privilege names.
 */

/* either ALL [PRIVILEGES] or a list of individual privileges */
privileges: privilege_list
				{ $$ = $1; }
			| ALL
				{ $$ = NIL; }
			| ALL PRIVILEGES
				{ $$ = NIL; }
			| ALL '(' columnList ')'
				{
					AccessPriv *n = makeNode(AccessPriv);
					n->priv_name = NULL;
					n->cols = $3;
					$$ = list_make1(n);
				}
			| ALL PRIVILEGES '(' columnList ')'
				{
					AccessPriv *n = makeNode(AccessPriv);
					n->priv_name = NULL;
					n->cols = $4;
					$$ = list_make1(n);
				}
		;

privilege_list:	privilege							{ $$ = list_make1($1); }
			| privilege_list ',' privilege			{ $$ = lappend($1, $3); }
		;

privilege:	SELECT opt_column_list
			{
				AccessPriv *n = makeNode(AccessPriv);
				n->priv_name = wpstrdup($1);
				n->cols = $2;
				$$ = n;
			}
		| REFERENCES opt_column_list
			{
				AccessPriv *n = makeNode(AccessPriv);
				n->priv_name = wpstrdup($1);
				n->cols = $2;
				$$ = n;
			}
		| CREATE opt_column_list
			{
				AccessPriv *n = makeNode(AccessPriv);
				n->priv_name = wpstrdup($1);
				n->cols = $2;
				$$ = n;
			}
		| ColId opt_column_list
			{
				AccessPriv *n = makeNode(AccessPriv);
				n->priv_name = $1;
				n->cols = $2;
				$$ = n;
			}
		;


/* Don't bother trying to fold the first two rules into one using
 * opt_table.  You're going to get conflicts.
 */
privilege_target:
			qualified_name_list
				{
					PrivTarget *n = (PrivTarget *) palloc(sizeof(PrivTarget));
					n->targtype = ACL_TARGET_OBJECT;
					n->objtype = ACL_OBJECT_RELATION;
					n->objs = $1;
					$$ = n;
				}
			| TABLE qualified_name_list
				{
					PrivTarget *n = (PrivTarget *) palloc(sizeof(PrivTarget));
					n->targtype = ACL_TARGET_OBJECT;
					n->objtype = ACL_OBJECT_RELATION;
					n->objs = $2;
					$$ = n;
				}
			| SEQUENCE qualified_name_list
				{
					PrivTarget *n = (PrivTarget *) palloc(sizeof(PrivTarget));
					n->targtype = ACL_TARGET_OBJECT;
					n->objtype = ACL_OBJECT_SEQUENCE;
					n->objs = $2;
					$$ = n;
				}
			| DATABASE name_list
				{
					PrivTarget *n = (PrivTarget *) palloc(sizeof(PrivTarget));
					n->targtype = ACL_TARGET_OBJECT;
					n->objtype = ACL_OBJECT_DATABASE;
					n->objs = $2;
					$$ = n;
				}
			| LARGE_P OBJECT_P NumericOnly_list
				{
					PrivTarget *n = (PrivTarget *) palloc(sizeof(PrivTarget));
					n->targtype = ACL_TARGET_OBJECT;
					n->objtype = ACL_OBJECT_LARGEOBJECT;
					n->objs = $3;
					$$ = n;
				}
		;


grantee_list:
			grantee									{ $$ = list_make1($1); }
			| grantee_list ',' grantee				{ $$ = lappend($1, $3); }
		;

grantee:	RoleId
				{
					PrivGrantee *n = makeNode(PrivGrantee);
					/* This hack lets us avoid reserving PUBLIC as a keyword*/
					if (strcmp($1, "public") == 0)
						n->rolname = NULL;
					else
						n->rolname = $1;
					$$ = (Node *)n;
				}
			| GROUP_P RoleId
				{
					PrivGrantee *n = makeNode(PrivGrantee);
					/* Treat GROUP PUBLIC as a synonym for PUBLIC */
					if (strcmp($2, "public") == 0)
						n->rolname = NULL;
					else
						n->rolname = $2;
					$$ = (Node *)n;
				}
		;


opt_grant_grant_option:
			WITH GRANT OPTION { $$ = TRUE; }
			| /*EMPTY*/ { $$ = FALSE; }
		;

/*****************************************************************************
 *
 * GRANT and REVOKE ROLE statements
 *
 *****************************************************************************/

GrantRoleStmt:
			GRANT privilege_list TO name_list opt_grant_admin_option opt_granted_by
				{
					GrantRoleStmt *n = makeNode(GrantRoleStmt);
					n->is_grant = true;
					n->granted_roles = $2;
					n->grantee_roles = $4;
					n->admin_opt = $5;
					n->grantor = $6;
					$$ = (Node*)n;
				}
		;

RevokeRoleStmt:
			REVOKE privilege_list FROM name_list opt_granted_by opt_drop_behavior
				{
					GrantRoleStmt *n = makeNode(GrantRoleStmt);
					n->is_grant = false;
					n->admin_opt = false;
					n->granted_roles = $2;
					n->grantee_roles = $4;
					n->behavior = $6;
					$$ = (Node*)n;
				}
			| REVOKE ADMIN OPTION FOR privilege_list FROM name_list opt_granted_by opt_drop_behavior
				{
					GrantRoleStmt *n = makeNode(GrantRoleStmt);
					n->is_grant = false;
					n->admin_opt = true;
					n->granted_roles = $5;
					n->grantee_roles = $7;
					n->behavior = $9;
					$$ = (Node*)n;
				}
		;

opt_grant_admin_option: WITH ADMIN OPTION				{ $$ = TRUE; }
			| /*EMPTY*/									{ $$ = FALSE; }
		;

opt_granted_by: GRANTED BY RoleId						{ $$ = $3; }
			| /*EMPTY*/									{ $$ = NULL; }
		;


/*****************************************************************************
 *
 *		QUERY: CREATE INDEX
 *
 * Note: we cannot put TABLESPACE clause after WHERE clause unless we are
 * willing to make TABLESPACE a fully reserved word.
 *****************************************************************************/

IndexStmt:	CREATE opt_unique INDEX opt_concurrently opt_index_name
			ON qualified_name access_method_clause '(' index_params ')'
			opt_reloptions OptTableSpace where_clause
				{
					IndexStmt *n = makeNode(IndexStmt);
					n->unique = $2;
					n->concurrent = $4;
					n->idxname = $5;
					n->relation = $7;
					n->accessMethod = $8;
					n->indexParams = $10;
					n->options = $12;
					n->tableSpace = $13;
					n->whereClause = $14;
					n->indexOid = InvalidOid;
					$$ = (Node *)n;
				}
		;

opt_unique:
			UNIQUE									{ $$ = TRUE; }
			| /*EMPTY*/								{ $$ = FALSE; }
		;

opt_concurrently:
			CONCURRENTLY							{ $$ = TRUE; }
			| /*EMPTY*/								{ $$ = FALSE; }
		;

opt_index_name:
			index_name								{ $$ = $1; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

access_method_clause:
			USING access_method						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = "btree"; }
		;

index_params:	index_elem							{ $$ = list_make1($1); }	%dprec 10
			| index_params ',' index_elem			{ $$ = lappend($1, $3); }	%dprec 1
		;

/*
 * Index attributes can be either simple column references, or arbitrary
 * expressions in parens.  For backwards-compatibility reasons, we allow
 * an expression that's just a function call to be written without parens.
 */
index_elem:	member_func_expr opt_collate opt_class opt_asc_desc opt_nulls_order
				{
					$$ = makeNode(IndexElem);
					$$->name = NULL;
					$$->expr = (Node *) $1;
					$$->indexcolname = NULL;
					$$->collation = $2;
					$$->opclass = $3;
					$$->ordering = $4;
					$$->nulls_ordering = $5;
				}
			| '(' a_expr ')' opt_collate opt_class opt_asc_desc opt_nulls_order
				{
					$$ = makeNode(IndexElem);
					$$->name = NULL;
					$$->expr = $2;
					$$->indexcolname = NULL;
					$$->collation = $4;
					$$->opclass = $5;
					$$->ordering = $6;
					$$->nulls_ordering = $7;
				}
		;

opt_collate: COLLATE any_name						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

opt_class:	any_name								{ $$ = $1; }
			| USING any_name						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

opt_asc_desc: ASC							{ $$ = SORTBY_ASC; }
			| DESC							{ $$ = SORTBY_DESC; }
			| /*EMPTY*/						{ $$ = SORTBY_DEFAULT; }
		;

opt_nulls_order: NULLS_FIRST				{ $$ = SORTBY_NULLS_FIRST; }
			| NULLS_LAST					{ $$ = SORTBY_NULLS_LAST; }
			| /*EMPTY*/						{ $$ = SORTBY_NULLS_DEFAULT; }
		;


/*****************************************************************************
 *
 *		QUERY:
 *				create [or replace] function <fname>
 *						[(<type-1> { , <type-n>})]
 *						returns <type-r>
 *						as <filename or code in language as appropriate>
 *						language <lang> [with parameters]
 *
 *****************************************************************************/

/*
 * Ideally param_name should be ColId, but that causes too many conflicts.
 */
param_name:	type_function_name
		;

/*
 * We would like to make the %TYPE productions here be ColId attrs etc,
 * but that causes reduce/reduce conflicts.  type_function_name
 * is next best choice.
 */
func_type:	Typename								{ $$ = $1; }
			| type_function_name attrs '%' TYPE_P
				{
					$$ = makeTypeNameFromNameList(lcons(makeString($1), $2));
					$$->pct_type = true;
					SET_LOCATION($$, @1);
				}
			| SETOF type_function_name attrs '%' TYPE_P
				{
					$$ = makeTypeNameFromNameList(lcons(makeString($2), $3));
					$$->pct_type = true;
					$$->setof = TRUE;
					SET_LOCATION($$, @2);
				}
		;

opt_definition:
			WITH definition							{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

/*****************************************************************************
 *
 *		QUERY:
 *
 *		DROP OPERATOR opname (leftoperand_typ, rightoperand_typ) [ RESTRICT | CASCADE ]
 *
 *****************************************************************************/

any_operator:
			all_Op
					{ $$ = list_make1(makeString($1)); }
			| ColId '.' any_operator
					{ $$ = lcons(makeString($1), $3); }
		;

/*****************************************************************************
 *
 *		QUERY:
 *
 *		REINDEX type <name> [FORCE]
 *
 * FORCE no longer does anything, but we accept it for backwards compatibility
 *****************************************************************************/

ReindexStmt:
			REINDEX reindex_type qualified_name opt_force
				{
					ReindexStmt *n = makeNode(ReindexStmt);
					n->kind = $2;
					n->relation = $3;
					n->name = NULL;
					$$ = (Node *)n;
				}
			| REINDEX SYSTEM_P name opt_force
				{
					ReindexStmt *n = makeNode(ReindexStmt);
					n->kind = OBJECT_DATABASE;
					n->name = $3;
					n->relation = NULL;
					n->do_system = true;
					n->do_user = false;
					$$ = (Node *)n;
				}
			| REINDEX DATABASE name opt_force
				{
					ReindexStmt *n = makeNode(ReindexStmt);
					n->kind = OBJECT_DATABASE;
					n->name = $3;
					n->relation = NULL;
					n->do_system = true;
					n->do_user = true;
					$$ = (Node *)n;
				}
		;

reindex_type:
			INDEX									{ $$ = OBJECT_INDEX; }
			| TABLE									{ $$ = OBJECT_TABLE; }
		;

opt_force:	FORCE									{  $$ = TRUE; }
			| /* EMPTY */							{  $$ = FALSE; }
		;


/*****************************************************************************
 *
 * ALTER THING name RENAME TO newname
 *
 *****************************************************************************/

opt_column: COLUMN									{ $$ = COLUMN; }
			| /*EMPTY*/								{ $$ = 0; }
		;

opt_set_data: SET DATA_P							{ $$ = 1; }
			| /*EMPTY*/								{ $$ = 0; }
		;

/*****************************************************************************
 *
 *		Transactions:
 *
 *		BEGIN / COMMIT / ROLLBACK
 *		(also older versions END / ABORT)
 *
 *****************************************************************************/

TransactionStmt:
			ABORT_P opt_transaction
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_ROLLBACK;
					n->options = NIL;
					$$ = (Node *)n;
				}
			| BEGIN_P opt_transaction transaction_mode_list_or_empty
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_BEGIN;
					n->options = $3;
					$$ = (Node *)n;
				}
			| START TRANSACTION transaction_mode_list_or_empty
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_START;
					n->options = $3;
					$$ = (Node *)n;
				}
			| COMMIT opt_transaction
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_COMMIT;
					n->options = NIL;
					$$ = (Node *)n;
				}
			| END_P opt_transaction
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_COMMIT;
					n->options = NIL;
					$$ = (Node *)n;
				}
			| ROLLBACK opt_transaction
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_ROLLBACK;
					n->options = NIL;
					$$ = (Node *)n;
				}
			| SAVEPOINT ColId
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_SAVEPOINT;
					n->options = list_make1(makeDefElem(L"savepoint_name",
														(Node *)makeString($2)));
					$$ = (Node *)n;
				}
			| RELEASE SAVEPOINT ColId
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_RELEASE;
					n->options = list_make1(makeDefElem(L"savepoint_name",
														(Node *)makeString($3)));
					$$ = (Node *)n;
				}
			| RELEASE ColId
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_RELEASE;
					n->options = list_make1(makeDefElem(L"savepoint_name",
														(Node *)makeString($2)));
					$$ = (Node *)n;
				}
			| ROLLBACK opt_transaction TO SAVEPOINT ColId
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_ROLLBACK_TO;
					n->options = list_make1(makeDefElem(L"savepoint_name",
														(Node *)makeString($5)));
					$$ = (Node *)n;
				}
			| ROLLBACK opt_transaction TO ColId
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_ROLLBACK_TO;
					n->options = list_make1(makeDefElem(L"savepoint_name",
														(Node *)makeString($4)));
					$$ = (Node *)n;
				}
			| PREPARE TRANSACTION Sconst
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_PREPARE;
					n->gid = $3;
					$$ = (Node *)n;
				}
			| COMMIT PREPARED Sconst
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_COMMIT_PREPARED;
					n->gid = $3;
					$$ = (Node *)n;
				}
			| ROLLBACK PREPARED Sconst
				{
					TransactionStmt *n = makeNode(TransactionStmt);
					n->kind = TRANS_STMT_ROLLBACK_PREPARED;
					n->gid = $3;
					$$ = (Node *)n;
				}
		;

opt_transaction:	WORK							{}
			| TRANSACTION							{}
			| /*EMPTY*/								{}
		;

transaction_mode_item:
			ISOLATION LEVEL iso_level
					{ $$ = makeDefElem(L"transaction_isolation",
									   makeStringConst($3, @3)); }
			| READ ONLY
					{ $$ = makeDefElem(L"transaction_read_only",
									   makeIntConst(TRUE, @1)); }
			| READ WRITE
					{ $$ = makeDefElem(L"transaction_read_only",
									   makeIntConst(FALSE, @1)); }
			| DEFERRABLE
					{ $$ = makeDefElem(L"transaction_deferrable",
									   makeIntConst(TRUE, @1)); }
			| NOT DEFERRABLE
					{ $$ = makeDefElem(L"transaction_deferrable",
									   makeIntConst(FALSE, @1)); }
		;

/* Syntax with commas is SQL-spec, without commas is Postgres historical */
transaction_mode_list:
			transaction_mode_item
					{ $$ = list_make1($1); }
			| transaction_mode_list ',' transaction_mode_item
					{ $$ = lappend($1, $3); }
			| transaction_mode_list transaction_mode_item
					{ $$ = lappend($1, $2); }
		;

transaction_mode_list_or_empty:
			transaction_mode_list
			| /* EMPTY */
					{ $$ = NIL; }
		;


/*****************************************************************************
 *
 *	QUERY:
 *		CREATE [ OR REPLACE ] [ TEMP ] VIEW <viewname> '('target-list ')'
 *			AS <query> [ WITH [ CASCADED | LOCAL ] CHECK OPTION ]
 *
 *****************************************************************************/

ViewStmt: CREATE OptTemp VIEW qualified_name opt_column_list
				AS SelectStmt opt_check_option
				{
					ViewStmt *n = makeNode(ViewStmt);
					n->view = $4;
					n->view->relpersistence = $2;
					n->aliases = $5;
					n->query = $7;
					n->replace = false;
					$$ = (Node *) n;
				}
		| CREATE OR REPLACE OptTemp VIEW qualified_name opt_column_list
				AS SelectStmt opt_check_option
				{
					ViewStmt *n = makeNode(ViewStmt);
					n->view = $6;
					n->view->relpersistence = $4;
					n->aliases = $7;
					n->query = $9;
					n->replace = true;
					$$ = (Node *) n;
				}
		;

opt_check_option:
		WITH CHECK OPTION
				{
					errprint("\nERROR FEATURE_NOT_SUPPORTED: WITH CHECK OPTION is not implemented");
					ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @1, $1, ScErrMessage("WITH CHECK OPTION is not supported"));
				}
		| WITH CASCADED CHECK OPTION
				{
					errprint("\nERROR FEATURE_NOT_SUPPORTED: WITH CHECK OPTION is not implemented");
					ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @1, $1, ScErrMessage("WITH CHECK OPTION is not supported"));
				}
		| WITH LOCAL CHECK OPTION
				{
					errprint("\nERROR FEATURE_NOT_SUPPORTED: WITH CHECK OPTION is not implemented");
					ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @1, $1, ScErrMessage("WITH CHECK OPTION is not supported"));
				}
		| /* EMPTY */							{ $$ = NIL; }
		;

/*****************************************************************************
 *
 *		CREATE DATABASE
 *
 *****************************************************************************/

CreatedbStmt:
			CREATE DATABASE database_name opt_with createdb_opt_list
				{
					CreatedbStmt *n = makeNode(CreatedbStmt);
					n->dbname = $3;
					n->options = $5;
					$$ = (Node *)n;
				}
		;

createdb_opt_list:
			createdb_opt_list createdb_opt_item		{ $$ = lappend($1, $2); }
			| /* EMPTY */							{ $$ = NIL; }
		;

createdb_opt_item:
			TEMPLATE opt_equal name
				{
					$$ = makeDefElem(L"template", (Node *)makeString($3));
				}
			| TEMPLATE opt_equal DEFAULT
				{
					$$ = makeDefElem(L"template", NULL);
				}
			| ENCODING opt_equal Sconst
				{
					$$ = makeDefElem(L"encoding", (Node *)makeString($3));
				}
			| ENCODING opt_equal Iconst
				{
					$$ = makeDefElem(L"encoding", (Node *)makeInteger($3));
				}
			| ENCODING opt_equal DEFAULT
				{
					$$ = makeDefElem(L"encoding", NULL);
				}
			| LC_COLLATE_P opt_equal Sconst
				{
					$$ = makeDefElem(L"lc_collate", (Node *)makeString($3));
				}
			| LC_COLLATE_P opt_equal DEFAULT
				{
					$$ = makeDefElem(L"lc_collate", NULL);
				}
			| LC_CTYPE_P opt_equal Sconst
				{
					$$ = makeDefElem(L"lc_ctype", (Node *)makeString($3));
				}
			| LC_CTYPE_P opt_equal DEFAULT
				{
					$$ = makeDefElem(L"lc_ctype", NULL);
				}
			| OWNER opt_equal name
				{
					$$ = makeDefElem(L"owner", (Node *)makeString($3));
				}
			| OWNER opt_equal DEFAULT
				{
					$$ = makeDefElem(L"owner", NULL);
				}
		;

/*
 *	Though the equals sign doesn't match other WITH options, pg_dump uses
 *	equals for backward compatibility, and it doesn't seem worth removing it.
 */
opt_equal:	'='										{}
			| /*EMPTY*/								{}
		;

/*****************************************************************************
 *
 *		DROP DATABASE [ IF EXISTS ]
 *
 * This is implicitly CASCADE, no need for drop behavior
 *****************************************************************************/

DropdbStmt: DROP DATABASE database_name
				{
					DropdbStmt *n = makeNode(DropdbStmt);
					n->dbname = $3;
					n->missing_ok = FALSE;
					$$ = (Node *)n;
				}
			| DROP DATABASE IF_P EXISTS database_name
				{
					DropdbStmt *n = makeNode(DropdbStmt);
					n->dbname = $5;
					n->missing_ok = TRUE;
					$$ = (Node *)n;
				}
		;


/*****************************************************************************
 *
 * Manipulate a domain
 *
 *****************************************************************************/

CreateDomainStmt:
			CREATE DOMAIN_P any_name opt_as Typename ColQualList
				{
					CreateDomainStmt *n = makeNode(CreateDomainStmt);
					n->domainname = $3;
					n->typeName = $5;
					SplitColQualList($6, &n->constraints, &n->collClause,
									 yyscanner);
					$$ = (Node *)n;
				}
		;

AlterDomainStmt:
			/* ALTER DOMAIN <domain> {SET DEFAULT <expr>|DROP DEFAULT} */
			ALTER DOMAIN_P any_name alter_column_default
				{
					AlterDomainStmt *n = makeNode(AlterDomainStmt);
					n->subtype = 'T';
					n->typeName = $3;
					n->def = $4;
					$$ = (Node *)n;
				}
			/* ALTER DOMAIN <domain> DROP NOT NULL */
			| ALTER DOMAIN_P any_name DROP NOT NULL_P
				{
					AlterDomainStmt *n = makeNode(AlterDomainStmt);
					n->subtype = 'N';
					n->typeName = $3;
					$$ = (Node *)n;
				}
			/* ALTER DOMAIN <domain> SET NOT NULL */
			| ALTER DOMAIN_P any_name SET NOT NULL_P
				{
					AlterDomainStmt *n = makeNode(AlterDomainStmt);
					n->subtype = 'O';
					n->typeName = $3;
					$$ = (Node *)n;
				}
			/* ALTER DOMAIN <domain> ADD CONSTRAINT ... */
			| ALTER DOMAIN_P any_name ADD_P TableConstraint
				{
					AlterDomainStmt *n = makeNode(AlterDomainStmt);
					n->subtype = 'C';
					n->typeName = $3;
					n->def = $5;
					$$ = (Node *)n;
				}
			/* ALTER DOMAIN <domain> DROP CONSTRAINT <name> [RESTRICT|CASCADE] */
			| ALTER DOMAIN_P any_name DROP CONSTRAINT name opt_drop_behavior
				{
					AlterDomainStmt *n = makeNode(AlterDomainStmt);
					n->subtype = 'X';
					n->typeName = $3;
					n->name = $6;
					n->behavior = $7;
					$$ = (Node *)n;
				}
			;

opt_as:		AS										{}
			| /* EMPTY */							{}
		;


/*****************************************************************************
 *
 *		QUERY:
 *				ANALYZE
 *
 *****************************************************************************/

analyze_keyword:
			ANALYZE									{}
			| ANALYSE /* British */					{}
		;

opt_verbose:
			VERBOSE									{ $$ = TRUE; }
			| /*EMPTY*/								{ $$ = FALSE; }
		;

opt_name_list:
			'(' name_list ')'						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;


/*****************************************************************************
 *
 *		QUERY:
 *				EXPLAIN [ANALYZE] [VERBOSE] query
 *				EXPLAIN ( options ) query
 *
 *****************************************************************************/

ExplainStmt:
		EXPLAIN ExplainableStmt
				{
					ExplainStmt *n = makeNode(ExplainStmt);
					n->query = $2;
					n->options = NIL;
					$$ = (Node *) n;
				}
		| EXPLAIN analyze_keyword opt_verbose ExplainableStmt
				{
					ExplainStmt *n = makeNode(ExplainStmt);
					n->query = $4;
					n->options = list_make1(makeDefElem(L"analyze", NULL));
					if ($3)
						n->options = lappend(n->options,
											 makeDefElem(L"verbose", NULL));
					$$ = (Node *) n;
				}
		| EXPLAIN VERBOSE ExplainableStmt
				{
					ExplainStmt *n = makeNode(ExplainStmt);
					n->query = $3;
					n->options = list_make1(makeDefElem(L"verbose", NULL));
					$$ = (Node *) n;
				}
		| EXPLAIN '(' explain_option_list ')' ExplainableStmt
				{
					ExplainStmt *n = makeNode(ExplainStmt);
					n->query = $5;
					n->options = $3;
					$$ = (Node *) n;
				}
		;

ExplainableStmt:
			SelectStmt
			| InsertStmt
			| UpdateStmt
			| DeleteStmt
			| CreateAsStmt
			| ExecuteStmt					/* by default all are $$=$1 */
		;

explain_option_list:
			explain_option_elem
				{
					$$ = list_make1($1);
				}
			| explain_option_list ',' explain_option_elem
				{
					$$ = lappend($1, $3);
				}
		;

explain_option_elem:
			explain_option_name explain_option_arg
				{
					$$ = makeDefElem($1, $2);
				}
		;

explain_option_name:
			ColId					{ $$ = $1; }
			| analyze_keyword		{ $$ = L"analyze"; }
			| VERBOSE				{ $$ = L"verbose"; }
		;

explain_option_arg:
			opt_boolean_or_string	{ $$ = (Node *) makeString($1); }
			| NumericOnly			{ $$ = (Node *) $1; }
			| /* EMPTY */			{ $$ = NULL; }
		;

/*****************************************************************************
 *
 *		QUERY:
 *				PREPARE <plan_name> [(args, ...)] AS <query>
 *
 *****************************************************************************/

PrepareStmt: PREPARE name prep_type_clause AS PreparableStmt
				{
					PrepareStmt *n = makeNode(PrepareStmt);
					n->name = $2;
					n->argtypes = $3;
					n->query = $5;
					$$ = (Node *) n;
				}
		;

prep_type_clause: '(' type_list ')'			{ $$ = $2; }
				| /* EMPTY */				{ $$ = NIL; }
		;

PreparableStmt:
			SelectStmt
			| InsertStmt
			| UpdateStmt
			| DeleteStmt					/* by default all are $$=$1 */
		;

/*****************************************************************************
 *
 * EXECUTE <plan_name> [(params, ...)]
 * CREATE TABLE <name> AS EXECUTE <plan_name> [(params, ...)]
 *
 *****************************************************************************/

ExecuteStmt: EXECUTE name execute_param_clause
				{
					ExecuteStmt *n = makeNode(ExecuteStmt);
					n->name = $2;
					n->params = $3;
					n->into = NULL;
					$$ = (Node *) n;
				}
			| CREATE OptTemp TABLE create_as_target AS
				EXECUTE name execute_param_clause
				{
					ExecuteStmt *n = makeNode(ExecuteStmt);
					n->name = $7;
					n->params = $8;
					$4->rel->relpersistence = $2;
					n->into = $4;
					if ($4->colNames)
					{
						errprint("\nERROR FEATURE_NOT_SUPPORTED: column name list not allowed in CREATE TABLE / AS EXECUTE");
						ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @4, $4, ScErrMessage("column name list not allowed in CREATE TABLE / AS EXECUTE"));
					}
					/* ... because it's not implemented, but it could be */
					$$ = (Node *) n;
				}
		;

execute_param_clause: '(' expr_list ')'				{ $$ = $2; }
					| /* EMPTY */					{ $$ = NIL; }
					;

/*****************************************************************************
 *
 *		QUERY:
 *				DEALLOCATE [PREPARE] <plan_name>
 *
 *****************************************************************************/

DeallocateStmt: DEALLOCATE name
					{
						DeallocateStmt *n = makeNode(DeallocateStmt);
						n->name = $2;
						$$ = (Node *) n;
					}
				| DEALLOCATE PREPARE name
					{
						DeallocateStmt *n = makeNode(DeallocateStmt);
						n->name = $3;
						$$ = (Node *) n;
					}
				| DEALLOCATE ALL
					{
						DeallocateStmt *n = makeNode(DeallocateStmt);
						n->name = NULL;
						$$ = (Node *) n;
					}
				| DEALLOCATE PREPARE ALL
					{
						DeallocateStmt *n = makeNode(DeallocateStmt);
						n->name = NULL;
						$$ = (Node *) n;
					}
		;

/*****************************************************************************
 *
 *		QUERY:
 *				INSERT STATEMENTS
 *
 *****************************************************************************/

InsertStmt:
			opt_with_clause INSERT INTO qualified_name insert_rest returning_clause
				{
					$5->relation = $4;
					$5->returningList = $6;
					$5->withClause = $1;
					$$ = (Node *) $5;
				}
		;

insert_rest:
			SelectStmt
				{
					$$ = makeNode(InsertStmt);
					$$->cols = NIL;
					$$->selectStmt = $1;
				}
			| '(' insert_column_list ')' SelectStmt
				{
					$$ = makeNode(InsertStmt);
					$$->cols = $2;
					$$->selectStmt = $4;
				}
			| DEFAULT VALUES
				{
					$$ = makeNode(InsertStmt);
					$$->cols = NIL;
					$$->selectStmt = NULL;
				}
		;

insert_column_list:
			insert_column_item
					{ $$ = list_make1($1); }
			| insert_column_list ',' insert_column_item
					{ $$ = lappend($1, $3); }
		;

insert_column_item:
			ColId opt_indirection
				{
					$$ = makeNode(ResTarget);
					$$->name = $1;
					$$->indirection = check_indirection($2, yyscanner);
					$$->val = NULL;
					SET_LOCATION($$, @1);
				}
		;

returning_clause:
			RETURNING target_list		{ $$ = $2; }
			| /* EMPTY */				{ $$ = NIL; }
		;


/*****************************************************************************
 *
 *		QUERY:
 *				DELETE STATEMENTS
 *
 *****************************************************************************/

DeleteStmt: opt_with_clause DELETE_P FROM relation_expr_opt_alias
			using_clause where_or_current_clause returning_clause
				{
					DeleteStmt *n = makeNode(DeleteStmt);
					n->relation = $4;
					n->usingClause = $5;
					n->whereClause = $6;
					n->returningList = $7;
					n->withClause = $1;
					$$ = (Node *)n;
				}
		;

using_clause:
	    		USING from_list						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;


/*****************************************************************************
 *
 *		QUERY:
 *				LOCK TABLE
 *
 *****************************************************************************/

LockStmt:	LOCK_P opt_table relation_expr_list opt_lock opt_nowait
				{
					LockStmt *n = makeNode(LockStmt);

					n->relations = $3;
					n->mode = $4;
					n->nowait = $5;
					$$ = (Node *)n;
				}
		;

opt_lock:	IN_P lock_type MODE 			{ $$ = $2; }
			| /*EMPTY*/						{ $$ = AccessExclusiveLock; }
		;

lock_type:	ACCESS SHARE					{ $$ = AccessShareLock; }
			| ROW SHARE						{ $$ = RowShareLock; }
			| ROW EXCLUSIVE					{ $$ = RowExclusiveLock; }
			| SHARE UPDATE EXCLUSIVE		{ $$ = ShareUpdateExclusiveLock; }
			| SHARE							{ $$ = ShareLock; }
			| SHARE ROW EXCLUSIVE			{ $$ = ShareRowExclusiveLock; }
			| EXCLUSIVE						{ $$ = ExclusiveLock; }
			| ACCESS EXCLUSIVE				{ $$ = AccessExclusiveLock; }
		;

opt_nowait:	NOWAIT							{ $$ = TRUE; }
			| /*EMPTY*/						{ $$ = FALSE; }
		;


/*****************************************************************************
 *
 *		QUERY:
 *				UpdateStmt (UPDATE)
 *
 *****************************************************************************/

UpdateStmt: opt_with_clause UPDATE relation_expr_opt_alias
			SET set_clause_list
			from_clause
			where_or_current_clause
			returning_clause
				{
					UpdateStmt *n = makeNode(UpdateStmt);
					n->relation = $3;
					n->targetList = $5;
					n->fromClause = $6;
					n->whereClause = $7;
					n->returningList = $8;
					n->withClause = $1;
					$$ = (Node *)n;
				}
		;

set_clause_list:
			set_clause							{ $$ = $1; }
			| set_clause_list ',' set_clause	{ $$ = list_concat($1,$3); }
		;

set_clause:
			single_set_clause						{ $$ = list_make1($1); }
			| multiple_set_clause					{ $$ = $1; }
		;

single_set_clause:
			set_target '=' ctext_expr
				{
					$$ = $1;
					$$->val = (Node *) $3;
				}
		;

multiple_set_clause:
			'(' set_target_list ')' '=' ctext_row
				{
					ListCell *col_cell;
					ListCell *val_cell;

					/*
					 * Break the ctext_row apart, merge individual expressions
					 * into the destination ResTargets.  XXX this approach
					 * cannot work for general row expressions as sources.
					 */
					if (list_length($2) != list_length($5))
					{
						errprint("\nERROR SYNTAX_ERROR: number of columns does not match number of values");
					}
					forboth(col_cell, $2, val_cell, $5)
					{
						ResTarget *res_col = (ResTarget *) lfirst(col_cell);
						Node *res_val = (Node *) lfirst(val_cell);

						res_col->val = res_val;
					}

					$$ = $2;
				}
		;

set_target:
			ColId opt_indirection
				{
					$$ = makeNode(ResTarget);
					$$->name = $1;
					$$->indirection = check_indirection($2, yyscanner);
					$$->val = NULL;	/* upper production sets this */
					SET_LOCATION($$, @1);
				}
		;

set_target_list:
			set_target								{ $$ = list_make1($1); }
			| set_target_list ',' set_target		{ $$ = lappend($1,$3); }
		;


/*****************************************************************************
 *
 *		QUERY:
 *				SELECT STATEMENTS
 *
 *****************************************************************************/

/* A complete SELECT statement looks like this.
 *
 * The rule returns either a single SelectStmt node or a tree of them,
 * representing a set-operation tree.
 *
 * There is an ambiguity when a sub-SELECT is within an a_expr and there
 * are excess parentheses: do the parentheses belong to the sub-SELECT or
 * to the surrounding a_expr?  We don't really care, but bison wants to know.
 * To resolve the ambiguity, we are careful to define the grammar so that
 * the decision is staved off as long as possible: as long as we can keep
 * absorbing parentheses into the sub-SELECT, we will do so, and only when
 * it's no longer possible to do that will we decide that parens belong to
 * the expression.	For example, in "SELECT (((SELECT 2)) + 3)" the extra
 * parentheses are treated as part of the sub-select.  The necessity of doing
 * it that way is shown by "SELECT (((SELECT 2)) UNION SELECT 2)".	Had we
 * parsed "((SELECT 2))" as an a_expr, it'd be too late to go back to the
 * SELECT viewpoint when we see the UNION.
 *
 * This approach is implemented by defining a nonterminal select_with_parens,
 * which represents a SELECT with at least one outer layer of parentheses,
 * and being careful to use select_with_parens, never '(' SelectStmt ')',
 * in the expression grammar.  We will then have shift-reduce conflicts
 * which we can resolve in favor of always treating '(' <select> ')' as
 * a select_with_parens.  To resolve the conflicts, the productions that
 * conflict with the select_with_parens productions are manually given
 * precedences lower than the precedence of ')', thereby ensuring that we
 * shift ')' (and then reduce to select_with_parens) rather than trying to
 * reduce the inner <select> nonterminal to something else.  We use UMINUS
 * precedence for this, which is a fairly arbitrary choice.
 *
 * To be able to define select_with_parens itself without ambiguity, we need
 * a nonterminal select_no_parens that represents a SELECT structure with no
 * outermost parentheses.  This is a little bit tedious, but it works.
 *
 * In non-expression contexts, we use SelectStmt which can represent a SELECT
 * with or without outer parentheses.
 */

SelectStmt: select_no_parens			%prec UMINUS
			| select_with_parens		%prec UMINUS
		;

select_with_parens:
			'(' select_no_parens ')'				{ $$ = $2; }
			| '(' select_with_parens ')'			{ $$ = $2; }
		;

/*
 * This rule parses the equivalent of the standard's <query expression>.
 * The duplicative productions are annoying, but hard to get rid of without
 * creating shift/reduce conflicts.
 *
 *	FOR UPDATE/SHARE may be before or after LIMIT/OFFSET.
 *	In <=7.2.X, LIMIT/OFFSET had to be after FOR UPDATE
 *	We now support both orderings, but prefer LIMIT/OFFSET before FOR UPDATE/SHARE
 *	2002-08-28 bjm
 */
select_no_parens:
			simple_select						{ $$ = $1; }
			| simple_select option_hint_clause	{ $$ = $1; }
			| select_clause sort_clause opt_option_hint_clause
				{
					insertSelectOptions((SelectStmt *) $1, $2, NIL,
										NULL, NULL, $3,
										yyscanner);
					$$ = $1;
				}
			| select_clause opt_sort_clause for_locking_clause opt_select_limit opt_option_hint_clause
				{
					insertSelectOptions((SelectStmt *) $1, $2, $3, $4, NULL, $5,
										yyscanner);
					$$ = $1;
				}
			| select_clause opt_sort_clause select_limit opt_for_locking_clause opt_option_hint_clause
				{
					insertSelectOptions((SelectStmt *) $1, $2, $4, $3, NULL, $5,
										yyscanner);
					$$ = $1;
				}
			| with_clause select_clause opt_option_hint_clause
				{
					insertSelectOptions((SelectStmt *) $2, NULL, NIL, NULL, $1, $3,
										yyscanner);
					$$ = $2;
				}
			| with_clause select_clause sort_clause opt_option_hint_clause
				{
					insertSelectOptions((SelectStmt *) $2, $3, NIL, NULL, $1, $4,
										yyscanner);
					$$ = $2;
				}
			| with_clause select_clause opt_sort_clause for_locking_clause opt_select_limit opt_option_hint_clause
				{
					insertSelectOptions((SelectStmt *) $2, $3, $4, $5, $1, $6,
										yyscanner);
					$$ = $2;
				}
			| with_clause select_clause opt_sort_clause select_limit opt_for_locking_clause opt_option_hint_clause
				{
					insertSelectOptions((SelectStmt *) $2, $3, $5, $4, $1, $6,
										yyscanner);
					$$ = $2;
				}
		;

select_clause:
			simple_select							{ $$ = $1; }
			| select_with_parens					{ $$ = $1; }
		;

/*
 * This rule parses SELECT statements that can appear within set operations,
 * including UNION, INTERSECT and EXCEPT.  '(' and ')' can be used to specify
 * the ordering of the set operations.	Without '(' and ')' we want the
 * operations to be ordered per the precedence specs at the head of this file.
 *
 * As with select_no_parens, simple_select cannot have outer parentheses,
 * but can have parenthesized subclauses.
 *
 * Note that sort clauses cannot be included at this level --- SQL92 requires
 *		SELECT foo UNION SELECT bar ORDER BY baz
 * to be parsed as
 *		(SELECT foo UNION SELECT bar) ORDER BY baz
 * not
 *		SELECT foo UNION (SELECT bar ORDER BY baz)
 * Likewise for WITH, FOR UPDATE and LIMIT.  Therefore, those clauses are
 * described as part of the select_no_parens production, not simple_select.
 * This does not limit functionality, because you can reintroduce these
 * clauses inside parentheses.
 *
 * NOTE: only the leftmost component SelectStmt should have INTO.
 * However, this is not checked by the grammar; parse analysis must check it.
 */
simple_select:
			SELECT opt_distinct target_list
			into_clause from_clause where_clause
			group_clause having_clause window_clause
				{
					SelectStmt *n = makeNode(SelectStmt);
					n->distinctClause = $2;
					n->targetList = $3;
					n->intoClause = $4;
					n->fromClause = $5;
					n->whereClause = $6;
					n->groupClause = $7;
					n->havingClause = $8;
					n->windowClause = $9;
					$$ = (Node *)n;
				}
			| values_clause							{ $$ = $1; }
			| TABLE relation_expr
				{
					/* same as SELECT * FROM relation_expr */
					ColumnRef *cr = makeNode(ColumnRef);
					ResTarget *rt = makeNode(ResTarget);
					SelectStmt *n = makeNode(SelectStmt);

					cr->fields = list_make1(makeNode(A_Star));
					cr->location = -1;

					rt->name = NULL;
					rt->indirection = NIL;
					rt->val = (Node *)cr;
					rt->location = -1;

					n->targetList = list_make1(rt);
					n->fromClause = list_make1($2);
					$$ = (Node *)n;
				}
			| select_clause UNION opt_all select_clause
				{
					$$ = makeSetOp(SETOP_UNION, $3, $1, $4);
				}
			| select_clause INTERSECT opt_all select_clause
				{
					$$ = makeSetOp(SETOP_INTERSECT, $3, $1, $4);
				}
			| select_clause EXCEPT opt_all select_clause
				{
					$$ = makeSetOp(SETOP_EXCEPT, $3, $1, $4);
				}
		;

/*
 * SQL standard WITH clause looks like:
 *
 * WITH [ RECURSIVE ] <query name> [ (<column>,...) ]
 *		AS (query) [ SEARCH or CYCLE clause ]
 *
 * We don't currently support the SEARCH or CYCLE clause.
 */
with_clause:
		WITH cte_list
			{
				$$ = makeNode(WithClause);
				$$->ctes = $2;
				$$->recursive = false;
				SET_LOCATION($$, @1);
			}
		| WITH RECURSIVE cte_list
			{
				$$ = makeNode(WithClause);
				$$->ctes = $3;
				$$->recursive = true;
				SET_LOCATION($$, @1);
			}
		;

cte_list:
		common_table_expr						{ $$ = list_make1($1); }
		| cte_list ',' common_table_expr		{ $$ = lappend($1, $3); }
		;

common_table_expr:  name opt_name_list AS '(' PreparableStmt ')'
			{
				CommonTableExpr *n = makeNode(CommonTableExpr);
				n->ctename = $1;
				n->aliascolnames = $2;
				n->ctequery = $5;
				SET_LOCATION(n, @1);
				$$ = (Node *) n;
			}
		;

opt_with_clause:
		with_clause								{ $$ = $1; }
		| /*EMPTY*/								{ $$ = NULL; }
		;

into_clause:
			INTO OptTempTableName
				{
					$$ = makeNode(IntoClause);
					$$->rel = $2;
					$$->colNames = NIL;
					$$->options = NIL;
					$$->onCommit = ONCOMMIT_NOOP;
					$$->tableSpaceName = NULL;
				}
			| /*EMPTY*/
				{ $$ = NULL; }
		;

/*
 * Redundancy here is needed to avoid shift/reduce conflicts,
 * since TEMP is not a reserved word.  See also OptTemp.
 */
OptTempTableName:
			TEMPORARY opt_table qualified_name
				{
					$$ = $3;
					$$->relpersistence = RELPERSISTENCE_TEMP;
				}
			| TEMP opt_table qualified_name
				{
					$$ = $3;
					$$->relpersistence = RELPERSISTENCE_TEMP;
				}
			| LOCAL TEMPORARY opt_table qualified_name
				{
					$$ = $4;
					$$->relpersistence = RELPERSISTENCE_TEMP;
				}
			| LOCAL TEMP opt_table qualified_name
				{
					$$ = $4;
					$$->relpersistence = RELPERSISTENCE_TEMP;
				}
			| GLOBAL TEMPORARY opt_table qualified_name
				{
					$$ = $4;
					$$->relpersistence = RELPERSISTENCE_TEMP;
				}
			| GLOBAL TEMP opt_table qualified_name
				{
					$$ = $4;
					$$->relpersistence = RELPERSISTENCE_TEMP;
				}
			| UNLOGGED opt_table qualified_name
				{
					$$ = $3;
					$$->relpersistence = RELPERSISTENCE_UNLOGGED;
				}
			| TABLE qualified_name
				{
					$$ = $2;
					$$->relpersistence = RELPERSISTENCE_PERMANENT;
				}
			| qualified_name
				{
					$$ = $1;
					$$->relpersistence = RELPERSISTENCE_PERMANENT;
				}
		;

opt_table:	TABLE									{}
			| /*EMPTY*/								{}
		;

opt_all:	ALL										{ $$ = TRUE; }
			| DISTINCT								{ $$ = FALSE; }
			| /*EMPTY*/								{ $$ = FALSE; }
		;

/* We use (NIL) as a placeholder to indicate that all target expressions
 * should be placed in the DISTINCT list during parsetree analysis.
 */
opt_distinct:
			DISTINCT								{ $$ = list_make1(NIL); }
			| DISTINCT ON '(' expr_list ')'			{ $$ = $4; }
			| ALL									{ $$ = NIL; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

opt_sort_clause:
			sort_clause								{ $$ = $1;}
			| /*EMPTY*/								{ $$ = NIL; }
		;

sort_clause:
			ORDER BY sortby_list					{ $$ = $3; }
		;

sortby_list:
			sortby									{ $$ = list_make1($1); }	%dprec 10
			| sortby_list ',' sortby				{ $$ = lappend($1, $3); }	%dprec 1
		;

sortby:		a_expr USING qual_all_Op opt_nulls_order
				{
					$$ = makeNode(SortBy);
					$$->node = $1;
					$$->sortby_dir = SORTBY_USING;
					$$->sortby_nulls = $4;
					$$->useOp = $3;
					SET_LOCATION($$, @3);
				}
			| a_expr opt_asc_desc opt_nulls_order
				{
					$$ = makeNode(SortBy);
					$$->node = $1;
					$$->sortby_dir = $2;
					$$->sortby_nulls = $3;
					$$->useOp = NIL;
					SET_LOCATION($$, -1);		/* no operator */
				}
		;


select_limit:
			limit_clause offset_clause			
				{ 
					$$ = makeNode(LimitOffset);
					$$->limitCount = $1;
					$$->limitOffset = $2;
					$$->limitOffsetkey = NULL;
					SET_LOCATION($$, @1);
				}
			| offset_clause limit_clause		
				{ 
					$$ = makeNode(LimitOffset);
					$$->limitCount = $2;
					$$->limitOffset = $1;
					$$->limitOffsetkey = NULL;
					SET_LOCATION($$, @1);
				}
			| limit_clause						
				{ 
					$$ = makeNode(LimitOffset);
					$$->limitCount = $1;
					$$->limitOffset = NULL;
					$$->limitOffsetkey = NULL;
					SET_LOCATION($$, @1);
				}
			| offset_clause						
				{ 
					$$ = makeNode(LimitOffset);
					$$->limitCount = NULL;
					$$->limitOffset = $1;
					$$->limitOffsetkey = NULL;
					SET_LOCATION($$, @1);
				}
			| limit_clause offsetkey_clause
				{
					$$ = makeNode(LimitOffset);
					$$->limitCount = $1;
					$$->limitOffset = NULL;
					$$->limitOffsetkey = $2;
					SET_LOCATION($$, @1);
				}
			| offsetkey_clause limit_clause
				{
					$$ = makeNode(LimitOffset);
					$$->limitCount = $2;
					$$->limitOffset = NULL;
					$$->limitOffsetkey = $1;
					SET_LOCATION($$, @1);
				}
			| offsetkey_clause
				{
					$$ = makeNode(LimitOffset);
					$$->limitCount = NULL;
					$$->limitOffset = NULL;
					$$->limitOffsetkey = $1;
					SET_LOCATION($$, @1);
				}
		;

opt_select_limit:
			select_limit						{ $$ = $1; }
			| /* EMPTY */						{ $$ = NULL; }
		;

limit_clause:
			LIMIT select_limit_value
				{ $$ = $2; }
			/* SQL:2008 syntax */
			| FETCH first_or_next opt_select_fetch_first_value row_or_rows ONLY
				{ $$ = $3; }
			| FETCH opt_select_fetch_first_value
				{ $$ = $2; }
		;

offset_clause:
			OFFSET select_offset_value
				{ $$ = $2; }
			/* SQL:2008 syntax */
			| OFFSET select_offset_value2 row_or_rows
				{ $$ = $2; }
		;

offsetkey_clause:
			OFFSETKEY select_offsetkey_value
				{ $$ = $2; }
		;

select_limit_value:
			a_expr									{ $$ = $1; }
			| ALL
				{
					/* LIMIT ALL is represented as a NULL constant */
					$$ = makeNullAConst(@1);
				}
		;

select_offset_value:
			a_expr									{ $$ = $1; }
		;

select_offsetkey_value:
			a_expr									{ $$ = $1; }
		;

/*
 * Allowing full expressions without parentheses causes various parsing
 * problems with the trailing ROW/ROWS key words.  SQL only calls for
 * constants, so we allow the rest only with parentheses.  If omitted,
 * default to 1.
 */
opt_select_fetch_first_value:
			SignedIconst						{ $$ = makeIntConst($1, @1); }
			| PARAM
				{
					ParamRef *p = makeNode(ParamRef);
					p->number = $1;
					SET_LOCATION(p, @1);
					$$ = (Node *) p;
				}
			| '?'
				{
					ParamRef *p = makeNode(ParamRef);
					p->number = -1;
					SET_LOCATION(p, @1);
					$$ = (Node *) p;
				}
			| '(' a_expr ')'					{ $$ = $2; }
			| /*EMPTY*/							{ $$ = makeIntConst(1, -1); }
		;

/*
 * Again, the trailing ROW/ROWS in this case prevent the full expression
 * syntax.  c_expr is the best we can do.
 */
select_offset_value2:
			c_expr									{ $$ = $1; }
		;

/* noise words */
row_or_rows: ROW									{ $$ = 0; }
			| ROWS									{ $$ = 0; }
		;

first_or_next: FIRST_P								{ $$ = 0; }
			| NEXT									{ $$ = 0; }
		;


group_clause:
			GROUP_P BY expr_list					{ $$ = $3; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

having_clause:
			HAVING a_expr							{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

for_locking_clause:
			for_locking_items						{ $$ = $1; }
			| FOR READ ONLY							{ $$ = NIL; }
		;

opt_for_locking_clause:
			for_locking_clause						{ $$ = $1; }
			| /* EMPTY */							{ $$ = NIL; }
		;

for_locking_items:
			for_locking_item						{ $$ = list_make1($1); }
			| for_locking_items for_locking_item	{ $$ = lappend($1, $2); }
		;

for_locking_item:
			FOR UPDATE locked_rels_list opt_nowait
				{
					LockingClause *n = makeNode(LockingClause);
					n->lockedRels = $3;
					n->forUpdate = TRUE;
					n->noWait = $4;
					$$ = (Node *) n;
				}
			| FOR SHARE locked_rels_list opt_nowait
				{
					LockingClause *n = makeNode(LockingClause);
					n->lockedRels = $3;
					n->forUpdate = FALSE;
					n->noWait = $4;
					$$ = (Node *) n;
				}
		;

locked_rels_list:
			OF qualified_name_list					{ $$ = $2; }
			| /* EMPTY */							{ $$ = NIL; }
		;


values_clause:
			VALUES ctext_row
				{
					SelectStmt *n = makeNode(SelectStmt);
					n->valuesLists = list_make1($2);
					$$ = (Node *) n;
				}
			| values_clause ',' ctext_row
				{
					SelectStmt *n = (SelectStmt *) $1;
					n->valuesLists = lappend(n->valuesLists, $3);
					$$ = (Node *) n;
				}
		;

/**************************************************************************
 * Starcounter extension to select statement
 *************************************************************************/
option_hint_clause:
			OPTION option_hint_lists				{ $$ = $2; }
		;

opt_option_hint_clause:
			option_hint_clause						{ $$ = $1; }
			| /* EMPTY */							{ $$ = NIL; }
		;

option_hint_lists:
			option_hint								{ $$ = list_make1($1); }
			| option_hint_lists ',' option_hint		{ $$ = lappend($1, $3); }
		;

option_hint:
			INDEX '(' qualified_name index_name ')'
				{
					HintIndex *n = (HintIndex *)makeNode(HintIndex);
					n->extentAlias = $3;
					n->indexName = $4;
					SET_LOCATION(n, @3);
					$$ = (Node *)n;
				}
//			| INDEX qualified_name index_name // Disqualified because join order does not work without parentheses
//				{
//					HintIndex *n = (HintIndex *)makeNode(HintIndex);
//					n->extentAlias = $2;
//					n->indexName = $3;
//					SET_LOCATION(n, @2);
//					$$ = (Node *)n;
//				}
			| JOIN ORDER '(' extent_alias_sequence ')'
				{
					HintJoinOrder *n = (HintJoinOrder *)makeNode(HintJoinOrder);
					n->joinOrder = $4;
					SET_LOCATION(n, @4);
					$$ = (Node *)n;
				}
//			| JOIN ORDER extent_alias_sequence
//				{
//					HintJoinOrder *n = (HintJoinOrder *)makeNode(HintJoinOrder);
//					n->joinOrder = $3;
//					SET_LOCATION(n, @3);
//					$$ = (Node *)n;
//				}
		;

extent_alias_sequence:
			qualified_name
				{
					$$ = list_make1($1);
				}
			| extent_alias_sequence ',' qualified_name
				{
					$$ = lappend($1, $3);
				}
		;

/*****************************************************************************
 *
 *	clauses common to all Optimizable Stmts:
 *		from_clause		- allow list of both JOIN expressions and table names
 *		where_clause	- qualifications for joins or restrictions
 *
 *****************************************************************************/

from_clause:
			FROM from_list							{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

from_list:
			table_ref								{ $$ = list_make1($1); }	%dprec 10
			| from_list ',' table_ref				{ $$ = lappend($1, $3); }	%dprec 1
		;

/*
 * table_ref is where an alias clause can be attached.	Note we cannot make
 * alias_clause have an empty production because that causes parse conflicts
 * between table_ref := '(' joined_table ')' alias_clause
 * and joined_table := '(' joined_table ')'.  So, we must have the
 * redundant-looking productions here instead.
 */
table_ref:	member_func_expr
				{
					/* default inheritance */
					RangeVar *r = (Node *) makeRangeVar($1, NULL, @1);
					r->inhOpt = INH_DEFAULT;
					$$ = (Node *) r;
				}
			| member_func_expr alias_clause
				{
					/* default inheritance */
					RangeVar *r = makeRangeVar($1, NULL, @1);
					r->inhOpt = INH_DEFAULT;
					r->alias = $2;
					$$ = (Node *) r;
				}
			| member_func_expr '*'
				{
					/* inheritance query */
					RangeVar *r = (Node *) makeRangeVar($1, NULL, @1);
					r->inhOpt = INH_YES;
					$$ = (Node *) r;
				}
			| member_func_expr '*' alias_clause 
				{
					/* inheritance query */
					RangeVar *r = makeRangeVar($1, NULL, @1);
					r->inhOpt = INH_YES;
					r->alias = $3;
					$$ = (Node *) r;
				}
			| ONLY member_func_expr
				{
					/* no inheritance */
					RangeVar *r = (Node *) makeRangeVar($2, NULL, @1);
					r->inhOpt = INH_NO;
					$$ = (Node *) r;
				}
			| ONLY member_func_expr alias_clause
				{
					/* no inheritance */
					RangeVar *r = makeRangeVar($2, NULL, @1);
					r->inhOpt = INH_NO;
					r->alias = $3;
					$$ = (Node *) r;
				}
			| ONLY '(' member_func_expr ')'
				{
					/* no inheritance, SQL99-style syntax */
					RangeVar *r = (Node *) makeRangeVar($3, NULL, @1);
					r->inhOpt = INH_NO;
					$$ = (Node *) r;
				}
			| ONLY '(' member_func_expr ')' alias_clause 
				{
					/* no inheritance, SQL99-style syntax */
					RangeVar *r = makeRangeVar($3, NULL, @1);
					r->inhOpt = INH_NO;
					r->alias = $5;
					$$ = (Node *) r;
				}
			| select_with_parens
				{
					/*
					 * The SQL spec does not permit a subselect
					 * (<derived_table>) without an alias clause,
					 * so we don't either.  This avoids the problem
					 * of needing to invent a unique refname for it.
					 * That could be surmounted if there's sufficient
					 * popular demand, but for now let's just implement
					 * the spec and see if anyone complains.
					 * However, it does seem like a good idea to emit
					 * an error message that's better than "syntax error".
					 */
					if (IsA($1, SelectStmt) &&
						((SelectStmt *) $1)->valuesLists)
					{
						errprint("\nERROR SYNTAX_ERROR: VALUES in FROM must have an alias\nFor example, FROM (VALUES ...) [AS] foo.");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $1, ScErrMessage("VALUES in FROM must have an alias\nFor example, FROM (VALUES ...) [AS] foo."));
					}
					else
					{
						errprint("\nERROR SYNTAX_ERROR: subquery in FROM must have an alias\nFor example, FROM (SELECT ...) [AS] foo.");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $1, ScErrMessage("VALUES in FROM must have an alias\nFor example, FROM (VALUES ...) [AS] foo."));
					}
					$$ = NULL;
				}
			| select_with_parens alias_clause
				{
					RangeSubselect *n = makeNode(RangeSubselect);
					n->subquery = $1;
					n->alias = $2;
					$$ = (Node *) n;
				}
			| joined_table
				{
					$$ = (Node *) $1;
				}
			| '(' joined_table ')' alias_clause
				{
					$2->alias = $4;
					$$ = (Node *) $2;
				}
		;


/*
 * It may seem silly to separate joined_table from table_ref, but there is
 * method in SQL92's madness: if you don't do it this way you get reduce-
 * reduce conflicts, because it's not clear to the parser generator whether
 * to expect alias_clause after ')' or not.  For the same reason we must
 * treat 'JOIN' and 'join_type JOIN' separately, rather than allowing
 * join_type to expand to empty; if we try it, the parser generator can't
 * figure out when to reduce an empty join_type right after table_ref.
 *
 * Note that a CROSS JOIN is the same as an unqualified
 * INNER JOIN, and an INNER JOIN/ON has the same shape
 * but a qualification expression to limit membership.
 * A NATURAL JOIN implicitly matches column names between
 * tables and the shape is determined by which columns are
 * in common. We'll collect columns during the later transformations.
 */

joined_table:
			'(' joined_table ')'
				{
					$$ = $2;
				}
			| table_ref CROSS JOIN table_ref
				{
					/* CROSS JOIN is same as unqualified inner join */
					JoinExpr *n = makeNode(JoinExpr);
					n->jointype = JOIN_INNER;
					n->isNatural = FALSE;
					n->larg = $1;
					n->rarg = $4;
					n->usingClause = NIL;
					n->quals = NULL;
					$$ = n;
				}
			| table_ref join_type JOIN table_ref join_qual
				{
					JoinExpr *n = makeNode(JoinExpr);
					n->jointype = $2;
					n->isNatural = FALSE;
					n->larg = $1;
					n->rarg = $4;
					if ($5 != NULL && IsA($5, List))
						n->usingClause = (List *) $5; /* USING clause */
					else
						n->quals = $5; /* ON clause */
					$$ = n;
				}
			| table_ref JOIN table_ref join_qual
				{
					/* letting join_type reduce to empty doesn't work */
					JoinExpr *n = makeNode(JoinExpr);
					n->jointype = JOIN_INNER;
					n->isNatural = FALSE;
					n->larg = $1;
					n->rarg = $3;
					if ($4 != NULL && IsA($4, List))
						n->usingClause = (List *) $4; /* USING clause */
					else
						n->quals = $4; /* ON clause */
					$$ = n;
				}
			| table_ref NATURAL join_type JOIN table_ref
				{
					JoinExpr *n = makeNode(JoinExpr);
					n->jointype = $3;
					n->isNatural = TRUE;
					n->larg = $1;
					n->rarg = $5;
					n->usingClause = NIL; /* figure out which columns later... */
					n->quals = NULL; /* fill later */
					$$ = n;
				}
			| table_ref NATURAL JOIN table_ref
				{
					/* letting join_type reduce to empty doesn't work */
					JoinExpr *n = makeNode(JoinExpr);
					n->jointype = JOIN_INNER;
					n->isNatural = TRUE;
					n->larg = $1;
					n->rarg = $4;
					n->usingClause = NIL; /* figure out which columns later... */
					n->quals = NULL; /* fill later */
					$$ = n;
				}
		;

alias_clause:
			AS ColLabel
				{
					$$ = makeNode(Alias);
					$$->aliasname = $2;
				}
			| ColId
				{
					$$ = makeNode(Alias);
					$$->aliasname = $1;
				}
		;

join_type:	FULL join_outer							{ $$ = JOIN_FULL; }
			| LEFT join_outer						{ $$ = JOIN_LEFT; }
			| RIGHT join_outer						{ $$ = JOIN_RIGHT; }
			| INNER_P								{ $$ = JOIN_INNER; }
		;

/* OUTER is just noise... */
join_outer: OUTER_P									{ $$ = NULL; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

/* JOIN qualification clauses
 * Possibilities are:
 *	USING ( column list ) allows only unqualified column names,
 *						  which must match between tables.
 *	ON expr allows more general qualifications.
 *
 * We return USING as a List node, while an ON-expr will not be a List.
 */

join_qual:	USING '(' name_list ')'					{ $$ = (Node *) $3; }
			| ON a_expr								{ $$ = $2; }
		;


relation_expr:
			qualified_name
				{
					/* default inheritance */
					$$ = $1;
					$$->inhOpt = INH_DEFAULT;
					$$->alias = NULL;
				}
			| qualified_name '*'
				{
					/* inheritance query */
					$$ = $1;
					$$->inhOpt = INH_YES;
					$$->alias = NULL;
				}
			| ONLY qualified_name
				{
					/* no inheritance */
					$$ = $2;
					$$->inhOpt = INH_NO;
					$$->alias = NULL;
				}
			| ONLY '(' qualified_name ')'
				{
					/* no inheritance, SQL99-style syntax */
					$$ = $3;
					$$->inhOpt = INH_NO;
					$$->alias = NULL;
				}
		;


relation_expr_list:
			relation_expr							{ $$ = list_make1($1); }
			| relation_expr_list ',' relation_expr	{ $$ = lappend($1, $3); }
		;


/*
 * Given "UPDATE foo set set ...", we have to decide without looking any
 * further ahead whether the first "set" is an alias or the UPDATE's SET
 * keyword.  Since "set" is allowed as a column name both interpretations
 * are feasible.  We resolve the shift/reduce conflict by giving the first
 * relation_expr_opt_alias production a higher precedence than the SET token
 * has, causing the parser to prefer to reduce, in effect assuming that the
 * SET is not an alias.
 */
relation_expr_opt_alias: relation_expr					%prec UMINUS
				{
					$$ = $1;
				}
			| relation_expr ColId
				{
					Alias *alias = makeNode(Alias);
					alias->aliasname = $2;
					$1->alias = alias;
					$$ = $1;
				}
			| relation_expr AS ColId
				{
					Alias *alias = makeNode(Alias);
					alias->aliasname = $3;
					$1->alias = alias;
					$$ = $1;
				}
		;


where_clause:
			WHERE a_expr							{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

/* variant for UPDATE and DELETE */
where_or_current_clause:
			WHERE a_expr							{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NULL; }
		;


/*****************************************************************************
 *
 *	Type syntax
 *		SQL92 introduces a large amount of type-specific syntax.
 *		Define individual clauses to handle these cases, and use
 *		 the generic case to handle regular type-extensible Postgres syntax.
 *		- thomas 1997-10-10
 *
 *****************************************************************************/

Typename:	SimpleTypename opt_array_bounds
				{
					$$ = $1;
					$$->arrayBounds = $2;
				}
			| SETOF SimpleTypename opt_array_bounds
				{
					$$ = $2;
					$$->arrayBounds = $3;
					$$->setof = TRUE;
				}
			/* SQL standard syntax, currently only one-dimensional */
			| SimpleTypename ARRAY '[' Iconst ']'
				{
					$$ = $1;
					$$->arrayBounds = list_make1(makeInteger($4));
				}
			| SETOF SimpleTypename ARRAY '[' Iconst ']'
				{
					$$ = $2;
					$$->arrayBounds = list_make1(makeInteger($5));
					$$->setof = TRUE;
				}
			| SimpleTypename ARRAY
				{
					$$ = $1;
					$$->arrayBounds = list_make1(makeInteger(-1));
				}
			| SETOF SimpleTypename ARRAY
				{
					$$ = $2;
					$$->arrayBounds = list_make1(makeInteger(-1));
					$$->setof = TRUE;
				}
		;

opt_array_bounds:
			opt_array_bounds '[' ']'
					{  $$ = lappend($1, makeInteger(-1)); }
			| opt_array_bounds '[' Iconst ']'
					{  $$ = lappend($1, makeInteger($3)); }
			| /*EMPTY*/
					{  $$ = NIL; }
		;

SimpleTypename:
			GenericType								{ $$ = $1; }
			| Numeric								{ $$ = $1; }
			| Bit									{ $$ = $1; }
			| Character								{ $$ = $1; }
			| ConstDatetime							{ $$ = $1; }
			| ConstInterval opt_interval
				{
					$$ = $1;
					$$->typmods = $2;
				}
			| ConstInterval '(' Iconst ')' opt_interval
				{
					$$ = $1;
					if ($5 != NIL)
					{
						if (list_length($5) != 1)
						{
							errprint("\nERROR SYNTAX_ERROR: interval precision specified twice");
							ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $5, ScErrMessage("interval precision specified twice"));
						}
						$$->typmods = lappend($5, makeIntConst($3, @3));
					}
					else
						$$->typmods = list_make2(makeIntConst(INTERVAL_FULL_RANGE, -1),
												 makeIntConst($3, @3));
				}
		;

/* We have a separate ConstTypename to allow defaulting fixed-length
 * types such as CHAR() and BIT() to an unspecified length.
 * SQL9x requires that these default to a length of one, but this
 * makes no sense for constructs like CHAR 'hi' and BIT '0101',
 * where there is an obvious better choice to make.
 * Note that ConstInterval is not included here since it must
 * be pushed up higher in the rules to accomodate the postfix
 * options (e.g. INTERVAL '1' YEAR). Likewise, we have to handle
 * the generic-type-name case in AExprConst to avoid premature
 * reduce/reduce conflicts against function names.
 */
ConstTypename:
			Numeric									{ $$ = $1; }
			| ConstBit								{ $$ = $1; }
			| ConstCharacter						{ $$ = $1; }
			| ConstDatetime							{ $$ = $1; }
		;

/*
 * GenericType covers all type names that don't have special syntax mandated
 * by the standard, including qualified names.  We also allow type modifiers.
 * To avoid parsing conflicts against function invocations, the modifiers
 * have to be shown as expr_list here, but parse analysis will only accept
 * constants for them.
 */
 // SC: type modifiers are removed
GenericType:
			type_function_name OptGenerics
				{
					$$ = makeTypeName($1);
					$$->generics = $2;
					SET_LOCATION($$, @1);
				}
			| type_function_name attrs OptGenerics
				{
					$$ = makeTypeNameFromNameList(lcons(makeString($1), $2));
					$$->generics = $3;
					SET_LOCATION($$, @1);
				}
		;

OptGenerics:
			'<' type_list '>'						{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
/*
 * SQL92 numeric data types
 */
Numeric:	INT_P
				{
					$$ = SystemTypeName("int4");
					SET_LOCATION($$, @1);
				}
			| INTEGER
				{
					$$ = SystemTypeName("int4");
					SET_LOCATION($$, @1);
				}
			| SMALLINT
				{
					$$ = SystemTypeName("int2");
					SET_LOCATION($$, @1);
				}
			| BIGINT
				{
					$$ = SystemTypeName("int8");
					SET_LOCATION($$, @1);
				}
			| REAL
				{
					$$ = SystemTypeName("float4");
					SET_LOCATION($$, @1);
				}
			| FLOAT_P opt_float
				{
					$$ = $2;
					SET_LOCATION($$, @1);
				}
			| DOUBLE_P PRECISION
				{
					$$ = SystemTypeName("float8");
					SET_LOCATION($$, @1);
				}
			| DECIMAL_P
				{
					$$ = SystemTypeName("numeric");
					SET_LOCATION($$, @1);
				}
			| DEC
				{
					$$ = SystemTypeName("numeric");
					SET_LOCATION($$, @1);
				}
			| NUMERIC
				{
					$$ = SystemTypeName("numeric");
					SET_LOCATION($$, @1);
				}
			| BOOLEAN_P
				{
					$$ = SystemTypeName("bool");
					SET_LOCATION($$, @1);
				}
		;

opt_float:	'(' Iconst ')'
				{
					/*
					 * Check FLOAT() precision limits assuming IEEE floating
					 * types - thomas 1997-09-18
					 */
					if ($2 < 1)
					{
						errprint("\nERROR INVALID_PARAMETER_VALUE: precision for type float must be at least 1 bit");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @2, $2, ScErrMessage("precision for type float must be at least 1 bit"));
					}
					else if ($2 <= 24)
						$$ = SystemTypeName("float4");
					else if ($2 <= 53)
						$$ = SystemTypeName("float8");
					else
					{
						errprint("\nERROR INVALID_PARAMETER_VALUE: precision for type float must be less than 54 bits");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @2, $2, ScErrMessage("precision for type float must be less than 54 bits"));
					}
				}
			| /*EMPTY*/
				{
					$$ = SystemTypeName("float8");
				}
		;

/*
 * SQL92 bit-field data types
 * The following implements BIT() and BIT VARYING().
 */
Bit:		BitWithLength
				{
					$$ = $1;
				}
			| BitWithoutLength
				{
					$$ = $1;
				}
		;

/* ConstBit is like Bit except "BIT" defaults to unspecified length */
/* See notes for ConstCharacter, which addresses same issue for "CHAR" */
ConstBit:	BitWithLength
				{
					$$ = $1;
				}
			| BitWithoutLength
				{
					$$ = $1;
					$$->typmods = NIL;
				}
		;

BitWithLength:
			BIT opt_varying '(' expr_list ')'
				{
					char *typname;

					typname = $2 ? "varbit" : "bit";
					$$ = SystemTypeName(typname);
					$$->typmods = $4;
					SET_LOCATION($$, @1);
				}
		;

BitWithoutLength:
			BIT opt_varying
				{
					/* bit defaults to bit(1), varbit to no limit */
					if ($2)
					{
						$$ = SystemTypeName("varbit");
					}
					else
					{
						$$ = SystemTypeName("bit");
						$$->typmods = list_make1(makeIntConst(1, -1));
					}
					SET_LOCATION($$, @1);
				}
		;


/*
 * SQL92 character data types
 * The following implements CHAR() and VARCHAR().
 */
Character:  CharacterWithLength
				{
					$$ = $1;
				}
			| CharacterWithoutLength
				{
					$$ = $1;
				}
		;

ConstCharacter:  CharacterWithLength
				{
					$$ = $1;
				}
			| CharacterWithoutLength
				{
					/* Length was not specified so allow to be unrestricted.
					 * This handles problems with fixed-length (bpchar) strings
					 * which in column definitions must default to a length
					 * of one, but should not be constrained if the length
					 * was not specified.
					 */
					$$ = $1;
					$$->typmods = NIL;
				}
		;

CharacterWithLength:  character '(' Iconst ')' opt_charset
				{
					if (($5 != NULL) && (strcmp($5, "sql_text") != 0))
					{
						char *type;

						type = palloc(strlen($1) + 1 + strlen($5) + 1);
						strcpy(type, $1);
						strcat(type, "_");
						strcat(type, $5);
						$$ = SystemTypeName(type);
					}
					else
						$$ = SystemTypeName($1);
					$$->typmods = list_make1(makeIntConst($3, @3));
					SET_LOCATION($$, @1);
				}
		;

CharacterWithoutLength:	 character opt_charset
				{
					if (($2 != NULL) && (strcmp($2, "sql_text") != 0))
					{
						char *type;

						type = palloc(strlen($1) + 1 + strlen($2) + 1);
						strcpy(type, $1);
						strcat(type, "_");
						strcat(type, $2);
						$$ = SystemTypeName(type);
					}
					else
						$$ = SystemTypeName($1);

					/* char defaults to char(1), varchar to no limit */
					if (strcmp($1, "bpchar") == 0)
						$$->typmods = list_make1(makeIntConst(1, -1));

					SET_LOCATION($$, @1);
				}
		;

character:	CHARACTER opt_varying
										{ $$ = $2 ? L"varchar": L"bpchar"; }
			| CHAR_P opt_varying
										{ $$ = $2 ? L"varchar": L"bpchar"; }
			| VARCHAR
										{ $$ = L"varchar"; }
			| NATIONAL CHARACTER opt_varying
										{ $$ = $3 ? L"varchar": L"bpchar"; }
			| NATIONAL CHAR_P opt_varying
										{ $$ = $3 ? L"varchar": L"bpchar"; }
			| NCHAR opt_varying
										{ $$ = $2 ? L"varchar": L"bpchar"; }
		;

opt_varying:
			VARYING									{ $$ = TRUE; }
			| /*EMPTY*/								{ $$ = FALSE; }
		;

opt_charset:
			CHARACTER SET ColId						{ $$ = $3; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

/*
 * SQL92 date/time types
 */
ConstDatetime:
			TIMESTAMP '(' Iconst ')' opt_timezone
				{
					if ($5)
						$$ = SystemTypeName("timestamptz");
					else
						$$ = SystemTypeName("timestamp");
					$$->typmods = list_make1(makeIntConst($3, @3));
					SET_LOCATION($$, @1);
				}
			| TIMESTAMP opt_timezone
				{
					if ($2)
						$$ = SystemTypeName("timestamptz");
					else
						$$ = SystemTypeName("timestamp");
					SET_LOCATION($$, @1);
				}
			| TIME '(' Iconst ')' opt_timezone
				{
					if ($5)
						$$ = SystemTypeName("timetz");
					else
						$$ = SystemTypeName("time");
					$$->typmods = list_make1(makeIntConst($3, @3));
					SET_LOCATION($$, @1);
				}
			| TIME opt_timezone
				{
					if ($2)
						$$ = SystemTypeName("timetz");
					else
						$$ = SystemTypeName("time");
					SET_LOCATION($$, @1);
				}
		;

ConstInterval:
			INTERVAL
				{
					$$ = SystemTypeName("interval");
					SET_LOCATION($$, @1);
				}
		;

opt_timezone:
			WITH_TIME ZONE							{ $$ = TRUE; }
			| WITHOUT TIME ZONE						{ $$ = FALSE; }
			| /*EMPTY*/								{ $$ = FALSE; }
		;

opt_interval:
			YEAR_P
				{ $$ = list_make1(makeIntConst(INTERVAL_MASK(YEAR), @1)); }
			| MONTH_P
				{ $$ = list_make1(makeIntConst(INTERVAL_MASK(MONTH), @1)); }
			| DAY_P
				{ $$ = list_make1(makeIntConst(INTERVAL_MASK(DAY), @1)); }
			| HOUR_P
				{ $$ = list_make1(makeIntConst(INTERVAL_MASK(HOUR), @1)); }
			| MINUTE_P
				{ $$ = list_make1(makeIntConst(INTERVAL_MASK(MINUTE), @1)); }
			| interval_second
				{ $$ = $1; }
			| YEAR_P TO MONTH_P
				{
					$$ = list_make1(makeIntConst(INTERVAL_MASK(YEAR) |
												 INTERVAL_MASK(MONTH), @1));
				}
			| DAY_P TO HOUR_P
				{
					$$ = list_make1(makeIntConst(INTERVAL_MASK(DAY) |
												 INTERVAL_MASK(HOUR), @1));
				}
			| DAY_P TO MINUTE_P
				{
					$$ = list_make1(makeIntConst(INTERVAL_MASK(DAY) |
												 INTERVAL_MASK(HOUR) |
												 INTERVAL_MASK(MINUTE), @1));
				}
			| DAY_P TO interval_second
				{
					$$ = $3;
					linitial($$) = makeIntConst(INTERVAL_MASK(DAY) |
												INTERVAL_MASK(HOUR) |
												INTERVAL_MASK(MINUTE) |
												INTERVAL_MASK(SECOND), @1);
				}
			| HOUR_P TO MINUTE_P
				{
					$$ = list_make1(makeIntConst(INTERVAL_MASK(HOUR) |
												 INTERVAL_MASK(MINUTE), @1));
				}
			| HOUR_P TO interval_second
				{
					$$ = $3;
					linitial($$) = makeIntConst(INTERVAL_MASK(HOUR) |
												INTERVAL_MASK(MINUTE) |
												INTERVAL_MASK(SECOND), @1);
				}
			| MINUTE_P TO interval_second
				{
					$$ = $3;
					linitial($$) = makeIntConst(INTERVAL_MASK(MINUTE) |
												INTERVAL_MASK(SECOND), @1);
				}
			| /*EMPTY*/
				{ $$ = NIL; }
		;

interval_second:
			SECOND_P
				{
					$$ = list_make1(makeIntConst(INTERVAL_MASK(SECOND), @1));
				}
			| SECOND_P '(' Iconst ')'
				{
					$$ = list_make2(makeIntConst(INTERVAL_MASK(SECOND), @1),
									makeIntConst($3, @3));
				}
		;


/*****************************************************************************
 *
 *	expression grammar
 *
 *****************************************************************************/

/*
 * General expressions
 * This is the heart of the expression syntax.
 *
 * We have two expression types: a_expr is the unrestricted kind, and
 * b_expr is a subset that must be used in some places to avoid shift/reduce
 * conflicts.  For example, we can't do BETWEEN as "BETWEEN a_expr AND a_expr"
 * because that use of AND conflicts with AND as a boolean operator.  So,
 * b_expr is used in BETWEEN and we remove boolean keywords from b_expr.
 *
 * Note that '(' a_expr ')' is a b_expr, so an unrestricted expression can
 * always be used by surrounding it with parens.
 *
 * c_expr is all the productions that are common to a_expr and b_expr;
 * it's factored out just to eliminate redundant coding.
 */
a_expr:		c_expr									{ $$ = $1; }
			| a_expr COLLATE any_name
				{
					CollateClause *n = makeNode(CollateClause);
					n->arg = $1;
					n->collname = $3;
					SET_LOCATION(n, @2);
					$$ = (Node *) n;
				}
			| a_expr AT TIME ZONE a_expr			%prec AT
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"timezone");
					n->args = list_make2($5, $1);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) n;
				}
		/*
		 * These operators must be called out explicitly in order to make use
		 * of bison's automatic operator-precedence handling.  All other
		 * operator names are handled by the generic productions using "Op",
		 * below; and all those operators will have the same precedence.
		 *
		 * If you add more explicitly-known operators, be sure to add them
		 * also to b_expr and to the MathOp list above.
		 */
			| '+' a_expr					%prec UMINUS
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "+", NULL, $2, @1); }
			| '-' a_expr					%prec UMINUS
				{ $$ = doNegate($2, @1); }
			| a_expr '+' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "+", $1, $3, @2); }
			| a_expr '-' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "-", $1, $3, @2); }
			| a_expr '*' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "*", $1, $3, @2); }
			| a_expr '/' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "/", $1, $3, @2); }
			| a_expr '%' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "%", $1, $3, @2); }
			| a_expr '^' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "^", $1, $3, @2); }
			| a_expr '<' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "<", $1, $3, @2); }
			| a_expr '>' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, ">", $1, $3, @2); }
			| a_expr '=' a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "=", $1, $3, @2); }

			| a_expr qual_Op a_expr				%prec Op
				{ $$ = (Node *) makeA_Expr(AEXPR_OP, $2, $1, $3, @2); }
			| qual_Op a_expr					%prec Op
				{ $$ = (Node *) makeA_Expr(AEXPR_OP, $1, NULL, $2, @1); }
//			| a_expr qual_Op					%prec POSTFIXOP
//				{ $$ = (Node *) makeA_Expr(AEXPR_OP, $2, $1, NULL, @2); }

			| a_expr AND a_expr
				{ $$ = (Node *) makeA_Expr(AEXPR_AND, NIL, $1, $3, @2); }
			| a_expr OR a_expr
				{ $$ = (Node *) makeA_Expr(AEXPR_OR, NIL, $1, $3, @2); }
			| NOT a_expr
				{ $$ = (Node *) makeA_Expr(AEXPR_NOT, NIL, NULL, $2, @1); }

			| a_expr LIKE a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "~~", $1, $3, @2); }
			| a_expr LIKE a_expr ESCAPE a_expr
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"like_escape");
					n->args = list_make2($3, $5);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "~~", $1, (Node *) n, @2);
				}
			| a_expr NOT LIKE a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "!~~", $1, $4, @2); }
			| a_expr NOT LIKE a_expr ESCAPE a_expr
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"like_escape");
					n->args = list_make2($4, $6);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "!~~", $1, (Node *) n, @2);
				}
			| a_expr STARTS WITH a_expr				%prec STARTS
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "STARTS_WITH", $1, $4, @2); }
			| a_expr ILIKE a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "~~*", $1, $3, @2); }
			| a_expr ILIKE a_expr ESCAPE a_expr
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"like_escape");
					n->args = list_make2($3, $5);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "~~*", $1, (Node *) n, @2);
				}
			| a_expr NOT ILIKE a_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "!~~*", $1, $4, @2); }
			| a_expr NOT ILIKE a_expr ESCAPE a_expr
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"like_escape");
					n->args = list_make2($4, $6);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "!~~*", $1, (Node *) n, @2);
				}

			| a_expr SIMILAR TO a_expr				%prec SIMILAR
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"similar_escape");
					n->args = list_make2($4, makeNullAConst(-1));
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "~", $1, (Node *) n, @2);
				}
			| a_expr SIMILAR TO a_expr ESCAPE a_expr
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"similar_escape");
					n->args = list_make2($4, $6);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "~", $1, (Node *) n, @2);
				}
			| a_expr NOT SIMILAR TO a_expr			%prec SIMILAR
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"similar_escape");
					n->args = list_make2($5, makeNullAConst(-1));
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "!~", $1, (Node *) n, @2);
				}
			| a_expr NOT SIMILAR TO a_expr ESCAPE a_expr
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"similar_escape");
					n->args = list_make2($5, $7);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @2);
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "!~", $1, (Node *) n, @2);
				}

			/* NullTest clause
			 * Define SQL92-style Null test clause.
			 * Allow two forms described in the standard:
			 *	a IS NULL
			 *	a IS NOT NULL
			 * Allow two SQL extensions
			 *	a ISNULL
			 *	a NOTNULL
			 */
			| a_expr IS NULL_P							%prec IS
				{
					NullTest *n = makeNode(NullTest);
					n->arg = (Expr *) $1;
					n->nulltesttype = IS_NULL;
					$$ = (Node *)n;
				}
			| a_expr ISNULL
				{
					NullTest *n = makeNode(NullTest);
					n->arg = (Expr *) $1;
					n->nulltesttype = IS_NULL;
					$$ = (Node *)n;
				}
			| a_expr IS NOT NULL_P						%prec IS
				{
					NullTest *n = makeNode(NullTest);
					n->arg = (Expr *) $1;
					n->nulltesttype = IS_NOT_NULL;
					$$ = (Node *)n;
				}
			| a_expr NOTNULL
				{
					NullTest *n = makeNode(NullTest);
					n->arg = (Expr *) $1;
					n->nulltesttype = IS_NOT_NULL;
					$$ = (Node *)n;
				}
			| row OVERLAPS row
				{
					Node *n = (Node *)makeOverlaps($1, $3, @2, yyscanner);
					if (n == NULL)
						YYERROR; // Sc: Not reached
					$$ = n;
				}
			| a_expr IS TRUE_P							%prec IS
				{
					BooleanTest *b = makeNode(BooleanTest);
					b->arg = (Expr *) $1;
					b->booltesttype = IS_TRUE;
					$$ = (Node *)b;
				}
			| a_expr IS NOT TRUE_P						%prec IS
				{
					BooleanTest *b = makeNode(BooleanTest);
					b->arg = (Expr *) $1;
					b->booltesttype = IS_NOT_TRUE;
					$$ = (Node *)b;
				}
			| a_expr IS FALSE_P							%prec IS
				{
					BooleanTest *b = makeNode(BooleanTest);
					b->arg = (Expr *) $1;
					b->booltesttype = IS_FALSE;
					$$ = (Node *)b;
				}
			| a_expr IS NOT FALSE_P						%prec IS
				{
					BooleanTest *b = makeNode(BooleanTest);
					b->arg = (Expr *) $1;
					b->booltesttype = IS_NOT_FALSE;
					$$ = (Node *)b;
				}
			| a_expr IS DISTINCT FROM a_expr			%prec IS
				{
					$$ = (Node *) makeSimpleA_Expr(AEXPR_DISTINCT, "=", $1, $5, @2);
				}
			| a_expr IS NOT DISTINCT FROM a_expr		%prec IS
				{
					$$ = (Node *) makeA_Expr(AEXPR_NOT, NIL, NULL,
									(Node *) makeSimpleA_Expr(AEXPR_DISTINCT,
															  "=", $1, $6, @2),
											 @2);

				}
			| a_expr IS OF '(' type_list ')'			%prec IS
				{
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OF, "=", $1, (Node *) $5, @2);
				}
			| a_expr IS NOT OF '(' type_list ')'		%prec IS
				{
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OF, "<>", $1, (Node *) $6, @2);
				}
			/*
			 *	Ideally we would not use hard-wired operators below but
			 *	instead use opclasses.  However, mixed data types and other
			 *	issues make this difficult:
			 *	http://archives.postgresql.org/pgsql-hackers/2008-08/msg01142.php
			 */
			| a_expr BETWEEN opt_asymmetric b_expr AND b_expr		%prec BETWEEN
				{
					$$ = (Node *) makeA_Expr(AEXPR_AND, NIL,
						(Node *) makeSimpleA_Expr(AEXPR_OP, ">=", $1, $4, @2),
						(Node *) makeSimpleA_Expr(AEXPR_OP, "<=", $1, $6, @2),
											 @2);
				}
			| a_expr NOT BETWEEN opt_asymmetric b_expr AND b_expr	%prec BETWEEN
				{
					$$ = (Node *) makeA_Expr(AEXPR_OR, NIL,
						(Node *) makeSimpleA_Expr(AEXPR_OP, "<", $1, $5, @2),
						(Node *) makeSimpleA_Expr(AEXPR_OP, ">", $1, $7, @2),
											 @2);
				}
			| a_expr BETWEEN SYMMETRIC b_expr AND b_expr			%prec BETWEEN
				{
					$$ = (Node *) makeA_Expr(AEXPR_OR, NIL,
						(Node *) makeA_Expr(AEXPR_AND, NIL,
						    (Node *) makeSimpleA_Expr(AEXPR_OP, ">=", $1, $4, @2),
						    (Node *) makeSimpleA_Expr(AEXPR_OP, "<=", $1, $6, @2),
											@2),
						(Node *) makeA_Expr(AEXPR_AND, NIL,
						    (Node *) makeSimpleA_Expr(AEXPR_OP, ">=", $1, $6, @2),
						    (Node *) makeSimpleA_Expr(AEXPR_OP, "<=", $1, $4, @2),
											@2),
											 @2);
				}
			| a_expr NOT BETWEEN SYMMETRIC b_expr AND b_expr		%prec BETWEEN
				{
					$$ = (Node *) makeA_Expr(AEXPR_AND, NIL,
						(Node *) makeA_Expr(AEXPR_OR, NIL,
						    (Node *) makeSimpleA_Expr(AEXPR_OP, "<", $1, $5, @2),
						    (Node *) makeSimpleA_Expr(AEXPR_OP, ">", $1, $7, @2),
											@2),
						(Node *) makeA_Expr(AEXPR_OR, NIL,
						    (Node *) makeSimpleA_Expr(AEXPR_OP, "<", $1, $7, @2),
						    (Node *) makeSimpleA_Expr(AEXPR_OP, ">", $1, $5, @2),
											@2),
											 @2);
				}
			| a_expr IN_P in_expr
				{
					/* in_expr returns a SubLink or a list of a_exprs */
					if (IsA($3, SubLink))
					{
						/* generate foo = ANY (subquery) */
						SubLink *n = (SubLink *) $3;
						n->subLinkType = ANY_SUBLINK;
						n->testexpr = $1;
						n->operName = list_make1(makeString("="));
						SET_LOCATION(n, @2);
						$$ = (Node *)n;
					}
					else
					{
						/* generate scalar IN expression */
						$$ = (Node *) makeSimpleA_Expr(AEXPR_IN, "=", $1, $3, @2);
					}
				}
			| a_expr NOT IN_P in_expr
				{
					/* in_expr returns a SubLink or a list of a_exprs */
					if (IsA($4, SubLink))
					{
						/* generate NOT (foo = ANY (subquery)) */
						/* Make an = ANY node */
						SubLink *n = (SubLink *) $4;
						n->subLinkType = ANY_SUBLINK;
						n->testexpr = $1;
						n->operName = list_make1(makeString("="));
						SET_LOCATION(n, @3);
						/* Stick a NOT on top */
						$$ = (Node *) makeA_Expr(AEXPR_NOT, NIL, NULL, (Node *) n, @2);
					}
					else
					{
						/* generate scalar NOT IN expression */
						$$ = (Node *) makeSimpleA_Expr(AEXPR_IN, "<>", $1, $4, @2);
					}
				}
			| a_expr subquery_Op sub_type select_with_parens	%prec Op
				{
					SubLink *n = makeNode(SubLink);
					n->subLinkType = $3;
					n->testexpr = $1;
					n->operName = $2;
					n->subselect = $4;
					SET_LOCATION(n, @2);
					$$ = (Node *)n;
				}
			| a_expr subquery_Op sub_type '(' a_expr ')'		%prec Op
				{
					if ($3 == ANY_SUBLINK)
						$$ = (Node *) makeA_Expr(AEXPR_OP_ANY, $2, $1, $5, @2);
					else
						$$ = (Node *) makeA_Expr(AEXPR_OP_ALL, $2, $1, $5, @2);
				}
			| UNIQUE select_with_parens
				{
					/* Not sure how to get rid of the parentheses
					 * but there are lots of shift/reduce errors without them.
					 *
					 * Should be able to implement this by plopping the entire
					 * select into a node, then transforming the target expressions
					 * from whatever they are into count(*), and testing the
					 * entire result equal to one.
					 * But, will probably implement a separate node in the executor.
					 */
					errprint("\nERROR FEATURE_NOT_SUPPORTED: UNIQUE predicate is not yet implemented");
					ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @2, $2, ScErrMessage("UNIQUE predicate is not supported"));
				}
		;

/*
 * Restricted expressions
 *
 * b_expr is a subset of the complete expression syntax defined by a_expr.
 *
 * Presently, AND, NOT, IS, and IN are the a_expr keywords that would
 * cause trouble in the places where b_expr is used.  For simplicity, we
 * just eliminate all the boolean-keyword-operator productions from b_expr.
 */
b_expr:		c_expr
				{ $$ = $1; }
			| '+' b_expr					%prec UMINUS
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "+", NULL, $2, @1); }
			| '-' b_expr					%prec UMINUS
				{ $$ = doNegate($2, @1); }
			| b_expr '+' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "+", $1, $3, @2); }
			| b_expr '-' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "-", $1, $3, @2); }
			| b_expr '*' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "*", $1, $3, @2); }
			| b_expr '/' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "/", $1, $3, @2); }
			| b_expr '%' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "%", $1, $3, @2); }
			| b_expr '^' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "^", $1, $3, @2); }
			| b_expr '<' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "<", $1, $3, @2); }
			| b_expr '>' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, ">", $1, $3, @2); }
			| b_expr '=' b_expr
				{ $$ = (Node *) makeSimpleA_Expr(AEXPR_OP, "=", $1, $3, @2); }
			| b_expr qual_Op b_expr				%prec Op
				{ $$ = (Node *) makeA_Expr(AEXPR_OP, $2, $1, $3, @2); }
			| qual_Op b_expr					%prec Op
				{ $$ = (Node *) makeA_Expr(AEXPR_OP, $1, NULL, $2, @1); }
//			| b_expr qual_Op					%prec POSTFIXOP
//				{ $$ = (Node *) makeA_Expr(AEXPR_OP, $2, $1, NULL, @2); }
			| b_expr IS DISTINCT FROM b_expr		%prec IS
				{
					$$ = (Node *) makeSimpleA_Expr(AEXPR_DISTINCT, "=", $1, $5, @2);
				}
			| b_expr IS NOT DISTINCT FROM b_expr	%prec IS
				{
					$$ = (Node *) makeA_Expr(AEXPR_NOT, NIL,
						NULL, (Node *) makeSimpleA_Expr(AEXPR_DISTINCT, "=", $1, $6, @2), @2);
				}
			| b_expr IS OF '(' type_list ')'		%prec IS
				{
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OF, "=", $1, (Node *) $5, @2);
				}
			| b_expr IS NOT OF '(' type_list ')'	%prec IS
				{
					$$ = (Node *) makeSimpleA_Expr(AEXPR_OF, "<>", $1, (Node *) $6, @2);
				}
		;

/*
 * Productions that can be used in both a_expr and b_expr.
 *
 * Note: productions that refer recursively to a_expr or b_expr mostly
 * cannot appear here.	However, it's OK to refer to a_exprs that occur
 * inside parentheses, such as function arguments; that cannot introduce
 * ambiguity to the b_expr syntax.
 */
c_expr:		member_func_expr						{ $$ = (Node *) $1; }
			| AexprConst							{ $$ = $1; }
			| PARAM
				{
					ParamRef *p = makeNode(ParamRef);
					p->number = $1;
					SET_LOCATION(p, @1);
						$$ = (Node *) p;
				}
			| '?'
				{
					ParamRef *p = makeNode(ParamRef);
					p->number = -1;
					SET_LOCATION(p, @1);
						$$ = (Node *) p;
				}
			| PARAM member_access_seq
				{
					ParamRef *p = makeNode(ParamRef);
					p->number = $1;
					SET_LOCATION(p, @1);
					if ($2)
					{
						A_Indirection *n = makeNode(A_Indirection);
						n->arg = (Node *) p;
						n->indirection = check_indirection($2, yyscanner);
						$$ = (Node *) n;
					}
					else
						$$ = (Node *) p;
				}
			| '?' member_access_seq
				{
					ParamRef *p = makeNode(ParamRef);
					p->number = -1;
					SET_LOCATION(p, @1);
					if ($2)
					{
						A_Indirection *n = makeNode(A_Indirection);
						n->arg = (Node *) p;
						n->indirection = check_indirection($2, yyscanner);
						$$ = (Node *) n;
					}
					else
						$$ = (Node *) p;
				}
			| '(' a_expr ')'
				{
						$$ = $2;
				}
			| '(' a_expr ')' member_access_seq
				{
						A_Indirection *n = makeNode(A_Indirection);
						n->arg = $2;
						n->indirection = check_indirection($4, yyscanner);
						$$ = (Node *)n;
				}
			| case_expr
				{ $$ = $1; }
			| select_with_parens			%prec UMINUS
				{
					SubLink *n = makeNode(SubLink);
					n->subLinkType = EXPR_SUBLINK;
					n->testexpr = NULL;
					n->operName = NIL;
					n->subselect = $1;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| EXISTS select_with_parens
				{
					SubLink *n = makeNode(SubLink);
					n->subLinkType = EXISTS_SUBLINK;
					n->testexpr = NULL;
					n->operName = NIL;
					n->subselect = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| ARRAY select_with_parens
				{
					SubLink *n = makeNode(SubLink);
					n->subLinkType = ARRAY_SUBLINK;
					n->testexpr = NULL;
					n->operName = NIL;
					n->subselect = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| ARRAY array_expr
				{
					A_ArrayExpr *n = (A_ArrayExpr *) $2;
					Assert(IsA(n, A_ArrayExpr));
					/* point outermost A_ArrayExpr to the ARRAY keyword */
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| row
				{
					RowExpr *r = makeNode(RowExpr);
					r->args = $1;
					r->row_typeid = InvalidOid;	/* not analyzed yet */
					SET_LOCATION(r, @1);
					$$ = (Node *)r;
				}
		;

/*
 * This expression specifies calls to standard functions,
 * object accesses, object member accesses, static member access
 * and possible path expressions.
 */
member_func_expr:
			member_access_el						{ $$ = list_make1($1); }
			| member_access_el member_access_seq	
				{
					$$ = lcons($1, $2);
				}
			| standard_func_call					{ $$ = list_make1($1); }
			| standard_func_call member_access_seq	
				{
					$$ = lcons($1, $2);
				}
		;

/*
 * Path expression, member access and array indexes.
 */
member_access_seq:
			'.' member_access_el					{ $$ = list_make1($2); }
			| member_access_indices					{ $$ = list_make1($1); }
			| '.' '*'
				{
					$$ = list_make1((Node *) makeNode(A_Star));
				}
			| member_access_seq '.' member_access_el
				{
					$$ = lappend($1, $3);
				}
			| member_access_seq '.' '*'
				{
					$$ = lappend($1, (Node *) makeNode(A_Star));
				}
			| member_access_seq member_access_indices
				{
					$$ = lappend($1, $2);
				}
		;

/*
 * Member accesses similar to C#
 */
member_access_el:
			type_function_name
				{
					ColumnRef *n = makeNode(ColumnRef);
					n->name = $1;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
					//$$ = makeColumnRef($1, NIL, @1, yyscanner);
				}
			| type_function_name '<' type_list '>'
				{
					TypeName *n = makeTypeName($1);
					n->generics = $3;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' ')'  over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = NIL;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $4;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '<' type_list '>' '(' ')'  over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = NIL;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $7;
					n->generics = $3;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| '<' type_list '>' type_function_name '(' ')'  over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $4;
					n->args = NIL;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $7;
					n->generics = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' func_arg_list ')'  over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $3;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $5;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '<' type_list '>' '(' func_arg_list ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $6;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $8;
					n->generics = $3;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| '<' type_list '>' type_function_name '(' func_arg_list ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $4;
					n->args = $6;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $8;
					n->generics = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' VARIADIC func_arg_expr ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = list_make1($4);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = TRUE;
					n->over = $6;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' func_arg_list ',' VARIADIC func_arg_expr ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = lappend($3, $6);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = TRUE;
					n->over = $8;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' func_arg_list sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $3;
					n->agg_order = $4;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $6;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '<' type_list '>' '(' func_arg_list sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $6;
					n->agg_order = $7;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $9;
					n->generics = $3;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| '<' type_list '>' type_function_name '(' func_arg_list sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $4;
					n->args = $6;
					n->agg_order = $7;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $9;
					n->generics = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' ALL func_arg_list opt_sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $4;
					n->agg_order = $5;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					/* Ideally we'd mark the FuncCall node to indicate
					 * "must be an aggregate", but there's no provision
					 * for that in FuncCall at the moment.
					 */
					n->func_variadic = FALSE;
					n->over = $7;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '<' type_list '>' '(' ALL func_arg_list opt_sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $7;
					n->agg_order = $8;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					/* Ideally we'd mark the FuncCall node to indicate
					 * "must be an aggregate", but there's no provision
					 * for that in FuncCall at the moment.
					 */
					n->func_variadic = FALSE;
					n->over = $10;
					n->generics = $3;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| '<' type_list '>' type_function_name '(' ALL func_arg_list opt_sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $4;
					n->args = $7;
					n->agg_order = $8;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					/* Ideally we'd mark the FuncCall node to indicate
					 * "must be an aggregate", but there's no provision
					 * for that in FuncCall at the moment.
					 */
					n->func_variadic = FALSE;
					n->over = $10;
					n->generics = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' DISTINCT func_arg_list opt_sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $4;
					n->agg_order = $5;
					n->agg_star = FALSE;
					n->agg_distinct = TRUE;
					n->func_variadic = FALSE;
					n->over = $7;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '<' type_list '>' '(' DISTINCT func_arg_list opt_sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = $7;
					n->agg_order = $8;
					n->agg_star = FALSE;
					n->agg_distinct = TRUE;
					n->func_variadic = FALSE;
					n->over = $10;
					n->generics = $3;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| '<' type_list '>' type_function_name '(' DISTINCT func_arg_list opt_sort_clause ')' over_clause
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $4;
					n->args = $7;
					n->agg_order = $8;
					n->agg_star = FALSE;
					n->agg_distinct = TRUE;
					n->func_variadic = FALSE;
					n->over = $10;
					n->generics = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '(' '*' ')' over_clause
				{
					/*
					 * We consider AGGREGATE(*) to invoke a parameterless
					 * aggregate.  This does the right thing for COUNT(*),
					 * and there are no other aggregates in SQL92 that accept
					 * '*' as parameter.
					 *
					 * The FuncCall node is also marked agg_star = true,
					 * so that later processing can detect what the argument
					 * really was.
					 */
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = NIL;
					n->agg_order = NIL;
					n->agg_star = TRUE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $5;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| type_function_name '<' type_list '>' '(' '*' ')' over_clause
				{
					/*
					 * We consider AGGREGATE(*) to invoke a parameterless
					 * aggregate.  This does the right thing for COUNT(*),
					 * and there are no other aggregates in SQL92 that accept
					 * '*' as parameter.
					 *
					 * The FuncCall node is also marked agg_star = true,
					 * so that later processing can detect what the argument
					 * really was.
					 */
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $1;
					n->args = NIL;
					n->agg_order = NIL;
					n->agg_star = TRUE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $8;
					n->generics = $3;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| '<' type_list '>' type_function_name '(' '*' ')' over_clause
				{
					/*
					 * We consider AGGREGATE(*) to invoke a parameterless
					 * aggregate.  This does the right thing for COUNT(*),
					 * and there are no other aggregates in SQL92 that accept
					 * '*' as parameter.
					 *
					 * The FuncCall node is also marked agg_star = true,
					 * so that later processing can detect what the argument
					 * really was.
					 */
					FuncCall *n = makeNode(FuncCall);
					n->funcname = $4;
					n->args = NIL;
					n->agg_order = NIL;
					n->agg_star = TRUE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = $8;
					n->generics = $2;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
		;

member_access_indices:
			'[' a_expr ']'
				{
					A_Indices *ai = makeNode(A_Indices);
					ai->lidx = NULL;
					ai->uidx = $2;
					$$ = (Node *) ai;
				}
			| '[' a_expr ':' a_expr ']'
				{
					A_Indices *ai = makeNode(A_Indices);
					ai->lidx = $2;
					ai->uidx = $4;
					$$ = (Node *) ai;
				}
		;

standard_func_call:
			CAST '(' a_expr AS Typename ')'
				{ $$ = makeTypeCast($3, $5, @1); }
			| TYPEOF '(' Typename ')'
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"typeof");
					n->args = $3;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| EXTRACT '(' extract_list ')'
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"date_part");
					n->args = $3;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| OVERLAY '(' overlay_list ')'
				{
					/* overlay(A PLACING B FROM C FOR D) is converted to
					 * overlay(A, B, C, D)
					 * overlay(A PLACING B FROM C) is converted to
					 * overlay(A, B, C)
					 */
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"overlay");
					n->args = $3;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| POSITION '(' position_list ')'
				{
					/* position(A in B) is converted to position(B, A) */
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"position");
					n->args = $3;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| SUBSTRING '(' substr_list ')'
				{
					/* substring(A from B for C) is converted to
					 * substring(A, B, C) - thomas 2000-11-28
					 */
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"substring");
					n->args = $3;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| TREAT '(' a_expr AS Typename ')'
				{
					/* TREAT(expr AS target) converts expr of a particular type to target,
					 * which is defined to be a subtype of the original expression.
					 * In SQL99, this is intended for use with structured UDTs,
					 * but let's make this a generally useful form allowing stronger
					 * coercions than are handled by implicit casting.
					 */
					FuncCall *n = makeNode(FuncCall);
					/* Convert SystemTypeName() to SystemFuncName() even though
					 * at the moment they result in the same thing.
					 */
					n->funcname = SystemFuncName(((Value *)llast($5->names))->val.str);
					n->args = list_make1($3);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| TRIM '(' BOTH trim_list ')'
				{
					/* various trim expressions are defined in SQL92
					 * - thomas 1997-07-19
					 */
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"btrim");
					n->args = $4;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| TRIM '(' LEADING trim_list ')'
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"ltrim");
					n->args = $4;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| TRIM '(' TRAILING trim_list ')'
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"rtrim");
					n->args = $4;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| TRIM '(' trim_list ')'
				{
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"btrim");
					n->args = $3;
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
/*			| NULLIF '(' a_expr ',' a_expr ')'
				{
					$$ = (Node *) makeSimpleA_Expr(AEXPR_NULLIF, "=", $3, $5, @1);
				}
			| COALESCE '(' expr_list ')'
				{
					CoalesceExpr *c = makeNode(CoalesceExpr);
					c->args = $3;
					c->location = @1;
					$$ = (Node *)c;
				}
			| GREATEST '(' expr_list ')'
				{
					MinMaxExpr *v = makeNode(MinMaxExpr);
					v->args = $3;
					v->op = IS_GREATEST;
					v->location = @1;
					$$ = (Node *)v;
				}
			| LEAST '(' expr_list ')'
				{
					MinMaxExpr *v = makeNode(MinMaxExpr);
					v->args = $3;
					v->op = IS_LEAST;
					v->location = @1;
					$$ = (Node *)v;
				}*/
			| XMLCONCAT '(' expr_list ')'
				{
					$$ = makeXmlExpr(IS_XMLCONCAT, NULL, NIL, $3, @1);
				}
			| XMLELEMENT '(' NAME_P ColLabel ')'
				{
					$$ = makeXmlExpr(IS_XMLELEMENT, $4, NIL, NIL, @1);
				}
			| XMLELEMENT '(' NAME_P ColLabel ',' xml_attributes ')'
				{
					$$ = makeXmlExpr(IS_XMLELEMENT, $4, $6, NIL, @1);
				}
			| XMLELEMENT '(' NAME_P ColLabel ',' expr_list ')'
				{
					$$ = makeXmlExpr(IS_XMLELEMENT, $4, NIL, $6, @1);
				}
			| XMLELEMENT '(' NAME_P ColLabel ',' xml_attributes ',' expr_list ')'
				{
					$$ = makeXmlExpr(IS_XMLELEMENT, $4, $6, $8, @1);
				}
			| XMLEXISTS '(' c_expr xmlexists_argument ')'
				{
					/* xmlexists(A PASSING [BY REF] B [BY REF]) is
					 * converted to xmlexists(A, B)*/
					FuncCall *n = makeNode(FuncCall);
					n->funcname = SystemFuncName(L"xmlexists");
					n->args = list_make2($3, $4);
					n->agg_order = NIL;
					n->agg_star = FALSE;
					n->agg_distinct = FALSE;
					n->func_variadic = FALSE;
					n->over = NULL;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
			| XMLFOREST '(' xml_attribute_list ')'
				{
					$$ = makeXmlExpr(IS_XMLFOREST, NULL, $3, NIL, @1);
				}
			| XMLPARSE '(' document_or_content a_expr xml_whitespace_option ')'
				{
					XmlExpr *x = (XmlExpr *)
						makeXmlExpr(IS_XMLPARSE, NULL, NIL,
									list_make2($4, makeBoolAConst($5, -1)),
									@1);
					x->xmloption = $3;
					$$ = (Node *)x;
				}
			| XMLPI '(' NAME_P ColLabel ')'
				{
					$$ = makeXmlExpr(IS_XMLPI, $4, NULL, NIL, @1);
				}
			| XMLPI '(' NAME_P ColLabel ',' a_expr ')'
				{
					$$ = makeXmlExpr(IS_XMLPI, $4, NULL, list_make1($6), @1);
				}
			| XMLROOT '(' a_expr ',' xml_root_version opt_xml_root_standalone ')'
				{
					$$ = makeXmlExpr(IS_XMLROOT, NULL, NIL,
									 list_make3($3, $5, $6), @1);
				}
			| XMLSERIALIZE '(' document_or_content a_expr AS SimpleTypename ')'
				{
					XmlSerialize *n = makeNode(XmlSerialize);
					n->xmloption = $3;
					n->expr = $4;
					n->typeName = $6;
					SET_LOCATION(n, @1);
					$$ = (Node *)n;
				}
		;	

/*
 * SQL/XML support
 */
xml_root_version: VERSION_P a_expr
				{ $$ = $2; }
			| VERSION_P NO VALUE_P
				{ $$ = makeNullAConst(-1); }
		;

opt_xml_root_standalone: ',' STANDALONE_P YES_P
				{ $$ = makeIntConst(XML_STANDALONE_YES, -1); }
			| ',' STANDALONE_P NO
				{ $$ = makeIntConst(XML_STANDALONE_NO, -1); }
			| ',' STANDALONE_P NO VALUE_P
				{ $$ = makeIntConst(XML_STANDALONE_NO_VALUE, -1); }
			| /*EMPTY*/
				{ $$ = makeIntConst(XML_STANDALONE_OMITTED, -1); }
		;

xml_attributes: XMLATTRIBUTES '(' xml_attribute_list ')'	{ $$ = $3; }
		;

xml_attribute_list:	xml_attribute_el					{ $$ = list_make1($1); }	%dprec 10
			| xml_attribute_list ',' xml_attribute_el	{ $$ = lappend($1, $3); }	%dprec 1
		;

xml_attribute_el: a_expr AS ColLabel
				{
					$$ = makeNode(ResTarget);
					$$->name = $3;
					$$->indirection = NIL;
					$$->val = (Node *) $1;
					SET_LOCATION($$, @1);
				}
			| a_expr
				{
					$$ = makeNode(ResTarget);
					$$->name = NULL;
					$$->indirection = NIL;
					$$->val = (Node *) $1;
					SET_LOCATION($$, @1);
				}
		;

document_or_content: DOCUMENT_P						{ $$ = XMLOPTION_DOCUMENT; }
			| CONTENT_P								{ $$ = XMLOPTION_CONTENT; }
		;

xml_whitespace_option: PRESERVE WHITESPACE_P		{ $$ = TRUE; }
			| STRIP_P WHITESPACE_P					{ $$ = FALSE; }
			| /*EMPTY*/								{ $$ = FALSE; }
		;

/* We allow several variants for SQL and other compatibility. */
xmlexists_argument:
			PASSING c_expr
				{
					$$ = $2;
				}
			| PASSING c_expr BY REF
				{
					$$ = $2;
				}
			| PASSING BY REF c_expr
				{
					$$ = $4;
				}
			| PASSING BY REF c_expr BY REF
				{
					$$ = $4;
				}
		;


/*
 * Window Definitions
 */
window_clause:
			WINDOW window_definition_list			{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

window_definition_list:
			window_definition						{ $$ = list_make1($1); }
			| window_definition_list ',' window_definition
													{ $$ = lappend($1, $3); }
		;

window_definition:
			ColId AS window_specification
				{
					WindowDef *n = $3;
					n->name = $1;
					$$ = n;
				}
		;

over_clause: OVER window_specification
				{ $$ = $2; }
			| OVER ColId
				{
					WindowDef *n = makeNode(WindowDef);
					n->name = $2;
					n->refname = NULL;
					n->partitionClause = NIL;
					n->orderClause = NIL;
					n->frameOptions = FRAMEOPTION_DEFAULTS;
					n->startOffset = NULL;
					n->endOffset = NULL;
					SET_LOCATION(n, @2);
					$$ = n;
				}
			| /*EMPTY*/
				{ $$ = NULL; }
		;

window_specification: '(' opt_existing_window_name opt_partition_clause
						opt_sort_clause opt_frame_clause ')'
				{
					WindowDef *n = makeNode(WindowDef);
					n->name = NULL;
					n->refname = $2;
					n->partitionClause = $3;
					n->orderClause = $4;
					/* copy relevant fields of opt_frame_clause */
					n->frameOptions = $5->frameOptions;
					n->startOffset = $5->startOffset;
					n->endOffset = $5->endOffset;
					SET_LOCATION(n, @1);
					$$ = n;
				}
		;

/*
 * If we see PARTITION, RANGE, or ROWS as the first token after the '('
 * of a window_specification, we want the assumption to be that there is
 * no existing_window_name; but those keywords are unreserved and so could
 * be ColIds.  We fix this by making them have the same precedence as IDENT
 * and giving the empty production here a slightly higher precedence, so
 * that the shift/reduce conflict is resolved in favor of reducing the rule.
 * These keywords are thus precluded from being an existing_window_name but
 * are not reserved for any other purpose.
 */
opt_existing_window_name: ColId						{ $$ = $1; }
			| /*EMPTY*/				%prec Op		{ $$ = NULL; }
		;

opt_partition_clause: PARTITION BY expr_list		{ $$ = $3; }
			| /*EMPTY*/								{ $$ = NIL; }
		;

/*
 * For frame clauses, we return a WindowDef, but only some fields are used:
 * frameOptions, startOffset, and endOffset.
 *
 * This is only a subset of the full SQL:2008 frame_clause grammar.
 * We don't support <window frame exclusion> yet.
 */
opt_frame_clause:
			RANGE frame_extent
				{
					WindowDef *n = $2;
					n->frameOptions |= FRAMEOPTION_NONDEFAULT | FRAMEOPTION_RANGE;
					if (n->frameOptions & (FRAMEOPTION_START_VALUE_PRECEDING |
										   FRAMEOPTION_END_VALUE_PRECEDING))
					{
						errprint("\nERROR FEATURE_NOT_SUPPORTED: RANGE PRECEDING is only supported with UNBOUNDED");
						ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @1, $1, ScErrMessage("RANGE PRECEDING is only supported with UNBOUNDED"));
					}
					if (n->frameOptions & (FRAMEOPTION_START_VALUE_FOLLOWING |
										   FRAMEOPTION_END_VALUE_FOLLOWING))
					{
						errprint("\nERROR FEATURE_NOT_SUPPORTED: RANGE FOLLOWING is only supported with UNBOUNDED");
						ThrowExceptionReport(SCERRSQLNOTSUPPORTED, @1, $1, ScErrMessage("RANGE FOLLOWING is only supported with UNBOUNDED"));
					}
					$$ = n;
				}
			| ROWS frame_extent
				{
					WindowDef *n = $2;
					n->frameOptions |= FRAMEOPTION_NONDEFAULT | FRAMEOPTION_ROWS;
					$$ = n;
				}
			| /*EMPTY*/
				{
					WindowDef *n = makeNode(WindowDef);
					n->frameOptions = FRAMEOPTION_DEFAULTS;
					n->startOffset = NULL;
					n->endOffset = NULL;
					$$ = n;
				}
		;

frame_extent: frame_bound
				{
					WindowDef *n = $1;
					/* reject invalid cases */
					if (n->frameOptions & FRAMEOPTION_START_UNBOUNDED_FOLLOWING)
					{
						errprint("\nERROR WINDOWING_ERROR: frame start cannot be UNBOUNDED FOLLOWING");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $1, ScErrMessage("frame start cannot be UNBOUNDED FOLLOWING"));
					}
					if (n->frameOptions & FRAMEOPTION_START_VALUE_FOLLOWING)
					{
						errprint("\nERROR WINDOWING_ERROR: frame starting from following row cannot end with current row");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $1, ScErrMessage("frame starting from following row cannot end with current row"));
					}
					n->frameOptions |= FRAMEOPTION_END_CURRENT_ROW;
					$$ = n;
				}
			| BETWEEN frame_bound AND frame_bound
				{
					WindowDef *n1 = $2;
					WindowDef *n2 = $4;
					/* form merged options */
					int		frameOptions = n1->frameOptions;
					/* shift converts START_ options to END_ options */
					frameOptions |= n2->frameOptions << 1;
					frameOptions |= FRAMEOPTION_BETWEEN;
					/* reject invalid cases */
					if (frameOptions & FRAMEOPTION_START_UNBOUNDED_FOLLOWING)
					{
						errprint("\nERROR WINDOWING_ERROR: frame start cannot be UNBOUNDED FOLLOWING");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @2, $2, ScErrMessage("frame start cannot be UNBOUNDED FOLLOWING"));
					}
					if (frameOptions & FRAMEOPTION_END_UNBOUNDED_PRECEDING)
					{
						errprint("\nERROR WINDOWING_ERROR: frame end cannot be UNBOUNDED PRECEDING");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @4, $4, ScErrMessage("frame end cannot be UNBOUNDED PRECEDING"));
					}
					if ((frameOptions & FRAMEOPTION_START_CURRENT_ROW) &&
						(frameOptions & FRAMEOPTION_END_VALUE_PRECEDING))
					{
						errprint("\nERROR WINDOWING_ERROR: frame starting from current row cannot have preceding rows");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @4, $4, ScErrMessage("frame starting from current row cannot have preceding rows"));
					}
					if ((frameOptions & FRAMEOPTION_START_VALUE_FOLLOWING) &&
						(frameOptions & (FRAMEOPTION_END_VALUE_PRECEDING |
										 FRAMEOPTION_END_CURRENT_ROW)))
					{
						errprint("\nERROR WINDOWING_ERROR: frame starting from following row cannot have preceding rows");
						ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @4, $4, ScErrMessage("frame starting from following row cannot have preceding rows"));
					}
					n1->frameOptions = frameOptions;
					n1->endOffset = n2->startOffset;
					$$ = n1;
				}
		;

/*
 * This is used for both frame start and frame end, with output set up on
 * the assumption it's frame start; the frame_extent productions must reject
 * invalid cases.
 */
frame_bound:
			UNBOUNDED PRECEDING
				{
					WindowDef *n = makeNode(WindowDef);
					n->frameOptions = FRAMEOPTION_START_UNBOUNDED_PRECEDING;
					n->startOffset = NULL;
					n->endOffset = NULL;
					$$ = n;
				}
			| UNBOUNDED FOLLOWING
				{
					WindowDef *n = makeNode(WindowDef);
					n->frameOptions = FRAMEOPTION_START_UNBOUNDED_FOLLOWING;
					n->startOffset = NULL;
					n->endOffset = NULL;
					$$ = n;
				}
			| CURRENT_P ROW
				{
					WindowDef *n = makeNode(WindowDef);
					n->frameOptions = FRAMEOPTION_START_CURRENT_ROW;
					n->startOffset = NULL;
					n->endOffset = NULL;
					$$ = n;
				}
			| a_expr PRECEDING
				{
					WindowDef *n = makeNode(WindowDef);
					n->frameOptions = FRAMEOPTION_START_VALUE_PRECEDING;
					n->startOffset = $1;
					n->endOffset = NULL;
					$$ = n;
				}
			| a_expr FOLLOWING
				{
					WindowDef *n = makeNode(WindowDef);
					n->frameOptions = FRAMEOPTION_START_VALUE_FOLLOWING;
					n->startOffset = $1;
					n->endOffset = NULL;
					$$ = n;
				}
		;


/*
 * Supporting nonterminals for expressions.
 */

/* Explicit row production.
 *
 * SQL99 allows an optional ROW keyword, so we can now do single-element rows
 * without conflicting with the parenthesized a_expr production.  Without the
 * ROW keyword, there must be more than one a_expr inside the parens.
 */
row:		ROW '(' expr_list ')'					{ $$ = $3; }
			| ROW '(' ')'							{ $$ = NIL; }
			| '(' expr_list ',' a_expr ')'			{ $$ = lappend($2, $4); }
		;

sub_type:	ANY										{ $$ = ANY_SUBLINK; }
			| SOME									{ $$ = ANY_SUBLINK; }
			| ALL									{ $$ = ALL_SUBLINK; }
		;

all_Op:		Op										{ $$ = $1; }
			| MathOp								{ $$ = $1; }
		;

MathOp:		 '+'									{ $$ = L"+"; }
			| '-'									{ $$ = L"-"; }
			| '*'									{ $$ = L"*"; }
			| '/'									{ $$ = L"/"; }
			| '%'									{ $$ = L"%"; }
			| '^'									{ $$ = L"^"; }
			| '<'									{ $$ = L"<"; }
			| '>'									{ $$ = L">"; }
			| '='									{ $$ = L"="; }
		;

qual_Op:	Op
					{ $$ = list_make1(makeString($1)); }
			| OPERATOR '(' any_operator ')'
					{ $$ = $3; }
		;

qual_all_Op:
			all_Op
					{ $$ = list_make1(makeString($1)); }
			| OPERATOR '(' any_operator ')'
					{ $$ = $3; }
		;

subquery_Op:
			all_Op
					{ $$ = list_make1(makeString($1)); }
			| OPERATOR '(' any_operator ')'
					{ $$ = $3; }
			| LIKE
					{ $$ = list_make1(makeString("~~")); }
			| NOT LIKE
					{ $$ = list_make1(makeString("!~~")); }
			| ILIKE
					{ $$ = list_make1(makeString("~~*")); }
			| NOT ILIKE
					{ $$ = list_make1(makeString("!~~*")); }
/* cannot put SIMILAR TO here, because SIMILAR TO is a hack.
 * the regular expression is preprocessed by a function (similar_escape),
 * and the ~ operator for posix regular expressions is used.
 *        x SIMILAR TO y     ->    x ~ similar_escape(y)
 * this transformation is made on the fly by the parser upwards.
 * however the SubLink structure which handles any/some/all stuff
 * is not ready for such a thing.
 */
			;

expr_list:	a_expr												%dprec 10
				{
					$$ = list_make1($1);
				}
			| expr_list ',' a_expr								%dprec 1
				{
					$$ = lappend($1, $3);
				}
		;

/* function arguments can have names */
func_arg_list:  func_arg_expr
				{
					$$ = list_make1($1);
				}														%dprec 10
			| func_arg_list ',' func_arg_expr
				{
					$$ = lappend($1, $3);
				}														%dprec 1
		;

func_arg_expr:  a_expr
				{
					$$ = $1;
				}
			| param_name COLON_EQUALS a_expr
				{
					NamedArgExpr *na = makeNode(NamedArgExpr);
					na->name = $1;
					na->arg = (Expr *) $3;
					na->argnumber = -1;		/* until determined */
					SET_LOCATION(na, @1);
					$$ = (Node *) na;
				}
		;

type_list:	Typename								{ $$ = list_make1($1); }
			| type_list ',' Typename				{ $$ = lappend($1, $3); }
		;

array_expr: '[' expr_list ']'
				{
					$$ = makeAArrayExpr($2, @1);
				}
			| '[' array_expr_list ']'
				{
					$$ = makeAArrayExpr($2, @1);
				}
			| '[' ']'
				{
					$$ = makeAArrayExpr(NIL, @1);
				}
		;

array_expr_list: array_expr							{ $$ = list_make1($1); }
			| array_expr_list ',' array_expr		{ $$ = lappend($1, $3); }
		;


extract_list:
			extract_arg FROM a_expr
				{
					$$ = list_make2(makeStringConst($1, @1), $3);
				}
			| /*EMPTY*/								{ $$ = NIL; }
		;

/* Allow delimited string Sconst in extract_arg as an SQL extension.
 * - thomas 2001-04-12
 */
extract_arg:
			IDENT									{ $$ = $1; }
			| YEAR_P								{ $$ = L"year"; }
			| MONTH_P								{ $$ = L"month"; }
			| DAY_P									{ $$ = L"day"; }
			| HOUR_P								{ $$ = L"hour"; }
			| MINUTE_P								{ $$ = L"minute"; }
			| SECOND_P								{ $$ = L"second"; }
			| Sconst								{ $$ = $1; }
		;

/* OVERLAY() arguments
 * SQL99 defines the OVERLAY() function:
 * o overlay(text placing text from int for int)
 * o overlay(text placing text from int)
 * and similarly for binary strings
 */
overlay_list:
			a_expr overlay_placing substr_from substr_for
				{
					$$ = list_make4($1, $2, $3, $4);
				}
			| a_expr overlay_placing substr_from
				{
					$$ = list_make3($1, $2, $3);
				}
		;

overlay_placing:
			PLACING a_expr
				{ $$ = $2; }
		;

/* position_list uses b_expr not a_expr to avoid conflict with general IN */

position_list:
			b_expr IN_P b_expr						{ $$ = list_make2($3, $1); }
			| /*EMPTY*/								{ $$ = NIL; }
		;

/* SUBSTRING() arguments
 * SQL9x defines a specific syntax for arguments to SUBSTRING():
 * o substring(text from int for int)
 * o substring(text from int) get entire string from starting point "int"
 * o substring(text for int) get first "int" characters of string
 * o substring(text from pattern) get entire string matching pattern
 * o substring(text from pattern for escape) same with specified escape char
 * We also want to support generic substring functions which accept
 * the usual generic list of arguments. So we will accept both styles
 * here, and convert the SQL9x style to the generic list for further
 * processing. - thomas 2000-11-28
 */
substr_list:
			a_expr substr_from substr_for
				{
					$$ = list_make3($1, $2, $3);
				}
			| a_expr substr_for substr_from
				{
					/* not legal per SQL99, but might as well allow it */
					$$ = list_make3($1, $3, $2);
				}
			| a_expr substr_from
				{
					$$ = list_make2($1, $2);
				}
			| a_expr substr_for
				{
					/*
					 * Since there are no cases where this syntax allows
					 * a textual FOR value, we forcibly cast the argument
					 * to int4.  The possible matches in pg_proc are
					 * substring(text,int4) and substring(text,text),
					 * and we don't want the parser to choose the latter,
					 * which it is likely to do if the second argument
					 * is unknown or doesn't have an implicit cast to int4.
					 */
					$$ = list_make3($1, makeIntConst(1, -1),
									makeTypeCast($2,
												 SystemTypeName("int4"), -1));
				}
			| expr_list
				{
					$$ = $1;
				}
			| /*EMPTY*/
				{ $$ = NIL; }
		;

substr_from:
			FROM a_expr								{ $$ = $2; }
		;

substr_for: FOR a_expr								{ $$ = $2; }
		;

trim_list:	a_expr FROM expr_list					{ $$ = lappend($3, $1); }
			| FROM expr_list						{ $$ = $2; }
			| expr_list								{ $$ = $1; }
		;

in_expr:	select_with_parens
				{
					SubLink *n = makeNode(SubLink);
					n->subselect = $1;
					/* other fields will be filled later */
					$$ = (Node *)n;
				}
			| '(' expr_list ')'						{ $$ = (Node *)$2; }
		;

/*
 * Define SQL92-style case clause.
 * - Full specification
 *	CASE WHEN a = b THEN c ... ELSE d END
 * - Implicit argument
 *	CASE a WHEN b THEN c ... ELSE d END
 */
case_expr:	CASE case_arg when_clause_list case_default END_P
				{
					CaseExpr *c = makeNode(CaseExpr);
					c->casetype = InvalidOid; /* not analyzed yet */
					c->arg = (Expr *) $2;
					c->args = $3;
					c->defresult = (Expr *) $4;
					SET_LOCATION(c, @1);
					$$ = (Node *)c;
				}
		;

when_clause_list:
			/* There must be at least one */
			when_clause								{ $$ = list_make1($1); }
			| when_clause_list when_clause			{ $$ = lappend($1, $2); }
		;

when_clause:
			WHEN a_expr THEN a_expr
				{
					CaseWhen *w = makeNode(CaseWhen);
					w->expr = (Expr *) $2;
					w->result = (Expr *) $4;
					SET_LOCATION(w, @1);
					$$ = (Node *)w;
				}
		;

case_default:
			ELSE a_expr								{ $$ = $2; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

case_arg:	a_expr									{ $$ = $1; }
			| /*EMPTY*/								{ $$ = NULL; }
		;

indirection_el:
			'.' attr_name
				{
					$$ = (Node *) makeString($2);
				}
			| '.' '*'
				{
					$$ = (Node *) makeNode(A_Star);
				}
			| '[' a_expr ']'
				{
					A_Indices *ai = makeNode(A_Indices);
					ai->lidx = NULL;
					ai->uidx = $2;
					$$ = (Node *) ai;
				}
			| '[' a_expr ':' a_expr ']'
				{
					A_Indices *ai = makeNode(A_Indices);
					ai->lidx = $2;
					ai->uidx = $4;
					$$ = (Node *) ai;
				}
		;

indirection:
			indirection_el							{ $$ = list_make1($1); }
			| indirection indirection_el			{ $$ = lappend($1, $2); }
		;

opt_indirection:
			/*EMPTY*/								{ $$ = NIL; }
			| opt_indirection indirection_el		{ $$ = lappend($1, $2); }
		;

opt_asymmetric: ASYMMETRIC
			| /*EMPTY*/
		;

/*
 * The SQL spec defines "contextually typed value expressions" and
 * "contextually typed row value constructors", which for our purposes
 * are the same as "a_expr" and "row" except that DEFAULT can appear at
 * the top level.
 */

ctext_expr:
			a_expr					{ $$ = (Node *) $1; }
			| DEFAULT
				{
					SetToDefault *n = makeNode(SetToDefault);
					SET_LOCATION(n, @1);
					$$ = (Node *) n;
				}
		;

ctext_expr_list:
			ctext_expr								{ $$ = list_make1($1); }	%dprec 10
			| ctext_expr_list ',' ctext_expr		{ $$ = lappend($1, $3); }	%dprec 1
		;

/*
 * We should allow ROW '(' ctext_expr_list ')' too, but that seems to require
 * making VALUES a fully reserved word, which will probably break more apps
 * than allowing the noise-word is worth.
 */
ctext_row: '(' ctext_expr_list ')'					{ $$ = $2; }
		;


/*****************************************************************************
 *
 *	target list for SELECT
 *
 *****************************************************************************/

target_list:
			target_el								{ $$ = list_make1($1); }	%dprec 10
			| target_list ',' target_el				{ $$ = lappend($1, $3); }	%dprec 1
		;

target_el:	a_expr AS ColLabel
				{
					$$ = makeNode(ResTarget);
					$$->name = $3;
					$$->indirection = NIL;
					$$->val = (Node *)$1;
					SET_LOCATION($$, @1);
				}
			/*
			 * We support omitting AS only for column labels that aren't
			 * any known keyword.  There is an ambiguity against postfix
			 * operators: is "a ! b" an infix expression, or a postfix
			 * expression and a column label?  We prefer to resolve this
			 * as an infix expression, which we accomplish by assigning
			 * IDENT a precedence higher than POSTFIXOP.
			 */
			| a_expr IDENT
				{
					$$ = makeNode(ResTarget);
					$$->name = $2;
					$$->indirection = NIL;
					$$->val = (Node *)$1;
					SET_LOCATION($$, @1);
				}
			| a_expr
				{
					$$ = makeNode(ResTarget);
					$$->name = NULL;
					$$->indirection = NIL;
					$$->val = (Node *)$1;
					SET_LOCATION($$, @1);
				}
			| '*'
				{
					ColumnRef *n = makeNode(ColumnRef);
					n->fields = list_make1(makeNode(A_Star));
					SET_LOCATION(n, @1);

					$$ = makeNode(ResTarget);
					$$->name = NULL;
					$$->indirection = NIL;
					$$->val = (Node *)n;
					SET_LOCATION($$, @1);
				}
		;


/*****************************************************************************
 *
 *	Names and constants
 *
 *****************************************************************************/

qualified_name_list:
			qualified_name							{ $$ = list_make1($1); }
			| qualified_name_list ',' qualified_name { $$ = lappend($1, $3); }
		;

/*
 * The production for a qualified relation name has to exactly match the
 * production for a qualified func_name, because in a FROM clause we cannot
 * tell which we are parsing until we see what comes after it ('(' for a
 * func_name, something else for a relation). Therefore we allow 'indirection'
 * which may contain subscripts, and reject that case in the C code.
 */
qualified_name:
			ColId
				{
					$$ = makeRangeVar(NULL, $1, @1);
				}
			| ColId indirection
				{
					check_qualified_name($2, yyscanner);
					$$ = makeRangeVar(NULL, NULL, @1);
					$$->relname = strVal(llast($2));
					$$->path = lcons(makeString($1), list_truncate($2,list_length($2)-1));
				}
		;

name_list:	name
					{ $$ = list_make1(makeString($1)); }
			| name_list ',' name
					{ $$ = lappend($1, makeString($3)); }
		;


name:		ColId									{ $$ = $1; };

database_name:
			ColId									{ $$ = $1; };

access_method:
			ColId									{ $$ = $1; };

attr_name:	ColLabel								{ $$ = $1; };

index_name: ColId									{ $$ = $1; };

/*
 * The production for a qualified func_name has to exactly match the
 * production for a qualified columnref, because we cannot tell which we
 * are parsing until we see what comes after it ('(' or Sconst for a func_name,
 * anything else for a columnref).  Therefore we allow 'indirection' which
 * may contain subscripts, and reject that case in the C code.  (If we
 * ever implement SQL99-like methods, such syntax may actually become legal!)
 */
func_name:	type_function_name
					{ $$ = list_make1(makeString($1)); }
			| ColId indirection
					{
						$$ = check_func_name(lcons(makeString($1), $2),
											 yyscanner);
					}
		;


/*
 * Constants
 */
AexprConst: Iconst
				{
					$$ = makeIntConst($1, @1);
				}
			| FCONST
				{
					$$ = makeFloatConst($1, @1);
				}
			| Sconst
				{
					$$ = makeStringConst($1, @1);
				}
			| BINARY Sconst
				{
					$$ = makeBinaryConst($2, @2);
				}
			| OBJECT_P Iconst
				{
					$$ = makeObjectConst($2, @2);
				}
			| BCONST
				{
					$$ = makeBitStringConst($1, @1);
				}
			| XCONST
				{
					/* This is a bit constant per SQL99:
					 * Without Feature F511, "BIT data type",
					 * a <general literal> shall not be a
					 * <bit string literal> or a <hex string literal>.
					 */
					$$ = makeBitStringConst($1, @1);
				}
			| type_function_name Sconst
				{
					/* generic type 'literal' syntax */
					TypeName *t = makeTypeNameFromNameList($1);
					SET_LOCATION(t, @1);
					$$ = makeStringConstCast($2, @2, t);
				}
			| type_function_name '(' func_arg_list ')' Sconst
				{
					/* generic syntax with a type modifier */
					TypeName *t = makeTypeNameFromNameList($1);
					ListCell *lc;

					/*
					 * We must use func_arg_list in the production to avoid
					 * reduce/reduce conflicts, but we don't actually wish
					 * to allow NamedArgExpr in this context.
					 */
					foreach(lc, $3)
					{
						NamedArgExpr *arg = (NamedArgExpr *) lfirst(lc);

						if (IsA(arg, NamedArgExpr))
						{
							errprint("\nERROR SYNTAX_ERROR: type modifier cannot have parameter name");
							ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, arg->location, arg->name, ScErrMessage("type modifier cannot have parameter name"));
						}
					}
					t->typmods = $3;
					SET_LOCATION(t, @1);
					$$ = makeStringConstCast($5, @5, t);
				}
			| ConstTypename Sconst
				{
					$$ = makeStringConstCast($2, @2, $1);
				}
			| ConstInterval Sconst opt_interval
				{
					TypeName *t = $1;
					t->typmods = $3;
					$$ = makeStringConstCast($2, @2, t);
				}
			| ConstInterval '(' Iconst ')' Sconst opt_interval
				{
					TypeName *t = $1;
					if ($6 != NIL)
					{
						if (list_length($6) != 1)
						{
							errprint("\nERROR SYNTAX_ERROR: interval precision specified twice");
							ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, @1, $1, ScErrMessage("interval precision specified twice"));
						}
						t->typmods = lappend($6, makeIntConst($3, @3));
					}
					else
						t->typmods = list_make2(makeIntConst(INTERVAL_FULL_RANGE, -1),
												makeIntConst($3, @3));
					$$ = makeStringConstCast($5, @5, t);
				}
			| TRUE_P
				{
					$$ = makeBoolAConst(TRUE, @1);
				}
			| FALSE_P
				{
					$$ = makeBoolAConst(FALSE, @1);
				}
			| NULL_P
				{
					$$ = makeNullAConst(@1);
				}
		;

Iconst:		ICONST									{ $$ = $1; };
Sconst:		SCONST									{ $$ = $1; };
RoleId:		ColId									{ $$ = $1; };

SignedIconst: Iconst								{ $$ = $1; }
			| '+' Iconst							{ $$ = + $2; }
			| '-' Iconst							{ $$ = - $2; }
		;

/*
 * Name classification hierarchy.
 *
 * IDENT is the lexeme returned by the lexer for identifiers that match
 * no known keyword.  In most cases, we can accept certain keywords as
 * names, not only IDENTs.	We prefer to accept as many such keywords
 * as possible to minimize the impact of "reserved words" on programmers.
 * So, we divide names into several possible classes.  The classification
 * is chosen in part to make keywords acceptable as names wherever possible.
 */

/* Column identifier --- names that can be column, table, etc names.
 */
ColId:		IDENT									{ $$ = $1; }
			| unreserved_keyword					{ $$ = wpstrdup($1); }
			| col_name_keyword						{ $$ = wpstrdup($1); }
		;

/* Type/function identifier --- names that can be type or function names.
 */
type_function_name:	IDENT							{ $$ = $1; }
			| unreserved_keyword					{ $$ = wpstrdup($1); }
			| type_func_name_keyword				{ $$ = wpstrdup($1); }
		;

/* Column label --- allowed labels in "AS" clauses.
 * This presently includes *all* Postgres keywords.
 */
ColLabel:	IDENT									{ $$ = $1; }
			| unreserved_keyword					{ $$ = wpstrdup($1); }
			| col_name_keyword						{ $$ = wpstrdup($1); }
			| type_func_name_keyword				{ $$ = wpstrdup($1); }
			| reserved_keyword						{ $$ = wpstrdup($1); }
		;


/*
 * Keyword category lists.  Generally, every keyword present in
 * the Postgres grammar should appear in exactly one of these lists.
 *
 * Put a new keyword into the first list that it can go into without causing
 * shift or reduce conflicts.  The earlier lists define "less reserved"
 * categories of keywords.
 *
 * Make sure that each keyword's category in kwlist.h matches where
 * it is listed here.  (Someday we may be able to generate these lists and
 * kwlist.h's table from a common master list.)
 */

/* "Unreserved" keywords --- available for use as any kind of name.
 */
unreserved_keyword:
			  ABORT_P
			| ACCESS
			| ACTION
			| ADD_P
			| ADMIN
			| AFTER
			| ALTER
			| ALWAYS
			| AT
			| BEFORE
			| BEGIN_P
			| BY
			| CACHE
			| CASCADE
			| CASCADED
			| CATALOG_P
			| CHARACTERISTICS
			| CHECK
			| CHECKPOINT
			| COALESCE
			| COLLATION
			| COMMENTS
			| COMMIT
			| COMMITTED
			| CONNECTION
			| CONSTRAINTS
			| CONTENT_P
			| CONTINUE_P
			| CURRENT_P
			| CYCLE
			| DATA_P
			| DATABASE
			| DAY_P
			| DEALLOCATE
			| DEFAULTS
			| DEFERRABLE
			| DEFERRED
			| DELETE_P
			| DISABLE_P
			| DISCARD
			| DO
			| DOCUMENT_P
			| DOMAIN_P
			| DOUBLE_P
			| DROP
			| EACH
			| ELSE
			| ENABLE_P
			| ENCODING
			| ENCRYPTED
			| END_P
			| ESCAPE
			| EXCLUDE
			| EXCLUDING
			| EXCLUSIVE
			| EXECUTE
			| EXPLAIN
			| FIRST_P
			| FOLLOWING
			| FORCE
			| GLOBAL
			| GRANT
			| GRANTED
			| GREATEST
			| HOUR_P
			| IDENTITY_P
			| IF_P
			| IMMEDIATE
			| IN_P
			| INCLUDING
			| INCREMENT
			| INDEX
			| INDEXES
			| INHERIT
			| INHERITS
			| INITIALLY
			| INSERT
			| INSTEAD
			| INTO
			| ISOLATION
			| KEY
			| LAST_P
			| LARGE_P
			| LC_COLLATE_P
			| LC_CTYPE_P
			| LEAST
			| LEVEL
			| LOCAL
			| LOCK_P
			| MATCH
			| MAXVALUE
			| MINUTE_P
			| MINVALUE
			| MODE
			| MONTH_P
			| NAME_P
			| NAMES
			| NEXT
			| NO
			| NOWAIT
			| NULLIF
			| NULLS_P
			| OBJECT_P
			| OF
			| OIDS
			| OPERATOR
			| OPTIONS
			| OWNED
			| OWNER
			| PARTIAL
			| PARTITION
			| PASSING
			| PASSWORD
			| PLACING
			| PLANS
			| PRECEDING
			| PRECISION
			| PREPARE
			| PREPARED
			| PRESERVE
			| PRIVILEGES
			| PROCEDURE
			| RANGE
			| READ
			| REASSIGN
			| RECURSIVE
			| REF
			| REINDEX
			| RELEASE
			| REPEATABLE
			| REPLACE
			| REPLICA
			| RESET
			| RESTART
			| RESTRICT
			| REVOKE
			| ROLE
			| ROLLBACK
			| ROWS
			| SAVEPOINT
			| SCHEMA
			| SECOND_P
			| SEQUENCE
			| SERIALIZABLE
			| SESSION
			| SET
			| SHARE
			| SHOW
			| SIMPLE
			| STANDALONE_P
			| START
			| STATEMENT
			| STORAGE
			| STRIP_P
			| SYSID
			| SYSTEM_P
			| TABLESPACE
			| THEN
			| TEMP
			| TEMPLATE
			| TEMPORARY
			| TRANSACTION
			| TRIGGER
			| TRUNCATE
			| TYPE_P
			| UNBOUNDED
			| UNCOMMITTED
			| UNENCRYPTED
			| UNLOGGED
			| UNTIL
			| UPDATE
			| VALID
			| VALIDATE
			| VALUE_P
			| VARYING
			| VERSION_P
			| VIEW
			| WHITESPACE_P
			| WITHOUT
			| WORK
			| WRITE
			| XML_P
			| YEAR_P
			| YES_P
			| ZONE
		;

/* Column identifier --- keywords that can be column, table, etc names.
 *
 * Many of these keywords will in fact be recognized as type or function
 * names too; but they have special productions for the purpose, and so
 * can't be treated as "generic" type or function names.
 *
 * The type names appearing here are not usable as function names
 * because they can be followed by '(' in typename productions, which
 * looks too much like a function call for an LR(1) parser.
 */
col_name_keyword:
			  BETWEEN
			| BIGINT
			| BIT
			| BOOLEAN_P
			| CHAR_P
			| CHARACTER
			| DEC
			| DECIMAL_P
			| EXISTS
			| EXTRACT
			| FLOAT_P
			| INT_P
			| INTEGER
			| INTERVAL
			| NATIONAL
			| NCHAR
			| NUMERIC
			| OVERLAY
			| POSITION
			| REAL
			| ROW
			| SETOF
			| SMALLINT
			| SUBSTRING
			| TIME
			| TIMESTAMP
			| TREAT
			| TRIM
			| TYPEOF
			| VALUES
			| VARCHAR
			| XMLATTRIBUTES
			| XMLCONCAT
			| XMLELEMENT
			| XMLEXISTS
			| XMLFOREST
			| XMLPARSE
			| XMLPI
			| XMLROOT
			| XMLSERIALIZE
		;

/* Type/function identifier --- keywords that can be type or function names.
 *
 * Most of these are keywords that are used as operators in expressions;
 * in general such keywords can't be column names because they would be
 * ambiguous with variables, but they are unambiguous as function identifiers.
 *
 * Do not include POSITION, SUBSTRING, etc here since they have explicit
 * productions in a_expr to support the goofy SQL9x argument syntax.
 * - thomas 2000-11-28
 */
type_func_name_keyword:
			ANALYSE
			| ANALYZE
			| AUTHORIZATION
			| COLLATE
			| COLUMN
			| CONCURRENTLY
			| CONSTRAINT
			| CREATE
			| CROSS
			| EXCEPT
			| FETCH
			| FOR
			| FOREIGN
			| FULL
			| GROUP_P
			| HAVING
			| ILIKE
			| INNER_P
			| INTERSECT
			| IS
			| ISNULL
			| JOIN
			| LEFT
			| LIKE
			| LIMIT
			| NATURAL
			| NOTNULL
			| OFFSET
			| OFFSETKEY
			| OPTION
			| ORDER
			| OUTER_P
			| OVER
			| OVERLAPS
			| PRIMARY
			| RETURNING
			| RIGHT
			| SIMILAR
			| STARTS
			| TO
			| UNION
			| USER
			| VERBOSE
			| WHERE
			| WINDOW
		;

/* Reserved keyword --- these keywords are usable only as a ColLabel.
 *
 * Keywords appear here if they could not be distinguished from variable,
 * type, or function names in some contexts.  Don't put things here unless
 * forced to.
 */
reserved_keyword:
			  ALL
			| AND
			| ANY
			| ARRAY
			| AS
			| ASC
			| ASYMMETRIC
			| BINARY
			| BOTH
			| CASE
			| CAST
			| DEFAULT
			| DESC
			| DISTINCT
			| FALSE_P
			| FROM
			| LEADING
			| NOT
			| NULL_P
			| ON
			| ONLY
			| OR
			| REFERENCES
			| SELECT
			| SOME
			| SYMMETRIC
			| TABLE
			| TRAILING
			| TRUE_P
			| UNIQUE
			| USING
			| VARIADIC
			| WHEN
			| WITH
		;

%%

/*
 * The signature of this function is required by bison.  However, we
 * ignore the passed yylloc and instead use the last token position
 * available from the scanner.
 */
static void
base_yyerror(YYLTYPE *locp, core_yyscan_t yyscanner, const char *msg)
{
	parser_yyerror(msg);
}

static Node *
makeColumnRef(char *colname, List *indirection,
			  YYLTYPE location, core_yyscan_t yyscanner)
{
	/*
	 * Generate a ColumnRef node, with an A_Indirection node added if there
	 * is any subscripting in the specified indirection list.  However,
	 * any field selection at the start of the indirection list must be
	 * transposed into the "fields" part of the ColumnRef node.
	 */
	ColumnRef  *c = makeNode(ColumnRef);
	int		nfields = 0;
	ListCell *l;

	SET_LOCATION(c, location);
	foreach(l, indirection)
	{
		if (IsA(lfirst(l), A_Indices))
		{
			A_Indirection *i = makeNode(A_Indirection);

			if (nfields == 0)
			{
				/* easy case - all indirection goes to A_Indirection */
				c->fields = list_make1(makeString(colname));
				i->indirection = check_indirection(indirection, yyscanner);
			}
			else
			{
				/* got to split the list in two */
				i->indirection = check_indirection(list_copy_tail(indirection,
																  nfields),
												   yyscanner);
				indirection = list_truncate(indirection, nfields);
				c->fields = lcons(makeString(colname), indirection);
			}
			i->arg = (Node *) c;
			return (Node *) i;
		}
		else if (IsA(lfirst(l), A_Star))
		{
			/* We only allow '*' at the end of a ColumnRef */
			if (lnext(l) != NULL)
				parser_yyerror("improper use of \"*\"");
		}
		nfields++;
	}
	/* No subscripting, so all indirection gets added to field list */
	c->fields = lcons(makeString(colname), indirection);
	return (Node *) c;
}

static Node *
makeTypeCast(Node *arg, TypeName *typename, YYLTYPE location)
{
	TypeCast *n = makeNode(TypeCast);
	n->arg = arg;
	n->typeName = typename;
	SET_LOCATION(n, location);
	return (Node *) n;
}

static Node *
makeStringConst(wchar_t *str, YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	n->val.type = T_String;
	n->val.val.str = str;
	SET_LOCATION(n, location);

	return (Node *)n;
}

static bool hexadecimalString(char *str)
{
	int i = 0;
	while (str[i] != '\0')
	{
		if ((str[i] >= '0' && str[i] <= '9') || (str[i] >= 'a' && str[i] <= 'f') || (str[i] >= 'A' && str[i] <= 'F'))
			i++;
		else return false;
	}
	return true;
}

static Node *
makeBinaryConst(char *str, YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	if (!hexadecimalString(str))
	{
		errprint("SYNTAX_ERROR: %s is not hexadecimal string", str);
		ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, location, str, ScErrMessage("not hexadecimal string"));
	}
	n->val.type = T_Binary;
	n->val.val.str = str;
	SET_LOCATION(n, location);

	return (Node *)n;
}

static Node *
makeStringConstCast(wchar_t *str, YYLTYPE location, TypeName *typename)
{
	Node *s = makeStringConst(str, location);

	return makeTypeCast(s, typename, -1);
}

static Node *
makeIntConst(__int64 val, YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	n->val.type = T_Integer;
	n->val.val.ival = val;
	SET_LOCATION(n, location);

	return (Node *)n;
}

static Node *
makeObjectConst(int val, YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	n->val.type = T_Object;
	n->val.val.ival = val;
	SET_LOCATION(n, location);

	return (Node *)n;
}

static Node *
makeFloatConst(wchar_t *str, YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	n->val.type = T_Float;
	n->val.val.str = str;
	SET_LOCATION(n, location);

	return (Node *)n;
}

static Node *
makeBitStringConst(wchar_t *str, YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	n->val.type = T_BitString;
	n->val.val.str = str;
	SET_LOCATION(n, location);

	return (Node *)n;
}

static Node *
makeNullAConst(YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	n->val.type = T_Null;
	SET_LOCATION(n, location);

	return (Node *)n;
}

static Node *
makeAConst(Value *v, YYLTYPE location)
{
	Node *n;

	switch (v->type)
	{
		case T_Float:
			n = makeFloatConst(v->val.str, location);
			break;

		case T_Integer:
			n = makeIntConst(v->val.ival, location);
			break;

		case T_String:
		default:
			n = makeStringConst(v->val.str, location);
			break;
	}

	return n;
}

/* makeBoolAConst()
 * Create an A_Const string node and put it inside a boolean cast.
 */
static Node *
makeBoolAConst(bool state, YYLTYPE location)
{
	A_Const *n = makeNode(A_Const);

	n->val.type = T_String;
	n->val.val.str = (state ? "t" : "f");
	SET_LOCATION(n, location);

	return makeTypeCast((Node *)n, SystemTypeName("bool"), -1);
}

/* makeOverlaps()
 * Create and populate a FuncCall node to support the OVERLAPS operator.
 */
static FuncCall *
makeOverlaps(List *largs, List *rargs, YYLTYPE location, core_yyscan_t yyscanner)
{
	FuncCall *n = makeNode(FuncCall);

	n->funcname = SystemFuncName(L"overlaps");
	if (list_length(largs) == 1)
		largs = lappend(largs, largs);
	else if (list_length(largs) != 2)
	{
		errprint("\nERROR: SYNTAX_ERROR: wrong number of parameters on left side of OVERLAPS expression");
		ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, location, n->funcname, ScErrMessage("wrong number of parameters on left side of OVERLAPS expression"));
		return NULL; // Sc: not reached
	}
	if (list_length(rargs) == 1)
		rargs = lappend(rargs, rargs);
	else if (list_length(rargs) != 2)
	{
		errprint("\nERROR SYNTAX_ERROR: wrong number of parameters on right side of OVERLAPS expression");
		ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, location, n->funcname, ScErrMessage("wrong number of parameters on right side of OVERLAPS expression"));
		return NULL; // Sc: not reached
	}
	n->args = list_concat(largs, rargs);
	n->agg_order = NIL;
	n->agg_star = FALSE;
	n->agg_distinct = FALSE;
	n->func_variadic = FALSE;
	n->over = NULL;
	SET_LOCATION(n, location);
	return n;
}

/* check_qualified_name --- check the result of qualified_name production
 *
 * It's easiest to let the grammar production for qualified_name allow
 * subscripts and '*', which we then must reject here.
 */
static void
check_qualified_name(List *names, core_yyscan_t yyscanner)
{
	check_func_name(names, yyscanner);
/*	ListCell   *i;

	foreach(i, names)
	{
		if (!IsA(lfirst(i), String))
			parser_yyerror("Expecting qualified name, but found different token.");
	}*/
}

/* check_func_name --- check the result of func_name production
 *
 * It's easiest to let the grammar production for func_name allow subscripts
 * and '*', which we then must reject here.
 */
static List *
check_func_name(List *names, core_yyscan_t yyscanner)
{
	ListCell   *i;

	foreach(i, names)
	{
		if (!IsA(lfirst(i), String))
			parser_yyerror("Expecting qualified identifier, but found different token.");
	}
	return names;
}

/* check_indirection --- check the result of indirection production
 *
 * We only allow '*' at the end of the list, but it's hard to enforce that
 * in the grammar, so do it here.
 */
static List *
check_indirection(List *indirection, core_yyscan_t yyscanner)
{
	ListCell *l;

	foreach(l, indirection)
	{
		if (IsA(lfirst(l), A_Star))
		{
			if (lnext(l) != NULL)
				parser_yyerror("improper use of \"*\"");
		}
	}
	return indirection;
}

/* findLeftmostSelect()
 * Find the leftmost component SelectStmt in a set-operation parsetree.
 */
static SelectStmt *
findLeftmostSelect(SelectStmt *node)
{
	while (node && node->op != SETOP_NONE)
		node = node->larg;
	Assert(node && IsA(node, SelectStmt) && node->larg == NULL);
	return node;
}

/* insertSelectOptions()
 * Insert ORDER BY, etc into an already-constructed SelectStmt.
 *
 * This routine is just to avoid duplicating code in SelectStmt productions.
 */
static void
insertSelectOptions(SelectStmt *stmt,
					List *sortClause, List *lockingClause,
					Node *limitOffset,
					WithClause *withClause,
					List *optionClause,
					core_yyscan_t yyscanner)
{
	Assert(IsA(stmt, SelectStmt));

	/*
	 * Tests here are to reject constructs like
	 *	(SELECT foo ORDER BY bar) ORDER BY baz
	 */
	if (sortClause)
	{
		if (stmt->sortClause)
		{
			errprint("\nERROR SYNTAX_ERROR: multiple ORDER BY clauses not allowed");
			ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, exprLocation((Node *) sortClause), NULL, ScErrMessage("multiple ORDER BY clauses not allowed"));
		}
		stmt->sortClause = sortClause;
	}
	/* We can handle multiple locking clauses, though */
	stmt->lockingClause = list_concat(stmt->lockingClause, lockingClause);
	if (limitOffset)
	{
		if (stmt->limitOffset)
		{
			errprint("\nERROR SYNTAX_ERROR: multiple OFFSET clauses not allowed");
			ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, exprLocation((Node *) sortClause), NULL, ScErrMessage("multiple OFFSET clauses not allowed"));
		}
		stmt->limitOffset = limitOffset;
	}
	if (withClause)
	{
		if (stmt->withClause)
		{
			errprint("\nERROR SYNTAX_ERROR: multiple WITH clauses not allowed");
			ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, exprLocation((Node *) withClause), NULL, ScErrMessage("multiple WITH clauses not allowed"));
		}
		stmt->withClause = withClause;
	}
	if (optionClause)
	{
		if (stmt->optionClause)
		{
			errprint("\nERROR SYNTAX_ERROR: multiple OPTION clauses not allowed");
			ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, exprLocation((Node *) optionClause), NULL, ScErrMessage("multiple OPTION clauses not allowed"));
		}
		if (stmt->op)
		{
			errprint("\nERROR SYNTAX_ERROR: OPTION clause on set operation not allowed");
			ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, exprLocation((Node *) optionClause), NULL, ScErrMessage("OPTION clause on set operation not allowed"));
		}
		stmt->optionClause = optionClause;
	}
}

static Node *
makeSetOp(SetOperation op, bool all, Node *larg, Node *rarg)
{
	SelectStmt *n = makeNode(SelectStmt);

	n->op = op;
	n->all = all;
	n->larg = (SelectStmt *) larg;
	n->rarg = (SelectStmt *) rarg;
	return (Node *) n;
}

/* SystemFuncName()
 * Build a properly-qualified reference to a built-in function.
 */
List *
SystemFuncName(char *name)
{
	return list_make2(makeString("pg_catalog"), makeString(name));
}

/* SystemTypeName()
 * Build a properly-qualified reference to a built-in type.
 *
 * typmod is defaulted, but may be changed afterwards by caller.
 * Likewise for the location.
 */
TypeName *
SystemTypeName(char *name)
{
	return makeTypeNameFromNameList(list_make2(makeString("pg_catalog"),
											   makeString(name)));
}

/* doNegate()
 * Handle negation of a numeric constant.
 *
 * Formerly, we did this here because the optimizer couldn't cope with
 * indexquals that looked like "var = -4" --- it wants "var = const"
 * and a unary minus operator applied to a constant didn't qualify.
 * As of Postgres 7.0, that problem doesn't exist anymore because there
 * is a constant-subexpression simplifier in the optimizer.  However,
 * there's still a good reason for doing this here, which is that we can
 * postpone committing to a particular internal representation for simple
 * negative constants.	It's better to leave "-123.456" in string form
 * until we know what the desired type is.
 */
static Node *
doNegate(Node *n, YYLTYPE location)
{
	if (IsA(n, A_Const))
	{
		A_Const *con = (A_Const *)n;

		/* report the constant's location as that of the '-' sign */
		SET_LOCATION(con, location);

		if (con->val.type == T_Integer)
		{
			con->val.val.ival = -con->val.val.ival;
			return n;
		}
		if (con->val.type == T_Float)
		{
			doNegateFloat(&con->val);
			return n;
		}
	}

	return (Node *) makeSimpleA_Expr(AEXPR_OP, "-", NULL, n, location);
}

static void
doNegateFloat(Value *v)
{
	wchar_t   *oldval = v->val.str;

	Assert(IsA(v, Float));
	if (*oldval == '+')
		oldval += sizeof(wchar_t);
	if (*oldval == '-')
		v->val.str = oldval+sizeof(wchar_t);	/* just strip the '-' */
	else
	{
		wchar_t   *newval = (wchar_t *) palloc((wcslen(oldval) + 2)*sizeof(wchar_t));

		*newval = L'-';
		wcscpy(newval+sizeof(wchar_t), oldval);
		v->val.str = newval;
	}
}

static Node *
makeAArrayExpr(List *elements, int location)
{
	A_ArrayExpr *n = makeNode(A_ArrayExpr);

	n->elements = elements;
	SET_LOCATION(n, location);
	return (Node *) n;
}

static Node *
makeXmlExpr(XmlExprOp op, wchar_t *name, List *named_args, List *args,
			YYLTYPE location)
{
	XmlExpr    *x = makeNode(XmlExpr);

	x->op = op;
	x->name = name;
	/*
	 * named_args is a list of ResTarget; it'll be split apart into separate
	 * expression and name lists in transformXmlExpr().
	 */
	x->named_args = named_args;
	x->arg_names = NIL;
	x->args = args;
	/* xmloption, if relevant, must be filled in by caller */
	/* type and typmod will be filled in during parse analysis */
	SET_LOCATION(x, location);
	return (Node *) x;
}

/*
 * Convert a list of (dotted) names to a RangeVar (like
 * makeRangeVarFromNameList, but with position support).  The
 * "AnyName" refers to the any_name production in the grammar.
 */
static RangeVar *
makeRangeVarFromAnyName(List *names, YYLTYPE position, core_yyscan_t yyscanner)
{
	RangeVar *r = makeNode(RangeVar);
	r->relname = strVal(llast(names));
	r->path = list_truncate(names,list_length(names)-1);

	r->relpersistence = RELPERSISTENCE_PERMANENT;
	SET_LOCATION(r, position);

	return r;
}

/* Separate Constraint nodes from COLLATE clauses in a ColQualList */
static void
SplitColQualList(List *qualList,
				 List **constraintList, CollateClause **collClause,
				 core_yyscan_t yyscanner)
{
	ListCell   *cell;
	ListCell   *prev;
	ListCell   *next;

	*collClause = NULL;
	prev = NULL;
	for (cell = list_head(qualList); cell; cell = next)
	{
		Node   *n = (Node *) lfirst(cell);

		next = lnext(cell);
		if (IsA(n, Constraint))
		{
			/* keep it in list */
			prev = cell;
			continue;
		}
		if (IsA(n, CollateClause))
		{
			CollateClause *c = (CollateClause *) n;

			if (*collClause)
			{
				errprint("\nERROR SYNTAX_ERROR: multiple COLLATE clauses not allowed");
				ThrowExceptionReport(SCERRSQLINCORRECTSYNTAX, c->location, NULL, ScErrMessage("multiple COLLATE clauses not allowed"));
			}
			*collClause = c;
		}
		else
		{
			errprint("\nERROR: unexpected node type %d", (int) n->type);
			ThrowExceptionCode(SCERRSQLINCORRECTSYNTAX, ScErrMessageInt("unexpected node type %d", (int) n->type));
		}
		/* remove non-Constraint nodes from qualList */
		qualList = list_delete_cell(qualList, cell, prev);
	}
	*constraintList = qualList;
}

/*
 * Process result of ConstraintAttributeSpec, and set appropriate bool flags
 * in the output command node.  Pass NULL for any flags the particular
 * command doesn't support.
 */
static void
processCASbits(int cas_bits, YYLTYPE location, const char *constrType,
			   bool *deferrable, bool *initdeferred, bool *not_valid,
			   core_yyscan_t yyscanner)
{
	/* defaults */
	if (deferrable)
		*deferrable = false;
	if (initdeferred)
		*initdeferred = false;
	if (not_valid)
		*not_valid = false;

	if (cas_bits & (CAS_DEFERRABLE | CAS_INITIALLY_DEFERRED))
	{
		if (deferrable)
			*deferrable = true;
		else
		{
			errprint("\nERROR FEATURE_NOT_SUPPORTED: %s constraints cannot be marked DEFERRABLE",
							constrType);
			ThrowExceptionReport(SCERRSQLNOTSUPPORTED, location, NULL, ScErrMessageStr("%s constraints cannot be marked DEFERRABLE", constrType));
					 /* translator: %s is CHECK, UNIQUE, or similar */
		}
	}

	if (cas_bits & CAS_INITIALLY_DEFERRED)
	{
		if (initdeferred)
			*initdeferred = true;
		else
		{
			errprint("\nERROR FEATURE_NOT_SUPPORTED: %s constraints cannot be marked DEFERRABLE",
							constrType);
			ThrowExceptionReport(SCERRSQLNOTSUPPORTED, location, NULL, ScErrMessageStr("%s constraints cannot be marked DEFERRABLE", constrType));
					 /* translator: %s is CHECK, UNIQUE, or similar */
		}
	}

	if (cas_bits & CAS_NOT_VALID)
	{
		if (not_valid)
			*not_valid = true;
		else
		{
			errprint("\nERROR FEATURE_NOT_SUPPORTED: %s constraints cannot be marked NOT VALID",
							constrType);
			ThrowExceptionReport(SCERRSQLNOTSUPPORTED, location, NULL, ScErrMessageStr("%s constraints cannot be marked NOT VALID", constrType));
					 /* translator: %s is CHECK, UNIQUE, or similar */
		}
	}
}

/* parser_init()
 * Initialize to parse one query string
 */
void
parser_init(base_yy_extra_type *yyext)
{
	yyext->parsetree = NIL;		/* in case grammar forgets to set it */
}

/*
 * Must undefine this stuff before including scan.c, since it has different
 * definitions for these macros.
 */
#undef yyerror
#undef yylval
#undef yylloc

#ifdef _MSC_VER
# pragma warning(pop)
#endif // _MSC_VER

#include "scan.c"

