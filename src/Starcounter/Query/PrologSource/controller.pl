
/* Controller, Peter Idestam-Almquist, Starcounter, 2013-02-06. */

/* TODO: Control HintSpec in control_aggregation_hintspec/3? */
/* TODO: Separate between likeStatic when pattern is strLiteral and likeDynamic otherwise. */
/* TODO: The first matching method (w.r.t. arguments) is chosen not the best matching method. */

/* 13-02-06: Fixed bug regarding istype_predicate/3. */
/* 13-01-28: Added support of is-type comparison by adding istype_predicate/3. */
/* 11-12-08: Changed fetch number and offset types to numerical (from integer). */
/* 11-11-30: Added fetch specification; modified select2, select3. */
/* 11-03-28: Updated schema format. */
/* 11-01-17: Case insensitive matching of schema identifiers (schema in uppercase).
/* 10-05-23: Fixed so that new variable representation ('?') works for string operations. */
/* 10-05-10: New representation of variable by '?'. */
/* 10-01-11: Support of index hints by name, and support of previuos index hints removed. */
/* 09-09-17: Return group-by-list as a sort-spec-list. */
/* 09-08-31: Support of combined-index hints. */
/* 09-02-24: Support of asterisks '*' in paths in select-list. */
/* 09-02-23: Control that result property names are unique. */
/* 09-02-18: Support of unqualified field identifiers. */
/* 09-02-16: Support of short class names (modified schema file). */
/* 09-01-26: Added string operation addMaxChar to better support STARTS WITH. */
/* 08-07-10: Updated create_comparison_struct/5/6, create_numoperation_struct/4/5. */
/* 08-07-10: Replace data_type(Type) with platform:sqltype(Type). */
/* 08-03-13: Replace property(cast(Type1,Type2),Name) with cast(property(Type1,Name),Type2) etc. */
/* 08-03-07: Support of object reference casting (cast(p.Father as Example.Employee).Manager). */
/* 08-02-29: Added class-hierarchy-info in schema file class(className,baseClassName). */
/* 07-12-07: Removed support of nested selects. */
/* 07-12-05: Support of hints. */
/* 07-09-28: Added schema_time/1 in schema file. */
/* 07-05-30: Fixed bug in type control of aggregations. */
/* 07-05-28: path(Type,Path). */
/* 07-04-22: path(Type,ExtNum,Path), method(Type,Name,Args), gmethod(Type,Name,TParams,Args), property(Type,Name). */
/* 06-10-10: New representation of object reference types object(Type). */
/* 06-10-10: Removal of type control of object reference comparisons. */
/* 06-10-23: Better type control of set-functions. */

:- module(controller,[]).

:- use_module(library(ordsets)).


/*===== Main. =====*/

/* control(+Tree1,+Tables,-Tree2,-TypeDef,-ErrHead,-ErrTail):- 
*	Controls that all referenced tables and columns exist, and that all operands are 
*	of appropriate types.
*	Also returns a difference list ErrHead-ErrTail of errors.
*/
control(noTree,[],noTree,noTypeDef,Err,Err):- !.
control(Tree1,Tables,Tree2,TypeDef,Err1,Err4):- 
	control_aggregation_select(Tree1,Err1,Err2), 
	control_select(Tree1,Tree2,Err2,Err3), 
	create_type_definition(Tree2,Tables,TypeDef,Err3,Err4), !.
control(_,_,noTree,noTree,['Unknown error in module controller.'|Err],Err):- !.


/*===== Control aggregation. =====*/

/* control_aggregation_xxx(+Expr,-ErrHead,-ErrTail):- 
*	Controls that all external column references in aggregations are to temp tables. 
*	Also returns a difference list ErrHead-ErrTail of errors.
*/

/* Traverse select. */

control_aggregation_select(select2(_,_,_,_,_,_,_),Err,Err):- !.
control_aggregation_select(select3(_,Columns,_,_,_,_,Having,_,SortSpec,_,_),Err1,Err4):- 
	control_aggregation_column_list(Columns,Err1,Err2), 
	control_aggregation_column_condition(Having,Err2,Err3), 
	control_aggregation_sortspec_list(SortSpec,Err3,Err4), !.

/* Traverse condition. */

control_aggregation_condition(literal(logical,_),Err,Err):- !.
control_aggregation_condition(operation(logical,_,Cond1,Cond2),Err1,Err3):- 
	control_aggregation_condition(Cond1,Err1,Err2), 
	control_aggregation_condition(Cond2,Err2,Err3), !.
control_aggregation_condition(operation(logical,_,Cond),Err1,Err2):- 
	control_aggregation_condition(Cond,Err1,Err2), !.
control_aggregation_condition(comparison(any,_,_,_),Err,Err):- !.
control_aggregation_condition(comparison(any,_,_,_,_),Err,Err):- !.
control_aggregation_condition(inPredicate(_,ValueList),Err1,Err2):- 
	control_aggregation_inpredicate(ValueList,Err1,Err2), !.
control_aggregation_condition(isTypePredicate(_,_,_),Err,Err):- !.

/* Traverse column_list. */

control_aggregation_column_list([Column|Cs],Err1,Err3):- 
	control_aggregation_column(Column,Err1,Err2), 
	control_aggregation_column_list(Cs,Err2,Err3), !.
control_aggregation_column_list([],Err,Err):- !.
control_aggregation_column_list(_,['Incorrect select-list w.r.t. group-by-clause.'|Err],Err):- !.

/* Traverse column. */

control_aggregation_column(as(Expr,_),Err1,Err2):- !, 
	control_aggregation_valueexpr(Expr,Err1,Err2), !.
control_aggregation_column(Expr,Err1,Err2):- 
	control_aggregation_valueexpr(Expr,Err1,Err2), !.

/* Traverse column_condition. */

control_aggregation_column_condition(literal(logical,_),Err,Err):- !.
control_aggregation_column_condition(operation(logical,_,Cond1,Cond2),Err1,Err3):- 
	control_aggregation_column_condition(Cond1,Err1,Err2), 
	control_aggregation_column_condition(Cond2,Err2,Err3), !.
control_aggregation_column_condition(operation(logical,_,Cond),Err1,Err2):- 
	control_aggregation_column_condition(Cond,Err1,Err2), !.
control_aggregation_column_condition(comparison(any,_,Expr1,Expr2),Err1,Err3):- 
	control_aggregation_valueexpr(Expr1,Err1,Err2), 
	control_aggregation_valueexpr(Expr2,Err2,Err3), !.
control_aggregation_column_condition(inPredicate(Expr,ValueList),Err1,Err3):- 
	control_aggregation_valueexpr(Expr,Err1,Err2), 
	control_aggregation_valueexpr_list(ValueList,Err2,Err3), !.
control_aggregation_column_condition(isTypePredicate(_,Expr,_),Err1,Err2):- 
	control_aggregation_valueexpr(Expr,Err1,Err2), !.
control_aggregation_column_condition(_,['Incorrect having-clause w.r.t. group-by-clause.'|Err],Err):- !.

/* Traverse sortspec_list. */

control_aggregation_sortspec_list([SortSpec|SSs],Err1,Err3):- 
	control_aggregation_sortspec(SortSpec,Err1,Err2), 
	control_aggregation_sortspec_list(SSs,Err2,Err3), !.
control_aggregation_sortspec_list([],Err,Err):- !.
control_aggregation_sortspec_list(_,['Incorrect order-by-clause w.r.t. group-by-clause.'|Err],Err):- !.

/* Traverse sortspec. */

control_aggregation_sortspec(sortSpec(any,Expr,_),Err1,Err2):- 
	control_aggregation_valueexpr(Expr,Err1,Err2), !.
control_aggregation_sortspec(random,Err,Err):- !.

/* Traverse valueexpr_list. */

control_aggregation_valueexpr_list([ValueExpr|VEs],Err1,Err3):- 
	control_aggregation_column(ValueExpr,Err1,Err2), 
	control_aggregation_column_list(VEs,Err2,Err3), !.
control_aggregation_valueexpr_list([],Err,Err):- !.

/* Traverse valueexpr. */

control_aggregation_valueexpr(operation(numerical,_,Expr1,Expr2),Err1,Err3):- 
	control_aggregation_valueexpr(Expr1,Err1,Err2), 
	control_aggregation_valueexpr(Expr2,Err2,Err3), !. 
control_aggregation_valueexpr(operation(string,_,Expr1,Expr2),Err1,Err3):- 
	control_aggregation_valueexpr(Expr1,Err1,Err2), 
	control_aggregation_valueexpr(Expr2,Err2,Err3), !. 
control_aggregation_valueexpr(literal(_,_),Err,Err):- !.
control_aggregation_valueexpr(variable(_,_),Err,Err):- !.
control_aggregation_valueexpr(tmpPath(_,_),Err,Err):- !.


/*===== Control identifiers and types. =====*/

/* Also transform format of group-by-list into a sort-spec-list. */

/* Traverse select. */

control_select(select2(Quant,Columns1,From1,Where1,SortSpec1,FetchSpec1,Hints1),Tree,Err1,Err8):-  
	control_select_quantifier(Quant,Err1,Err2), 
	control_column_list(Columns1,Columns2,[],Err2,Err3), 
	control_tableref(From1,From2,Err3,Err4), 
	control_condition(Where1,Where2,Err4,Err5), 
	control_sortspec_list(SortSpec1,SortSpec2,Err5,Err6), 
	control_fetchspec(FetchSpec1,FetchSpec2,Err6,Err7), 
	control_hint_list(Hints1,Hints2,Err7,Err8), 
	Tree = select2(Quant,Columns2,From2,Where2,SortSpec2,FetchSpec2,Hints2), !.

control_select(select3(Quant,Columns1,From1,Where1,GroupBy1,SetFuncs1,Having1,TempExtent,SortSpec1,FetchSpec1,Hints1),Tree,Err1,Err11):- 
	control_select_quantifier(Quant,Err1,Err2), 
	control_column_list(Columns1,Columns2,[],Err2,Err3), 
	control_tableref(From1,From2,Err3,Err4), 
	control_condition(Where1,Where2,Err4,Err5), 
	control_valueexpr_list(GroupBy1,GroupBy2,_,Err5,Err6), 
	control_valueexpr_list(SetFuncs1,SetFuncs2,_,Err6,Err7), 
	control_condition(Having1,Having2,Err7,Err8), 
	control_sortspec_list(SortSpec1,SortSpec2,Err8,Err9), 
	control_fetchspec(FetchSpec1,FetchSpec2,Err9,Err10), 
	control_hint_list(Hints1,Hints2,Err10,Err11), 
	create_sortspec_list(GroupBy2,GroupBy3),
	Tree = select3(Quant,Columns2,From2,Where2,GroupBy3,SetFuncs2,Having2,TempExtent,SortSpec2,FetchSpec2,Hints2), !.

control_select_quantifier(all,Err,Err):- !.
control_select_quantifier(distinct,['The quantifier distinct is not supported.'|Err],Err):- !.

/* Traverse fetchspec. */

control_fetchspec(fetch(Number1,Offset1),fetch(Number2,Offset2),Err1,Err3):- 
	control_fetchspec_number(Number1,Number2,Err1,Err2), 
	control_fetchspec_offset(Offset1,Offset2,Err2,Err3), !.

control_fetchspec_number(noNumber,noNumber,Err,Err):- !.
control_fetchspec_number(variable(Type,Num),variable(Type,Num),Err,Err):- !.
control_fetchspec_number(literal(integer,Value),literal(integer,Value),Err,Err):- !.
control_fetchspec_number(Number,Number,['The number of rows to fetch must be of type integer.'|Err],Err):- !.

control_fetchspec_offset(noOffset,noOffset,Err,Err):- !.
control_fetchspec_offset(variable(Type,Num),variable(Type,Num),Err,Err):- !.
control_fetchspec_offset(key(literal(binary,Value)),literal(binary,Value),Err,Err):- !.
control_fetchspec_offset(key(Literal),Literal,['The fetch offset key must be of type binary.'|Err],Err):- !.
control_fetchspec_offset(literal(integer,Value),literal(integer,Value),Err,Err):- !.
control_fetchspec_offset(Literal,Literal,['The fetch offset must be of type integer.'|Err],Err):- !.

/* Traverse hintlist. */

control_hint_list(Hints1,Hints3,['More than one join-order hint is not allowed.'|Err1],Err2):- 
	lists:select(joinOrderHint(_),Hints1,Hints2), 
	lists:select(joinOrderHint(_),Hints2,_), !, 
	control_hint_list2(Hints1,Hints3,Err1,Err2), !.
control_hint_list(Hints1,Hints2,Err1,Err2):- 
	control_hint_list2(Hints1,Hints2,Err1,Err2), !.

control_hint_list2([Hint1|Hs1],[Hint2|Hs2],Err1,Err3):- 
	control_hint(Hint1,Hint2,Err1,Err2), 
	control_hint_list2(Hs1,Hs2,Err2,Err3), !.
control_hint_list2([],[],Err,Err):- !.

/* Traverse hint. */

control_hint(indexHint(Extent1,indexName(Name)),indexHint(Extent2,indexName(Name)),Err1,Err2):- 
	control_path([Extent1],[Extent2],_,Err1,Err2), !.
control_hint(joinOrderHint(fixed),joinOrderHint(fixed),Err,Err):- !.
control_hint(joinOrderHint(ExtentList1),joinOrderHint(ExtentList2),Err1,Err2):- 
	control_extent_list(ExtentList1,ExtentList2,Err1,Err2), !.

/* Traverse extent_list. */

control_extent_list([Extent1|Es1],[Extent2|Es2],Err1,Err3):- 
	control_path([Extent1],[Extent2],_,Err1,Err2), 
	control_extent_list(Es1,Es2,Err2,Err3), !.
control_extent_list([],[],Err,Err):- !.

/* Traverse sortspec_list. */

control_sortspec_list([SortSpec1|SSs1],[SortSpec2|SSs2],Err1,Err3):- 
	control_sortspec(SortSpec1,SortSpec2,Err1,Err2), 
	control_sortspec_list(SSs1,SSs2,Err2,Err3), !.
control_sortspec_list([],[],Err,Err):- !.

/* Traverse sortspec. */

control_sortspec(sortSpec(any,Expr1,SortOrder),SortSpecStruct,Err1,Err2):- 
	control_valueexpr(Expr1,Expr2,Type,Err1,Err2), 
	create_sortspec_struct(Type,Expr2,SortOrder,SortSpecStruct), !.
control_sortspec(random,random,Err,Err):- !.

/* Traverse column_list. */

/* control_column_list(+ColumnList,-ColumnsHead,-ColumnsTail,-ErrHead,-ErrTail) */
/***090223
control_column_list([Column|Cs1],Cs4,Err1,Err3):- 
	control_column(Column,Cs2,Err1,Err2), 
	control_column_list(Cs1,Cs3,Err2,Err3), 
	lists:append(Cs2,Cs3,Cs4), !.
control_column_list([],[],Err,Err):- !.
***/
control_column_list([Column|Cs1],Cs2,Cs4,Err1,Err3):- 
	control_column(Column,Cs2,Cs3,Err1,Err2), 
	control_column_list(Cs1,Cs3,Cs4,Err2,Err3), !.
control_column_list([],Cs,Cs,Err,Err):- !.

/* Traverse column. */

/***090223
control_column(as(path(many,Path),name(noName)),ColumnList,Err1,Err2):- !, 
	control_path_asterisk(Path,ColumnList,Err1,Err2), !.
control_column(as(Expr1,Name),[as(Expr2,Name)],Err1,Err2):- !, 
	control_valueexpr(Expr1,Expr2,_,Err1,Err2), !.
control_column(Expr1,[Expr2],Err1,Err2):- 
	control_valueexpr(Expr1,Expr2,_,Err1,Err2), !.
***/
control_column(as(path(many,Path),name(noName)),Cs1,Cs2,Err1,Err2):- !, 
	control_path_asterisk(Path,Cs1,Cs2,Err1,Err2), !.
control_column(as(Expr1,Name),[as(Expr2,Name)|Cs],Cs,Err1,Err2):- !, 
	control_valueexpr(Expr1,Expr2,_,Err1,Err2), !.
control_column(Expr1,[Expr2|Cs],Cs,Err1,Err2):- 
	control_valueexpr(Expr1,Expr2,_,Err1,Err2), !.

/* Traverse tableref. */

control_tableref(extent(Num,Id1),extent(Num,Id2),Err1,Err2):- !, 
	control_table(Id1,Id2,Err1,Err2), !.
control_tableref(join2(Type,Table1,Table2),join2(Type,Table3,Table4),Err1,Err4):- !, 
	control_jointype(Type,Err1,Err2), 
	control_tableref(Table1,Table3,Err2,Err3), 
	control_tableref(Table2,Table4,Err3,Err4), !.
control_tableref(QuerySpec1,QuerySpec2,Err1,Err2):- 
	control_queryspec(QuerySpec1,QuerySpec2,_,Err1,Err2), !.


/* Traverse condition. */

control_condition(literal(logical,Value),literal(logical,Value),Err,Err):- !.
control_condition(operation(logical,Operator,Cond1,Cond2),operation(logical,Operator,Cond3,Cond4),Err1,Err3):- 
	control_condition(Cond1,Cond3,Err1,Err2), 
	control_condition(Cond2,Cond4,Err2,Err3), !.
control_condition(operation(logical,Operator,Cond1),operation(logical,Operator,Cond2),Err1,Err2):- 
	control_condition(Cond1,Cond2,Err1,Err2), !.
control_condition(comparison(any,Operator,Expr1,Expr2),CompStruct,Err1,Err4):- 
	control_valueexpr(Expr1,Expr3,Type1,Err1,Err2), 
	control_valueexpr(Expr2,Expr4,Type2,Err2,Err3), 
	control_comparison_types(Operator,Type1,Type2,Type3,Err3,Err4), 
	create_comparison_struct(Type3,Operator,Expr3,Expr4,CompStruct), !.
control_condition(comparison(any,Operator,Expr1,Expr2,Expr3),CompStruct,Err1,Err5):- 
	control_valueexpr(Expr1,Expr4,Type1,Err1,Err2), 
	control_valueexpr(Expr2,Expr5,Type2,Err2,Err3), 
	control_valueexpr(Expr3,Expr6,Type3,Err3,Err4), 
	control_comparison_types(Operator,Type1,Type2,Type3,Type4,Err4,Err5), 
	create_comparison_struct(Type4,Operator,Expr4,Expr5,Expr6,CompStruct), !.
/*** 100923
control_condition(inPredicate(Expr1,ValueList1),inPredicate(Expr2,ValueList2),Err1,Err4):- 
	control_valueexpr(Expr1,Expr2,Type,Err1,Err2), 
	control_inpredicate(ValueList1,ValueList2,Types,Err2,Err3), 
	control_in_types(Type,Types,Err3,Err4), !.
***/
control_condition(inPredicate(Expr,ValueList),inPredicate(Expr,ValueList),Err1,Err2):- 
	Err1 = ['The in-predicate is not supported.'|Err2], !.
control_condition(isTypePredicate(Op,Expr1,TypeExpr1),isTypePredicate(Op,Expr3,TypeExpr2),Err1,Err3):- 
	control_valueexpr(Expr1,Expr2,_,Err1,Err2), 
	control_typeexpr(TypeExpr1,TypeExpr2,Err2,Err3), 
	type_any(Expr2,object('Starcounter.Entity'),Expr3), !.

/* Traverse inpredicate. */

control_inpredicate(ValueList1,ValueList2,Types,Err1,Err2):- 
	control_valueexpr_list(ValueList1,ValueList2,Types,Err1,Err2), !.
control_inpredicate(ValueList1,ValueList2,Types,Err1,Err3):- 
	control_queryspec(ValueList1,ValueList2,Types,Err1,Err2), 
	control_in_query(Types,Err2,Err3), !.

/* Traverse type expression. */

control_typeexpr(variable(any,Num),variable(type,Num),Err,Err):- !.
control_typeexpr(literal(type,Type1),literal(type,Type2),Err1,Err2):- 
	control_table(Type1,Type2,Err1,Err2), !.

/* Traverse valueexpr_list. */

control_valueexpr_list([Expr1|Es1],[Expr2|Es2],[Type|Ts],Err1,Err3):- 
	control_valueexpr(Expr1,Expr2,Type,Err1,Err2), 
	control_valueexpr_list(Es1,Es2,Ts,Err2,Err3), !.
control_valueexpr_list([],[],[],Err,Err):- !.

/* control_valueexpr(+Expr1,-Expr2,-Type,-ErrHead,-ErrTail):- 
*	Controls identifiers and types in value expression Expr1, 
*	replaces some general structures to more specific ones obtaining value expression Expr2, 
*	and returns the type Type of the value expression. 
*	Also returns a difference list ErrHead-ErrTail of errors.
*/
control_valueexpr(operation(numerical,Operator,Expr1,Expr2),NumOpStruct,Type3,Err1,Err4):- 
	control_valueexpr(Expr1,Expr3,Type1,Err1,Err2),
	control_valueexpr(Expr2,Expr4,Type2,Err2,Err3),
	control_operation_types(Operator,Type1,Type2,Type3,Err3,Err4), 
	create_numoperation_struct(Type3,Operator,Expr3,Expr4,NumOpStruct), !.
control_valueexpr(operation(numerical,Operator,Expr1),NumOpStruct,Type2,Err1,Err3):- 
	control_valueexpr(Expr1,Expr2,Type1,Err1,Err2),
	control_operation_types(Operator,Type1,Type2,Err2,Err3), 
	create_numoperation_struct(Type2,Operator,Expr2,NumOpStruct), !.
control_valueexpr(operation(string,Operator,Expr1,Expr2),StrOpStruct,Type3,Err1,Err4):- 
	control_valueexpr(Expr1,Expr3,Type1,Err1,Err2),
	control_valueexpr(Expr2,Expr4,Type2,Err2,Err3),
	control_operation_types(Operator,Type1,Type2,Type3,Err3,Err4), 
	create_stringoperation_struct(Type3,Operator,Expr3,Expr4,StrOpStruct), !.
control_valueexpr(operation(string,Operator,Expr1),StrOpStruct,Type2,Err1,Err3):- 
	control_valueexpr(Expr1,Expr2,Type1,Err1,Err2),
	control_operation_types(Operator,Type1,Type2,Err2,Err3), 
	create_stringoperation_struct(Type2,Operator,Expr2,StrOpStruct), !.
control_valueexpr(setFunction(Function,Quant,Expr1),SetFuncStruct,Type2,Err1,Err3):- 
	control_valueexpr(Expr1,Expr2,Type1,Err1,Err2),
	control_setfunction_types(Function,Type1,Type2,Err2,Err3), 
	create_setfunction_struct(Type2,Function,Quant,Expr2,SetFuncStruct), !.
control_valueexpr(path(any,Path1),PathStruct,Type,Err1,Err2):- 
	control_path(Path1,Path2,Type,Err1,Err2), 
	create_path_struct(Path2,Type,PathStruct), !.
control_valueexpr(tmpPath(TmpPath1,path(any,Path1)),PathStruct,Type2,Err1,Err2):- !, 
	TmpPath1 = [extent(Num,temp),property(any,Id)|TmpRest1], 
	lists:append(PrePath1,TmpRest1,Path1), 
	control_path(PrePath1,PrePath2,Type1,_,[]), 
	control_path(Path1,Path2,Type2,Err1,Err2), 
	lists:append(PrePath2,TmpRest2,Path2), 
	lists:append([extent(Num,temp),property(Type1,Id)],TmpRest2,TmpPath2), 
	create_path_struct(TmpPath2,Type2,PathStruct), !.
control_valueexpr(tmpPath([extent(Num,temp),property(any,Id)],SetFunc),PathStruct,Type,Err1,Err2):- 
	control_valueexpr(SetFunc,_,Type,Err1,Err2), 
	create_path_struct([extent(Num,temp),property(Type,Id)],Type,PathStruct), !.
control_valueexpr(literal(any,null),literal(any,null),any,Err,Err):- !.
control_valueexpr(literal(Type,Literal),literal(Type,Literal),Type,Err,Err):- 
	platform:sqltype(Type), !.
control_valueexpr(variable(any,Num),variable(any,Num),any,Err,Err):- !.

/* control_table(+TableName,-FullTableName,-ErrHead,-ErrTail):- 
*	Returns an error if the table identifier TableName is not included in the schema or if it is ambiguous.
*	The predicate class/3 is specified in separate schema info. 
*/

control_table(Table1,Table2,Err,Err):- 
	get_tablename(Table1,Table2), !.
control_table(Table,Table,[Error|Es],Es):- 
	get_tablename_alternatives(Table,[_,_|_]), 
	list_to_atom(['Ambiguous class ',Table,'.'],Error), !.
control_table(Table,Table,[Error|Es],Es):- 
	list_to_atom(['Unknown class ',Table,'.'],Error), !.

get_tablename(Table1,Table2):- 
	get_tablename_alternatives(Table1,[Table2]), !.

get_tablename_alternatives(Table1,TableList):-
	findall(Table3,(to_upper(Table1,Table2),sql:class(Table2,Table3,_)),TableList), !.


/* control_jointype(+Type,-ErrHead,-ErrTail):- 
*	Returns an error if the join type Type is full (full-outer join), 
*	which are not supported. 
*/
control_jointype(inner,Err,Err):- !.
control_jointype(cross,Err,Err):- !.
control_jointype(left,Err,Err):- !.
control_jointype(right,Err,Err):- !.
control_jointype(Type,[Error|Es],Es):- 
	list_to_atom(['Unsupported join type ',Type,'.'],Error), !.

/* control_path_asterisk(+Path,-ColumnsHead,-ColumnsTail,-ErrHead,-ErrTail):- 
*	Handle the case where the path Path ends with an asterisk.
*/
/* Unqualified asterisk ("SELECT * FROM Extent"). */
control_path_asterisk([extent(unspec,ExtentList),property(many,'*')],Cs1,Cs2,Err1,Err2):- !, 
	control_path_asterisk2(ExtentList,Cs1,Cs2,Err1,Err2), !.	
/* Qualified asterisk ("SELECT e.* FROM Extent"). */
control_path_asterisk(Path,Cs1,Cs2,Err1,Err2):- 
	control_path_asterisk3(Path,Cs1,Cs2,Err1,Err2), !. 

control_path_asterisk2([Extent|Es],Cs1,Cs3,Err1,Err3):- 
	control_path_asterisk3([Extent,property(many,'*')],Cs1,Cs2,Err1,Err2), 
	control_path_asterisk2(Es,Cs2,Cs3,Err2,Err3), !.
control_path_asterisk2([],Cs,Cs,Err,Err):- !.

control_path_asterisk3(Path,Cs1,Cs2,Err1,Err2):- 
	control_path(Path,MultiPath,Type,Err1,Err2), 
	control_path_asterisk4(Type,MultiPath,Cs1,Cs2), !.

control_path_asterisk4(unknown,Path,[as(path(unknown,Path),name(noName))|Cs],Cs):- !.
control_path_asterisk4(many,MultiPath,Cs1,Cs2):- 
	create_column_list(MultiPath,PathList), 
	add_struct_to_all(PathList,Cs1,Cs2), !.

create_column_list([PropList],PathList):- !, 
	create_column_list2(PropList,PathList), !.
create_column_list([Element|Path],PathList2):- 
	create_column_list(Path,PathList1), 
	add_element_to_all(PathList1,Element,PathList2), !.

create_column_list2([property(Type,Name)|PropList],[[Type,property(Type,Name)]|PathList]):- 
	create_column_list2(PropList,PathList), !.
create_column_list2([],[]):- !.

add_element_to_all([[Type|Path]|Ps1],Element,[[Type,Element|Path]|Ps2]):- 
	add_element_to_all(Ps1,Element,Ps2), !.
add_element_to_all([],_,[]):- !.

add_struct_to_all([[Type|Path]|Ps],[as(path(Type,Path),name(noName))|Cs1],Cs2):- 
	add_struct_to_all(Ps,Cs1,Cs2), !.
add_struct_to_all([],Cs,Cs):- !.


/* control_path(+Path1,-Path2,-Type,-ErrHead,-ErrTail):- 
*	Returns an error if the path Path1 includes some identifier not included in the schema. 
*	Also returns an updated path Path2 and the type Type of the path.
*/

/* Unqualified path. */
control_path([extent(unspec,ExtentList),cast(Element1,Type2)|Path1],[Extent,cast(Element2,SqlType2)|Path2],Type4,Err1,Err4):- !, 
	control_path_unspec(Element1,ExtentList,Extent,Element2,Type1,Err1,Err2), 
	get_tablename(Type2,Type3), !, 
	platform:dbtype_to_sqltype(Type1,SqlType1), 
	platform:dbtype_to_sqltype(Type3,SqlType2), 
	control_cast(SqlType1,SqlType2,Err2,Err3), 
	control_path2(Path1,Path2,Type3,Type4,Err3,Err4), !.	
control_path([extent(unspec,ExtentList),Element1|Path1],[Extent,Element2|Path2],Type2,Err1,Err3):- !, 
	control_path_unspec(Element1,ExtentList,Extent,Element2,Type1,Err1,Err2), 
	control_path2(Path1,Path2,Type1,Type2,Err2,Err3), !.

/* Qualified path. */
control_path([Extent1|Path1],[Extent2|Path2],Type2,Err1,Err3):- 
	control_path_extent(Extent1,Extent2,Type1,Err1,Err2), 
	control_path2(Path1,Path2,Type1,Type2,Err2,Err3), !.

control_path2(Path,Path,unknown,unknown,Err,Err):- !.
control_path2([],[],many,many,Err,Err):- !.
control_path2([],[],Type1,Type2,Err,Err):- 
	platform:dbtype_to_sqltype(Type1,Type2), !.
control_path2([Element1|Path1],[Element2|Path2],Type1,Type3,Err1,Err3):- 
	control_path_element(Element1,Element2,Type1,Type2,Err1,Err2), 
	control_path2(Path1,Path2,Type2,Type3,Err2,Err3), !.


control_path_unspec(property(any,Name1),ExtentList,Extent2,property(SqlType2,Name3),Type2,Err1,Err2):- 
	findall((Extent1,Name2,SqlType1,Type1),
		control_path_unspec_property(Name1,ExtentList,Extent1,Name2,SqlType1,Type1),
		Bag), 
	control_path_unspec_property_bag(Bag,Name1,Extent2,Name3,SqlType2,Type2,Err1,Err2), !.

control_path_unspec(gmethod(any,Name1,TParams1,Args1),ExtentList,Extent2,gmethod(SqlType2,Name3,TParams3,Args3),Type2,Err3,Err4):- 
	findall((Extent1,Name2,SqlType1,TParams2,Args2,Type1,Err1,Err2),
		control_path_unspec_gmethod(Name1,TParams1,Args1,ExtentList,Extent1,Name2,SqlType1,TParams2,Args2,Type1,Err1,Err2),
		Bag), 
	control_path_unspec_gmethod_bag(Bag,Name1,TParams1,Args1,Extent2,Name3,SqlType2,TParams3,Args3,Type2,Err3,Err4), !. 

control_path_unspec(method(any,Name1,Args1),ExtentList,Extent2,method(SqlType2,Name3,Args3),Type2,Err3,Err4):- 
	findall((Extent1,Name2,SqlType1,Args2,Type1,Err1,Err2),
		control_path_unspec_method(Name1,Args1,ExtentList,Extent1,Name2,SqlType1,Args2,Type1,Err1,Err2),
		Bag), 
	control_path_unspec_method_bag(Bag,Name1,Args1,Extent2,Name3,SqlType2,Args3,Type2,Err3,Err4), !. 


control_path_unspec_property(Name1,ExtentList,extent(Num,Type1),Name3,SqlType,Type2):- 
	lists:select(extent(Num,Id),ExtentList,_), 
	get_tablename(Id,Type1), 
	to_upper(Name1,Name2), 
	sql:property(Type1,Name2,Name3,Type2), 
	platform:dbtype_to_sqltype(Type2,SqlType).

control_path_unspec_property_bag([(Extent,Name,SqlType,Type)],_,Extent,Name,SqlType,Type,Err,Err):- !.
control_path_unspec_property_bag([_,_|_],Name,extent(unspec,unknown),Name,unknown,unknown,[Error|Err],Err):- 
	list_to_atom(['Ambiguous property ',Name,'.'],Error), !.
control_path_unspec_property_bag([],Name,extent(unspec,unknown),Name,unknown,unknown,[Error|Err],Err):- 
	list_to_atom(['Unknown property or alias ',Name,'.'],Error), !.


control_path_unspec_gmethod(Name1,TParams1,Args1,ExtentList,extent(Num,Type1),Name2,SqlType,TParams2,Args2,Type2,Err1,Err3):- 
	lists:select(extent(Num,Id),ExtentList,_), 
	get_tablename(Id,Type1), 
	control_typeparam_list(Type1,TParams1,TParams2,Err1,Err2), 
	control_valueexpr_list(Args1,Args2,ArgTypes1,Err2,Err3), 
	control_path_unspec_gmethod2(Type1,Name1,TParams2,ArgTypes1,Name2,Type2,SqlType).

control_path_unspec_gmethod2(Type1,Name1,TParams,ArgTypes1,Name3,Type2,SqlType):- 
	to_upper(Name1,Name2), 
	sql:gmethod(Type1,Name2,Name3,TParams,ArgTypes2,Type2), 
	control_method_argument_types(ArgTypes1,ArgTypes2), 
	platform:dbtype_to_sqltype(Type2,SqlType), !.
control_path_unspec_gmethod2(_,Name,_,_,Name,unknown,unknown):- !.

control_path_unspec_gmethod_bag([(Extent,Name,SqlType,TParams,Args,Type,Err1,Err2)],_,_,_,Extent,Name,SqlType,TParams,Args,Type,Err1,Err2):- !. 
control_path_unspec_gmethod_bag([_,_|_],Name,TParams,Args1,extent(unspec,unknown),Name,unknown,TParams,Args2,unknown,[Error|Err1],Err2):- 
	control_valueexpr_list(Args1,Args2,ArgTypes,Err1,Err2), 
	create_typeparam_list(TParams,TPList), 
	create_argument_list(ArgTypes,ArgList), 
	lists:append([Name|TPList],ArgList,MethodList), 
	list_to_atom(MethodList,MethodAtom), 
	list_to_atom(['Ambiguous method ',MethodAtom,'.'],Error), !.
control_path_unspec_gmethod_bag([],Name,TParams,Args1,extent(unspec,unknown),Name,unknown,TParams,Args2,unknown,[Error|Err1],Err2):- 
	control_valueexpr_list(Args1,Args2,ArgTypes,Err1,Err2), 
	create_typeparam_list(TParams,TPList), 
	create_argument_list(ArgTypes,ArgList), 
	lists:append([Name|TPList],ArgList,MethodList), 
	list_to_atom(MethodList,MethodAtom), 
	list_to_atom(['Unknown method ',MethodAtom,'.'],Error), !.


control_path_unspec_method(Name1,Args1,ExtentList,extent(Num,Type1),Name2,SqlType,Args2,Type2,Err1,Err2):- 
	lists:select(extent(Num,Id),ExtentList,_), 
	get_tablename(Id,Type1), 
	control_valueexpr_list(Args1,Args2,ArgTypes1,Err1,Err2), 
	control_path_unspec_method2(Type1,Name1,ArgTypes1,Name2,Type2,SqlType).

control_path_unspec_method2(Type1,Name1,ArgTypes1,Name3,Type2,SqlType):- 
	to_upper(Name1,Name2), 
	sql:method(Type1,Name2,Name3,ArgTypes2,Type2), 
	control_method_argument_types(ArgTypes1,ArgTypes2), 
	platform:dbtype_to_sqltype(Type2,SqlType), !.
control_path_unspec_method2(_,Name,_,Name,unknown,unknown):- !.

control_path_unspec_method_bag([(Extent,Name,SqlType,Args,Type,Err1,Err2)],_,_,Extent,Name,SqlType,Args,Type,Err1,Err2):- !. 
control_path_unspec_method_bag([_,_|_],Name,Args1,extent(unspec,unknown),Name,unknown,Args2,unknown,[Error|Err1],Err2):- 
	control_valueexpr_list(Args1,Args2,ArgTypes,Err1,Err2), 
	create_argument_list(ArgTypes,ArgList), 
	list_to_atom([Name|ArgList],MethodAtom), 
	list_to_atom(['Ambiguous method ',MethodAtom,'.'],Error), !.
control_path_unspec_method_bag([],Name,Args1,extent(unspec,unknown),Name,unknown,Args2,unknown,[Error|Err1],Err2):- 
	control_valueexpr_list(Args1,Args2,ArgTypes,Err1,Err2), 
	create_argument_list(ArgTypes,ArgList), 
	list_to_atom([Name|ArgList],MethodAtom), 
	list_to_atom(['Unknown method ',MethodAtom,'.'],Error), !.


control_path_extent(cast(Extent1,_),cast(Extent2,unknown),unknown,Err1,Err2):- 
	control_path_extent(Extent1,Extent2,Type,Err1,Err2), 
	Type = unknown, !.
control_path_extent(cast(Extent1,Type2),cast(Extent2,SqlType2),Type3,Err1,Err3):- 
	control_path_extent(Extent1,Extent2,Type1,Err1,Err2), 
	get_tablename(Type2,Type3), !, 
	platform:dbtype_to_sqltype(Type1,SqlType1), 
	platform:dbtype_to_sqltype(Type3,SqlType2), 
	control_cast(SqlType1,SqlType2,Err2,Err3), !.
control_path_extent(cast(Extent,Type),cast(Extent,unknown),unknown,[Error|Err],Err):- 
	list_to_atom(['Cast to unknown class ',Type,'.'],Error), !.
control_path_extent(extent(Num,Type1),extent(Num,Type2),Type2,Err1,Err2):- 
	control_table(Type1,Type2,Err1,Err2), !.
control_path_extent(extent(Num,Type),extent(Num,Type),unknown,[Error|Err],Err):- 
	list_to_atom(['Unknown class ',Type,'.'],Error), !.
control_path_extent(alias(Type,Id),alias(Type,Id),unknown,[Error|Err],Err):- 
	list_to_atom(['Unable to resolve alias ',Id,'.'],Error), !.


control_path_element(cast(Element1,_),cast(Element2,unknown),Type1,unknown,Err1,Err2):- 
	control_path_element(Element1,Element2,Type1,Type2,Err1,Err2), 
	Type2 = unknown, !.
control_path_element(cast(Element1,Type3),cast(Element2,SqlType2),Type1,Type4,Err1,Err3):- 
	control_path_element(Element1,Element2,Type1,Type2,Err1,Err2), 
	get_tablename(Type3,Type4), !, 
	platform:dbtype_to_sqltype(Type2,SqlType1), 
	platform:dbtype_to_sqltype(Type4,SqlType2), 
	control_cast(SqlType1,SqlType2,Err2,Err3), !.
control_path_element(cast(Element1,Type3),cast(Element2,unknown),Type1,unknown,Err1,Err3):- 
	control_path_element(Element1,Element2,Type1,Type2,Err1,Err2), 
	platform:dbtype_to_sqltype(Type2,SqlType1), 
	platform:dbtype_to_sqltype(Type3,SqlType2), 
	control_cast(SqlType1,SqlType2,Err2,Err3), !.
control_path_element(property(any,Name1),property(SqlType,Name3),Type1,Type2,Err,Err):- 
	to_upper(Name1,Name2), 
	sql:property(Type1,Name2,Name3,Type2), !, 
	platform:dbtype_to_sqltype(Type2,SqlType), !.
control_path_element(property(many,'*'),PropertyBag,Type1,many,Err,Err):- 
	findall(property(SqlType,Name),
		(sql:property(Type1,_,Name,Type2),platform:dbtype_to_sqltype(Type2,SqlType)), 
		PropertyBag), !.
control_path_element(property(any,Name),property(unknown,Name),Type,unknown,[Error|Err],Err):- 
	list_to_atom(['Unknown property ',Name,' of class ',Type,'.'],Error), !.
control_path_element(method(any,Name1,Args1),method(SqlType,Name3,Args2),Type1,Type2,Err1,Err2):- 
	control_valueexpr_list(Args1,Args2,ArgTypes1,Err1,Err2), 
	to_upper(Name1,Name2), 
	sql:method(Type1,Name2,Name3,ArgTypes2,Type2), 
	control_method_argument_types(ArgTypes1,ArgTypes2), !, 
	platform:dbtype_to_sqltype(Type2,SqlType), !.
control_path_element(method(any,Name,Args1),method(unknown,Name,Args2),Type,unknown,[Error|Err1],Err2):- 
	control_valueexpr_list(Args1,Args2,ArgTypes,Err1,Err2), 
	create_argument_list(ArgTypes,ArgList), 
	list_to_atom([Name|ArgList],MethodAtom), 
	list_to_atom(['Unknown method ',MethodAtom,' of class ',Type,'.'],Error), !.
control_path_element(gmethod(any,Name1,TParams1,Args1),gmethod(SqlType,Name3,TParams2,Args2),Type1,Type2,Err1,Err3):- 
	control_typeparam_list(Type1,TParams1,TParams2,Err1,Err2), 
	control_valueexpr_list(Args1,Args2,ArgTypes1,Err2,Err3), 
	to_upper(Name1,Name2), 
	sql:gmethod(Type1,Name2,Name3,TParams2,ArgTypes2,Type2), 
	control_method_argument_types(ArgTypes1,ArgTypes2), !, 
	platform:dbtype_to_sqltype(Type2,SqlType), !.
control_path_element(gmethod(any,Name,TParams1,Args1),gmethod(unknown,Name,TParams2,Args2),Type,unknown,[Error|Err1],Err3):- 
	control_typeparam_list(Type,TParams1,TParams2,Err1,Err2), 
	control_valueexpr_list(Args1,Args2,ArgTypes,Err2,Err3), 
	create_typeparam_list(TParams2,TPList), 
	create_argument_list(ArgTypes,ArgList), 
	lists:append([Name|TPList],ArgList,MethodList), 
	list_to_atom(MethodList,MethodAtom), 
	list_to_atom(['Unknown method ',MethodAtom,' of class ',Type,'.'],Error), !.


/* Control type parameters for the only supported generic method GetExtension<T>(). */
control_typeparam_list(_,[],[],Err,Err):- !.
control_typeparam_list(Type,[TParam1|TPs1],[TParam2|TPs2],Err1,Err3):- 
	control_typeparam_list2(Type,TParam1,TParam2,Err1,Err2), 
	control_typeparam_list(Type,TPs1,TPs2,Err2,Err3), !.

control_typeparam_list2(Type,TParam1,TParam4,Err,Err):- 
	findall(TParam3,(to_upper(TParam1,TParam2),sql:extension(Type,TParam2,TParam3)),[TParam4]), !.
control_typeparam_list2(Type,TParam1,TParam1,[Error|Err],Err):- 
	findall(1,(to_upper(TParam1,TParam2),sql:extension(Type,TParam2,_)),[_,_|_]), 
	list_to_atom(['Ambiguous type parameter ',TParam1,'.'],Error), !.
control_typeparam_list2(Type,TParam1,TParam1,[Error|Err],Err):- 
	findall(1,(to_upper(TParam1,TParam2),sql:extension(Type,TParam2,_)),[]), 
	list_to_atom(['Unknown type parameter ',TParam1,'.'],Error), !.


control_cast(object(_),object(_),Err,Err):- !.
control_cast(Type1,Type2,[Error|Es],Es):- 
	list_to_atom(['Unsupported cast from ',Type1,' to ',Type2,'.'],Error), !.
	

create_argument_list(Arguments,['('|ArgList]):- 
	create_argument_list2(Arguments,ArgList), !.

create_argument_list2([Argument|As],[Argument|ArgList]):- 
	create_argument_list3(As,ArgList), !.
create_argument_list2([],[')']):- !.

create_argument_list3([Argument|As],[',',Argument|ArgList]):- 
	create_argument_list3(As,ArgList), !.
create_argument_list3([],[')']):- !.


create_typeparam_list(TypeParams,['<'|TPList]):- 
	create_typeparam_list2(TypeParams,TPList), !.

create_typeparam_list2([TypeParam|TPs],[TypeParam|TPList]):- 
	create_typeparam_list3(TPs,TPList), !.
create_typeparam_list2([],['>']):- !.

create_typeparam_list3([Argument|As],[',',Argument|ArgList]):- 
	create_typeparam_list3(As,ArgList), !.
create_typeparam_list3([],['>']):- !.


/*===== Type handling. =====*/

/* Control operation types of unary operations. */

control_operation_types(_,unknown,unknown,Err,Err):- !.
control_operation_types(Operator,InType,OutType,Err,Err):- 
	platform:operation_types(Operator,InType,OutType), !.
control_operation_types(Operator,InType,unknown,[Error|Es],Es):- 
	list_to_atom(['Incorrect argument of type ',InType,' to operator ',Operator,'.'],Error), !.

/* Control operation types of binary operations. */

control_operation_types(_,unknown,_,unknown,Err,Err):- !.
control_operation_types(_,_,unknown,unknown,Err,Err):- !.
control_operation_types(Operator,InType1,InType2,OutType,Err,Err):- 
	platform:operation_types(Operator,InType1,InType2,OutType), !.
control_operation_types(Operator,InType1,InType2,unknown,[Error|Es],Es):- 
	list_to_atom(['Incorrect arguments of types ',InType1,' and ',InType2,' to operator ',Operator,'.'],Error), !.

/* Control operation types of set functions. */

control_setfunction_types(_,unknown,unknown,Err,Err):- !.
control_setfunction_types(SetFunc,InType,OutType,Err,Err):- 
	platform:setfunction_types(SetFunc,InType,OutType), !.
control_setfunction_types(SetFunc,InType,unknown,[Error|Es],Es):- 
	list_to_atom(['Incorrect argument of type ',InType,' to set-function ',SetFunc,'.'],Error), !.

/* Control comparison types of ordinary comparisons. */

control_comparison_types(_,unknown,_,unknown,Err,Err):- !.
control_comparison_types(_,_,unknown,unknown,Err,Err):- !.
control_comparison_types(Operator,InType1,InType2,CompType,Err,Err):- 
	platform:comparison_types(Operator,InType1,InType2,CompType), !.
control_comparison_types(Operator,InType1,InType2,unknown,[Error|Es],Es):- 
	list_to_atom(['Incorrect arguments of types ',InType1,' and ',InType2,
		' to operator ',Operator,'.'],Error), !.

/* Control comparison types of comparisons with extra argument. */

control_comparison_types(Operator,InType1,InType2,InType3,CompType,Err,Err):- 
	platform:comparison_types(Operator,InType1,InType2,InType3,CompType), !.
control_comparison_types(Operator,InType1,InType2,InType3,unknown,[Error|Es],Es):- 
	list_to_atom(['Incorrect arguments of types ',InType1,'\, ',InType2,' and ',InType3,
		' to comparison operator ',Operator,'.'],Error), !.

/* Control method argument types. */

control_method_argument_types([],[]):- !.
control_method_argument_types([object(Type1)|Ts1],[Type2|Ts2]):- !, 
	platform:sqltype_to_dbtype(object(Type1),Type2), 
	control_method_argument_types(Ts1,Ts2), !.
control_method_argument_types([Type1|Ts1],[Type2|Ts2]):- 
	platform:sqltype_to_dbtype(Type1,Type2), 
	control_method_argument_types(Ts1,Ts2), !.

/* Control in types. */

control_in_types(Type1,[Type2|Ts],Err1,Err3):- 
	control_comparison_types(in,Type1,Type2,_,Err1,Err2), 
	control_in_types(Type1,Ts,Err2,Err3), !.
control_in_types(_,[],Err,Err):- !.

/* Control in query. */

control_in_query([_],Err,Err):- !.
control_in_query([_|_],['Incorrect select-list in argument to in-predicate.'|Err],Err):- !.


/*===== Create type structures. =====*/

/* create_sortspec_struct(+Type,+Expr,+SortOrder,-SortSpecStruct):- 
*	Creates a type specific structure SortSpecStruct for sort specifications.
*/
create_sortspec_struct(Type,Expr1,SortOrder,sortSpec(Type,Expr2,SortOrder)):- 
	type_any(Expr1,Type,Expr2), !.

type_any(literal(any,null),Type,literal(Type,null)):- !.
type_any(variable(any,Num),Type,variable(numerical,Num)):- 
	numerical_type(Type), !.
type_any(variable(any,Num),Type,variable(Type,Num)):- !.
type_any(Expr,_,Expr):- !.

numerical_type(integer):- !.
numerical_type(uinteger):- !.
numerical_type(decimal):- !.
numerical_type(double):- !.


/* create_comparison_struct(+Type,+Operator,+Expr1,+Expr2,-CompStruct):- 
*	Creates a type specific structure CompStruct for ordinary comparisons.
*/

create_comparison_struct(unknown,Operator,Expr1,Expr2,comparison(unknown,Operator,Expr1,Expr2)):- !.
create_comparison_struct(Type,Operator,Expr1,Expr2,comparison(Type,Operator,Expr3,Expr4)):- 
	type_any(Expr1,Type,Expr3), 
	type_any(Expr2,Type,Expr4), !.

/* create_comparison_struct(+Type,+Operator,+Expr1,+Expr2,+Expr3,-CompStruct):- 
*	Creates a type specific structure CompStruct for comparisons with extra argument 
*	(LIKE-operator with ESCAPE-argument).
*/
create_comparison_struct(unknown,Operator,Expr1,Expr2,Expr3,comparison(unknown,Operator,Expr1,Expr2,Expr3)):- !.
create_comparison_struct(Type,Operator,Expr1,Expr2,Expr3,comparison(Type,Operator,Expr4,Expr5,Expr6)):- 
	type_any(Expr1,Type,Expr4), 
	type_any(Expr2,Type,Expr5), 
	type_any(Expr3,Type,Expr6), !.

/* create_numoperation_struct(+Type,+Operator,+Expr1,+Expr2,-NumOpStruct):- 
*	Creates a type specific structure NumOpStruct for binary numeric operations.
*/
create_numoperation_struct(unknown,Operator,Expr1,Expr2,operation(numerical,Operator,Expr1,Expr2)):- !.
create_numoperation_struct(Type,Operator,Expr1,Expr2,operation(Type,Operator,Expr3,Expr4)):- 
	type_any(Expr1,Type,Expr3), 
	type_any(Expr2,Type,Expr4), !.

/* create_numoperation_struct(+Type,+Operator,+Expr,-NumOpStruct):- 
*	Creates a type specific structure NumOpStruct for unary numeric operations.
*/
create_numoperation_struct(unknown,Operator,Expr,operation(numerical,Operator,Expr)):- !.
create_numoperation_struct(Type,Operator,Expr1,operation(Type,Operator,Expr2)):- 
	type_any(Expr1,Type,Expr2), !.

/* create_stringoperation_struct(+Type,+Operator,+Expr1,+Expr2,-StrOpStruct):- 
*	Creates a type specific structure StrOpStruct for binary string operations.
*/
create_stringoperation_struct(unknown,Operator,Expr1,Expr2,operation(string,Operator,Expr1,Expr2)):- !.
create_stringoperation_struct(Type,Operator,Expr1,Expr2,operation(Type,Operator,Expr3,Expr4)):- 
	type_any(Expr1,Type,Expr3), 
	type_any(Expr2,Type,Expr4), !.

/* create_stringoperation_struct(+Type,+Operator,+Expr,-StrOpStruct):- 
*	Creates a type specific structure StrOpStruct for unary string operations.
*/
create_stringoperation_struct(unknown,Operator,Expr,operation(string,Operator,Expr)):- !.
create_stringoperation_struct(Type,Operator,Expr1,operation(Type,Operator,Expr2)):- 
	type_any(Expr1,Type,Expr2), !.

/* create_setfunction_struct(+Type,+Function,+Quant,+Expr,-SetFuncStruct):- 
*	Creates a type specific structure SetFuncStruct for set functions.
*/
create_setfunction_struct(Type,Function,Quant,Expr1,setFunction(Type,Function,Quant,Expr2)):- 
	type_any(Expr1,Type,Expr2), !.

/* create_path_struct(+Path,+Type,-PathStruct):- 
*	Creates an instantiated path structure PathStruct from a path Path and a type Type.
*/
create_path_struct(Path,Type,path(Type,Path)):- !.


/*===== Create type definition. =====*/

create_type_definition(Tree,Tables1,typeDef(Tables2,Mappings),Err1,Err2):- 
	create_tablespec(Tables1,Tables2), 
	create_mappings(Tree,Mappings), 
	control_mappings(Mappings,Err1,Err2), !. 

create_tablespec([extent(Num,Expr1)|Ts1],[extent(Num,Expr2)|Ts2]):- 
	create_tablespec2(Expr1,Expr2), 
	create_tablespec(Ts1,Ts2), !.
create_tablespec([],[]):- !.

create_tablespec2([Column1|Cs1],[Column2|Cs2]):- !, 
	create_tablespec3(Column1,Column2), 
	create_tablespec2(Cs1,Cs2), !.
create_tablespec2([],[]):- !.
create_tablespec2(TableName1,TableName2):- 
	control_table(TableName1,TableName2,_,_), !.

create_tablespec3(property(_,Expr),Type):- 
	control_valueexpr(Expr,_,Type,_,[]), !.


create_mappings(select2(_,Columns,_,_,_,_,_),Mappings):- 
	create_mappings2(Columns,0,Mappings), !.
create_mappings(select3(_,Columns,_,_,_,_,_,_,_,_,_),Mappings):- 
	create_mappings2(Columns,0,Mappings), !.

/***
create_mappings2([as(Expr,name(noName))|Aliases],Num1,[map(Atom,Expr)|Mappings]):- !, 
	number_codes(Num1,Codes1),
	lists:append([110,111,78,97,109,101],Codes1,Codes2), //noName
	atom_codes(Atom,Codes2), 
	Num2 is Num1 + 1, 
	create_mappings2(Aliases,Num2,Mappings), !.
***/
create_mappings2([as(Expr,name(noName))|Aliases],Num1,[map(Atom,Expr)|Mappings]):- !, 
	number_codes(Num1,Codes),
	atom_codes(Atom,Codes), 
	Num2 is Num1 + 1, 
	create_mappings2(Aliases,Num2,Mappings), !.
create_mappings2([as(Expr,name(Name))|Aliases],Num1,[map(Name,Expr)|Mappings]):- 
	Num2 is Num1 + 1, 
	create_mappings2(Aliases,Num2,Mappings), !.
create_mappings2([],_,[]):- !.

control_mappings(Mappings1,[Error|Es],Es):- 
	lists:select(map(Name,_),Mappings1,Mappings2), 
	lists:select(map(Name,_),Mappings2,_), 
	list_to_atom(['Non-unique column name ',Name,'.'],Error), !.
control_mappings(_,Err,Err):- !.


/*===== Library predicates. =====*/

/* list_to_atom(+List,-Atom):- 
*	Concatenates a list List of terms to a single atom Atom. 
*/
list_to_atom(List,Atom):- 
	atom_codes(EmptyAtom,[]), 
	list_to_atom2(List,EmptyAtom,Atom), !.

list_to_atom2([Element|List],Atom1,Atom4):- 
	term_to_atom(Element,Atom2), 
	atom_concat(Atom1,Atom2,Atom3), 
	list_to_atom2(List,Atom3,Atom4), !.
list_to_atom2([],Atom,Atom):- !.

term_to_atom(Atom,Atom):- 
	atom(Atom), !.
term_to_atom(Num,Atom):- 
	number(Num), 
	number_codes(Num,Codes), 
	atom_codes(Atom,Codes), !.
term_to_atom(Compound,Atom):- 
	compound(Compound), 
	compound_to_atom(Compound,Atom), !.

compound_to_atom(List,Atom2):- 
	list(List), !, 
	compound_to_atom_list(List,'[',Atom1), 
	atom_concat(Atom1,']',Atom2), !.
compound_to_atom(Compound,Atom3):- 
	functor(Compound,Func,Arity), 
	atom_concat(Func,'(',Atom1), 
	compound_to_atom_args(0,Arity,Compound,Atom1,Atom2), 
	atom_concat(Atom2,')',Atom3), !.

compound_to_atom_list([],Atom,Atom):- !.
compound_to_atom_list([Term|Ts],Atom1,Atom4):- 
	term_to_atom(Term,Atom2), 
	atom_concat(Atom1,Atom2,Atom3), 
	compound_to_atom_list2(Ts,Atom3,Atom4), !.

compound_to_atom_list2([],Atom,Atom):- !.
compound_to_atom_list2([Term|Ts],Atom1,Atom5):- 
	atom_concat(Atom1,',',Atom2), 
	term_to_atom(Term,Atom3), 
	atom_concat(Atom2,Atom3,Atom4), 
	compound_to_atom_list2(Ts,Atom4,Atom5), !.

compound_to_atom_args(Arity,Arity,_,Atom,Atom):- !.
compound_to_atom_args(0,Arity,Compound,Atom1,Atom4):- 
	0 < Arity, 
	arg(1,Compound,Term), 
	term_to_atom(Term,Atom2), 
	atom_concat(Atom1,Atom2,Atom3), 
	compound_to_atom_args(1,Arity,Compound,Atom3,Atom4), !.
compound_to_atom_args(Num1,Arity,Compound,Atom1,Atom5):- 
	Num1 < Arity, 
	atom_concat(Atom1,',',Atom2), 
	Num2 is Num1 + 1, 
	arg(Num2,Compound,Term), 
	term_to_atom(Term,Atom3), 
	atom_concat(Atom2,Atom3,Atom4), 
	compound_to_atom_args(Num2,Arity,Compound,Atom4,Atom5), !.

list([]):- !.
list([_|_]):- !.

create_sortspec_list([ValueExpr|VEs],[SortSpec|SSs]):- 
	get_type(ValueExpr,Type), 
	create_sortspec_struct(Type,ValueExpr,asc,SortSpec), 
	create_sortspec_list(VEs,SSs), !.
create_sortspec_list([],[]):- !.

get_type(literal(Type,_),Type):- !.
get_type(variable(Type,_),Type):- !.
get_type(path(Type,_),Type):- !.
get_type(operation(numerical,_,_,_),unknown):- !.
get_type(operation(numerical,_,_),unknown):- !.
get_type(operation(Type,_,_,_),Type):- !.
get_type(operation(Type,_,_),Type):- !.
get_type(setFunction(Type,_,_,_),Type):- !.


to_upper_list([Atom|As],[UpperCaseAtom|UAs]):- 
	to_upper(Atom,UpperCaseAtom), 
	to_upper_list(As,UAs), !.
to_upper_list([],[]):- !.

to_upper(Atom,UpperCaseAtom):- 
	atom_codes(Atom,Codes), 
	to_upper2(Codes,UpperCaseCodes), 
	atom_codes(UpperCaseAtom,UpperCaseCodes), !.

to_upper2([Code|Cs],[UpperCaseCode|UCs]):- 
	to_upper3(Code,UpperCaseCode), 
	to_upper2(Cs,UCs), !.
to_upper2([],[]):- !.

to_upper3(Code,UpperCaseCode):- 
	Code >= 97, 
	Code =< 122, 
	UpperCaseCode is Code - 32, !.
to_upper3(Code,Code).






