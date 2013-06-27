

/* Platform, Peter Idestam-Almquist, Starcounter, 2013-06-04. */

/* Platform dependent configuration. */

/* 13-06-04: Modified is and isNot operators to deal with outer join bug. */
/* 13-04-12: Added new types to dbtype_to_sqltype/2. */
/* 13-03-08: Changed return types of some arithmetic operations and set-functions. */
/* 10-05-24: Modifications of the configuration of comparison types. */
/* 10-05-10: New representation of variable by '?' (Type 'null' replaced by type 'any', and new type 'numerical'.) */
/* 09-01-26: Added string operation addMaxChar to better support STARTS WITH. */
/* 08-07-10: Added sqltype(Type). */
/* 08-07-10: Added support of datatype Binary. */
/* 07-12-12: Added cut (!). */
/* 07-04-14: Allowed specification of value types as 'System.ValueType'. */
/* 06-11-21: Introduced new operator isNot. */
/* 06-10-10: New representation of object reference types 'obj(Type)'. */

:- module(platform,[]).


/* sqltype(+Type):- 
*	Controls that the type is a supported internal SQL-type.
*/
sqltype(binary).
sqltype(boolean).
sqltype(datetime).
sqltype(decimal).
sqltype(double).
sqltype(integer).
sqltype(object(_)).
sqltype(string).
sqltype(uinteger).


/* dbtype_to_sqltype(+DbType,-SqlType):- 
*	Translates a database type DbType to an internal SQL-type SqlType.
*/
dbtype_to_sqltype('Binary',binary):- !.
dbtype_to_sqltype('Boolean',boolean):- !.
dbtype_to_sqltype(bool,boolean):- !.
dbtype_to_sqltype('Byte',uinteger):- !.
dbtype_to_sqltype(byte,uinteger):- !.
dbtype_to_sqltype('DateTime',datetime):- !.
dbtype_to_sqltype(datetime,datetime):- !.
dbtype_to_sqltype('Decimal',decimal):- !.
dbtype_to_sqltype(decimal,decimal):- !.
dbtype_to_sqltype('Double',double):- !.
dbtype_to_sqltype(double,double):- !.
dbtype_to_sqltype('Int16',integer):- !.
dbtype_to_sqltype(short,integer):- !.
dbtype_to_sqltype('Int32',integer):- !.
dbtype_to_sqltype(int,integer):- !.
dbtype_to_sqltype('Int64',integer):- !.
dbtype_to_sqltype(long,integer):- !.
dbtype_to_sqltype('SByte',integer):- !.
dbtype_to_sqltype(sbyte,integer):- !.
dbtype_to_sqltype('Single',double):- !.
dbtype_to_sqltype(float,double):- !.
dbtype_to_sqltype('String',string):- !.
dbtype_to_sqltype(string,string):- !.
dbtype_to_sqltype('UInt16',uinteger):- !.
dbtype_to_sqltype(ushort,uinteger):- !.
dbtype_to_sqltype('UInt32',uinteger):- !.
dbtype_to_sqltype(uint,uinteger):- !.
dbtype_to_sqltype('UInt64',uinteger):- !.
dbtype_to_sqltype(ulong,uinteger):- !.
dbtype_to_sqltype('Starcounter.Binary',binary):- !.
dbtype_to_sqltype('System.Boolean',boolean):- !.
dbtype_to_sqltype('System.Byte',uinteger):- !.
dbtype_to_sqltype('System.DateTime',datetime):- !.
dbtype_to_sqltype('System.Decimal',decimal):- !.
dbtype_to_sqltype('System.Double',double):- !.
dbtype_to_sqltype('System.Int16',integer):- !.
dbtype_to_sqltype('System.Int32',integer):- !.
dbtype_to_sqltype('System.Int64',integer):- !.
dbtype_to_sqltype('System.SByte',integer):- !.
dbtype_to_sqltype('System.Single',double):- !.
dbtype_to_sqltype('System.String',string):- !.
dbtype_to_sqltype('System.UInt16',uinteger):- !.
dbtype_to_sqltype('System.UInt32',uinteger):- !.
dbtype_to_sqltype('System.UInt64',uinteger):- !.
dbtype_to_sqltype(Type,object(Type)):- !.


/* sqltype_to_dbtype(+SqlType,+DbType):- 
*	Controls that the internal SQL-type SqlType implicitely can be converted to 
*	the database type DbType.
*/
sqltype_to_dbtype(binary,'Binary'):- !.
sqltype_to_dbtype(boolean,'Boolean'):- !.
sqltype_to_dbtype(datetime,'DateTime'):- !.
sqltype_to_dbtype(decimal,'Decimal'):- !.
sqltype_to_dbtype(decimal,'Double'):- !.
sqltype_to_dbtype(double,'Double'):- !.
sqltype_to_dbtype(double,'Single'):- !.
sqltype_to_dbtype(integer,'Int64'):- !.
sqltype_to_dbtype(integer,'Int32'):- !.
sqltype_to_dbtype(integer,'Int16'):- !.
sqltype_to_dbtype(integer,'SByte'):- !.
sqltype_to_dbtype(integer,'Decimal'):- !.
sqltype_to_dbtype(integer,'Double'):- !.
sqltype_to_dbtype(string,'String'):- !.
sqltype_to_dbtype(uinteger,'UInt64'):- !.
sqltype_to_dbtype(uinteger,'UInt32'):- !.
sqltype_to_dbtype(uinteger,'UInt16'):- !.
sqltype_to_dbtype(uinteger,'Byte'):- !.
sqltype_to_dbtype(uinteger,'Decimal'):- !.
sqltype_to_dbtype(uinteger,'Double'):- !.
sqltype_to_dbtype(binary,'Starcounter.Binary'):- !.
sqltype_to_dbtype(boolean,'System.Boolean'):- !.
sqltype_to_dbtype(datetime,'System.DateTime'):- !.
sqltype_to_dbtype(decimal,'System.Decimal'):- !.
sqltype_to_dbtype(decimal,'System.Double'):- !.
sqltype_to_dbtype(double,'System.Double'):- !.
sqltype_to_dbtype(double,'System.Single'):- !.
sqltype_to_dbtype(integer,'System.Int64'):- !.
sqltype_to_dbtype(integer,'System.Int32'):- !.
sqltype_to_dbtype(integer,'System.Int16'):- !.
sqltype_to_dbtype(integer,'System.SByte'):- !.
sqltype_to_dbtype(integer,'System.Decimal'):- !.
sqltype_to_dbtype(integer,'System.Double'):- !.
sqltype_to_dbtype(string,'System.String'):- !.
sqltype_to_dbtype(uinteger,'System.UInt64'):- !.
sqltype_to_dbtype(uinteger,'System.UInt32'):- !.
sqltype_to_dbtype(uinteger,'System.UInt16'):- !.
sqltype_to_dbtype(uinteger,'System.Byte'):- !.
sqltype_to_dbtype(uinteger,'System.Decimal'):- !.
sqltype_to_dbtype(uinteger,'System.Double'):- !.
sqltype_to_dbtype(object(Type),Type):- !.
sqltype_to_dbtype(object(_),'Starcounter.IObjectView'):- !.


/* operation_types(+Op,+InType,-OutType):- 
*	Returns the resulting type OutType when applying the unary operator Op on 
*	an argument of type InType.
*/
operation_types(plus,any,numerical):- !.
operation_types(plus,numerical,numerical):- !.
operation_types(plus,integer,integer):- !.
operation_types(plus,uinteger,uinteger):- !.
operation_types(plus,decimal,decimal):- !.
operation_types(plus,double,double):- !.

operation_types(minus,any,numerical):- !.
operation_types(minus,numerical,numerical):- !.
operation_types(minus,integer,integer):- !.
operation_types(minus,uinteger,decimal):- !.
operation_types(minus,decimal,decimal):- !.
operation_types(minus,double,double):- !.

operation_types(addMaxChar,any,string):- !.
operation_types(addMaxChar,string,string):- !.

/* operation_types(+Op,+Intype1,+InType2,-OutType):- 
*	Returns the resulting type OutType when applying the binary operator Op on 
*	arguments of type InType1 and InType2.
*/
operation_types(addition,any,any,numerical):- !.
operation_types(addition,any,numerical,numerical):- !.
operation_types(addition,any,integer,numerical):- !.
operation_types(addition,any,uinteger,numerical):- !.
operation_types(addition,any,decimal,numerical):- !.
operation_types(addition,any,double,numerical):- !.
operation_types(addition,numerical,any,numerical):- !.
operation_types(addition,numerical,numerical,numerical):- !.
operation_types(addition,numerical,integer,numerical):- !.
operation_types(addition,numerical,uinteger,numerical):- !.
operation_types(addition,numerical,decimal,numerical):- !.
operation_types(addition,numerical,double,numerical):- !.
operation_types(addition,integer,any,numerical):- !.
operation_types(addition,integer,numerical,numerical):- !.
operation_types(addition,integer,integer,integer):- !.
operation_types(addition,integer,uinteger,integer):- !.
operation_types(addition,integer,decimal,decimal):- !.
operation_types(addition,integer,double,double):- !.
operation_types(addition,uinteger,any,numerical):- !.
operation_types(addition,uinteger,numerical,numerical):- !.
operation_types(addition,uinteger,integer,integer):- !.
operation_types(addition,uinteger,uinteger,uinteger):- !.
operation_types(addition,uinteger,decimal,decimal):- !.
operation_types(addition,uinteger,double,double):- !.
operation_types(addition,decimal,any,numerical):- !.
operation_types(addition,decimal,numerical,numerical):- !.
operation_types(addition,decimal,integer,decimal):- !.
operation_types(addition,decimal,uinteger,decimal):- !.
operation_types(addition,decimal,decimal,decimal):- !.
operation_types(addition,decimal,double,double):- !.
operation_types(addition,double,any,numerical):- !.
operation_types(addition,double,numerical,numerical):- !.
operation_types(addition,double,integer,double):- !.
operation_types(addition,double,uinteger,double):- !.
operation_types(addition,double,decimal,double):- !.
operation_types(addition,double,double,double):- !.

operation_types(subtraction,any,any,numerical):- !.
operation_types(subtraction,any,numerical,numerical):- !.
operation_types(subtraction,any,integer,numerical):- !.
operation_types(subtraction,any,uinteger,numerical):- !.
operation_types(subtraction,any,decimal,numerical):- !.
operation_types(subtraction,any,double,numerical):- !.
operation_types(subtraction,numerical,any,numerical):- !.
operation_types(subtraction,numerical,numerical,numerical):- !.
operation_types(subtraction,numerical,integer,numerical):- !.
operation_types(subtraction,numerical,uinteger,numerical):- !.
operation_types(subtraction,numerical,decimal,numerical):- !.
operation_types(subtraction,numerical,double,numerical):- !.
operation_types(subtraction,integer,any,numerical):- !.
operation_types(subtraction,integer,numerical,numerical):- !.
operation_types(subtraction,integer,integer,integer):- !.
operation_types(subtraction,integer,uinteger,integer):- !.
operation_types(subtraction,integer,decimal,decimal):- !.
operation_types(subtraction,integer,double,double):- !.
operation_types(subtraction,uinteger,any,numerical):- !.
operation_types(subtraction,uinteger,numerical,numerical):- !.
operation_types(subtraction,uinteger,integer,integer):- !.
operation_types(subtraction,uinteger,uinteger,integer):- !.
operation_types(subtraction,uinteger,decimal,decimal):- !.
operation_types(subtraction,uinteger,double,double):- !.
operation_types(subtraction,decimal,any,numerical):- !.
operation_types(subtraction,decimal,numerical,numerical):- !.
operation_types(subtraction,decimal,integer,decimal):- !.
operation_types(subtraction,decimal,uinteger,decimal):- !.
operation_types(subtraction,decimal,decimal,decimal):- !.
operation_types(subtraction,decimal,double,double):- !.
operation_types(subtraction,double,any,numerical):- !.
operation_types(subtraction,double,numerical,numerical):- !.
operation_types(subtraction,double,integer,double):- !.
operation_types(subtraction,double,uinteger,double):- !.
operation_types(subtraction,double,decimal,double):- !.
operation_types(subtraction,double,double,double):- !.

operation_types(multiplication,any,any,numerical):- !.
operation_types(multiplication,any,numerical,numerical):- !.
operation_types(multiplication,any,integer,numerical):- !.
operation_types(multiplication,any,uinteger,numerical):- !.
operation_types(multiplication,any,decimal,numerical):- !.
operation_types(multiplication,any,double,numerical):- !.
operation_types(multiplication,numerical,any,numerical):- !.
operation_types(multiplication,numerical,numerical,numerical):- !.
operation_types(multiplication,numerical,integer,numerical):- !.
operation_types(multiplication,numerical,uinteger,numerical):- !.
operation_types(multiplication,numerical,decimal,numerical):- !.
operation_types(multiplication,numerical,double,numerical):- !.
operation_types(multiplication,integer,any,numerical):- !.
operation_types(multiplication,integer,numerical,numerical):- !.
operation_types(multiplication,integer,integer,integer):- !.
operation_types(multiplication,integer,uinteger,integer):- !.
operation_types(multiplication,integer,decimal,decimal):- !.
operation_types(multiplication,integer,double,double):- !.
operation_types(multiplication,uinteger,any,numerical):- !.
operation_types(multiplication,uinteger,numerical,numerical):- !.
operation_types(multiplication,uinteger,integer,integer):- !.
operation_types(multiplication,uinteger,uinteger,uinteger):- !.
operation_types(multiplication,uinteger,decimal,decimal):- !.
operation_types(multiplication,uinteger,double,double):- !.
operation_types(multiplication,decimal,any,numerical):- !.
operation_types(multiplication,decimal,numerical,numerical):- !.
operation_types(multiplication,decimal,integer,decimal):- !.
operation_types(multiplication,decimal,uinteger,decimal):- !.
operation_types(multiplication,decimal,decimal,decimal):- !.
operation_types(multiplication,decimal,double,double):- !.
operation_types(multiplication,double,any,numerical):- !.
operation_types(multiplication,double,numerical,numerical):- !.
operation_types(multiplication,double,integer,double):- !.
operation_types(multiplication,double,uinteger,double):- !.
operation_types(multiplication,double,decimal,double):- !.
operation_types(multiplication,double,double,double):- !.

operation_types(division,any,any,numerical):- !.
operation_types(division,any,numerical,numerical):- !.
operation_types(division,any,integer,numerical):- !.
operation_types(division,any,uinteger,numerical):- !.
operation_types(division,any,decimal,numerical):- !.
operation_types(division,any,double,numerical):- !.
operation_types(division,numerical,any,numerical):- !.
operation_types(division,numerical,numerical,numerical):- !.
operation_types(division,numerical,integer,numerical):- !.
operation_types(division,numerical,uinteger,numerical):- !.
operation_types(division,numerical,decimal,numerical):- !.
operation_types(division,numerical,double,numerical):- !.
operation_types(division,integer,any,numerical):- !.
operation_types(division,integer,numerical,numerical):- !.
operation_types(division,integer,integer,decimal):- !.
operation_types(division,integer,uinteger,decimal):- !.
operation_types(division,integer,decimal,decimal):- !.
operation_types(division,integer,double,double):- !.
operation_types(division,uinteger,any,numerical):- !.
operation_types(division,uinteger,numerical,numerical):- !.
operation_types(division,uinteger,integer,decimal):- !.
operation_types(division,uinteger,uinteger,decimal):- !.
operation_types(division,uinteger,decimal,decimal):- !.
operation_types(division,uinteger,double,double):- !.
operation_types(division,decimal,any,numerical):- !.
operation_types(division,decimal,numerical,numerical):- !.
operation_types(division,decimal,integer,decimal):- !.
operation_types(division,decimal,uinteger,decimal):- !.
operation_types(division,decimal,decimal,decimal):- !.
operation_types(division,decimal,double,double):- !.
operation_types(division,double,any,numerical):- !.
operation_types(division,double,numerical,numerical):- !.
operation_types(division,double,integer,double):- !.
operation_types(division,double,uinteger,double):- !.
operation_types(division,double,decimal,double):- !.
operation_types(division,double,double,double):- !.

operation_types(concatenation,any,any,string):- !.
operation_types(concatenation,any,string,string):- !.
operation_types(concatenation,string,any,string):- !.
operation_types(concatenation,string,string,string):- !.

/* setfunction_types(+SetFunc,+Intype,-OutType):- 
*	Returns the resulting type OutType of the set-function SetFunc with 
*	an argument of type InType.
*/
setfunction_types(count,_,integer):- !.
setfunction_types(sum,any,numerical):- !.
setfunction_types(sum,numerical,numerical):- !.
setfunction_types(sum,integer,integer):- !.
setfunction_types(sum,uinteger,uinteger):- !.
setfunction_types(sum,decimal,decimal):- !.
setfunction_types(sum,double,double):- !.
setfunction_types(avg,any,numerical):- !.
setfunction_types(avg,numerical,numerical):- !.
setfunction_types(avg,integer,decimal):- !.
setfunction_types(avg,uinteger,decimal):- !.
setfunction_types(avg,decimal,decimal):- !.
setfunction_types(avg,double,double):- !.
setfunction_types(max,any,string):- !.
setfunction_types(max,numerical,numerical):- !.
setfunction_types(max,binary,binary):- !.
setfunction_types(max,boolean,boolean):- !.
setfunction_types(max,datetime,datetime):- !.
setfunction_types(max,string,string):- !.
setfunction_types(max,integer,integer):- !.
setfunction_types(max,uinteger,uinteger):- !.
setfunction_types(max,decimal,decimal):- !.
setfunction_types(max,double,double):- !.
setfunction_types(min,any,string):- !.
setfunction_types(min,numerical,numerical):- !.
setfunction_types(min,binary,binary):- !.
setfunction_types(min,boolean,boolean):- !.
setfunction_types(min,datetime,datetime):- !.
setfunction_types(min,string,string):- !.
setfunction_types(min,integer,integer):- !.
setfunction_types(min,uinteger,uinteger):- !.
setfunction_types(min,decimal,decimal):- !.
setfunction_types(min,double,double):- !.

/* comparison_types(+Op,+Intype1,+InType2,-CompType):- 
*	Returns the comparison type CompType when applying the comparison operator Op 
*	on arguments of type InType1 and InType2.
*/
comparison_types(equal,any,any,string):- !.
comparison_types(equal,any,numerical,numerical):- !.
comparison_types(equal,any,binary,binary):- !.
comparison_types(equal,any,boolean,boolean):- !.
comparison_types(equal,any,datetime,datetime):- !.
comparison_types(equal,any,string,string):- !.
comparison_types(equal,any,integer,numerical):- !.
comparison_types(equal,any,uinteger,numerical):- !.
comparison_types(equal,any,decimal,numerical):- !.
comparison_types(equal,any,double,numerical):- !.
comparison_types(equal,any,object(Type),object(Type)):- !.
comparison_types(equal,numerical,any,numerical):- !.
comparison_types(equal,numerical,numerical,numerical):- !.
comparison_types(equal,numerical,integer,numerical):- !.
comparison_types(equal,numerical,uinteger,numerical):- !.
comparison_types(equal,numerical,decimal,numerical):- !.
comparison_types(equal,numerical,double,numerical):- !.
comparison_types(equal,binary,any,binary):- !.
comparison_types(equal,binary,binary,binary):- !.
comparison_types(equal,boolean,any,boolean):- !.
comparison_types(equal,boolean,boolean,boolean):- !.
comparison_types(equal,datetime,any,datetime):- !.
comparison_types(equal,datetime,datetime,datetime):- !.
comparison_types(equal,string,any,string):- !.
comparison_types(equal,string,string,string):- !.
comparison_types(equal,integer,any,numerical):- !.
comparison_types(equal,integer,numerical,numerical):- !.
comparison_types(equal,integer,integer,integer):- !.
comparison_types(equal,integer,uinteger,integer):- !.
comparison_types(equal,integer,decimal,decimal):- !.
comparison_types(equal,integer,double,double):- !.
comparison_types(equal,uinteger,any,numerical):- !.
comparison_types(equal,uinteger,numerical,numerical):- !.
comparison_types(equal,uinteger,integer,integer):- !.
comparison_types(equal,uinteger,uinteger,uinteger):- !.
comparison_types(equal,uinteger,decimal,decimal):- !.
comparison_types(equal,uinteger,double,double):- !.
comparison_types(equal,decimal,any,numerical):- !.
comparison_types(equal,decimal,numerical,numerical):- !.
comparison_types(equal,decimal,integer,decimal):- !.
comparison_types(equal,decimal,uinteger,decimal):- !.
comparison_types(equal,decimal,decimal,decimal):- !.
comparison_types(equal,decimal,double,double):- !.
comparison_types(equal,double,any,numerical):- !.
comparison_types(equal,double,numerical,numerical):- !.
comparison_types(equal,double,integer,double):- !.
comparison_types(equal,double,uinteger,double):- !.
comparison_types(equal,double,decimal,double):- !.
comparison_types(equal,double,double,double):- !.
comparison_types(equal,object(Type),any,object(Type)):- !.
comparison_types(equal,object(Type),object(Type),object(Type)):- !.
comparison_types(equal,object(_),object(_),object(unknown)):- !.

comparison_types(notEqual,any,any,string):- !.
comparison_types(notEqual,any,numerical,numerical):- !.
comparison_types(notEqual,any,binary,binary):- !.
comparison_types(notEqual,any,boolean,boolean):- !.
comparison_types(notEqual,any,datetime,datetime):- !.
comparison_types(notEqual,any,string,string):- !.
comparison_types(notEqual,any,integer,numerical):- !.
comparison_types(notEqual,any,uinteger,numerical):- !.
comparison_types(notEqual,any,decimal,numerical):- !.
comparison_types(notEqual,any,double,numerical):- !.
comparison_types(notEqual,any,object(Type),object(Type)):- !.
comparison_types(notEqual,numerical,any,numerical):- !.
comparison_types(notEqual,numerical,numerical,numerical):- !.
comparison_types(notEqual,numerical,integer,numerical):- !.
comparison_types(notEqual,numerical,uinteger,numerical):- !.
comparison_types(notEqual,numerical,decimal,numerical):- !.
comparison_types(notEqual,numerical,double,numerical):- !.
comparison_types(notEqual,binary,any,binary):- !.
comparison_types(notEqual,binary,binary,binary):- !.
comparison_types(notEqual,boolean,any,boolean):- !.
comparison_types(notEqual,boolean,boolean,boolean):- !.
comparison_types(notEqual,datetime,any,datetime):- !.
comparison_types(notEqual,datetime,datetime,datetime):- !.
comparison_types(notEqual,string,any,string):- !.
comparison_types(notEqual,string,string,string):- !.
comparison_types(notEqual,integer,any,numerical):- !.
comparison_types(notEqual,integer,numerical,numerical):- !.
comparison_types(notEqual,integer,integer,integer):- !.
comparison_types(notEqual,integer,uinteger,integer):- !.
comparison_types(notEqual,integer,decimal,decimal):- !.
comparison_types(notEqual,integer,double,double):- !.
comparison_types(notEqual,uinteger,any,numerical):- !.
comparison_types(notEqual,uinteger,numerical,numerical):- !.
comparison_types(notEqual,uinteger,integer,integer):- !.
comparison_types(notEqual,uinteger,uinteger,uinteger):- !.
comparison_types(notEqual,uinteger,decimal,decimal):- !.
comparison_types(notEqual,uinteger,double,double):- !.
comparison_types(notEqual,decimal,any,numerical):- !.
comparison_types(notEqual,decimal,numerical,numerical):- !.
comparison_types(notEqual,decimal,integer,decimal):- !.
comparison_types(notEqual,decimal,uinteger,decimal):- !.
comparison_types(notEqual,decimal,decimal,decimal):- !.
comparison_types(notEqual,decimal,double,double):- !.
comparison_types(notEqual,double,any,numerical):- !.
comparison_types(notEqual,double,numerical,numerical):- !.
comparison_types(notEqual,double,integer,double):- !.
comparison_types(notEqual,double,uinteger,double):- !.
comparison_types(notEqual,double,decimal,double):- !.
comparison_types(notEqual,double,double,double):- !.
comparison_types(notEqual,object(Type),any,object(Type)):- !.
comparison_types(notEqual,object(Type),object(Type),object(Type)):- !.
comparison_types(notEqual,object(_),object(_),object(unknown)):- !.

comparison_types(is(_),any,any,string):- !.
comparison_types(is(_),any,numerical,numerical):- !.
comparison_types(is(_),any,binary,binary):- !.
comparison_types(is(_),any,boolean,boolean):- !.
comparison_types(is(_),any,datetime,datetime):- !.
comparison_types(is(_),any,string,string):- !.
comparison_types(is(_),any,integer,numerical):- !.
comparison_types(is(_),any,uinteger,numerical):- !.
comparison_types(is(_),any,decimal,numerical):- !.
comparison_types(is(_),any,double,numerical):- !.
comparison_types(is(_),any,object(Type),object(Type)):- !.
comparison_types(is(_),numerical,any,numerical):- !.
comparison_types(is(_),numerical,numerical,numerical):- !.
comparison_types(is(_),numerical,integer,numerical):- !.
comparison_types(is(_),numerical,uinteger,numerical):- !.
comparison_types(is(_),numerical,decimal,numerical):- !.
comparison_types(is(_),numerical,double,numerical):- !.
comparison_types(is(_),binary,any,binary):- !.
comparison_types(is(_),binary,binary,binary):- !.
comparison_types(is(_),boolean,any,boolean):- !.
comparison_types(is(_),boolean,boolean,boolean):- !.
comparison_types(is(_),datetime,any,datetime):- !.
comparison_types(is(_),datetime,datetime,datetime):- !.
comparison_types(is(_),string,any,string):- !.
comparison_types(is(_),string,string,string):- !.
comparison_types(is(_),integer,any,numerical):- !.
comparison_types(is(_),integer,numerical,numerical):- !.
comparison_types(is(_),integer,integer,integer):- !.
comparison_types(is(_),integer,uinteger,integer):- !.
comparison_types(is(_),integer,decimal,decimal):- !.
comparison_types(is(_),integer,double,double):- !.
comparison_types(is(_),uinteger,any,numerical):- !.
comparison_types(is(_),uinteger,numerical,numerical):- !.
comparison_types(is(_),uinteger,integer,integer):- !.
comparison_types(is(_),uinteger,uinteger,uinteger):- !.
comparison_types(is(_),uinteger,decimal,decimal):- !.
comparison_types(is(_),uinteger,double,double):- !.
comparison_types(is(_),decimal,any,numerical):- !.
comparison_types(is(_),decimal,numerical,numerical):- !.
comparison_types(is(_),decimal,integer,decimal):- !.
comparison_types(is(_),decimal,uinteger,decimal):- !.
comparison_types(is(_),decimal,decimal,decimal):- !.
comparison_types(is(_),decimal,double,double):- !.
comparison_types(is(_),double,any,numerical):- !.
comparison_types(is(_),double,numerical,numerical):- !.
comparison_types(is(_),double,integer,double):- !.
comparison_types(is(_),double,uinteger,double):- !.
comparison_types(is(_),double,decimal,double):- !.
comparison_types(is(_),double,double,double):- !.
comparison_types(is(_),object(Type),any,object(Type)):- !.
comparison_types(is(_),object(Type),object(Type),object(Type)):- !.
comparison_types(is(_),object(_),object(_),object(unknown)):- !.

comparison_types(isNot(_),any,any,string):- !.
comparison_types(isNot(_),any,numerical,numerical):- !.
comparison_types(isNot(_),any,binary,binary):- !.
comparison_types(isNot(_),any,boolean,boolean):- !.
comparison_types(isNot(_),any,datetime,datetime):- !.
comparison_types(isNot(_),any,string,string):- !.
comparison_types(isNot(_),any,integer,numerical):- !.
comparison_types(isNot(_),any,uinteger,numerical):- !.
comparison_types(isNot(_),any,decimal,numerical):- !.
comparison_types(isNot(_),any,double,numerical):- !.
comparison_types(isNot(_),any,object(Type),object(Type)):- !.
comparison_types(isNot(_),numerical,any,numerical):- !.
comparison_types(isNot(_),numerical,numerical,numerical):- !.
comparison_types(isNot(_),numerical,integer,numerical):- !.
comparison_types(isNot(_),numerical,uinteger,numerical):- !.
comparison_types(isNot(_),numerical,decimal,numerical):- !.
comparison_types(isNot(_),numerical,double,numerical):- !.
comparison_types(isNot(_),binary,any,binary):- !.
comparison_types(isNot(_),binary,binary,binary):- !.
comparison_types(isNot(_),boolean,any,boolean):- !.
comparison_types(isNot(_),boolean,boolean,boolean):- !.
comparison_types(isNot(_),datetime,any,datetime):- !.
comparison_types(isNot(_),datetime,datetime,datetime):- !.
comparison_types(isNot(_),string,any,string):- !.
comparison_types(isNot(_),string,string,string):- !.
comparison_types(isNot(_),integer,any,numerical):- !.
comparison_types(isNot(_),integer,numerical,numerical):- !.
comparison_types(isNot(_),integer,integer,integer):- !.
comparison_types(isNot(_),integer,uinteger,integer):- !.
comparison_types(isNot(_),integer,decimal,decimal):- !.
comparison_types(isNot(_),integer,double,double):- !.
comparison_types(isNot(_),uinteger,any,numerical):- !.
comparison_types(isNot(_),uinteger,numerical,numerical):- !.
comparison_types(isNot(_),uinteger,integer,integer):- !.
comparison_types(isNot(_),uinteger,uinteger,uinteger):- !.
comparison_types(isNot(_),uinteger,decimal,decimal):- !.
comparison_types(isNot(_),uinteger,double,double):- !.
comparison_types(isNot(_),decimal,any,numerical):- !.
comparison_types(isNot(_),decimal,numerical,numerical):- !.
comparison_types(isNot(_),decimal,integer,decimal):- !.
comparison_types(isNot(_),decimal,uinteger,decimal):- !.
comparison_types(isNot(_),decimal,decimal,decimal):- !.
comparison_types(isNot(_),decimal,double,double):- !.
comparison_types(isNot(_),double,any,numerical):- !.
comparison_types(isNot(_),double,numerical,numerical):- !.
comparison_types(isNot(_),double,integer,double):- !.
comparison_types(isNot(_),double,uinteger,double):- !.
comparison_types(isNot(_),double,decimal,double):- !.
comparison_types(isNot(_),double,double,double):- !.
comparison_types(isNot(_),object(Type),any,object(Type)):- !.
comparison_types(isNot(_),object(Type),object(Type),object(Type)):- !.
comparison_types(isNot(_),object(_),object(_),object(unknown)):- !.

comparison_types(lessThan,any,any,string):- !.
comparison_types(lessThan,any,numerical,numerical):- !.
comparison_types(lessThan,any,binary,binary):- !.
comparison_types(lessThan,any,boolean,boolean):- !.
comparison_types(lessThan,any,datetime,datetime):- !.
comparison_types(lessThan,any,string,string):- !.
comparison_types(lessThan,any,integer,numerical):- !.
comparison_types(lessThan,any,uinteger,numerical):- !.
comparison_types(lessThan,any,decimal,numerical):- !.
comparison_types(lessThan,any,double,numerical):- !.
comparison_types(lessThan,numerical,any,numerical):- !.
comparison_types(lessThan,numerical,numerical,numerical):- !.
comparison_types(lessThan,numerical,integer,numerical):- !.
comparison_types(lessThan,numerical,uinteger,numerical):- !.
comparison_types(lessThan,numerical,decimal,numerical):- !.
comparison_types(lessThan,numerical,double,numerical):- !.
comparison_types(lessThan,binary,any,binary):- !.
comparison_types(lessThan,binary,binary,binary):- !.
comparison_types(lessThan,boolean,any,boolean):- !.
comparison_types(lessThan,boolean,boolean,boolean):- !.
comparison_types(lessThan,datetime,any,datetime):- !.
comparison_types(lessThan,datetime,datetime,datetime):- !.
comparison_types(lessThan,string,any,string):- !.
comparison_types(lessThan,string,string,string):- !.
comparison_types(lessThan,integer,any,numerical):- !.
comparison_types(lessThan,integer,numerical,numerical):- !.
comparison_types(lessThan,integer,integer,integer):- !.
comparison_types(lessThan,integer,uinteger,integer):- !.
comparison_types(lessThan,integer,decimal,decimal):- !.
comparison_types(lessThan,integer,double,double):- !.
comparison_types(lessThan,uinteger,any,numerical):- !.
comparison_types(lessThan,uinteger,numerical,numerical):- !.
comparison_types(lessThan,uinteger,integer,integer):- !.
comparison_types(lessThan,uinteger,uinteger,uinteger):- !.
comparison_types(lessThan,uinteger,decimal,decimal):- !.
comparison_types(lessThan,uinteger,double,double):- !.
comparison_types(lessThan,decimal,any,numerical):- !.
comparison_types(lessThan,decimal,numerical,numerical):- !.
comparison_types(lessThan,decimal,integer,decimal):- !.
comparison_types(lessThan,decimal,uinteger,decimal):- !.
comparison_types(lessThan,decimal,decimal,decimal):- !.
comparison_types(lessThan,decimal,double,double):- !.
comparison_types(lessThan,double,any,numerical):- !.
comparison_types(lessThan,double,numerical,numerical):- !.
comparison_types(lessThan,double,integer,double):- !.
comparison_types(lessThan,double,uinteger,double):- !.
comparison_types(lessThan,double,decimal,double):- !.
comparison_types(lessThan,double,double,double):- !.

comparison_types(greaterThan,any,any,string):- !.
comparison_types(greaterThan,any,numerical,numerical):- !.
comparison_types(greaterThan,any,binary,binary):- !.
comparison_types(greaterThan,any,boolean,boolean):- !.
comparison_types(greaterThan,any,datetime,datetime):- !.
comparison_types(greaterThan,any,string,string):- !.
comparison_types(greaterThan,any,integer,numerical):- !.
comparison_types(greaterThan,any,uinteger,numerical):- !.
comparison_types(greaterThan,any,decimal,numerical):- !.
comparison_types(greaterThan,any,double,numerical):- !.
comparison_types(greaterThan,numerical,any,numerical):- !.
comparison_types(greaterThan,numerical,numerical,numerical):- !.
comparison_types(greaterThan,numerical,integer,numerical):- !.
comparison_types(greaterThan,numerical,uinteger,numerical):- !.
comparison_types(greaterThan,numerical,decimal,numerical):- !.
comparison_types(greaterThan,numerical,double,numerical):- !.
comparison_types(greaterThan,binary,any,binary):- !.
comparison_types(greaterThan,binary,binary,binary):- !.
comparison_types(greaterThan,boolean,any,boolean):- !.
comparison_types(greaterThan,boolean,boolean,boolean):- !.
comparison_types(greaterThan,datetime,any,datetime):- !.
comparison_types(greaterThan,datetime,datetime,datetime):- !.
comparison_types(greaterThan,string,any,string):- !.
comparison_types(greaterThan,string,string,string):- !.
comparison_types(greaterThan,integer,any,numerical):- !.
comparison_types(greaterThan,integer,numerical,numerical):- !.
comparison_types(greaterThan,integer,integer,integer):- !.
comparison_types(greaterThan,integer,uinteger,integer):- !.
comparison_types(greaterThan,integer,decimal,decimal):- !.
comparison_types(greaterThan,integer,double,double):- !.
comparison_types(greaterThan,uinteger,any,numerical):- !.
comparison_types(greaterThan,uinteger,numerical,numerical):- !.
comparison_types(greaterThan,uinteger,integer,integer):- !.
comparison_types(greaterThan,uinteger,uinteger,uinteger):- !.
comparison_types(greaterThan,uinteger,decimal,decimal):- !.
comparison_types(greaterThan,uinteger,double,double):- !.
comparison_types(greaterThan,decimal,any,numerical):- !.
comparison_types(greaterThan,decimal,numerical,numerical):- !.
comparison_types(greaterThan,decimal,integer,decimal):- !.
comparison_types(greaterThan,decimal,uinteger,decimal):- !.
comparison_types(greaterThan,decimal,decimal,decimal):- !.
comparison_types(greaterThan,decimal,double,double):- !.
comparison_types(greaterThan,double,any,numerical):- !.
comparison_types(greaterThan,double,numerical,numerical):- !.
comparison_types(greaterThan,double,integer,double):- !.
comparison_types(greaterThan,double,uinteger,double):- !.
comparison_types(greaterThan,double,decimal,double):- !.
comparison_types(greaterThan,double,double,double):- !.

comparison_types(lessThanOrEqual,any,any,string):- !.
comparison_types(lessThanOrEqual,any,numerical,numerical):- !.
comparison_types(lessThanOrEqual,any,binary,binary):- !.
comparison_types(lessThanOrEqual,any,boolean,boolean):- !.
comparison_types(lessThanOrEqual,any,datetime,datetime):- !.
comparison_types(lessThanOrEqual,any,string,string):- !.
comparison_types(lessThanOrEqual,any,integer,numerical):- !.
comparison_types(lessThanOrEqual,any,uinteger,numerical):- !.
comparison_types(lessThanOrEqual,any,decimal,numerical):- !.
comparison_types(lessThanOrEqual,any,double,numerical):- !.
comparison_types(lessThanOrEqual,numerical,any,numerical):- !.
comparison_types(lessThanOrEqual,numerical,numerical,numerical):- !.
comparison_types(lessThanOrEqual,numerical,integer,numerical):- !.
comparison_types(lessThanOrEqual,numerical,uinteger,numerical):- !.
comparison_types(lessThanOrEqual,numerical,decimal,numerical):- !.
comparison_types(lessThanOrEqual,numerical,double,numerical):- !.
comparison_types(lessThanOrEqual,binary,any,binary):- !.
comparison_types(lessThanOrEqual,binary,binary,binary):- !.
comparison_types(lessThanOrEqual,boolean,any,boolean):- !.
comparison_types(lessThanOrEqual,boolean,boolean,boolean):- !.
comparison_types(lessThanOrEqual,datetime,any,datetime):- !.
comparison_types(lessThanOrEqual,datetime,datetime,datetime):- !.
comparison_types(lessThanOrEqual,string,any,string):- !.
comparison_types(lessThanOrEqual,string,string,string):- !.
comparison_types(lessThanOrEqual,integer,any,numerical):- !.
comparison_types(lessThanOrEqual,integer,numerical,numerical):- !.
comparison_types(lessThanOrEqual,integer,integer,integer):- !.
comparison_types(lessThanOrEqual,integer,uinteger,integer):- !.
comparison_types(lessThanOrEqual,integer,decimal,decimal):- !.
comparison_types(lessThanOrEqual,integer,double,double):- !.
comparison_types(lessThanOrEqual,uinteger,any,numerical):- !.
comparison_types(lessThanOrEqual,uinteger,numerical,numerical):- !.
comparison_types(lessThanOrEqual,uinteger,integer,integer):- !.
comparison_types(lessThanOrEqual,uinteger,uinteger,uinteger):- !.
comparison_types(lessThanOrEqual,uinteger,decimal,decimal):- !.
comparison_types(lessThanOrEqual,uinteger,double,double):- !.
comparison_types(lessThanOrEqual,decimal,any,numerical):- !.
comparison_types(lessThanOrEqual,decimal,numerical,numerical):- !.
comparison_types(lessThanOrEqual,decimal,integer,decimal):- !.
comparison_types(lessThanOrEqual,decimal,uinteger,decimal):- !.
comparison_types(lessThanOrEqual,decimal,decimal,decimal):- !.
comparison_types(lessThanOrEqual,decimal,double,double):- !.
comparison_types(lessThanOrEqual,double,any,numerical):- !.
comparison_types(lessThanOrEqual,double,numerical,numerical):- !.
comparison_types(lessThanOrEqual,double,integer,double):- !.
comparison_types(lessThanOrEqual,double,uinteger,double):- !.
comparison_types(lessThanOrEqual,double,decimal,double):- !.
comparison_types(lessThanOrEqual,double,double,double):- !.

comparison_types(greaterThanOrEqual,any,any,string):- !.
comparison_types(greaterThanOrEqual,any,numerical,numerical):- !.
comparison_types(greaterThanOrEqual,any,binary,binary):- !.
comparison_types(greaterThanOrEqual,any,boolean,boolean):- !.
comparison_types(greaterThanOrEqual,any,datetime,datetime):- !.
comparison_types(greaterThanOrEqual,any,string,string):- !.
comparison_types(greaterThanOrEqual,any,integer,numerical):- !.
comparison_types(greaterThanOrEqual,any,uinteger,numerical):- !.
comparison_types(greaterThanOrEqual,any,decimal,numerical):- !.
comparison_types(greaterThanOrEqual,any,double,numerical):- !.
comparison_types(greaterThanOrEqual,numerical,any,numerical):- !.
comparison_types(greaterThanOrEqual,numerical,numerical,numerical):- !.
comparison_types(greaterThanOrEqual,numerical,integer,numerical):- !.
comparison_types(greaterThanOrEqual,numerical,uinteger,numerical):- !.
comparison_types(greaterThanOrEqual,numerical,decimal,numerical):- !.
comparison_types(greaterThanOrEqual,numerical,double,numerical):- !.
comparison_types(greaterThanOrEqual,binary,any,binary):- !.
comparison_types(greaterThanOrEqual,binary,binary,binary):- !.
comparison_types(greaterThanOrEqual,boolean,any,boolean):- !.
comparison_types(greaterThanOrEqual,boolean,boolean,boolean):- !.
comparison_types(greaterThanOrEqual,datetime,any,datetime):- !.
comparison_types(greaterThanOrEqual,datetime,datetime,datetime):- !.
comparison_types(greaterThanOrEqual,string,any,string):- !.
comparison_types(greaterThanOrEqual,string,string,string):- !.
comparison_types(greaterThanOrEqual,integer,any,numerical):- !.
comparison_types(greaterThanOrEqual,integer,numerical,numerical):- !.
comparison_types(greaterThanOrEqual,integer,integer,integer):- !.
comparison_types(greaterThanOrEqual,integer,uinteger,integer):- !.
comparison_types(greaterThanOrEqual,integer,decimal,decimal):- !.
comparison_types(greaterThanOrEqual,integer,double,double):- !.
comparison_types(greaterThanOrEqual,uinteger,any,numerical):- !.
comparison_types(greaterThanOrEqual,uinteger,numerical,numerical):- !.
comparison_types(greaterThanOrEqual,uinteger,integer,integer):- !.
comparison_types(greaterThanOrEqual,uinteger,uinteger,uinteger):- !.
comparison_types(greaterThanOrEqual,uinteger,decimal,decimal):- !.
comparison_types(greaterThanOrEqual,uinteger,double,double):- !.
comparison_types(greaterThanOrEqual,decimal,any,numerical):- !.
comparison_types(greaterThanOrEqual,decimal,numerical,numerical):- !.
comparison_types(greaterThanOrEqual,decimal,integer,decimal):- !.
comparison_types(greaterThanOrEqual,decimal,uinteger,decimal):- !.
comparison_types(greaterThanOrEqual,decimal,decimal,decimal):- !.
comparison_types(greaterThanOrEqual,decimal,double,double):- !.
comparison_types(greaterThanOrEqual,double,any,numerical):- !.
comparison_types(greaterThanOrEqual,double,numerical,numerical):- !.
comparison_types(greaterThanOrEqual,double,integer,double):- !.
comparison_types(greaterThanOrEqual,double,uinteger,double):- !.
comparison_types(greaterThanOrEqual,double,decimal,double):- !.
comparison_types(greaterThanOrEqual,double,double,double):- !.

comparison_types(like,any,any,string):- !.
comparison_types(like,any,string,string):- !.
comparison_types(like,string,any,string):- !.
comparison_types(like,string,string,string):- !.

/* comparison_types(+Op,+Intype1,+InType2,+InType3,-CompType):- 
*	Returns the comparison type CompType when applying the comparison operator Op 
*	on arguments of type InType1, InType2 and InType3.
*/
comparison_types(like,any,any,any,string):- !.
comparison_types(like,any,any,string,string):- !.
comparison_types(like,any,string,any,string):- !.
comparison_types(like,any,string,string,string):- !.
comparison_types(like,string,any,any,string):- !.
comparison_types(like,string,any,string,string):- !.
comparison_types(like,string,string,any,string):- !.
comparison_types(like,string,string,string,string):- !.




