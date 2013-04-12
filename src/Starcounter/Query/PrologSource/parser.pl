

/* Parser, Peter Idestam-Almquist, Starcounter, 2013-02-07. */

/* A parser for SQL following SQL92. The numbers of the sections below refer to the chapters in 
*  the specification of the ANSI-standard SQL92. 
*/

/* 13-02-07: Removed support of object-variable as first operand to istype-predicate. */
/* 13-02-04: Added support of offset not only offsetkey in fetch clause. */
/* 13-01-28: Added support of is-type comparison by adding istype_predicate/3. */
/* 12-11-29: Fixed bug regarding order of arithmetic operations. */
/* 11-12-08: Changed fetch number and offset types to numerical (from integer). */
/* 11-11-30: Added fetch specification; modified select1. */
/* 11-02-24: Fixed problem with too slow response when incorrect syntax. */
/* 10-12-13: Support of Binary literal. */
/* 10-09-21: Support of old version of from clause. */
/* 10-05-10: New representation of variable by '?'. */
/* 10-01-11: Support of index hints by name, and support of previuos index hints removed. */
/* 09-08-31: Support of combined-index hints. */
/* 09-02-24: Support of asterisks '*' in paths in select-list. */
/* 09-02-18: Support of unqualified field identifiers. */
/* 09-01-26: Added string operation addMaxChar to better support STARTS WITH. */
/* 08-07-10: Updated variable/3. */
/* 08-04-09: Not allowed column names (aliases) as sort keys. */
/* 08-03-27: Allowed identifiers to start with underscore (_identifier). */
/* 08-03-13: Replaced property(cast(Type1,Type2),Name) with cast(property(Type1,Name),Type2) etc. */
/* 08-03-10: Removed support of paths without table-reference-aliases (Example.Employee.FirstName). */
/* 08-03-07: Support of object reference casting (cast(p.Father as Example.Employee).Manager). */
/* 08-02-07: Support of object-literals (OBJECT Number). */
/* 08-01-03: Removed modify_select/2. */
/* 07-12-14: Support of join-order hints (OPTION JOIN ORDER (a,b,...), ...). */
/* 07-12-06: Removed support of nested selects. */
/* 07-12-05: Support of index hints (OPTION INDEX(Path), ...). */
/* 07-05-28: path(Type,Path). */
/* 07-05-14: Support of starts-with (STARTS WITH 'prefix'). */
/* 07-04-24: New token format of reserved words <WORD>. */
/* 07-04-22: path(Type,ExtNum,Path), method(Type,Name,Args), gmethod(Type,Name,TParams,Args). */
/* 06-11-21: Introduced operator isNot. */
/* 06-10-30: Simplified datetime_literal/3. */
/* 06-10-23: Empty query ("") gives error 'Incorrect syntax'. */

:- module(parser,[]).


/*===== Main. =====*/

/* parse(+Tokens,-Tree,-ErrHead,-ErrTail):-
*	Creates a parse tree Tree in an internal format from a list of tokens Tokens.
*	Also returns a difference list ErrHead-ErrTail of errors.
*/
parse(Tokens,Tree,Err,Err):- 
	hint_specification(Tokens,[],Tree), !.
parse(_,noTree,['Incorrect syntax.'|Err],Err):- !.


/*===== 5 Lexical elements. =====*/
/* Arguments to predicates with suffix '_code' are lists of ASCII-codes. */

/*===== 5.1 <SQL terminal character>. =====*/

simple_latin_letter_code(Cs1,Cs2):- 
	simple_latin_upper_case_letter_code(Cs1,Cs2), !.
simple_latin_letter_code(Cs1,Cs2):- 
	simple_latin_lower_case_letter_code(Cs1,Cs2), !.

simple_latin_upper_case_letter_code([Code|Cs],Cs):- /* 'A'-'Z' */
	Code >= 65, 
	Code =< 90, !. 
simple_latin_lower_case_letter_code([Code|Cs],Cs):- /* 'a'-'z' */
	Code >= 97, 
	Code =< 122, !. 

digit_code([Code|Cs],Cs):- /* '0'-'9' */
	Code >= 48, 
	Code =< 57, !. 

quote_code([39|Cs],Cs):- !. /* ''' */

underscore_code([95|Cs],Cs):- !. /* '_' */

minus_code([45|Cs],Cs):- !. /* '-' */

colon_code([58|Cs],Cs):- !. /* ':' */

decimal_code([46|Cs],Cs):- !. /* '.' */

space_code([32|Cs],Cs):- !. /* ' ' */

terminal([Code|Cs],Code,Cs):- !. /* Used to represent terminal symbols. */

empty_string_literal(literal(string,Literal)):- 
	atom_codes(Literal,[39,39]), !.


/*===== 5.2 <token> and <separator>. =====*/

/* Unlike SQL92 identifiers are allowed to start with an underscore. */

regular_identifier([Token|_],_,_):- 
	tokenizer:reserved_word(Token), !, 
	fail.
regular_identifier([Token|Ts],Ts,id(Token)):- 
	atom_codes(Token,Codes), 
	identifier_body_code(Codes,[]), !.

identifier_body_code(Ts1,Ts3):- 
	identifier_start_code(Ts1,Ts2), 
	identifier_body_code2(Ts2,Ts3), !.

identifier_body_code2(Ts1,Ts3):- 
	identifier_part_code(Ts1,Ts2), !, 
	identifier_body_code2(Ts2,Ts3), !.
identifier_body_code2(Ts,Ts):- !.

identifier_start_code(Ts1,Ts2):- 
	simple_latin_letter_code(Ts1,Ts2), !.
identifier_start_code(Ts1,Ts2):- 
	underscore_code(Ts1,Ts2), !. 

identifier_part_code(Ts1,Ts2):- 
	identifier_start_code(Ts1,Ts2), !.
identifier_part_code(Ts1,Ts2):- 
	digit_code(Ts1,Ts2), !.


/*===== 5.3 <literal>. =====*/

literal(Ts1,Ts2,Literal):- 
	null_literal(Ts1,Ts2,Literal), !.
literal(Ts1,Ts2,Literal):- 
	numeric_literal(Ts1,Ts2,Literal), !.
literal(Ts1,Ts2,Literal):- 
	boolean_literal(Ts1,Ts2,Literal), !.
literal(Ts1,Ts2,Literal):- 
	datetime_literal(Ts1,Ts2,Literal), !.
literal(Ts1,Ts2,Literal):- 
	string_literal(Ts1,Ts2,Literal), !.
literal(Ts1,Ts2,Literal):- 
	object_literal(Ts1,Ts2,Literal), !.
literal(Ts1,Ts2,Literal):- 
	binary_literal(Ts1,Ts2,Literal), !.

null_literal(Ts1,Ts2,literal(any,null)):-
	terminal(Ts1,'<NULL>',Ts2), !.

numeric_literal(Ts1,Ts2,Literal):- 
	double_literal(Ts1,Ts2,Literal), !.
numeric_literal(Ts1,Ts2,Literal):- 
	decimal_literal(Ts1,Ts2,Literal), !.
/* numeric_literal(Ts1,Ts2,Literal):- 
	unsigned_integer_literal(Ts1,Ts2,Literal), !. */
numeric_literal(Ts1,Ts2,Literal):- 
	integer_literal(Ts1,Ts2,Literal), !.

double_literal(Ts1,Ts4,literal(double,Literal)):- 
	mantissa(Ts1,Ts2,Mantissa), 
	terminal(Ts2,'<E>',Ts3), 
	exponent(Ts3,Ts4,Exponent), 
	atom_concat(Mantissa,'E',MantissaE), 
	atom_concat(MantissaE,Exponent,Literal), !.

mantissa(Ts1,Ts2,Literal):- 
	decimal_literal(Ts1,Ts2,literal(decimal,Literal)), !.
mantissa(Ts1,Ts2,Literal):- 
	integer_literal(Ts1,Ts2,literal(integer,Literal)), !.

exponent(Ts1,Ts2,Literal):- 
	integer_literal(Ts1,Ts2,literal(integer,Literal)), !.

decimal_literal(Ts1,Ts3,literal(decimal,Literal2)):-
	sign_symbol(Ts1,Ts2,Sign), 
	unsigned_decimal_literal(Ts2,Ts3,literal(decimal,Literal1)), 
	atom_concat(Sign,Literal1,Literal2), !.
decimal_literal(Ts1,Ts2,Literal):-
	unsigned_decimal_literal(Ts1,Ts2,Literal), !.
	
unsigned_decimal_literal(Ts1,Ts4,literal(decimal,Literal)):-
	unsigned_integer_literal(Ts1,Ts2,literal(uinteger,Integer)), 
	terminal(Ts2,'.',Ts3),
	unsigned_integer_literal(Ts3,Ts4,literal(uinteger,Decimals)), 
	atom_concat(Integer,'.',IntegerDot), 
	atom_concat(IntegerDot,Decimals,Literal), !.
	
integer_literal(Ts1,Ts3,literal(integer,Atom3)):-
	sign_symbol(Ts1,Ts2,Atom1), 
	unsigned_integer_literal(Ts2,Ts3,literal(uinteger,Atom2)), 
	atom_concat(Atom1,Atom2,Atom3), !.
integer_literal(Ts1,Ts2,literal(integer,Literal)):-
	unsigned_integer_literal(Ts1,Ts2,literal(uinteger,Literal)), !.

sign_symbol(Ts1,Ts2,'+'):- 
	terminal(Ts1,'+',Ts2), !.
sign_symbol(Ts1,Ts2,'-'):- 
	terminal(Ts1,'-',Ts2), !.

unsigned_integer_literal([Token|Ts],Ts,literal(uinteger,Token)):- 
	atom_codes(Token,Codes), 
	integer_codes(Codes,[]), !.

integer_codes(Cs1,Cs3):- 
	digit_code(Cs1,Cs2), !, 
	integer_codes2(Cs2,Cs3), !.

integer_codes2(Cs1,Cs3):- 
	digit_code(Cs1,Cs2), !, 
	integer_codes2(Cs2,Cs3), !.
integer_codes2(Cs,Cs):- !.

boolean_literal(Ts1,Ts2,literal(boolean,true)):-
	terminal(Ts1,'<TRUE>',Ts2), !.
boolean_literal(Ts1,Ts2,literal(boolean,false)):-
	terminal(Ts1,'<FALSE>',Ts2), !.

/* Simplified datetime_literal/3. */

datetime_literal(Ts1,Ts3,literal(datetime,String)):- 
	terminal(Ts1,'<DATE>',Ts2), 
	string_literal(Ts2,Ts3,literal(string,String)), !.
datetime_literal(Ts1,Ts3,literal(datetime,String)):- 
	terminal(Ts1,'<TIME>',Ts2), 
	string_literal(Ts2,Ts3,literal(string,String)), !.
datetime_literal(Ts1,Ts3,literal(datetime,String)):- 
	terminal(Ts1,'<TIMESTAMP>',Ts2), 
	string_literal(Ts2,Ts3,literal(string,String)), !.

string_literal([Token|Ts],Ts,literal(string,Token)):- 
	atom_codes(Token,Codes), 
	string_codes(Codes,[]), !.

string_codes(Cs1,Cs4):- 
	quote_code(Cs1,Cs2), 
	string_codes2(Cs2,Cs3), 
	quote_code(Cs3,Cs4), !.

string_codes2(Cs1,Cs3):- 
	character_representation_code(Cs1,Cs2), 
	string_codes2(Cs2,Cs3), !.
string_codes2(Cs,Cs):- !.

character_representation_code(Cs1,Cs2):- 
	non_quote_character_code(Cs1,Cs2), !.
character_representation_code(Cs1,Cs2):- 
	quote_symbol_code(Cs1,Cs2), !.

non_quote_character_code(Cs1,Cs2):- 
	quote_code(Cs1,Cs2), !, fail.
non_quote_character_code([_|Cs],Cs):- !.

quote_symbol_code(Cs1,Cs3):- 
	quote_code(Cs1,Cs2), 
	quote_code(Cs2,Cs3), !.

/* Object literal (not SQL92). */

object_literal(Ts1,Ts3,literal(object(unknown),Atom)):-
	terminal(Ts1,'<OBJECT>',Ts2), 
	unsigned_integer_literal(Ts2,Ts3,literal(uinteger,Atom)), !.
object_literal(Ts1,Ts3,literal(object(unknown),Atom)):-
	terminal(Ts1,'<OBJ>',Ts2), 
	unsigned_integer_literal(Ts2,Ts3,literal(uinteger,Atom)), !. 

/* Binary literal (not SQL92). */

binary_literal(Ts1,Ts3,literal(binary,String)):-
	terminal(Ts1,'<BINARY>',Ts2), 
	string_literal(Ts2,Ts3,literal(string,String)), !.


/*===== Variables (not SQL92). =====*/

variable(Ts1,Ts3, variable(any,Num)):- 
	terminal(Ts1,'?',Ts2), 
	terminal(Ts2,Num,Ts3), !.


/*===== 5.4 Names and identifiers. =====*/

identifier(Ts1,Ts2,Identifier):- 
	regular_identifier(Ts1,Ts2,Identifier), !.

table_name(Ts1,Ts2,extent(any,TypeAtom)):- 
	type_name(Ts1,Ts2,TypeAtom), !.

column_name(Ts1,Ts2,name(Name)):- 
	identifier(Ts1,Ts2,id(Name)), !.

correlation_name(Ts1,Ts2,alias(any,Name)):- 
	identifier(Ts1,Ts2,id(Name)), !.

type_name(Ts1,Ts2,TypeAtom):- 
	identifier_list(Ts1,Ts2,IdList), 
	idlist_to_atom(IdList,TypeAtom), !.

identifier_list(Ts1,Ts3,[Identifier|Is]):- 
	identifier(Ts1,Ts2,Identifier), 
	identifier_list2(Ts2,Ts3,Is), !.

identifier_list2(Ts1,Ts4,[Identifier|Is]):- 
	terminal(Ts1,'.',Ts2), !, 
	identifier(Ts2,Ts3,Identifier), 
	identifier_list2(Ts3,Ts4,Is), !.
identifier_list2(Ts,Ts,[]):- !.


/*===== Properties and methods (not SQL92). =====*/

property(Ts1,Ts2,property(any,Name)):- 
	identifier(Ts1,Ts2,id(Name)), !.

method(Ts1,Ts5,method(any,Name,Arguments)):- 
	identifier(Ts1,Ts2,id(Name)), 
	terminal(Ts2,'(',Ts3), 
	method_arguments(Ts3,Ts4,Arguments), 
	terminal(Ts4,')',Ts5), !.

method_arguments(Ts1,Ts3,[ValueExpr|VEs]):- 
	value_expression(Ts1,Ts2,ValueExpr), 
	method_arguments2(Ts2,Ts3,VEs), !.
method_arguments(Ts,Ts,[]):- !.

method_arguments2(Ts1,Ts4,[ValueExpr|VEs]):- 
	terminal(Ts1,',',Ts2), 
	value_expression(Ts2,Ts3,ValueExpr), 
	method_arguments2(Ts3,Ts4,VEs), !.
method_arguments2(Ts,Ts,[]):- !.

generic_method(Ts1,Ts8,gmethod(any,Name,TypeParameters,Arguments)):- 
	identifier(Ts1,Ts2,id(Name)), 
	terminal(Ts2,'<',Ts3), 
	type_parameters(Ts3,Ts4,TypeParameters), 
	terminal(Ts4,'>',Ts5), 
	terminal(Ts5,'(',Ts6), 
	method_arguments(Ts6,Ts7,Arguments), 
	terminal(Ts7,')',Ts8), !.

type_parameters(Ts1,Ts3,[TypeParam|TPs]):- 
	type_name(Ts1,Ts2,TypeParam), 
	type_parameters2(Ts2,Ts3,TPs), !.
type_parameters(Ts,Ts,[]):- !.

type_parameters2(Ts1,Ts4,[TypeParam|TPs]):- 
	terminal(Ts1,',',Ts2), 
	type_name(Ts2,Ts3,TypeParam), 
	type_parameters2(Ts3,Ts4,TPs), !.
type_parameters2(Ts,Ts,[]):- !.


/*===== 6 Scalar expressions. =====*/

/*===== 6.1 <data type>. =====*/


/*===== 6.2 <value specification> and <target specification>. =====*/


/*===== 6.3 <table reference>. =====*/
/* single_table_reference/3 introduced to avoid infinite loops. */

table_reference(Ts1,Ts2,Table):- 
	joined_table(Ts1,Ts2,Table), !.

single_table_reference(Ts1,Ts4,as(Table,Corr)):- 
	correlation_name(Ts1,Ts2,Corr), 
	terminal(Ts2,'<IN>',Ts3), 
	table_name(Ts3,Ts4,Table), !.
single_table_reference(Ts1,Ts3,as(Table,Corr)):- 
	table_name(Ts1,Ts2,Table), 
	single_table_reference2(Ts2,Ts3,Corr), !.
single_table_reference(Ts1,Ts4,Table):- 
	terminal(Ts1,'(',Ts2), 
	table_reference(Ts2,Ts3,Table), 
	terminal(Ts3,')',Ts4), !.

single_table_reference2(Ts1,Ts3,Corr):- 
	single_table_reference3(Ts1,Ts2), 
	correlation_name(Ts2,Ts3,Corr), !.
single_table_reference2(Ts,Ts,alias(any,noName)):- !.

single_table_reference3(Ts1,Ts2):- 
	terminal(Ts1,'<AS>',Ts2), !.
single_table_reference3(Ts,Ts):- !.


/*===== 6.4 <column reference>. =====*/

/* Casts within column references (path expressions) is not SQL92. */
/* Asterisk specifies whether path should end with asterisk (asterisk) or not (noAsterisk). */

column_reference(Ts1,Ts2,asterisk,path(many,Elements)):- 
	column_reference2(Ts1,Ts2,asterisk,0,Elements), !.
column_reference(Ts1,Ts2,noAsterisk,path(any,Elements)):- 
	column_reference2(Ts1,Ts2,noAsterisk,0,Elements), !.

/***BU090224
column_reference2(Ts1,Ts4,noAsterisk,Num1,Elements):- 
	terminal(Ts1,'<CAST>',Ts2), 
	terminal(Ts2,'(',Ts3), 
	Num2 is Num1 + 1,
	column_reference2(Ts3,Ts4,noAsterisk,Num2,Elements), !.
column_reference2(Ts1,Ts4,Asterisk,Num1,[Element2|Es]):- 
	column_reference_element(Ts1,Ts2,Element1), 
	column_reference3(Ts2,Ts3,Asterisk,Num1,Element1,Num2,Element2), 
	column_reference4(Ts3,Ts4,Asterisk,Num2,Es), !.

column_reference3(Ts1,Ts4,noAsterisk,Num1,Element,Num2,cast(Element,Type)):- 
	terminal(Ts1,'<AS>',Ts2), 
	type_name(Ts2,Ts3,Type), 
	terminal(Ts3,')',Ts4), 
	Num2 is Num1 - 1, !.
column_reference3(Ts,Ts,_,Num,Element,Num,Element):- !.

column_reference4(Ts1,Ts3,asterisk,0,[property(many,'*')]):- 
	terminal(Ts1,'.',Ts2), 
	terminal(Ts2,'*',Ts3), !.
column_reference4(Ts1,Ts5,Asterisk,Num1,[Element2|Es]):- 
	terminal(Ts1,'.',Ts2), 
	column_reference_element(Ts2,Ts3,Element1), 
	column_reference3(Ts3,Ts4,Asterisk,Num1,Element1,Num2,Element2), 
	column_reference4(Ts4,Ts5,Asterisk,Num2,Es), !.
column_reference4(Ts,Ts,noAsterisk,0,[]):- !.
***/

column_reference2(Ts1,Ts4,Asterisk,Num1,Elements):- 
	terminal(Ts1,'<CAST>',Ts2), 
	terminal(Ts2,'(',Ts3), 
	Num2 is Num1 + 1,
	column_reference2(Ts3,Ts4,Asterisk,Num2,Elements), !.
column_reference2(Ts1,Ts4,Asterisk,Num1,[Element2|Es]):- 
	column_reference_element(Ts1,Ts2,Element1), 
	column_reference3(Ts2,Ts3,Asterisk,Num1,Element1,Num2,Element2), 
	column_reference4(Ts3,Ts4,Asterisk,Num2,Es), !.

column_reference3(Ts1,Ts4,_,Num1,Element,Num2,cast(Element,Type)):- 
	terminal(Ts1,'<AS>',Ts2), 
	type_name(Ts2,Ts3,Type), 
	terminal(Ts3,')',Ts4), 
	Num2 is Num1 - 1, !.
column_reference3(Ts,Ts,_,Num,Element,Num,Element):- !.

column_reference4(Ts1,Ts3,asterisk,0,[property(many,'*')]):- 
	terminal(Ts1,'.',Ts2), 
	terminal(Ts2,'*',Ts3), !.
column_reference4(Ts1,Ts5,Asterisk,Num1,[Element2|Es]):- 
	terminal(Ts1,'.',Ts2), 
	column_reference_element(Ts2,Ts3,Element1), 
	column_reference3(Ts3,Ts4,Asterisk,Num1,Element1,Num2,Element2), 
	column_reference4(Ts4,Ts5,Asterisk,Num2,Es), !.
column_reference4(Ts,Ts,noAsterisk,0,[]):- !.


column_reference_element(Ts1,Ts2,Method):- 
	method(Ts1,Ts2,Method), !.
column_reference_element(Ts1,Ts2,GMethod):- 
	generic_method(Ts1,Ts2,GMethod), !.
column_reference_element(Ts1,Ts2,Property):- 
	property(Ts1,Ts2,Property), !.


/*===== 6.5 <set function specification>. =====*/

set_function_specification(Ts1,Ts5,setFunction(count,all,literal(integer,'1'))):- 
	terminal(Ts1,'<COUNT>',Ts2), 
	terminal(Ts2,'(',Ts3), 
	terminal(Ts3,'*',Ts4), 
	terminal(Ts4,')',Ts5), !.
set_function_specification(Ts1,Ts2,SetFunction):- 
	general_set_function(Ts1,Ts2,SetFunction), !.

general_set_function(Ts1,Ts6,setFunction(SetFunction,Quantifier,ValueExpr)):- 
	set_function_type(Ts1,Ts2,SetFunction), 
	terminal(Ts2,'(',Ts3), 
	general_set_function2(Ts3,Ts4,Quantifier), 
	value_expression(Ts4,Ts5,ValueExpr), 
	simple_value_expression(ValueExpr), 
	terminal(Ts5,')',Ts6), !.

general_set_function2(Ts1,Ts2,Quantifier):- 
	set_quantifier(Ts1,Ts2,Quantifier), !.
general_set_function2(Ts,Ts,all):- !.

set_function_type(Ts1,Ts2,avg):- 
	terminal(Ts1,'<AVG>',Ts2), !.
set_function_type(Ts1,Ts2,max):- 
	terminal(Ts1,'<MAX>',Ts2), !.
set_function_type(Ts1,Ts2,min):- 
	terminal(Ts1,'<MIN>',Ts2), !.
set_function_type(Ts1,Ts2,sum):- 
	terminal(Ts1,'<SUM>',Ts2), !.
set_function_type(Ts1,Ts2,count):- 
	terminal(Ts1,'<COUNT>',Ts2), !.

set_quantifier(Ts1,Ts2,distinct):- 
	terminal(Ts1,'<DISTINCT>',Ts2), !.
set_quantifier(Ts1,Ts2,all):- 
	terminal(Ts1,'<ALL>',Ts2), !.

/* TODO: Move this control to Controller-module. */
/* simple_value_expression(+ValExpr):-
*	Controls that the value expression ValExpr does not contain any set functions. */
simple_value_expression(literal(_,_)):- !.
simple_value_expression(path(_,_)):- !.
simple_value_expression(operation(numerical,_,Expr1,Expr2)):- 
	simple_value_expression(Expr1), 
	simple_value_expression(Expr2), !.
simple_value_expression(operation(numerical,_,Expr)):- 
	simple_value_expression(Expr), !.
simple_value_expression(operation(string,_,Expr1,Expr2)):- 
	simple_value_expression(Expr1), 
	simple_value_expression(Expr2), !.


/*===== 6.6 <numeric value function>. =====*/


/*===== 6.7 <string value function>. =====*/


/*===== 6.8 <datetime value function>. =====*/


/*===== 6.9 <case expression>. =====*/


/*===== 6.10 <cast specification>. =====*/


/*===== 6.11 <value expression>. =====*/

value_expression(Ts1,Ts2,ValueExpr):- 
	numeric_value_expression(Ts1,Ts2,ValueExpr), !.
value_expression(Ts1,Ts2,ValueExpr):- 
	string_value_expression(Ts1,Ts2,ValueExpr), !.
value_expression(Ts1,Ts2,ValueExpr):- 
	datetime_value_expression(Ts1,Ts2,ValueExpr), !.
value_expression(Ts1,Ts2,ValueExpr):- 
	value_expression_primary(Ts1,Ts2,ValueExpr), !.

value_expression_primary(Ts1,Ts2,Literal):- 
	literal(Ts1,Ts2,Literal), !.
value_expression_primary(Ts1,Ts2,Variable):- 
	variable(Ts1,Ts2,Variable), !.
value_expression_primary(Ts1,Ts2,Path):- 
	column_reference(Ts1,Ts2,noAsterisk,Path), !.
value_expression_primary(Ts1,Ts2,SetFunction):- 
	set_function_specification(Ts1,Ts2,SetFunction), !.
value_expression_primary(Ts1,Ts4,ValueExpr):- 
	terminal(Ts1,'(',Ts2), 
	value_expression(Ts2,Ts3,ValueExpr), 
	terminal(Ts3,')',Ts4), !.


/*===== 6.12 <numeric value expression>. =====*/

/***
numeric_value_expression(Ts1,Ts2,ValueExpr):- 
	numeric_value_expression2(Ts1,Ts2,ValueExpr), 
	numeric_value_control(ValueExpr), !.

numeric_value_expression2(Ts1,Ts4,operation(numerical,addition,ValueExpr1,ValueExpr2)):- 
	term(Ts1,Ts2,ValueExpr1), 
	terminal(Ts2,'+',Ts3), 
	numeric_value_expression2(Ts3,Ts4,ValueExpr2), !.
numeric_value_expression2(Ts1,Ts4,operation(numerical,subtraction,ValueExpr1,ValueExpr2)):- 
	term(Ts1,Ts2,ValueExpr1), 
	terminal(Ts2,'-',Ts3), 
	numeric_value_expression2(Ts3,Ts4,ValueExpr2), !.
numeric_value_expression2(Ts1,Ts2,ValueExpr):- 
	term(Ts1,Ts2,ValueExpr), !.
***/

numeric_value_expression(Ts1,Ts4,ValueExpr):- 
	factor(Ts1,Ts2,Factor),
	term(Ts2,Ts3,Factor,Term), 
	numeric_value_expression2(Ts3,Ts4,Term,ValueExpr), 
	numeric_value_control(ValueExpr), !.

numeric_value_expression2(Ts1,Ts5,Term1,ValueExpr):- 
	terminal(Ts1,'+',Ts2), 
	factor(Ts2,Ts3,Factor),
	term(Ts3,Ts4,Factor,Term2), 
	numeric_value_expression2(Ts4,Ts5,operation(numerical,addition,Term1,Term2),ValueExpr), !.
numeric_value_expression2(Ts1,Ts5,Term1,ValueExpr):- 
	terminal(Ts1,'-',Ts2),
	factor(Ts2,Ts3,Factor),  
	term(Ts3,Ts4,Factor,Term2), 
	numeric_value_expression2(Ts4,Ts5,operation(numerical,subtraction,Term1,Term2),ValueExpr), !.
numeric_value_expression2(Ts,Ts,Term,Term):- !.

term(Ts1,Ts4,Factor1,ValueExpr):- 
	terminal(Ts1,'*',Ts2),
	factor(Ts2,Ts3,Factor2), 
	term(Ts3,Ts4,operation(numerical,multiplication,Factor1,Factor2),ValueExpr), !.
term(Ts1,Ts4,Factor1,ValueExpr):- 
	terminal(Ts1,'/',Ts2), 
	factor(Ts2,Ts3,Factor2), 
	term(Ts3,Ts4,operation(numerical,division,Factor1,Factor2),ValueExpr), !.
term(Ts,Ts,Factor,Factor):- !.

factor(Ts1,Ts2,ValueExpr):- 
	numeric_primary(Ts1,Ts2,ValueExpr), !.
factor(Ts1,Ts3,operation(numerical,Operator,ValueExpr)):- 
	sign_name(Ts1,Ts2,Operator), !, 
	numeric_primary(Ts2,Ts3,ValueExpr), !.

numeric_primary(Ts1,Ts2,ValueExpr):- 
	value_expression_primary(Ts1,Ts2,ValueExpr), !.

sign_name(Ts1,Ts2,plus):- 
	terminal(Ts1,'+',Ts2), !.
sign_name(Ts1,Ts2,minus):- 
	terminal(Ts1,'-',Ts2), !.

/* TODO: Remove type control in parser, or is it necessary for performance? */
numeric_value_control(operation(numerical,_,_,_)):- !.
numeric_value_control(operation(numerical,_,_)):- !.
numeric_value_control(literal(double,_)):- !.
numeric_value_control(literal(decimal,_)):- !.
numeric_value_control(literal(integer,_)):- !.
numeric_value_control(literal(uinteger,_)):- !.


/*===== 6.13 <string value expression>. =====*/

string_value_expression(Ts1,Ts2,ValueExpr):- 
	string_value_expression2(Ts1,Ts2,ValueExpr), 
	string_value_control(ValueExpr), !.

string_value_expression2(Ts1,Ts2,ValueExpr):- 
	character_value_expression(Ts1,Ts2,ValueExpr), !.

character_value_expression(Ts1,Ts2,ValueExpr):- 
	concatenation(Ts1,Ts2,ValueExpr), !.
character_value_expression(Ts1,Ts2,ValueExpr):- 
	character_factor(Ts1,Ts2,ValueExpr), !.

concatenation(Ts1,Ts4,operation(string,concatenation,ValueExpr1,ValueExpr2)):- 
	character_factor(Ts1,Ts2,ValueExpr1), 
	terminal(Ts2,'||',Ts3), 
	character_value_expression(Ts3,Ts4,ValueExpr2), !.

character_factor(Ts1,Ts2,ValueExpr):- 
	character_primary(Ts1,Ts2,ValueExpr), !.

character_primary(Ts1,Ts2,ValueExpr):- 
	value_expression_primary(Ts1,Ts2,ValueExpr), !.

/* TODO: Remove type control in parser, or is it necesary for performance? */
string_value_control(operation(string,_,_,_)):- !.
string_value_control(literal(string,_)):- !.


/*===== 6.14 <datetime value expression>. =====*/

datetime_value_expression(Ts1,Ts2,ValueExpr):- 
	datetime_primary(Ts1,Ts2,ValueExpr), 
	datetime_value_control(ValueExpr), !.

datetime_primary(Ts1,Ts2,ValueExpr):- 
	value_expression_primary(Ts1,Ts2,ValueExpr), !.	

/* TODO: Remove type control in parser, or is it necesary for performance? */
datetime_value_control(literal(datetime,_)):- !.


/*===== 6.15 <interval value expression>. =====*/


/*===== 7 Query expressions. =====*/

/*===== 7.1 <row value constructor>. =====*/

row_value_constructor(Ts1,Ts2,Row):- 
	row_value_constructor_element(Ts1,Ts2,Row), !.
row_value_constructor(Ts1,Ts4,Row):- 
	terminal(Ts1,'(',Ts2), 
	row_value_constructor_list(Ts2,Ts3,Row), 
	terminal(Ts3,')',Ts4), !.

row_value_constructor_list(Ts1,Ts3,[Element|Es]):- 
	row_value_constructor_element(Ts1,Ts2,Element), 
	row_value_constructor_list2(Ts2,Ts3,Es), !.
	
row_value_constructor_list2(Ts1,Ts4,[Element|Es]):- 
	terminal(Ts1,',',Ts2), 
	row_value_constructor_element(Ts2,Ts3,Element), 
	row_value_constructor_list2(Ts3,Ts4,Es), !.
row_value_constructor_list2(Ts,Ts,[]):- !.

row_value_constructor_element(Ts1,Ts2,ValueExpr):- 
	value_expression(Ts1,Ts2,ValueExpr), !.


/*===== 7.2 <table value constructor>. =====*/

table_value_constructor(Ts1,Ts3,Table):- 
	terminal(Ts1,'<VALUES>',Ts2), 
	table_value_constructor_list(Ts2,Ts3,Table), !.

table_value_constructor_list(Ts1,Ts3,[Row|Rs]):- 
	row_value_constructor(Ts1,Ts2,Row), 
	table_value_constructor_list2(Ts2,Ts3,Rs), !.
	
table_value_constructor_list2(Ts1,Ts4,[Row|Rs]):- 
	terminal(Ts1,',',Ts2), 
	row_value_constructor(Ts2,Ts3,Row), 
	table_value_constructor_list2(Ts3,Ts4,Rs), !.
table_value_constructor_list2(Ts,Ts,[]):- !.


/*===== 7.3 <table expression>. =====*/

table_expression(Ts1,Ts5,tableExpr(From,Where,GroupBy,Having)):- 
	from_clause(Ts1,Ts2,From), 
	table_expression2(Ts2,Ts3,Where), 
	table_expression3(Ts3,Ts4,GroupBy), 
	table_expression4(Ts4,Ts5,Having), !.

table_expression2(Ts1,Ts2,Where):- 
	where_clause(Ts1,Ts2,Where), !.
table_expression2(Ts,Ts,literal(logical,true)):- !.

table_expression3(Ts1,Ts2,GroupBy):- 
	groupby_clause(Ts1,Ts2,GroupBy), !.
table_expression3(Ts,Ts,[]):- !.

table_expression4(Ts1,Ts2,Having):- 
	having_clause(Ts1,Ts2,Having), !.
table_expression4(Ts,Ts,literal(logical,true)):- !.


/*===== 7.4 <from clause>. =====*/

from_clause(Ts1,Ts4,Table2):- 
	terminal(Ts1,'<FROM>',Ts2), 
	table_reference(Ts2,Ts3,Table1), 
	from_clause2(Ts3,Ts4,Table1,Table2), !.

from_clause2(Ts1,Ts4,Table1,Table4):- 
	terminal(Ts1,',',Ts2), !, 
	table_reference(Ts2,Ts3,Table2), 
	Table3 = join1(inner,Table1,Table2,literal(logical,true)), 
	from_clause2(Ts3,Ts4,Table3,Table4), !.
from_clause2(Ts,Ts,Table,Table):- !.


/*===== 7.5 <joined table>. =====*/

joined_table(Ts1,Ts3,Table2):- 
	single_table_reference(Ts1,Ts2,Table1), 
	joined_table2(Ts2,Ts3,Table1,Table2), !.

joined_table2(Ts1,Ts3,Table1,Table3):- 
	cross_join(Ts1,Ts2,Table1,Table2), 
	joined_table2(Ts2,Ts3,Table2,Table3), !.
joined_table2(Ts1,Ts3,Table1,Table3):- 
	qualified_join(Ts1,Ts2,Table1,Table2), 
	joined_table2(Ts2,Ts3,Table2,Table3), !.
joined_table2(Ts,Ts,Table,Table):- !.

cross_join(Ts1,Ts4,Table1,join1(cross,Table1,Table2,literal(logical,true))):- 
	terminal(Ts1,'<CROSS>',Ts2), 
	terminal(Ts2,'<JOIN>',Ts3), 
	single_table_reference(Ts3,Ts4,Table2), !.

qualified_join(Ts1,Ts5,Table1,join1(Type,Table1,Table2,Cond)):- 
	qualified_join2(Ts1,Ts2,Type), 
	terminal(Ts2,'<JOIN>',Ts3), 
	single_table_reference(Ts3,Ts4,Table2), 
	qualified_join3(Ts4,Ts5,Cond), !.

qualified_join2(Ts1,Ts2,Type):- 
	join_type(Ts1,Ts2,Type), !.
qualified_join2(Ts,Ts,inner):- !.

qualified_join3(Ts1,Ts2,Cond):- 
	join_specification(Ts1,Ts2,Cond), !.
qualified_join3(Ts,Ts,literal(logical,true)):- !.

join_specification(Ts1,Ts3,Cond):- 
	terminal(Ts1,'<ON>',Ts2), 
	search_condition(Ts2,Ts3,Cond), !.

join_type(Ts1,Ts2,inner):- 
	terminal(Ts1,'<INNER>',Ts2), !.
join_type(Ts1,Ts3,Type):- 
	outer_join_type(Ts1,Ts2,Type), 
	join_type2(Ts2,Ts3), !.

join_type2(Ts1,Ts2):- 
	terminal(Ts1,'<OUTER>',Ts2), !.
join_type2(Ts,Ts):- !.

outer_join_type(Ts1,Ts2,left):- 
	terminal(Ts1,'<LEFT>',Ts2), !.
outer_join_type(Ts1,Ts2,right):- 
	terminal(Ts1,'<RIGHT>',Ts2), !.
outer_join_type(Ts1,Ts2,full):- 
	terminal(Ts1,'<FULL>',Ts2), !.


/*===== 7.6 <where clause>. =====*/

where_clause(Ts1,Ts3,Cond):- 
	terminal(Ts1,'<WHERE>',Ts2), 
	search_condition(Ts2,Ts3,Cond), !.


/*===== 7.7 <group by clause>. =====*/

groupby_clause(Ts1,Ts4,ColumnRefs):- 
	terminal(Ts1,'<GROUP>',Ts2), 
	terminal(Ts2,'<BY>',Ts3), 
	grouping_column_reference_list(Ts3,Ts4,ColumnRefs), !.

grouping_column_reference_list(Ts1,Ts3,[ColumnRef|CRs]):- 
	grouping_column_reference(Ts1,Ts2,ColumnRef), 
	grouping_column_reference_list2(Ts2,Ts3,CRs), !.

grouping_column_reference_list2(Ts1,Ts4,[ColumnRef|CRs]):- 
	terminal(Ts1,',',Ts2), 
	grouping_column_reference(Ts2,Ts3,ColumnRef), 
	grouping_column_reference_list2(Ts3,Ts4,CRs), !.
grouping_column_reference_list2(Ts,Ts,[]):- !.

grouping_column_reference(Ts1,Ts2,ColumnRef):- 
	column_reference(Ts1,Ts2,noAsterisk,ColumnRef), !.	


/*===== 7.8 <having clause>. =====*/

having_clause(Ts1,Ts3,Cond):- 
	terminal(Ts1,'<HAVING>',Ts2), 
	search_condition(Ts2,Ts3,Cond), !.


/*===== 7.9 <query specification>. =====*/

query_specification(Ts1,Ts5,select1(Quantifier,Columns,From,Where,GroupBy,Having,[],noFetchSpec,[])):- 
	terminal(Ts1,'<SELECT>',Ts2), 
	query_specification2(Ts2,Ts3,Quantifier), 
	select_list(Ts3,Ts4,Columns), 
	table_expression(Ts4,Ts5,tableExpr(From,Where,GroupBy,Having)), !.

query_specification2(Ts1,Ts2,Quantifier):- 
	set_quantifier(Ts1,Ts2,Quantifier), !.
query_specification2(Ts,Ts,all):- !.

select_list(Ts1,Ts2,[as(path(many,[property(many,'*')]),name(noName))]):- 
	terminal(Ts1,'*',Ts2), !. 
select_list(Ts1,Ts3,[Column|Cs]):- 
	select_sublist(Ts1,Ts2,Column), 
	select_list2(Ts2,Ts3,Cs), !.

select_list2(Ts1,Ts4,[Column|Cs]):- 
	terminal(Ts1,',',Ts2), !, 
	select_sublist(Ts2,Ts3,Column), 
	select_list2(Ts3,Ts4,Cs), !.
select_list2(Ts,Ts,[]):- !.

select_sublist(Ts1,Ts2,as(Column,name(noName))):- 
	column_reference(Ts1,Ts2,asterisk,Column), !.
select_sublist(Ts1,Ts2,Column):- 
	derived_column(Ts1,Ts2,Column), !.

derived_column(Ts1,Ts3,Column):- 
	value_expression(Ts1,Ts2,ValueExpr), 
	derived_column2(Ts2,Ts3,Name), 
	concat_as(ValueExpr,Name,Column), !.

derived_column2(Ts1,Ts2,Name):- 
	as_clause(Ts1,Ts2,Name), !.
derived_column2(Ts,Ts,noAs):- !.

as_clause(Ts1,Ts3,Name):- 
	terminal(Ts1,'<AS>',Ts2), !, 
	column_name(Ts2,Ts3,Name), !.
as_clause(Ts1,Ts2,Name):- 
	column_name(Ts1,Ts2,Name), !.

concat_as(Expr,noAs,as(Expr,name(noName))):- !.
concat_as(Expr,Name,as(Expr,Name)):- !.


/*===== 7.10 <query expression>. =====*/

query_expression(Ts1,Ts2,Query):- 
	nonjoin_query_expression(Ts1,Ts2,Query), !.

nonjoin_query_expression(Ts1,Ts2,Query):- 
	nonjoin_query_term(Ts1,Ts2,Query), !.

nonjoin_query_term(Ts1,Ts2,Query):- 
	nonjoin_query_primary(Ts1,Ts2,Query), !.

nonjoin_query_primary(Ts1,Ts2,Query):- 
	simple_table(Ts1,Ts2,Query), !.
nonjoin_query_primary(Ts1,Ts4,Query):- 
	terminal(Ts1,'(',Ts2), 
	nonjoin_query_expression(Ts2,Ts3,Query), 
	terminal(Ts3,')',Ts4), !.

simple_table(Ts1,Ts2,Table):- 
	query_specification(Ts1,Ts2,Table), !.
simple_table(Ts1,Ts2,Table):- 
	table_value_constructor(Ts1,Ts2,Table), !.


/*===== 7.11 <scalar subquery>, <row subquery> and <table subquery>. =====*/

/***071206
table_subquery(Ts1,Ts2,Query):- 
	subquery(Ts1,Ts2,Query), !.

subquery(Ts1,Ts4,Query):- 
	terminal(Ts1,'(',Ts2), 
	query_expression(Ts2,Ts3,Query), 
	terminal(Ts3,')',Ts4), !.
***/


/*===== 8 Predicates =====*/

/*===== 8.1 <predicate>. =====*/

predicate(Ts1,Ts2,Predicate):- 
	comparison_predicate(Ts1,Ts2,Predicate), !.
predicate(Ts1,Ts2,Predicate):- 
	in_predicate(Ts1,Ts2,Predicate), !.
predicate(Ts1,Ts2,Predicate):- 
	like_predicate(Ts1,Ts2,Predicate), !.
predicate(Ts1,Ts2,Predicate):- 
	startswith_predicate(Ts1,Ts2,Predicate), !.
predicate(Ts1,Ts2,Predicate):- 
	null_predicate(Ts1,Ts2,Predicate), !.
predicate(Ts1,Ts2,Predicate):- 
	istype_predicate(Ts1,Ts2,Predicate), !.


/*===== 8.2 <comparison predicate>. =====*/

comparison_predicate(Ts1,Ts4,comparison(any,Operator,ValueExpr1,ValueExpr2)):- 
	value_expression(Ts1,Ts2,ValueExpr1), 
	comp_op(Ts2,Ts3,Operator), 
	value_expression(Ts3,Ts4,ValueExpr2), !.

comp_op(Ts1,Ts2,equal):- 
	terminal(Ts1,'=',Ts2), !.
comp_op(Ts1,Ts2,notEqual):- 
	terminal(Ts1,'<>',Ts2), !.
comp_op(Ts1,Ts2,lessThan):- 
	terminal(Ts1,'<',Ts2), !.
comp_op(Ts1,Ts2,greaterThan):- 
	terminal(Ts1,'>',Ts2), !.
comp_op(Ts1,Ts2,lessThanOrEqual):- 
	terminal(Ts1,'<=',Ts2), !.
comp_op(Ts1,Ts2,greaterThanOrEqual):- 
	terminal(Ts1,'>=',Ts2), !.


/*===== 8.3 <between predicate>. =====*/


/*===== 8.4 <in predicate>. =====*/

in_predicate(Ts1,Ts5,Cond):- 
	value_expression(Ts1,Ts2,ValueExpr), 
	in_predicate2(Ts2,Ts3,Neg), 
	terminal(Ts3,'<IN>',Ts4), 
	in_predicate_value(Ts4,Ts5,ValueList), 
	concat_not(Neg,inPredicate(ValueExpr,ValueList),Cond), !.

in_predicate2(Ts1,Ts2,not):- 
	terminal(Ts1,'<NOT>',Ts2), !.
in_predicate2(Ts,Ts,noNot):- !.

/***071206
in_predicate_value(Ts1,Ts2,ValueList):- 
	table_subquery(Ts1,Ts2,ValueList), !.
***/
in_predicate_value(Ts1,Ts4,ValueList):- 
	terminal(Ts1,'(',Ts2), 
	in_value_list(Ts2,Ts3,ValueList), 
	terminal(Ts3,')',Ts4), !.

in_value_list(Ts1,Ts3,[ValueExpr|VEs]):- 
	value_expression(Ts1,Ts2,ValueExpr), 
	in_value_list2(Ts2,Ts3,VEs), !.

in_value_list2(Ts1,Ts4,[ValueExpr|VEs]):- 
	terminal(Ts1,',',Ts2), 
	value_expression(Ts2,Ts3,ValueExpr), 
	in_value_list2(Ts3,Ts4,VEs), !.
in_value_list2(Ts,Ts,[]):- !.

concat_not(not,Cond,operation(logical,not,Cond)):- !.
concat_not(noNot,Cond,Cond):- !.


/*===== 8.5 <like predicate>. =====*/

like_predicate(Ts1,Ts6,Cond):- 
	match_value(Ts1,Ts2,ValueExpr), 
	like_predicate2(Ts2,Ts3,Neg), 
	terminal(Ts3,'<LIKE>',Ts4), 
	pattern(Ts4,Ts5,Pattern), 
	like_predicate3(Ts5,Ts6,ValueExpr,Pattern,LogFunc), 
	concat_not(Neg,LogFunc,Cond), !.

like_predicate2(Ts1,Ts2,not):- 
	terminal(Ts1,'<NOT>',Ts2), !.
like_predicate2(Ts,Ts,noNot):- !.

like_predicate3(Ts1,Ts2,ValueExpr,Pattern,comparison(any,like,ValueExpr,Pattern,Escape)):- 
	escape(Ts1,Ts2,Escape), !.
like_predicate3(Ts,Ts,ValueExpr,Pattern,comparison(any,like,ValueExpr,Pattern,Literal)):- 
	empty_string_literal(Literal), !.

match_value(Ts1,Ts2,ValueExpr):- 
	character_value_expression(Ts1,Ts2,ValueExpr), !.

pattern(Ts1,Ts2,Pattern):- 
	character_value_expression(Ts1,Ts2,Pattern), !.

escape(Ts1,Ts3,Escape):- 
	terminal(Ts1,'<ESCAPE>',Ts2), !, 
	character_value_expression(Ts2,Ts3,Escape), !.


/*===== Starts-with predicate (not SQL92). =====*/

startswith_predicate(Ts1,Ts5,operation(logical,and,Comp1,Comp2)):- 
	match_value(Ts1,Ts2,ValueExpr), 
	terminal(Ts2,'<STARTS>',Ts3), 
	terminal(Ts3,'<WITH>',Ts4), 
	pattern(Ts4,Ts5,Pattern), 
	Comp1 = comparison(any,greaterThanOrEqual,ValueExpr,Pattern), 
	Comp2 = comparison(any,lessThan,ValueExpr,operation(string,addMaxChar,Pattern)), !.


/*===== 8.6 <null predicate>. =====*/

null_predicate(Ts1,Ts4,comparison(any,Operator,ValueExpr,Literal)):- 
	value_expression(Ts1,Ts2,ValueExpr), 
	is_operator(Ts2,Ts3,Operator), 
	null_literal(Ts3,Ts4,Literal), !.

is_operator(Ts1,Ts3,isNot):- 
	terminal(Ts1,'<IS>',Ts2), 
	terminal(Ts2,'<NOT>',Ts3), !.
is_operator(Ts1,Ts2,is):- 
	terminal(Ts1,'<IS>',Ts2), !.


/*===== Is-type predicate (not SQL92). =====*/

istype_predicate(Ts1,Ts4,isTypePredicate(Operator,ValueExpr,TypeExpr)):- 
	value_expression(Ts1,Ts2,ValueExpr), 
	not_variable(ValueExpr), 
	is_operator(Ts2,Ts3,Operator), 
	type_expression(Ts3,Ts4,TypeExpr), !.

not_variable(variable(_,_)):- !, fail.
not_variable(_):- !.

type_expression(Ts1,Ts2,literal(type,Type)):- 
	type_name(Ts1,Ts2,Type), !.
type_expression(Ts1,Ts2,Variable):- 
	variable(Ts1,Ts2,Variable), !.


/*===== 8.12 <search condition>. =====*/

search_condition(Ts1,Ts3,Cond3):- 
	boolean_term(Ts1,Ts2,Cond1), 
	search_condition2(Ts2,Ts3,Cond2), 
	concat_cond(Cond1,or,Cond2,Cond3), !.

search_condition2(Ts1,Ts3,Cond):- 
	terminal(Ts1,'<OR>',Ts2), !, 
	search_condition(Ts2,Ts3,Cond), !.
search_condition2(Ts,Ts,noCond):- !.

concat_cond(Cond,_,noCond,Cond):- !.
concat_cond(Cond1,Op,Cond2,operation(logical,Op,Cond1,Cond2)):- !.

boolean_term(Ts1,Ts3,Cond3):- 
	boolean_factor(Ts1,Ts2,Cond1), 
	boolean_term2(Ts2,Ts3,Cond2), 
	concat_cond(Cond1,and,Cond2,Cond3), !.

boolean_term2(Ts1,Ts3,Cond):- 
	terminal(Ts1,'<AND>',Ts2), !, 
	boolean_term(Ts2,Ts3,Cond), !.
boolean_term2(Ts,Ts,noCond):- !.

boolean_factor(Ts1,Ts3,Cond3):- 
	boolean_factor2(Ts1,Ts2,Cond1), 
	boolean_test(Ts2,Ts3,Cond2), 
	concat_not(Cond1,Cond2,Cond3), !.

boolean_factor2(Ts1,Ts2,not):- 
	terminal(Ts1,'<NOT>',Ts2), !.
boolean_factor2(Ts,Ts,noNot):- !.

boolean_test(Ts1,Ts3,Cond2):- 
	boolean_primary(Ts1,Ts2,Cond1), 
	boolean_test2(Ts2,Ts3,Cond1,Cond2), !.

/*** 
* No support of Boolean test operator IS (071213).
boolean_test2(Ts1,Ts4,Cond1,Cond2):- 
	terminal(Ts1,'<IS>',Ts2), !, 
	boolean_test3(Ts2,Ts3,Neg), 
	truth_value(Ts3,Ts4,TruthValue), 
	concat_not(Neg,operation(logical,is,Cond1,TruthValue),Cond2), !.
***/
boolean_test2(Ts,Ts,Cond,Cond):- !.

boolean_test3(Ts1,Ts2,not):- 
	terminal(Ts1,'<NOT>',Ts2), !.
boolean_test3(Ts,Ts,noNot):- !.

boolean_primary(Ts1,Ts2,Cond):- 
	predicate(Ts1,Ts2,Cond), !.
boolean_primary(Ts1,Ts4,Cond):- 
	terminal(Ts1,'(',Ts2), 
	search_condition(Ts2,Ts3,Cond), 
	terminal(Ts3,')',Ts4), !.
boolean_primary(Ts1,Ts2,literal(logical,TruthValue)):- 
	truth_value(Ts1,Ts2,TruthValue), !.

truth_value(Ts1,Ts2,true):- 
	terminal(Ts1,'<TRUE>',Ts2), !.
truth_value(Ts1,Ts2,false):- 
	terminal(Ts1,'<FALSE>',Ts2), !.
truth_value(Ts1,Ts2,unknown):- 
	terminal(Ts1,'<UNKNOWN>',Ts2), !.


/*===== 13 Data manipulation. =====*/

/*===== 13.1 <declare cursor>. =====*/

cursor_specification(Ts1,Ts3,CursorSpec):- 
	query_expression(Ts1,Ts2,select1(Quant,Columns,From,Where,GroupBy,Having,[],noFetchSpec,[])), 
	cursor_specification2(Ts2,Ts3,SortSpecList), 
	CursorSpec = select1(Quant,Columns,From,Where,GroupBy,Having,SortSpecList,noFetchSpec,[]), !.

cursor_specification2(Ts1,Ts2,SortSpecList):- 
	orderby_clause(Ts1,Ts2,SortSpecList), !.
cursor_specification2(Ts,Ts,[]):- !.

orderby_clause(Ts1,Ts4,SortSpecList):-
	terminal(Ts1,'<ORDER>',Ts2), 
	terminal(Ts2,'<BY>',Ts3), 
	sort_specification_list(Ts3,Ts4,SortSpecList), !.

sort_specification_list(Ts1,Ts2,[random]):- 
	terminal(Ts1,'<RANDOM>',Ts2), !.
sort_specification_list(Ts1,Ts3,[SortSpec|SSs]):- 
	sort_specification(Ts1,Ts2,SortSpec), 
	sort_specification_list2(Ts2,Ts3,SSs), !.

sort_specification_list2(Ts1,Ts4,[SortSpec|SSs]):- 
	terminal(Ts1,',',Ts2), !, 
	sort_specification(Ts2,Ts3,SortSpec), 
	sort_specification_list2(Ts3,Ts4,SSs), !.
sort_specification_list2(Ts,Ts,[]):- !.

sort_specification(Ts1,Ts3,sortSpec(any,ValueExpr,SortOrder)):- 
	sort_key(Ts1,Ts2,ValueExpr), 
	sort_specification2(Ts2,Ts3,SortOrder), !.

sort_specification2(Ts1,Ts2,SortOrder):- 
	ordering_specification(Ts1,Ts2,SortOrder), !.
sort_specification2(Ts,Ts,asc):- !.

/***080409
sort_key(Ts1,Ts2,Name):- 
	column_name(Ts1,Ts2,Name), 
	not_dot_control(Ts2), !.
***/
sort_key(Ts1,Ts2,ValueExpr):- 
	value_expression(Ts1,Ts2,ValueExpr), !.	

ordering_specification(Ts1,Ts2,asc):- 
	terminal(Ts1,'<ASC>',Ts2), !.
ordering_specification(Ts1,Ts2,desc):- 
	terminal(Ts1,'<DESC>',Ts2), !.

not_dot_control(Ts):- 
	terminal(Ts,'.',_), !, 
	fail.
not_dot_control(_):- !.


/*===== Fetch specification (not SQL92, but SQL2008). =====*/

fetch_specification(Ts1,Ts3,Tree):- 
	cursor_specification(Ts1,Ts2,select1(Quant,Columns,From,Where,GroupBy,Having,SortSpec,noFetchSpec,[])), 
	fetch_specification2(Ts2,Ts3,FetchSpec), 
	Tree = select1(Quant,Columns,From,Where,GroupBy,Having,SortSpec,FetchSpec,[]), !.

fetch_specification2(Ts1,Ts3,fetch(Number,Offset)):- 
	fetch_number(Ts1,Ts2,Number), 
	fetch_offset(Ts2,Ts3,Offset), !. 


fetch_number(Ts1,Ts5,Number):- 
	fetch_number2(Ts1,Ts2), !, 
	fetch_number4(Ts2,Ts3,Number), 
	fetch_number5(Ts3,Ts4), 
	fetch_number6(Ts4,Ts5), !.
fetch_number(Ts,Ts,noNumber):- !.

fetch_number2(Ts1,Ts2):- 
	terminal(Ts1,'<LIMIT>',Ts2), !.
fetch_number2(Ts1,Ts3):- 
	terminal(Ts1,'<FETCH>',Ts2), !, 
	fetch_number3(Ts2,Ts3), !.

fetch_number3(Ts1,Ts2):- 
	terminal(Ts1,'<FIRST>',Ts2), !.
fetch_number3(Ts,Ts):- !.

fetch_number4(Ts1,Ts2,Literal):-
	literal(Ts1,Ts2,Literal), !.
fetch_number4(Ts1,Ts2,variable(numerical,Num)):-
	variable(Ts1,Ts2,variable(any,Num)), !.

fetch_number5(Ts1,Ts2):- 
	terminal(Ts1,'<ROWS>',Ts2), !.
fetch_number5(Ts,Ts):- !.

fetch_number6(Ts1,Ts2):- 
	terminal(Ts1,'<ONLY>',Ts2), !.
fetch_number6(Ts,Ts):- !.


fetch_offset(Ts1,Ts3,Offset):- 
	fetch_offset2(Ts1,Ts2,Type), !, 
	fetch_offset3(Ts2,Ts3,Type,Offset), !. 
fetch_offset(Ts,Ts,noOffset):- !. 

fetch_offset2(Ts1,Ts2,numerical):- 
	terminal(Ts1,'<OFFSET>',Ts2), !.
fetch_offset2(Ts1,Ts2,binary):- 
	terminal(Ts1,'<OFFSETKEY>',Ts2), !.

fetch_offset3(Ts1,Ts2,numerical,Literal):-
	literal(Ts1,Ts2,Literal), !.
fetch_offset3(Ts1,Ts2,binary,key(Literal)):-
	literal(Ts1,Ts2,Literal), !.
fetch_offset3(Ts1,Ts2,Type,variable(Type,Num)):-
	variable(Ts1,Ts2,variable(any,Num)), !.


/*===== Hints (not SQL92). =====*/

hint_specification(Ts1,Ts3,Tree):- 
	fetch_specification(Ts1,Ts2,select1(Quant,Columns,From,Where,GroupBy,Having,SortSpec,FetchSpec,[])), 
	hint_specification2(Ts2,Ts3,Hints), 
	Tree = select1(Quant,Columns,From,Where,GroupBy,Having,SortSpec,FetchSpec,Hints), !.

hint_specification2(Ts1,Ts3,Hints):- 
	terminal(Ts1,'<OPTION>',Ts2), 
	hint_specification3(Ts2,Ts3,Hints,[]), !.
hint_specification2(Ts,Ts,[]):- !.

hint_specification3(Ts1,Ts3,Hints1,Hints3):- 
	single_hint(Ts1,Ts2,Hints1,Hints2), 
	hint_specification4(Ts2,Ts3,Hints2,Hints3), !. 

hint_specification4(Ts1,Ts4,Hints1,Hints3):- 
	terminal(Ts1,',',Ts2), 
	single_hint(Ts2,Ts3,Hints1,Hints2), 
	hint_specification4(Ts3,Ts4,Hints2,Hints3), !.
hint_specification4(Ts,Ts,Hints,Hints):- !.

single_hint(Ts1,Ts2,Hints1,Hints2):- 
	index_hint(Ts1,Ts2,Hints1,Hints2), !.
single_hint(Ts1,Ts2,Hints1,Hints2):- 
	joinorder_hint(Ts1,Ts2,Hints1,Hints2), !.


index_hint(Ts1,Ts6,[indexHint(alias(any,ExtentAlias),indexName(IndexName))|Hs],Hs):- 
	terminal(Ts1,'<INDEX>',Ts2), 
	terminal(Ts2,'(',Ts3), 
	identifier(Ts3,Ts4,id(ExtentAlias)), 
	identifier(Ts4,Ts5,id(IndexName)), 
	terminal(Ts5,')',Ts6), !.


joinorder_hint(Ts1,Ts4,[joinOrderHint(IdList)|Hs],Hs):- 
	terminal(Ts1,'<JOIN>',Ts2), 
	terminal(Ts2,'<ORDER>',Ts3),
	joinorder_hint2(Ts3,Ts4,IdList), !.

/*** 101004
joinorder_hint2(Ts1,Ts2,fixed):- 
	terminal(Ts1,'<FIXED>',Ts2), !.
***/
joinorder_hint2(Ts1,Ts5,[alias(any,Name)|Ids]):- 
	terminal(Ts1,'(',Ts2), 
	identifier(Ts2,Ts3,id(Name)), 
	joinorder_hint3(Ts3,Ts4,Ids), 
	terminal(Ts4,')',Ts5), !.

joinorder_hint3(Ts1,Ts4,[alias(any,Name)|Ids]):- 
	terminal(Ts1,',',Ts2), 
	identifier(Ts2,Ts3,id(Name)), 
	joinorder_hint3(Ts3,Ts4,Ids), !.
joinorder_hint3(Ts,Ts,[]):- !.


/*===== Library predicates. =====*/

/* idlist_to_atom(+IdList,-Atom):- 
*	Concatenates a list IdList of identifiers to a single atom Atom. 
*/
idlist_to_atom([id(Atom1)|Ids],Atom2):- 
	idlist_to_atom2(Atom1,Ids,Atom2), !.

idlist_to_atom2(Atom1,[id(Atom2)|Ids],Atom5):- 
	atom_concat(Atom1,'.',Atom3), 
	atom_concat(Atom3,Atom2,Atom4), 
	idlist_to_atom2(Atom4,Ids,Atom5), !.
idlist_to_atom2(Atom,[],Atom):- !.




