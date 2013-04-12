
/* Test, Peter Idestam-Almquist, Starcounter, 2012-10-31. */

:- module(test,[test/1,testfile/0]).

:- load_files([sql,examples]).

:- sql:load_schema('schema.pl').


/*===== Test predicates. =====*/

test(ok):- 
	sql_example(Type,Database,Query),
	test_output(user_output,Type,Database,Query).

testfile:- 
	open('C:/Users/peteria/Documents/_Working/Prolog/Dev/output.txt',write,Out), 
	testfile2(Out), !.

testfile2(Out):- 
	sql_example(Type,Database,Query),
	test_output(Out,Type,Database,Query), 
	fail.
testfile2(Out):- 
	close(Out), !.

test_output(Out,Type,Database,Query):- 
	statistics(runtime,_), 
	sql:sql2(Database,Query,Tokens,Parse,Tree1,Tree2,Tree3,Tree4,TypeDef1,TypeDef2,VarNum,ErrList), 
	statistics(runtime,[_,Runtime]), 
	nl(Out), 
	write(Out,'Runtime: '), 
	write(Out,Runtime), 
	nl(Out), 
	write_query(Out,Type,Database,Query), 
	write_tokens(Out,Tokens), 
	write_output(Out,'Parse tree: ',Parse), 
	write_output(Out,'Rewritten tree 1: ',Tree1), 
	write_output(Out,'Rewritten tree 2: ',Tree2), 
	write_output(Out,'Controlled tree: ',Tree3),
	write_output(Out,'Modified tree: ',Tree4),
 	write_output(Out,'Type definition: ',TypeDef1), 
 	write_output(Out,'Modified type definition: ',TypeDef2), 
 	write_output(Out,'Number of variables: ',VarNum), 
	write_output(Out,'Errors: ',ErrList), !.


/*===== Present output. =====*/

write_query(Out,Type,Database,Query):- 
	write(Out,'Type: '), 
	write(Out,Type), 
	nl(Out), 
	write(Out,'Database: '), 
	write(Out,Database), 
	nl(Out), 
	write(Out,'Query: '), 
	nl(Out), 
	atom_codes(Atom,Query), 
	write(Out,Atom), 
	nl(Out), 
	nl(Out), !.

write_tokens(Out,Tokens):- 
	write(Out,'Tokens: '), 
	nl(Out), 
	write_tokens2(Out,1,Tokens), 
	nl(Out), !.

write_tokens2(Out,Num1,[]):- 
	Num2 is Num1 + 13, 
	write_tokens_nl(Out,Num2,_), !.
write_tokens2(Out,Num1,[Token|Ts]):- 
	write_token(Out,Token), 
	write_tokens_nl(Out,Num1,Num2), 
	write_tokens2(Out,Num2,Ts), !.

write_token(Out,Token):- 
	write(Out,'['), 
	write(Out,Token), 
	write(Out,']'), !.

write_tokens_nl(Out,Num,1):- 
	Num >= 15, 
	nl(Out), !.
write_tokens_nl(_,Num1,Num2):- 
	Num1 < 15, 
	Num2 is Num1 + 1, !.

write_output(Out,Text,Term):- 
	write(Out,Text), 
	nl(Out), 
	write_indent(Out,0,Term), 
	nl(Out), !.

write_indent(Out,Tabs1,Term):- 
	functor(Term,Func,Arity), 
	indent_functor(Func), 
	indent(Out,Tabs1), 
	write(Out,Func), 
	write(Out,'('), 
	nl(Out), 
	Tabs2 is Tabs1 + 1, 
	write_indent_args(Out,Tabs2,1,Arity,Term), 
	indent(Out,Tabs1), 
	write(Out,')'), 
	nl(Out), !.
write_indent(Out,Tabs1,List):-
	is_list(List), 
	indent(Out,Tabs1), 
	write(Out,'['), 
	nl(Out), 
	Tabs2 is Tabs1 + 1, 
	write_indent_list(Out,Tabs2,List), 
	indent(Out,Tabs1), 
	write(Out,']'), 
	nl(Out), !.
write_indent(Out,Tabs,Term):-
	indent(Out,Tabs), 
	write(Out,Term), 
	nl(Out), !.

write_indent_args(Out,Tabs,Num1,Arity,Term):- 
	Num1 =< Arity, 
	arg(Num1,Term,Arg), 
	write_indent(Out,Tabs,Arg), 
	Num2 is Num1 + 1, 
	write_indent_args(Out,Tabs,Num2,Arity,Term), !.
write_indent_args(_,_,Num,Arity,_):- 
	Num > Arity, !.

is_list([_|_]):- !.
is_list([]):- !.

write_indent_list(Out,Tabs,[Term|Ts]):- 
	write_indent(Out,Tabs,Term), 
	write_indent_list(Out,Tabs,Ts), !.
write_indent_list(_,_,[]):- !.

indent(Out,Num1):- 
	Num1 > 0, 
	write(Out,'    '), 
	Num2 is Num1 - 1, 
	indent(Out,Num2), !.
indent(_,0):- !.

indent_functor(aggregation).
indent_functor(as).
indent_functor(comparison).
indent_functor(constraint).
indent_functor(distinct).
indent_functor(extentScan).
indent_functor(grouping).
indent_functor(indexScan).
indent_functor(inPredicate).
indent_functor(join1).
indent_functor(join2).
indent_functor(joinOperation).
indent_functor(map).
indent_functor(operation).
indent_functor(orderby).
indent_functor(range).
indent_functor(refLookup).
indent_functor(select1).
indent_functor(select2).
indent_functor(select3).
indent_functor(setFunction).
indent_functor(sort).
indent_functor(sortSpec).
indent_functor(typeDef).




