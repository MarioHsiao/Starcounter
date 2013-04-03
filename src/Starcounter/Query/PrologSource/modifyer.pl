
/* Modifyer, Peter Idestam-Almquist, Starcounter, 2011-11-30. */

/* 11-11-30: Added fetch specification; modified select2, select3. */
/* 11-01-18: Added control that operator is not 'is'. */
/* 10-09-17: Replaces path-expressions in the where clause with joins. */

:- module(modifyer,[]).


/*===== Main. =====*/

modify(noTree,noTypeDef,noNum,noTree,noTypeDef,Err,Err):- !.
modify(Tree1,TypeDef1,Num,Tree2,TypeDef2,Err,Err):- 
	modify_select(Tree1,TypeDef1,Num,Tree2,TypeDef2), !.
modify(_,_,_,noTree,noTypeDef,['Unknown error in module modifyer.'|Err],Err):- !.


modify_select(select2(Quant,Columns,From1,Where1,SortSpec,FetchSpec,Hints),typeDef(Tables1,Mappings),Num,Tree,TypeDef):- !,
	modify_where(Where1,From1,Num,Where2,From2,_,Tables2),
	Tree = select2(Quant,Columns,From2,Where2,SortSpec,FetchSpec,Hints),
	lists:append(Tables1,Tables2,Tables3), 
	TypeDef = typeDef(Tables3,Mappings), !.

/*** 101007: Not modifying aggregation because it is unclear how extent numbers should be handled.
modify_select(select3(Quant,Columns,From1,Where1,GroupBy,SetFuncs,Having,Extent,SortSpec,FetchSpec,Hints),typeDef(Tables1,Mappings),Num,Tree,TypeDef):- !, 
	modify_where(Where1,From1,Num,Where2,From2,_,Tables2),
	Tree = select2(Quant,Columns,From2,Where2,GroupBy,SetFuncs,Having,Extent,SortSpec,FetchSpec,Hints),
	lists:append(Tables1,Tables2,Tables3), 
	TypeDef = typeDef(Tables3,Mappings), !.
***/
modify_select(Tree,TypeDef,_,Tree,TypeDef):- 
	Tree = select3(_,_,_,_,_,_,_,_,_,_,_), !.


modify_where(comparison(Type,Operator,Path1,Expr),From1,Num1,Condition2,From3,Num3,[extent(Num1,IdAtom)|Tables]):- 
	Operator \= is, 
	modify_path(Path1,Num1,Path2,Path3,Num2,IdAtom),
	Comparison1 = comparison(object(IdAtom),equal,Path2,path(object(IdAtom),[extent(Num1,IdAtom)])),
	Comparison2 = comparison(Type,Operator,Path3,Expr),
	Condition1 = operation(logical,and,Comparison1,Comparison2),
	From2 = join2(inner,From1,extent(Num1,IdAtom)), !, 
	modify_where(Condition1,From2,Num2,Condition2,From3,Num3,Tables), !.
modify_where(comparison(Type,Operator,Expr,Path1),From1,Num1,Condition2,From3,Num3,[extent(Num1,IdAtom)|Tables]):- 
	Operator \= is, 
	modify_path(Path1,Num1,Path2,Path3,Num2,IdAtom),
	Comparison1 = comparison(object(IdAtom),equal,Path2,path(object(IdAtom),[extent(Num1,IdAtom)])),
	Comparison2 = comparison(Type,Operator,Expr,Path3),
	Condition1 = operation(logical,and,Comparison1,Comparison2),
	From2 = join2(inner,From1,extent(Num1,IdAtom)), !,
	modify_where(Condition1,From2,Num2,Condition2,From3,Num3,Tables), !.
modify_where(operation(logical,and,Condition1,Condition2),From1,Num1,Condition5,From3,Num3,Tables3):-
	modify_where(Condition1,From1,Num1,Condition3,From2,Num2,Tables1), 
	modify_where(Condition2,From2,Num2,Condition4,From3,Num3,Tables2), 
	Condition5 = operation(logical,and,Condition3,Condition4), 
	lists:append(Tables1,Tables2,Tables3), !.
modify_where(Condition,From,Num,Condition,From,Num,[]):- !.


modify_path(path(Type1,[Extent,cast(property(object(IdAtom),Name),Type2),Property|Path1]),Num1,Path2,Path3,Num2,IdAtom):-
	Num2 is Num1 + 1,
	Path2 = path(object(IdAtom),[Extent,property(object(IdAtom),Name)]), 
	Path3 = path(Type1,[cast(extent(Num1,IdAtom),Type2),Property|Path1]), !.

modify_path(path(Type,[Extent,property(object(IdAtom),Name),Property|Path1]),Num1,Path2,Path3,Num2,IdAtom):-
	Num2 is Num1 + 1,
	Path2 = path(object(IdAtom),[Extent,property(object(IdAtom),Name)]), 
	Path3 = path(Type,[extent(Num1,IdAtom),Property|Path1]), !.




	