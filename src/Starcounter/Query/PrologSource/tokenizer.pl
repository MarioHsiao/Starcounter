

/* Tokenizer, Peter Idestam-Almquist, Starcounter, 2013-04-17. */

/* A tokenizer for SQL following SQL92. */

/* 13-04-17: Added support of strings defined by quote symbol '"'. */
/* 11-11-30: Added reserved words CREATE, DATETIME, DELETE, FETCH, FIRST, INSERT, LIMIT, OFFSET, OFFSETKEY, ONLY, OUT, OUTPUT, PROC, PROCEDURE, ROWS, UNIQUE and UPDATE. */
/* 10-12-13: Added reserved word BINARY. */
/* 10-09-21: Number variables (?N). */
/* 10-05-10: New representation of variable by '?'. */
/* 08-03-07: Added reserved word CAST. */
/* 08-01-16: Added reserved words OBJECT and OBJ. */
/* 07-12-13: Added reserved word FIXED. */
/* 07-12-04: Added reserved word OPTION. */
/* 07-11-29: Added reserved word INDEX. */
/* 07-05-15: Added reserved word STARTS. */
/* 07-04-24: Tokens in the format [token] are not interpreted as reserved words. */
/* 07-04-24: New token format of reserved words <WORD>. */
/* 07-03-27: Added reserved words WITH, EXISTS and FORALL, and symbol ':'. */
/* 07-03-30: Added digit_codes/1. */

:- module(tokenizer,[]).


/*===== Main. =====*/

/* tokenize(+Codes,-Tokens,-VarNum,-ErrHead,-ErrTail):-
*	Creates a list of tokens from a list of ASCII-codes. 
*	Also returns the number of variables VarNum and 
*	a difference list ErrHead-ErrTail of errors.
*/
tokenize(Codes,Tokens2,VarNum,Err,Err):- 
	tokenize2(Codes,Xs,Xs,Tokens1), 
	number_variables(Tokens1,0,Tokens2,VarNum), !.
tokenize(_,[],0,['Unable to tokenize the string.'|Err],Err):- !.


/* tokenize2(+Codes,+Head,+Tail,-Tokens):-
*	Head-Tail is a difference list for accumulating codes to build a token.
*/
tokenize2([],[],[],[]):- !.
tokenize2([],AccCodes,[],[Token]):- !, 
	atom_codes_spec(Token,AccCodes).
tokenize2([Code|Cs],[],[],Tokens):- 
	space_code(Code), !, 
	tokenize2(Cs,List,List,Tokens).
tokenize2([Code|Cs],AccCodes,[],[Token|Ts]):- 
	space_code(Code), !, 
	atom_codes_spec(Token,AccCodes), 
	tokenize2(Cs,List,List,Ts).
tokenize2([Code1,Code2|Cs],[],[],[Token|Ts]):- 
	symbol_codes(Code1,Code2), !, 
	atom_codes_spec(Token,[Code1,Code2]), 
	tokenize2(Cs,List,List,Ts).
tokenize2([Code1,Code2|Cs],AccCodes,[],[Token1,Token2|Ts]):- 
	symbol_codes(Code1,Code2), !, 
	atom_codes_spec(Token1,AccCodes), 
	atom_codes_spec(Token2,[Code1,Code2]), 
	tokenize2(Cs,List,List,Ts).
tokenize2([Code|Cs],[],[],[Token|Ts]):- 
	symbol_code(Code), !, 
	atom_codes_spec(Token,[Code]), 
	tokenize2(Cs,List,List,Ts).
tokenize2([Code|Cs],AccCodes,[],[Token1,Token2|Ts]):- 
	symbol_code(Code), !, 
	atom_codes_spec(Token1,AccCodes), 
	atom_codes_spec(Token2,[Code]), 
	tokenize2(Cs,List,List,Ts).
tokenize2([Code|Cs],[],[],Tokens):- 
	single_quote_code(Code), !, 
	tokenize_str_single(Cs,[Code|List],List,Tokens).
tokenize2([Code|Cs],AccCodes,[],[Token|Ts]):- 
	single_quote_code(Code), !, 
	atom_codes_spec(Token,AccCodes), 
	tokenize_str_single(Cs,[Code|List],List,Ts).
tokenize2([Code|Cs],[],[],Tokens):- 
	double_quote_code(Code), !, 
	tokenize_str_double(Cs,[Code|List],List,Tokens).
tokenize2([Code|Cs],AccCodes,[],[Token|Ts]):- 
	double_quote_code(Code), !, 
	atom_codes_spec(Token,AccCodes), 
	tokenize_str_double(Cs,[Code|List],List,Ts).
tokenize2([Code|Cs],[],[],Tokens):- 
	digit_code(Code), !, 
	tokenize_num(Cs,[Code|List],List,Tokens).
tokenize2([Code|Cs],Head,[Code|Tail],Tokens):- 
	tokenize2(Cs,Head,Tail,Tokens).


/* tokenize_str_single(+Codes,+Head,+Tail,-Tokens):-
*	Head-Tail is a difference list for accumulating codes to build a string-atom, 
*	defined by single quotation marks ('...').
*/
tokenize_str_single([Code1,Code2|Cs],Head,[Code1,Code2|Tail],Tokens):- 
	single_quote_code(Code1), 
	single_quote_code(Code2), !, 
	tokenize_str_single(Cs,Head,Tail,Tokens).
tokenize_str_single([Code|Cs],AccCodes,[Code],[Token|Ts]):- 
	single_quote_code(Code), !, 
	atom_codes(Token,AccCodes), 
	tokenize2(Cs,List,List,Ts).
tokenize_str_single([Code|Cs],Head,[Code|Tail],Tokens):- 
	tokenize_str_single(Cs,Head,Tail,Tokens).


/* tokenize_str_double(+Codes,+Head,+Tail,-Tokens):-
*	Head-Tail is a difference list for accumulating codes to build a string-atom, 
*	defined by double quotation marks ("...").
*/
tokenize_str_double([Code1,Code2|Cs],Head,[Code1,Code2|Tail],Tokens):- 
	double_quote_code(Code1), 
	double_quote_code(Code2), !, 
	tokenize_str_double(Cs,Head,Tail,Tokens).
tokenize_str_double([Code|Cs],AccCodes,[Code],[Token|Ts]):- 
	double_quote_code(Code), !, 
	atom_codes(Token,AccCodes), 
	tokenize2(Cs,List,List,Ts).
tokenize_str_double([Code|Cs],Head,[Code|Tail],Tokens):- 
	tokenize_str_double(Cs,Head,Tail,Tokens).


/* tokenize_num(+Codes,+Head,+Tail,-Tokens):-
*	Head-Tail is a difference list for accumulating codes to build a number-atom.
*/
tokenize_num([],AccCodes,[],[Token]):- !, 
	atom_codes_spec(Token,AccCodes).
tokenize_num([Code|Cs],Head,[Code|Tail],Tokens):- 
	digit_code(Code), !, 
	tokenize_num(Cs,Head,Tail,Tokens).
tokenize_num([Code|Xs],AccCodes,[],[Token1,Token2|Ts]):- 
	exp_symbol_code(Code), !, 
	atom_codes(Token1,AccCodes), 
	internal_format([Code],NewCodes), 
	atom_codes(Token2,NewCodes), 
	tokenize2(Xs,List,List,Ts).
tokenize_num([Code|Cs],AccCodes,[],[Token|Ts]):- 
	atom_codes(Token,AccCodes), 
	tokenize2([Code|Cs],List,List,Ts).


/*===== Token creation. =====*/

/* atom_codes_spec(-Atom,+Codes):- 
*	An atom is constructed from a list of ASCII-codes.
*	A reserved word is returned in the internal format <WORD>, 
*	and a token in the format [Token] is returned as Token. 
*/
atom_codes_spec(Atom,Codes1):- 
	internal_format(Codes1,Codes2), 
	atom_codes(Atom,Codes2), 
	reserved_word(Atom), !.
atom_codes_spec(Atom,Codes1):- 
	fixed_format(Codes1,Codes2), 
	atom_codes(Atom,Codes2).
atom_codes_spec(Atom,Codes):- 
	atom_codes(Atom,Codes).


/* internal_format(+Codes1,-Codes2):- 
*	Translates a token represented by the codes Codes1 to a token represented by the codes Codes2 
*	in the internal format of reserved words '<WORD>'.
*/
internal_format(Codes1,[Code|Codes2]):- 
	reserved_word_begin_code(Code), 
	internal_format2(Codes1,Codes2), !.

internal_format2([],[Code]):- 
	reserved_word_end_code(Code), !.
internal_format2([Code1|Cs1],[Code2|Cs2]):- 
	internal_format3(Code1,Code2), 
	internal_format2(Cs1,Cs2).

internal_format3(Code1,Code2):- 
	Code1 >= 97, 
	Code1 =< 122, 
	Code2 is Code1 - 32, !. /* Upper case. */
internal_format3(Code,Code).


/* fixed_format(+Codes1,-Codes2):- 
*	Translates a token in the fixed format '[token]' represented by the codes Codes1 to a token 
*	in the format 'token' represented by the codes Codes2.
*/
fixed_format([Code|Codes1],Codes2):- 
	fixed_format_begin_code(Code), 
	fixed_format2(Codes1,Codes2), !.

fixed_format2([Code],[]):- !, 
	fixed_format_end_code(Code), !.
fixed_format2([Code|Cs1],[Code|Cs2]):- 
	fixed_format2(Cs1,Cs2).


/*===== Token symbols. =====*/

space_code(9). /* TAB */
space_code(10). /* LF (new line) */
space_code(32). /* ' ' */

single_quote_code(39). /* ''' */

double_quote_code(34). /* '"' */

digit_codes([Code|Cs]):- 
	digit_code(Code), 
	digit_codes(Cs), !.
digit_codes([]):- !.

digit_code(Code):- 
	Code >= 48, 
	Code =< 57.

exp_symbol_code(69). /* 'E' */
exp_symbol_code(101). /* 'e' */

symbol_codes(60,62). /* '<>' */
symbol_codes(62,61). /* '>=' */
symbol_codes(60,61). /* '<=' */
symbol_codes(124,124). /* '||' */
symbol_codes(46,46). /* '..' */

symbol_code(37). /* % */
symbol_code(40). /* '(' */
symbol_code(41). /* ')' */
symbol_code(42). /* '*' */
symbol_code(43). /* '+' */
symbol_code(44). /* ',' */
symbol_code(45). /* '-' */
symbol_code(46). /* '.' */
symbol_code(47). /* '/' */
symbol_code(58). /* ':' */
symbol_code(60). /* '<' */
symbol_code(61). /* '=' */
symbol_code(62). /* '>' */
symbol_code(63). /* '?' */

reserved_word_begin_code(60). /* '<' */
reserved_word_end_code(62). /* '>' */

fixed_format_begin_code(91). /* '[' */
fixed_format_end_code(93). /* ']' */


/*===== Reserved words. =====*/

reserved_word('<ALL>').
reserved_word('<AND>').
reserved_word('<AS>').
reserved_word('<ASC>').
reserved_word('<AVG>').
reserved_word('<BY>').
reserved_word('<BINARY>').
reserved_word('<CAST>').
reserved_word('<COUNT>').
reserved_word('<CREATE>').
reserved_word('<CROSS>').
reserved_word('<DATE>').
reserved_word('<DATETIME>').
reserved_word('<DELETE>').
reserved_word('<DESC>').
reserved_word('<DISTINCT>').
reserved_word('<ESCAPE>').
reserved_word('<EXISTS>').
reserved_word('<FALSE>').
reserved_word('<FETCH>').
reserved_word('<FIRST>').
reserved_word('<FIXED>').
reserved_word('<FORALL>').
reserved_word('<FROM>').
reserved_word('<FULL>').
reserved_word('<GROUP>').
reserved_word('<HAVING>').
reserved_word('<IN>').
reserved_word('<INDEX>').
reserved_word('<INNER>').
reserved_word('<INSERT>').
reserved_word('<IS>').
reserved_word('<JOIN>').
reserved_word('<LEFT>').
reserved_word('<LIKE>').
reserved_word('<LIMIT>').
reserved_word('<MAX>').
reserved_word('<MIN>').
reserved_word('<NOT>').
reserved_word('<NULL>').
reserved_word('<OBJ>').
reserved_word('<OBJECT>').
reserved_word('<OFFSET>').
reserved_word('<OFFSETKEY>').
reserved_word('<ON>').
reserved_word('<ONLY>').
reserved_word('<OPTION>').
reserved_word('<OR>').
reserved_word('<ORDER>').
reserved_word('<OUT>').
reserved_word('<OUTER>').
reserved_word('<OUTPUT>').
reserved_word('<PROC>').
reserved_word('<PROCEDURE>').
reserved_word('<RANDOM>').
reserved_word('<RIGHT>').
reserved_word('<ROWS>').
reserved_word('<SELECT>').
reserved_word('<STARTS>').
reserved_word('<SUM>').
reserved_word('<TIME>').
reserved_word('<TIMESTAMP>').
reserved_word('<TRUE>').
reserved_word('<UNIQUE>').
reserved_word('<UNKNOWN>').
reserved_word('<UPDATE>').
reserved_word('<VALUES>').
reserved_word('<VAR>').
reserved_word('<VARIABLE>').
reserved_word('<WHERE>').
reserved_word('<WITH>').


/*===== Number variables. =====*/

number_variables(['?'|Ts1],Num1,['?',Num1|Ts2],Num3):- !, 
	Num2 is Num1 + 1, 
	number_variables(Ts1,Num2,Ts2,Num3), !. 
number_variables([Token|Ts1],Num1,[Token|Ts2],Num2):- 
	number_variables(Ts1,Num1,Ts2,Num2), !.
number_variables([],Num,[],Num):- !.




