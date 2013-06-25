

/* SQL, Peter Idestam-Almquist, Starcounter, 2013-06-04. */

/* A query processor of SQL-queries. */

/*** Modifications in interface: ***/

/* 13-04-15: Added Database parameter to delete_schemainfo and process_version_and_delete_schemainfo. */
/* 13-04-12: Added Database parameter to schemafile, class, extension, property, method, gmethod. */
/* 13-02-04: Added support of offset not only offsetkey in fetch clause. */
/* 13-01-28: Added condition isTypePredicate/3. */
/* 12-10-02: Added methods: add_schemainfo/1, load_schemainfo/1, delete_schemainfo/0 and current_schemafiles/1. */
/* 11-11-30: Added fetch specification to select2 and select3. */
/* 11-03-28: Identifiers in schema file are both in upper case and normal case letters. */
/* 11-01-18: Identifiers in schema file are in upper case letters. */
/* 10-10-06: Added a parameter that returns the number of variables in the query. */
/* 09-08-20: The main output is a parsed rewritten controlled tree structure instead of an execution plan. */
/* 09-02-16: Added info about short class and extensions names in schema file. */
/* 09-01-26: Added string operation addMaxChar to better support STARTS WITH. */

/***/

/* 12-10-17: Added method: process_version_and_delete_schemainfo/1. */
/* 12-10-02: Support of dynamic schema updates. */
/* 11-03-28: Updated schema format. */
/* 11-02-24: Fixed problem with too slow response when incorrect syntax. */
/* 11-01-18: Made identifiers case insensitive. */
/* 11-01-18: Added binary literal. */
/* 11-01-18: Added control in modifier that operator is not 'is'. */
/* 10-09-21: Numbering variables. */
/* 10-09-16: Added a modifyer that replaces path expressions with joins. */
/* 10-05-24: Modifications of the platform configuration of comparison types. */
/* 10-05-10: New representation of variable by '?'. */
/* 10-01-11: Support of index hints by name, and support of previuos index hints removed. */
/* 09-08-20: Removed optimization module. */
/* 09-02-16: Support of short class names (modified schema file). */
/* 08-07-10: Added support of datatype Binary. */
/* 08-03-13: Added support of cast-operation. */
/* 07-10-19: Replaced verify_schema/1 with verify/2. */
/* 07-10-09: Added accepted_hosts in start/1. */
/* 07-09-28: Added verify_schema/1. */
/* 07-08-30: Added kill/0 which halts the execution (kills the process). */
/* 07-04-18: Schema filename as argument to standalone-executable. */
/* 07-03-30: Port as argument to standalone-executable. */
/* 07-01-25: Replaced strSortSpec(Expr,SortOrder) with sortSpec(string,Expr,SortOrder) etc. */
/* 07-01-25: Replaced strSetFunction(Func,Quant,Expr) with setFunction(string,Func,Quant,Expr) etc. */
/* 07-01-25: Replaced strGMethod(Num,GMethod) with path(string,Num,[],GMethod) etc. */
/* 07-01-25: Replaced strMethod(Num,Method) with path(string,Num,[],Method) etc. */
/* 07-01-25: Replaced strProperty(Num,Prop) with path(string,Num,[],Prop) etc. */
/* 07-01-25: Replaced strPath(Num,RestPath,Element) with path(string,Num,RestPath,Element) etc. */
/* 07-01-25: Replaced objThis(Num,Type) with this(object(Type),Num). */
/* 07-01-24: Replaced strComparison(Op,Expr1,Expr2) with comparison(string,Op,Expr1,Expr2) etc. */
/* 07-01-24: Replaced strOperation(Op,Expr1,Expr2) with operation(string,Op,Expr1,Expr2) etc. */
/* 07-01-24: Replaced strVariable(Variable) with variable(string,Variable) etc. */
/* 07-01-24: Replaced strLiteral(Literal) with literal(string,Literal) etc. */
/* 07-01-10: nestedLoopJoin/3 is replaced by join/3. */

:- module(sql,[]).

:- use_module(library(prologbeans)).

:- multifile schemafile/1, class/3, extension/3, property/4, method/5, gmethod/6.
:- dynamic schemafile/1, class/3, extension/3, property/4, method/5, gmethod/6.

:- load_files([tokenizer,parser,rewriter,controller,modifyer,platform]).

sql_version('130604').


/*===== Stand-alone executable. =====*/

/* Register acceptable queries and start the server (>StarcounterSQL.exe Port SchemaFile). */

user:runtime_entry(start):-
	register_query(process_version_prolog(Version),process_version(Version)), 
	register_query(kill_prolog,kill), 
	register_query(add_schemainfo_prolog(SchemaInfo),add_schemainfo(SchemaInfo)), 
	register_query(load_schemainfo_prolog(SchemaFile),load_schemainfo(SchemaFile)), 
	register_query(current_schemafiles_prolog(SchemaFileList),current_schemafiles(SchemaFileList)), 
	register_query(delete_schemainfo_prolog(Database),delete_schemainfo(Database)), 
	register_query(process_version_and_delete_schemainfo_prolog(Database,Version),process_version_and_delete_schemainfo(Database,Version)), 
	register_query(sql_prolog(Database,Query,TypeDef,Tree,VarNum,ErrList),sql(Database,Query,TypeDef,Tree,VarNum,ErrList)), 
	prolog_flag(argv,[Arg1|_]), 
	atom_codes(Arg1,NumCodes), 
	number_codes(Num,NumCodes), 
	start([port(Num),accepted_hosts(['127.0.0.1'])]), !.


replace([Code1|Cs1],Code1,Code2,[Code2|Cs2]):- !, 
	replace(Cs1,Code1,Code2,Cs2), !.
replace([Code1|Cs1],Code2,Code3,[Code1|Cs2]):- 
	replace(Cs1,Code2,Code3,Cs2), !.
replace([],_,_,[]):- !.


process_version_and_delete_schemainfo(DatabaseCodes,Version):- 
	process_version(Version), 
	delete_schemainfo(DatabaseCodes), !.

process_version(Version):-
	sql_version(Version), !.

kill:- shutdown.

load_schemainfo(FilePathCodes):- 
	atom_codes(FilePathAtom,FilePathCodes), 
	compile(FilePathAtom), !.

current_schemafiles(FileNameBag):- 
	findall(FileName,schemafile(_,FileName),FileNameBag), !.

delete_schemainfo(DatabaseCodes):- 
	atom_codes(DatabaseAtom,DatabaseCodes), 
	retractall(schemafile(DatabaseAtom,_)), 
	retractall(class(DatabaseAtom,_,_,_)), 
	retractall(extension(DatabaseAtom,_,_,_)), 
	retractall(property(DatabaseAtom,_,_,_,_)), 
	retractall(method(DatabaseAtom,_,_,_,_,_)), 
	retractall(gmethod(DatabaseAtom,_,_,_,_,_,_)), !.

load_schema(SchemaFile):- 
	compile(SchemaFile), !.


/*===== Process SQL query. =====*/

/* sql(+Database,+Query,-TypeDef,-Tree,-VarNum,-ErrList):-
*	Creates a composite type definition TypeDef and a controlled tree Tree 
*	from the query-string (a list of ASCII-codes) Query. 
*/
sql(DatabaseCodes,Query,TypeDef,Tree,VarNum,ErrList):- 
	atom_codes(DatabaseAtom,DatabaseCodes), 
	sql2(DatabaseAtom,Query,_,_,_,_,_,Tree,_,TypeDef,VarNum,ErrList), !.

sql2(Database,Query,Tokens,Parse,Tree1,Tree2,Tree3,Tree4,TypeDef1,TypeDef2,VarNum,Err6):- 
	tokenizer:tokenize(Query,Tokens,VarNum,Err1,Err2), 
	parser:parse(Tokens,Parse,Err2,Err3), 
	rewriter:rewrite(Parse,Tree1,Tree2,Tables,TableNum,Err3,Err4), 
	controller:control(Database,Tree2,Tables,Tree3,TypeDef1,Err4,Err5),
	modifyer:modify(Tree3,TypeDef1,TableNum,Tree4,TypeDef2,Err5,[]), 
	lists:remove_duplicates(Err1,Err6), !.


/*===== Add schema information. =====*/

add_schemainfo(SchemaInfo):- 
	open('C:/Temp/schema1.pl',append,Output), 
	write(Output,'SchemaInfo: '), write(Output,SchemaInfo), nl(Output), 
	add_schemainfo2(SchemaInfo,Output), 
	close(Output), !.


add_schemainfo2([],_):- !.
add_schemainfo2(SchemaInfo1,Output):- 
	get_schemainfo_item(SchemaInfo1,SchemaInfo2,SchemaInfoItem), 
	add_schemainfo_item(SchemaInfoItem,Output), 
	add_schemainfo2(SchemaInfo2,Output), !.

get_schemainfo_item([Code|Cs],Cs,[]):- 
	schemainfo_item_end(Code), !.
get_schemainfo_item([Code|Cs1],Cs2,[[]|Cs3]):- 
	schemainfo_token_separator(Code), 
	get_schemainfo_item(Cs1,Cs2,Cs3), !.
get_schemainfo_item([Code|Cs1],Cs2,[[Code|Cs3]|Cs4]):- 
	get_schemainfo_item(Cs1,Cs2,[Cs3|Cs4]), !.

schemainfo_item_end(59) :- !. /* 59 is the code of ';'. */

schemainfo_token_separator(40):- !. /* 40 is the code of '('. */
schemainfo_token_separator(41):- !. /* 41 is the code of ')'. */
schemainfo_token_separator(44):- !. /* 44 is the code of ','. */

add_schemainfo_item(CodeLists,Output):- 
	schemainfo_tokens(CodeLists,Tokens), 
	Clause =.. Tokens, 
	write(Output,Clause), nl(Output), 
	assert(Clause), !.

schemainfo_tokens([],[]):- !.
schemainfo_tokens([CodeList|CLs],[Token|Ts]):- 
	atom_codes(Token,CodeList), 
	schemainfo_tokens(CLs,Ts), !.


/*===== Datastructure. =====*/

/***

Type-values: any, unknown, numerical, logical, binary, boolean, datetime, decimal, double, integer, object(Type), string, uinteger.

ExtentNum: 0, 1, ...

path(Type,ExtentNum,PathList), 
where PathList is a list of: 
extent(ExtentNum,FullClassName), property(Type,Name), method(Type,Name,ArgList), gmethod(Type,Name,TypeParamList,ArgList).

sortSpec(Type,ValueExpr,SortOrder).

fetchSpec(Limit,Offset).

indexHint(Path,SortOrder).
joinOrderHint(ExtentList).

select1(Quant,Columns,From,Where,GroupBy,Having,SortSpec,FetchSpec,Hints).
select2(Quant,Columns,From,Where,SortSpec,FetchSpec,Hints).
select3(Quant,Columns,From,Where,GroupBy,SetFuncs,Having,TempExtent,SortSpec,FetchSpec,Hints).

join1(JoinType,TableRef1,TableRef2,Cond).
join2(JoinType,TableRef1,TableRef2).

***/


/*===== Schema information. =====*/

/***

schemafile(Database,schemaFilePath).

class(Database,fullClassNameUpper,fullClassName,baseClassName).
class(Database,shortClassNameUpper,fullClassName,baseClassName).

extension(Database,fullClassName,fullExtensionNameUpper,fullExtensionName). 
extension(Database,fullClassName,shortExtensionNameUpper,fullExtensionName). 

property(Database,fullClassName,propertyNameUpper,propertyName,propertyType).

method(Database,fullClassName,methodNameUpper,methodName,argumentTypes,returnType).

gmethod(Database,fullClassName,methodNameUpper,methodName,typeParameters,argumentTypes,returnType).

***/





