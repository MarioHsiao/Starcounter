
/* Rewriter, Peter Idestam-Almquist, Starcounter, 2013-04-11. */

/* TODO: Rewrite replace_aliases_xxx? (replace_extent_aliases_xxx and replace_aggr_aliases_xxx) */
/* TODO: Modify HintSpec in replace_derived_table_aliases_hintspec/2? */

/* 13-04-11: Fixed alias bug (problem with different cases). */
/* 13-01-28: Added support of is-type comparison by adding istype_predicate/3. */
/* 11-11-30: Added fetch specification; modified select1, select2, select3. */
/* 10-05-10: New representation of variable by '?'. */
/* 10-01-11: Support of index hints by name, and support of previuos index hints removed. */
/* 09-08-31: Support of combined-index hints. */
/* 09-02-24: Support of asterisks '*' in paths in select-list. */
/* 09-02-18: Support of unqualified field identifiers. */
/* 09-01-26: Added string operation addMaxChar to better support STARTS WITH. */
/* 08-04-09: Not allowed column names (aliases) as sort keys. */
/* 08-03-13: Replace property(cast(Type1,Type2),Name) with cast(property(Type1,Name),Type2) etc. */
/* 08-03-10: Removed support of paths without table-reference-aliases (Example.Employee.FirstName). */
/* 08-03-07: Support of object reference casting (cast(p.Father as Example.Employee).Manager). */
/* 08-01-03: Added modify_select/2. */
/* 07-12-10: Standardized where-condition including all join conditions. */
/* 07-12-06: Removed support of nested selects. */
/* 07-12-05: Support of hints. */
/* 07-05-28: path(Type,Path). */
/* 07-04-22: path(Type,ExtNum,Path), method(Type,Name,Args), gmethod(Type,Name,TParams,Args). */

:- module(rewriter,[]).


/*===== Main. =====*/

/* rewrite(+Parse,-Tree1,-Tree2,-Tables,-Num,-ErrHead,-ErrTail):- 
*	Rewrites a parse tree Parse to a tree Tree2 where all paths (column references) are explicit, 
*	and collects a list Tables of all referenced tables. 
*	Also returns an intermediate result Tree1, the number of tables Num, and 
*	a difference list ErrHead-ErrTail of errors. 
*/
rewrite(noTree,noTree,noTree,[],noNum,Err,Err):- !.
rewrite(Parse,Tree4,Tree5,Tables1,Num,Err,Err):- 
	add_table_numbers_select(Parse,0,Num,Tree1,Tables1,Tables2), 
	modify_select(Tree1,Num,_,Tree2), 
	replace_table_aliases_select(Tree2,Tree3), 
	replace_aggregation_aliases_select(Tree3,Tree4,Tables2,[]), 
	move_join_conditions_select(Tree4,Tree5), !.
rewrite(_,noTree,noTree,[],noNum,['Unknown error in module rewriter.'|Err],Err):- !.


/*===== Add table numbers. =====*/

/* add_table_numbers_xxx(+Tree1,+Num1,-Num2,-Tree2,-Head,-Tail):- 
*	Replaces structure extent(any,IdAtom) with extent(Num,IdAtom) in the input Tree1 obtaining 
*	the output Tree2. 
*	Num1 and Num2 are input and output of next available extent number (starting with 1). 
*	Head-Tail is an output difference list of numbered extent references extent(Num,IdAtom).
*/

/* Traverse select. */

add_table_numbers_select(select1(Quant,Columns,From1,Where,GroupBy,Having,SortSpec,FetchSpec,Hints),Num1,Num2,Tree,Tables1,Tables2):- 
	add_table_numbers_tableref(From1,Num1,Num2,From2,Tables1,Tables2), 
	Tree = select1(Quant,Columns,From2,Where,GroupBy,Having,SortSpec,FetchSpec,Hints), !.

/* Traverse tableref. */

add_table_numbers_tableref(as(Table1,Name),Num1,Num2,as(Table2,Name),Tables1,Tables2):- !, 
	add_table_numbers_tableref(Table1,Num1,Num2,Table2,Tables1,Tables2), !.
add_table_numbers_tableref(extent(any,IdAtom),Num1,Num2,extent(Num1,IdAtom),[extent(Num1,IdAtom)|Tables],Tables):- !, 
	Num2 is Num1 + 1, !.
add_table_numbers_tableref(join1(Type,Table1,Table2,Cond),Num1,Num3,join1(Type,Table3,Table4,Cond),Tables1,Tables3):- !, 
	add_table_numbers_tableref(Table1,Num1,Num2,Table3,Tables1,Tables2), 
	add_table_numbers_tableref(Table2,Num2,Num3,Table4,Tables2,Tables3), !.


/*===== Modify select. =====*/

/* modify_select(+Tree1,+Num1,-Num2,-Tree2):- 
*	Replaces structure select1(Quant,Columns,From,Where,GroupBy,Having,SortSpec,FetchSpec,Hints)
*	with select2(Quant,Columns,From,Where,SortSpec,FetchSpec,Hints) for simple select clauses and 
*	with select3(Quant,Columns,From,Where,GroupBy,SetFuncs,Having,TempExtent,SortSpec,FetchSpec,Hints) for select clauses with aggregations, 
*	in the input Tree1 obtaining the output Tree2. 
*	Num1 is input and Num2 is output, next available extent number (number of extents). 
*/

modify_select(select1(Quant,Columns,From,Where,GroupBy,Having,SortSpec,FetchSpec,Hints),Num1,Num2,Tree):- 
	get_setfunctions_column_list(Columns,SetFuncs1,SetFuncs2), 
	get_setfunctions_condition(Having,SetFuncs2,[]), 
	modify_select2(Quant,Columns,From,Where,GroupBy,SetFuncs1,Having,SortSpec,FetchSpec,Hints,Num1,Num2,Tree), !. 

modify_select2(Quant,Columns,From,Where,[],[],literal(logical,true),SortSpec,FetchSpec,Hints,Num,Num,Tree):- 
	Tree = select2(Quant,Columns,From,Where,SortSpec,FetchSpec,Hints), !.
modify_select2(Quant,Columns,From,Where,GroupBy,SetFuncs,Having,SortSpec,FetchSpec,Hints,Num1,Num2,Tree):- 
	Tree = select3(Quant,Columns,From,Where,GroupBy,SetFuncs,Having,extent(Num1,temp),SortSpec,FetchSpec,Hints), 
	Num2 is Num1 + 1, !.


/*===== Get set functions. =====*/

/* get_setfunctions_xxx(+Tree,-Head,-Tail):- 
*	Returns a difference list Heads-Tail of all set-functions in the input Tree.
*/

/* Traverse column_list. */

get_setfunctions_column_list([Column|Cs],SetFuncs1,SetFuncs3):- !, 
	get_setfunctions_column(Column,SetFuncs1,SetFuncs2), 
	get_setfunctions_column_list(Cs,SetFuncs2,SetFuncs3), !.
get_setfunctions_column_list([],SetFuncs,SetFuncs):- !.

/* Traverse column. */

get_setfunctions_column(as(Expr,_),SetFuncs1,SetFuncs2):- !, 
	get_setfunctions_valueexpr(Expr,SetFuncs1,SetFuncs2), !.
get_setfunctions_column(Expr,SetFuncs1,SetFuncs2):- 
	get_setfunctions_valueexpr(Expr,SetFuncs1,SetFuncs2), !.

/* Traverse valueexpr_list. */

get_setfunctions_valueexpr_list([ValueExpr|VEs],SetFuncs1,SetFuncs3):- !, 
	get_setfunctions_valueexpr(ValueExpr,SetFuncs1,SetFuncs2), 
	get_setfunctions_valueexpr_list(VEs,SetFuncs2,SetFuncs3), !.
get_setfunctions_valueexpr_list([],SetFuncs,SetFuncs):- !.

/* Traverse valueexpr. */

get_setfunctions_valueexpr(operation(_,_,Expr1,Expr2),SetFuncs1,SetFuncs3):- 
	get_setfunctions_valueexpr(Expr1,SetFuncs1,SetFuncs2), 
	get_setfunctions_valueexpr(Expr2,SetFuncs2,SetFuncs3), !. 
get_setfunctions_valueexpr(operation(_,_,Expr),SetFuncs1,SetFuncs2):- 
	get_setfunctions_valueexpr(Expr,SetFuncs1,SetFuncs2), !. 
get_setfunctions_valueexpr(setFunction(Func,Quant,Expr),[setFunction(Func,Quant,Expr)|SetFuncs],SetFuncs):- !.
get_setfunctions_valueexpr(path(_,_),SetFuncs,SetFuncs):- !.
get_setfunctions_valueexpr(literal(_,_),SetFuncs,SetFuncs):- !.
get_setfunctions_valueexpr(variable(_,_),SetFuncs,SetFuncs):- !.


/* Traverse condition. */

get_setfunctions_condition(literal(logical,_),SetFuncs,SetFuncs):- !.
get_setfunctions_condition(operation(logical,_,Expr1,Expr2),SetFuncs1,SetFuncs3):- 
	get_setfunctions_condition(Expr1,SetFuncs1,SetFuncs2), 
	get_setfunctions_condition(Expr2,SetFuncs2,SetFuncs3), !. 
get_setfunctions_condition(operation(logical,_,Expr),SetFuncs1,SetFuncs2):- 
	get_setfunctions_condition(Expr,SetFuncs1,SetFuncs2), !. 
get_setfunctions_condition(comparison(any,_,Expr1,Expr2),SetFuncs1,SetFuncs3):- 
	get_setfunctions_valueexpr(Expr1,SetFuncs1,SetFuncs2), 
	get_setfunctions_valueexpr(Expr2,SetFuncs2,SetFuncs3), !.
get_setfunctions_condition(comparison(any,_,Expr1,Expr2,Expr3),SetFuncs1,SetFuncs4):- 
	get_setfunctions_valueexpr(Expr1,SetFuncs1,SetFuncs2), 
	get_setfunctions_valueexpr(Expr2,SetFuncs2,SetFuncs3), 
	get_setfunctions_valueexpr(Expr3,SetFuncs3,SetFuncs4), !.
get_setfunctions_condition(inPredicate(Expr,_),SetFuncs1,SetFuncs2):- 
	get_setfunctions_valueexpr(Expr,SetFuncs1,SetFuncs2), !.


/*===== Replace table aliases. =====*/

/* replace_table_aliases_xxx(+Tree1,-Tree2):- 
*	Replaces extent aliases property(any,Alias) with extent references extent(Num,IdAtom), 
*	in paths in the input Tree1 obtaining the output Tree2. 
*	Adds unspecified extent references extent(unspec,ExtentList) to paths without extent alias.
*/

/* Traverse select. */

replace_table_aliases_select(select2(Quant,Columns1,From1,Where1,SortSpec1,FetchSpec,Hints1),Tree):- 
	get_table_aliases(From1,From2,[],Aliases), 
	replace_aliases_column_list(Columns1,Aliases,extent,Columns2), 
	replace_aliases_tableref(From2,Aliases,extent,From3), 
	replace_aliases_condition(Where1,Aliases,extent,Where2), 
	replace_aliases_sortspec_list(SortSpec1,Aliases,extent,SortSpec2), 
	replace_aliases_hint_list(Hints1,Aliases,extent,Hints2), 
	Tree = select2(Quant,Columns2,From3,Where2,SortSpec2,FetchSpec,Hints2), !.
replace_table_aliases_select(select3(Quant,Columns1,From1,Where1,GroupBy1,SetFuncs1,Having1,TempExtent,SortSpec1,FetchSpec,Hints1),Tree):- 
	get_table_aliases(From1,From2,[],Aliases), 
	replace_aliases_column_list(Columns1,Aliases,extent,Columns2), 
	replace_aliases_tableref(From2,Aliases,extent,From3), 
	replace_aliases_condition(Where1,Aliases,extent,Where2), 
	replace_aliases_valueexpr_list(GroupBy1,Aliases,extent,GroupBy2), 
	replace_aliases_valueexpr_list(SetFuncs1,Aliases,extent,SetFuncs2), 
	replace_aliases_condition(Having1,Aliases,extent,Having2), 
	replace_aliases_sortspec_list(SortSpec1,Aliases,extent,SortSpec2), 
	replace_aliases_hint_list(Hints1,Aliases,extent,Hints2), 
	Tree = select3(Quant,Columns2,From3,Where2,GroupBy2,SetFuncs2,Having2,TempExtent,SortSpec2,FetchSpec,Hints2), !.


/*===== Get table aliases. =====*/

/* get_table_aliases(+TableRef1,-TableRef2,+Aliases1,-Aliases2):- 
*	Adds table aliases to the input list Aliases1 obtaining the output list Aliases2. 
*	The aliases are of the form as(extent(Num,IdAtom),alias(any,Name)). 
*	Also replaces the structure as(extent(Num,IdAtom),alias(any,Name)) with extent(Num,IdAtom), 
*	in the table reference TableRef1 obtaining the output table reference TableRef2.
*/
get_table_aliases(join1(Type,Table1,Table2,Cond),join1(Type,Table3,Table4,Cond),Aliases1,Aliases3):- 
	get_table_aliases(Table2,Table4,Aliases1,Aliases2), 
	get_table_aliases(Table1,Table3,Aliases2,Aliases3), !.
get_table_aliases(as(extent(Num,IdAtom),alias(any,Name1)),extent(Num,IdAtom),Aliases,[Alias|Aliases]):- 
	controller:to_upper(Name1,Name2), 
	Alias = as(extent(Num,IdAtom),alias(any,Name2)), !.


/*===== Replace aggregation aliases. =====*/

/* replace_aggregation_aliases_xxx(+Tree1,-Tree2,-Head,-Tail):- 
*	Replaces paths (path/2) to tables hidden by aggregation with paths (tmpPath/2) to temp tables, 
*	in the input Tree1 obtaining the output Tree2.
* 	Also returnes a difference list Head-Tail of temp table specifications.
*/

/* Traverse select. */

replace_aggregation_aliases_select(select2(Quant,Columns,From,Where,SortSpec,FetchSpec,Hints),Tree,TempTables,TempTables):- 
	Tree = select2(Quant,Columns,From,Where,SortSpec,FetchSpec,Hints), !.

replace_aggregation_aliases_select(select3(Quant,Columns1,From,Where,GroupBy,SetFuncs,Having1,extent(ExtNum,temp),SortSpec1,FetchSpec,Hints),Tree,[extent(ExtNum,PropSpec)|Tables],Tables):- 
	get_aggregation_aliases(GroupBy,SetFuncs,ExtNum,0,[],Aliases,PropSpec), 
	replace_aliases_column_list(Columns1,Aliases,aggr,Columns2), 
	replace_aliases_condition(Having1,Aliases,aggr,Having2), 
	replace_aliases_sortspec_list(SortSpec1,Aliases,aggr,SortSpec2),
	Tree = select3(Quant,Columns2,From,Where,GroupBy,SetFuncs,Having2,extent(ExtNum,temp),SortSpec2,FetchSpec,Hints), !.


/*===== Get aggregation aliases. =====*/

get_aggregation_aliases([GrCol|GCs],SetFunc,TableNum,ColNum1,As1,[Alias|As2],[ColSpec|CSs]):- 
	number_codes(ColNum1,Codes), 
	atom_codes(Id,Codes), 
	Alias = as(path(any,[extent(TableNum,temp),property(any,Id)]),GrCol), 
	ColSpec = property(Id,GrCol), 
	ColNum2 is ColNum1 + 1, 
	get_aggregation_aliases(GCs,SetFunc,TableNum,ColNum2,As1,As2,CSs), !.
get_aggregation_aliases([],SetFunc,TableNum,ColNum,Aliases1,Aliases2,ColSpec):- 
	get_aggregation_aliases2(SetFunc,TableNum,ColNum,Aliases1,Aliases2,ColSpec), !.

get_aggregation_aliases2([SetFunc|SFs],TableNum,ColNum1,As1,[Alias|As2],[ColSpec|CSs]):- 
	number_codes(ColNum1,Codes), 
	atom_codes(Id,Codes), 
	Alias = as(path(any,[extent(TableNum,temp),property(any,Id)]),SetFunc), 
	ColSpec = property(ColNum1,SetFunc), 
	ColNum2 is ColNum1 + 1, 
	get_aggregation_aliases2(SFs,TableNum,ColNum2,As1,As2,CSs), !.
get_aggregation_aliases2([],_,_,As,As,[]):- !.


/*===== Replace aliases. =====*/

/* replace_aliases_xxx(+Expr1,+Aliases,+AType,-Expr2):- 
*	Replaces structures in Expr1 according to aliases Aliases obtaining Expr2.
*	When AType=extent the aliases are of the form as(extent(Num,IdAtom),alias(any,Name)), and 
*	when AType=aggr the aliases are either of the form as(path(any,[extent(TableNum,temp),property(any,Id)]),path(any,Path))
*	or the form as(path(any,[extent(TableNum,temp),property(any,Id)]),setFunction(Func,Quant,Expr)).
*/

/* Traverse hint_list. */

replace_aliases_hint_list([Hint1|Hs1],Aliases,AType,[Hint2|Hs2]):- 
	replace_aliases_hint(Hint1,Aliases,AType,Hint2), 
	replace_aliases_hint_list(Hs1,Aliases,AType,Hs2), !.
replace_aliases_hint_list([],_,_,[]):- !.

/* Traverse hint. */

replace_aliases_hint(indexHint(ExtentAlias,IndexName),Aliases,_,indexHint(Extent,IndexName)):- 
	do_replace_aliases_extentalias(Aliases,ExtentAlias,Extent), !.
replace_aliases_hint(joinOrderHint(fixed),_,_,joinOrderHint(fixed)):- !.
replace_aliases_hint(joinOrderHint(IdList),Aliases,AType,joinOrderHint(ExtentList)):- 
	replace_aliases_extentalias_list(IdList,Aliases,AType,ExtentList), !.

/* Traverse extentalias_list. */

replace_aliases_extentalias_list([Id|Is],Aliases,AType,[Extent|Es]):- 
	do_replace_aliases_extentalias(Aliases,Id,Extent), 
	replace_aliases_extentalias_list(Is,Aliases,AType,Es), !.
replace_aliases_extentalias_list([],_,_,[]):- !.

/* Traverse sortspec_list. */

replace_aliases_sortspec_list([SortSpec1|SSs1],Aliases,AType,[SortSpec2|SSs2]):- 
	replace_aliases_sortspec(SortSpec1,Aliases,AType,SortSpec2), 
	replace_aliases_sortspec_list(SSs1,Aliases,AType,SSs2), !.
replace_aliases_sortspec_list([],_,_,[]):- !.

/* Traverse sortspec. */

replace_aliases_sortspec(sortSpec(any,Expr1,SortOrder),Aliases,AType,sortSpec(any,Expr2,SortOrder)):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr2), !.
replace_aliases_sortspec(random,_,_,random):- !.

/* Traverse column_list. */

replace_aliases_column_list([Column1|Cs1],Aliases,AType,[Column2|Cs2]):- 
	replace_aliases_column(Column1,Aliases,AType,Column2), 
	replace_aliases_column_list(Cs1,Aliases,AType,Cs2), !.
replace_aliases_column_list([],_,_,[]):- !.

/* Traverse column. */

replace_aliases_column(as(Expr1,Name),Aliases,AType,as(Expr2,Name)):- !, 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr2), !.
replace_aliases_column(Expr1,Aliases,AType,Expr2):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr2), !.

/* Traverse tableref. */

replace_aliases_tableref(as(Table1,Name),Aliases,AType,as(Table2,Name)):- !, 
	replace_aliases_tableref(Table1,Aliases,AType,Table2), !.
replace_aliases_tableref(extent(Num,Id),_,_,extent(Num,Id)):- !.
replace_aliases_tableref(join1(JType,Table1,Table2,Cond1),Aliases,AType,join1(JType,Table3,Table4,Cond2)):- !, 
	replace_aliases_tableref(Table1,Aliases,AType,Table3), 
	replace_aliases_tableref(Table2,Aliases,AType,Table4), 
	replace_aliases_condition(Cond1,Aliases,AType,Cond2), !.

/* Traverse condition. */

replace_aliases_condition(literal(logical,Value),_,_,literal(logical,Value)):- !. 
replace_aliases_condition(operation(logical,Operator,Expr1,Expr2),Aliases,AType,operation(logical,Operator,Expr3,Expr4)):- 
	replace_aliases_condition(Expr1,Aliases,AType,Expr3), 
	replace_aliases_condition(Expr2,Aliases,AType,Expr4), !.
replace_aliases_condition(operation(logical,Operator,Expr1),Aliases,AType,operation(logical,Operator,Expr2)):- 
	replace_aliases_condition(Expr1,Aliases,AType,Expr2), !.
replace_aliases_condition(comparison(any,Operator,Expr1,Expr2),Aliases,AType,comparison(any,Operator,Expr3,Expr4)):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr3), 
	replace_aliases_valueexpr(Expr2,Aliases,AType,Expr4), !.
replace_aliases_condition(comparison(any,Op,Expr1,Expr2,Expr3),Aliases,AType,comparison(any,Op,Expr4,Expr5,Expr6)):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr4), 
	replace_aliases_valueexpr(Expr2,Aliases,AType,Expr5), 
	replace_aliases_valueexpr(Expr3,Aliases,AType,Expr6), !.
replace_aliases_condition(inPredicate(ValueExpr1,ValueList1),Aliases,AType,inPredicate(ValueExpr2,ValueList2)):- 
	replace_aliases_valueexpr(ValueExpr1,Aliases,AType,ValueExpr2), 
	replace_aliases_inpredicate(ValueList1,Aliases,AType,ValueList2), !.
replace_aliases_condition(isTypePredicate(Op,ValueExpr1,TypeExpr),Aliases,AType,isTypePredicate(Op,ValueExpr2,TypeExpr)):- 
	replace_aliases_valueexpr(ValueExpr1,Aliases,AType,ValueExpr2), !.

/* Traverse inpredicate. */

replace_aliases_inpredicate(ValueList1,Aliases,AType,ValueList2):- 
	replace_aliases_valueexpr_list(ValueList1,Aliases,AType,ValueList2), !.

/* Traverse valueexpr_list. */

replace_aliases_valueexpr_list([Expr1|Es1],Aliases,AType,[Expr2|Es2]):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr2), 
	replace_aliases_valueexpr_list(Es1,Aliases,AType,Es2), !.
replace_aliases_valueexpr_list([],_,_,[]):- !.

/* Traverse valueexpr. */

replace_aliases_valueexpr(operation(numerical,Operator,Expr1,Expr2),Aliases,AType,operation(numerical,Operator,Expr3,Expr4)):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr3), 
	replace_aliases_valueexpr(Expr2,Aliases,AType,Expr4), !.
replace_aliases_valueexpr(operation(numerical,Operator,Expr1),Aliases,AType,operation(numerical,Operator,Expr2)):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr2), !.
replace_aliases_valueexpr(operation(string,Operator,Expr1,Expr2),Aliases,AType,operation(string,Operator,Expr3,Expr4)):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr3), 
	replace_aliases_valueexpr(Expr2,Aliases,AType,Expr4), !.
replace_aliases_valueexpr(operation(string,Operator,Expr1),Aliases,AType,operation(string,Operator,Expr2)):- 
	replace_aliases_valueexpr(Expr1,Aliases,AType,Expr2), !.
replace_aliases_valueexpr(path(Type,Path1),Aliases,AType,Expr):- 
	replace_aliases_path(Path1,Aliases,AType,Path2), 
	do_replace_aliases_path(Aliases,AType,path(Type,Path2),Expr), !.
replace_aliases_valueexpr(tmpPath(Path,Expr),_,_,tmpPath(Path,Expr)):- !.
replace_aliases_valueexpr(literal(Type,Literal),_,_,literal(Type,Literal)):- !.
replace_aliases_valueexpr(variable(Type,Num),_,_,variable(Type,Num)):- !.
/* AType=extent */
replace_aliases_valueexpr(setFunction(Function,Quant,Expr1),Aliases,extent,setFunction(Function,Quant,Expr2)):- 
	replace_aliases_valueexpr(Expr1,Aliases,extent,Expr2), !.
/* AType=aggr */
replace_aliases_valueexpr(setFunction(Function,Quant,Expr1),Aliases,aggr,Expr2):- 
	do_replace_aliases_setfunction(Aliases,setFunction(Function,Quant,Expr1),Expr2), !.

/* Traverse path. */
/* TODO: Simplify. */
replace_aliases_path([extent(Num,Id)|Ids1],Aliases,AType,[extent(Num,Id)|Ids2]):- 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([cast(extent(Num,Id),Type)|Ids1],Aliases,AType,[cast(extent(Num,Id),Type)|Ids2]):- 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([alias(Type,Name)|Ids1],Aliases,AType,[alias(Type,Name)|Ids2]):- 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([cast(alias(Type1,Name),Type2)|Ids1],Aliases,AType,[cast(alias(Type1,Name),Type2)|Ids2]):- 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([property(Type,Name)|Ids1],Aliases,AType,[property(Type,Name)|Ids2]):- 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([cast(property(Type1,Name),Type2)|Ids1],Aliases,AType,[cast(property(Type1,Name),Type2)|Ids2]):- 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([method(Type,Name,Arguments1)|Ids1],Aliases,AType,[method(Type,Name,Arguments2)|Ids2]):- 
	replace_aliases_valueexpr_list(Arguments1,Aliases,AType,Arguments2), 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([cast(method(Type1,Name,Arguments1),Type2)|Ids1],Aliases,AType,[cast(method(Type1,Name,Arguments2),Type2)|Ids2]):- 
	replace_aliases_valueexpr_list(Arguments1,Aliases,AType,Arguments2), 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([gmethod(Type,Name,TParams,Arguments1)|Ids1],Aliases,AType,[gmethod(Type,Name,TParams,Arguments2)|Ids2]):- 
	replace_aliases_valueexpr_list(Arguments1,Aliases,AType,Arguments2), 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([cast(gmethod(Type1,Name,TParams,Arguments1),Type2)|Ids1],Aliases,AType,[cast(gmethod(Type1,Name,TParams,Arguments2),Type2)|Ids2]):- 
	replace_aliases_valueexpr_list(Arguments1,Aliases,AType,Arguments2), 
	replace_aliases_path(Ids1,Aliases,AType,Ids2), !.
replace_aliases_path([],_,_,[]):- !.


/* do_replace_aliases_path(+Aliases,+AType,+Path,-Expr):- 
*	Replaces the input path Path with an expression Expr according to applicable alias in Aliases.
*	AType describes the type of the aliases (extent or aggr).
*/

do_replace_aliases_path(Aliases,extent,Path,Expr):- 
	do_replace_aliases_path_extent(Aliases,Aliases,Path,Expr), !.
do_replace_aliases_path(Aliases,aggr,Path,Expr):- 
	do_replace_aliases_path_aggr(Aliases,Path,Expr), !.


do_replace_aliases_path_extent([as(extent(Num,IdAtom),alias(any,Name1))|_],_,path(Type,[property(any,Name2)|Path]),path(Type,[extent(Num,IdAtom)|Path])):- 
	controller:to_upper(Name2,Name3), Name1 = Name3, !.
do_replace_aliases_path_extent([as(extent(Num,IdAtom),alias(any,Name1))|_],_,path(Type1,[cast(property(any,Name2),Type2)|Path]),path(Type1,[cast(extent(Num,IdAtom),Type2)|Path])):- 
	controller:to_upper(Name2,Name3), Name1 = Name3, !.
do_replace_aliases_path_extent([_|Aliases1],Aliases2,Path1,Path2):- 
	do_replace_aliases_path_extent(Aliases1,Aliases2,Path1,Path2), !.
do_replace_aliases_path_extent([],Aliases,path(Type1,[property(Type2,Name)|Path]),path(Type1,[extent(unspec,Extents),property(Type2,Name)|Path])):- 
	get_extents_from_aliases(Aliases,Extents), !.
do_replace_aliases_path_extent([],Aliases,path(Type1,[cast(property(Type2,Name),Type3)|Path]),path(Type1,[extent(unspec,Extents),cast(property(Type2,Name),Type3)|Path])):- 
	get_extents_from_aliases(Aliases,Extents), !.
do_replace_aliases_path_extent([],Aliases,path(Type,[gmethod(any,Name,TParams,Args)|Path]),path(Type,[extent(unspec,Extents),gmethod(any,Name,TParams,Args)|Path])):- 
	get_extents_from_aliases(Aliases,Extents), !.
do_replace_aliases_path_extent([],Aliases,path(Type1,[cast(gmethod(any,Name,TParams,Args),Type2)|Path]),path(Type1,[extent(unspec,Extents),cast(gmethod(any,Name,TParams,Args),Type2)|Path])):- 
	get_extents_from_aliases(Aliases,Extents), !.
do_replace_aliases_path_extent([],Aliases,path(Type,[method(any,Name,Args)|Path]),path(Type,[extent(unspec,Extents),method(any,Name,Args)|Path])):- 
	get_extents_from_aliases(Aliases,Extents), !.
do_replace_aliases_path_extent([],Aliases,path(Type1,[cast(method(any,Name,Args),Type2)|Path]),path(Type1,[extent(unspec,Extents),cast(method(any,Name,Args),Type2)|Path])):- 
	get_extents_from_aliases(Aliases,Extents), !.

get_extents_from_aliases([as(Extent,_)|As],[Extent|Es]):- 
	get_extents_from_aliases(As,Es), !.
get_extents_from_aliases([],[]):- !.


do_replace_aliases_path_aggr([as(path(any,TempRef),path(any,Alias))|_],path(any,Path1),tmpPath(Path2,path(any,Path1))):- 
	lists:append(Alias,RestPath,Path1), !, 
	lists:append(TempRef,RestPath,Path2), !.
do_replace_aliases_path_aggr([_|Aliases],Expr,Path):- 
	do_replace_aliases_path_aggr(Aliases,Expr,Path), !.
do_replace_aliases_path_aggr([],Expr,Expr):- !.


/* do_replace_aliases_setfunction(+Aliases,+SetFunc,-TmpPath):- 
*	Replaces the input set function SetFunc with a temporary path TmpPath according to applicable alias in Aliases.
*/
do_replace_aliases_setfunction([as(path(any,TempRef),SetFunc)|_],SetFunc,tmpPath(TempRef,SetFunc)):- !. 
do_replace_aliases_setfunction([_|Aliases],Expr,Path):- 
	do_replace_aliases_setfunction(Aliases,Expr,Path), !.
do_replace_aliases_setfunction([],Expr,Expr):- !.


/* do_replace_aliases_extentalias(+Aliases,+Id,-Extent):- 
*	Replaces the input extent-alias Id (alias(any,Name)) with an extent reference Extent (extent(Num,IdAtom)) according to applicable alias in Aliases.
*/
do_replace_aliases_extentalias([as(extent(Num,IdAtom),alias(any,Name1))|_],alias(any,Name2),extent(Num,IdAtom)):- 
	controller:to_upper(Name2,Name3), Name1 = Name3, !.
do_replace_aliases_extentalias([_|Aliases],Id,Extent):- 
	do_replace_aliases_extentalias(Aliases,Id,Extent), !.
do_replace_aliases_extentalias([],Id,Id):- !.


/*===== Move join-conditions. =====*/

/* Traverse select. */

move_join_conditions_select(select2(Quant,Columns,From1,Where1,SortSpec,FetchSpec,Hints),Tree):- 
	move_join_conditions_tableref(From1,From2,Cond), 
	concat_cond(Cond,Where1,Where2), 
	Tree = select2(Quant,Columns,From2,Where2,SortSpec,FetchSpec,Hints), !.
move_join_conditions_select(select3(Quant,Columns,From1,Where1,GroupBy,SetFuncs,Having,TempExtent,SortSpec,FetchSpec,Hints),Tree):- 
	move_join_conditions_tableref(From1,From2,Cond), 
	concat_cond(Cond,Where1,Where2), 
	Tree = select3(Quant,Columns,From2,Where2,GroupBy,SetFuncs,Having,TempExtent,SortSpec,FetchSpec,Hints), !.

/* Traverse tableref. */

move_join_conditions_tableref(extent(Num,IdList),extent(Num,IdList),literal(logical,true)):- !.
move_join_conditions_tableref(join1(Type,Table1,Table2,Cond3),join2(Type,Table3,Table4),Cond5):- !, 
	move_join_conditions_tableref(Table1,Table3,Cond1), 
	move_join_conditions_tableref(Table2,Table4,Cond2), 
	concat_cond(Cond1,Cond2,Cond4), 
	concat_cond(Cond4,Cond3,Cond5), !.


/*===== Library predicates. =====*/

concat_cond(literal(logical,true),Cond,Cond):- !.
concat_cond(Cond,literal(logical,true),Cond):- !.
concat_cond(Cond1,Cond2,operation(logical,and,Cond1,Cond2)):- !.




