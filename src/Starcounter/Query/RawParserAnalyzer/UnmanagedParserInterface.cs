// Contains mapping of structures and methods from unmanaged C to managed C#

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

//[assembly: InternalsVisibleTo("SQLParserTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100cb72cb1b43c21cfd712282069ce84d8adf9126b15ab29f3124e26b8f92203ec713d42db86b516796d7b94bf2363a9188ba1295ade13e0bf8b47aafacf6d92ce56e4915c2829bea0fa882e3d9648d44db97ccf64a3206ce3b8cd58a60da73aad221e872557734fecb141b98df46e167e2ba1656041b29624b31f64905d4a0f1c1")]

namespace Starcounter.Query.RawParserAnalyzer
{
    internal static class UnmanagedParserInterface
    {
        /// Methods to deal with parser

        [DllImport("Starcounter.SqlParser.dll")]
        internal static extern void InitParser();

        [DllImport("Starcounter.SqlParser.dll")]
        internal static unsafe extern List* ParseQuery([MarshalAs(UnmanagedType.LPWStr)]string query, int* scerrorcode);

        [DllImport("Starcounter.SqlParser.dll")]
        internal static unsafe extern ScError* GetScError();

        [DllImport("Starcounter.SqlParser.dll")]
        internal static extern void ResetMemoryContext();

        [DllImport("Starcounter.SqlParser.dll")]
        internal static extern bool DumpMemoryLeaks();

        [DllImport("Starcounter.SqlParser.dll")]
        internal static extern bool CleanMemoryContext();

        /// Interface to help methods on unmanaged structures

        [DllImport("Starcounter.SqlParser.dll")]
        private static unsafe extern IntPtr StrVal(Node* node);

        internal static unsafe String GetStrVal(Node* node) {
            return Marshal.PtrToStringAuto(StrVal(node));
        }

        [DllImport("Starcounter.SqlParser.dll")]
        internal static unsafe extern int Location(Node* node);

        [DllImport("Starcounter.SqlParser.dll")]
        internal static unsafe extern List* LAppend(List* flist, Node* slist);

        [DllImport("Starcounter.SqlParser.dll")]
        internal static unsafe extern List* LCons(Node* flist, List* slist);
    }

    #region General structures

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct ScError
        {
            internal int scerrorcode;
            internal sbyte* scerrmessage;
            internal int scerrposition;
            internal byte isKeyword;
            private IntPtr _token;

            internal String token {
                get {
                    return Marshal.PtrToStringAuto(_token);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct List
        {
            internal NodeTag type;
            internal int length;
            internal ListCell* head;
            internal ListCell* tail;
        }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct Data
    {
        [FieldOffset(0)]
        internal void* ptr_value;
        [FieldOffset(0)]
        internal int int_value;
        [FieldOffset(0)]
        internal int oid_value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ListCell
    {
        internal Data data;
        internal ListCell* next;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Node
    {
        internal NodeTag type;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct ValUnion
    {
        [FieldOffset(0)]
        internal long ival;		/* machine integer */
        [FieldOffset(0)]
        private IntPtr _str;		/* string */

        internal String str {
            get {
                return Marshal.PtrToStringAuto(_str);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Value
    {
        internal NodeTag type;			/* tag appropriately (eg. T_String) */
        internal ValUnion val;
    }

    #endregion

    #region Select statement structures
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SelectStmt
    {
        internal NodeTag type;

        /*
         * These fields are used only in "leaf" SelectStmts.
         */
        internal List* distinctClause; /* NULL, list of DISTINCT ON exprs, or
								 * lcons(NIL,NIL) for all (SELECT DISTINCT) */
        internal Node* intoClause;		/* target for SELECT INTO / CREATE TABLE AS */
        internal List* targetList;		/* the target list (of ResTarget) */
        internal List* fromClause;		/* the FROM clause */
        internal Node* whereClause;	/* WHERE qualification */
        internal List* groupClause;	/* GROUP BY clauses */
        internal Node* havingClause;	/* HAVING conditional-expression */
        internal List* windowClause;	/* WINDOW window_name AS (...), ... */
        internal Node* withClause;		/* WITH clause */

        /*
         * In a "leaf" node representing a VALUES list, the above fields are all
         * null, and instead this field is set.  Note that the elements of the
         * sublists are just expressions, without ResTarget decoration. Also note
         * that a list element can be DEFAULT (represented as a SetToDefault
         * node), regardless of the context of the VALUES list. It's up to parse
         * analysis to reject that where not valid.
         */
        internal List* valuesLists;	/* untransformed list of expression lists */

        /*
         * These fields are used in both "leaf" SelectStmts and upper-level
         * SelectStmts.
         */
        internal List* sortClause;		/* sort clause (a list of SortBy's) */
        internal Node* limitOffset;	/* # of result tuples to skip */
        //internal Node* limitCount;		/* # of result tuples to return */
        internal List* lockingClause;	/* FOR UPDATE (list of LockingClause's) */
        internal List* optionClause;   /* option clause defining hints*/

        /*
         * These fields are used only in upper-level SelectStmts.
         */
        internal SetOperation op;			/* type of set op */
        internal bool all;			/* ALL specified? */
        internal SelectStmt* larg;	/* left child */
        internal SelectStmt* rarg;	/* right child */
        /* Eventually add fields for CORRESPONDING spec here */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Alias
    {
        internal NodeTag type;
        private IntPtr _aliasname;		/* aliased rel name (never qualified) */
        internal List* colnames;		/* optional list of column aliases */

        internal String aliasname {
            get {
                return Marshal.PtrToStringAuto(_aliasname);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RangeVar
    {
        internal NodeTag type;
        internal List* path;     /* Names of namespaces in order from most outer to most inner */
        private IntPtr _relname;		/* the relation/sequence name */
        internal InhOption inhOpt;			/* expand rel by inheritance? recursively act
								 * on children? */
        internal char relpersistence; /* see RELPERSISTENCE_* in pg_class.h */
        internal Alias* alias;			/* table alias & optional column aliases */
        internal int location;		/* token location, or -1 if unknown */

        internal String relname {
            get {
                return Marshal.PtrToStringAuto(_relname);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RangeSubselect
    {
        internal NodeTag type;
        internal Node* subquery;		/* the untransformed sub-select clause */
        internal Alias* alias;			/* table alias & optional column aliases */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ResTarget
    {
        internal NodeTag type;
        private IntPtr _name;			/* column name or NULL */
        internal List* indirection;	/* subscripts, field names, and '*', or NIL */
        internal Node* val;			/* the value expression to compute or assign */
        private int int_location;		/* token location, or -1 if unknown */

        internal String name {
            get {
                return Marshal.PtrToStringAuto(_name);
            }
        }
        //internal int location {
        //    get {
        //        return int_location > 0 ? int_location / 2 : int_location;
        //    }
        //}
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ColumnRef
    {
        internal NodeTag type;
        internal List* fields;			/* field names (Value strings) or A_Star */
        private IntPtr _name;			/* name of a filed */
        internal int location;		/* token location, or -1 if unknown */

        internal String name {
            get {
                return Marshal.PtrToStringAuto(_name);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FuncCall
    {
        internal NodeTag type;
        private IntPtr _funcname;		/* qualified name of function */
        internal List* args;			/* the arguments (list of exprs) */
        internal List* agg_order;		/* ORDER BY (list of SortBy) */
        internal bool agg_star;		/* argument was really '*' */
        internal bool agg_distinct;	/* arguments were labeled DISTINCT */
        internal bool func_variadic;	/* last argument was labeled VARIADIC */
        internal WindowDef* over;		/* OVER clause, if any */
        internal List* generics;		/* Generic method invocation */
        internal int location;		/* token location, or -1 if unknown */

        internal String funcname {
            get {
                return Marshal.PtrToStringAuto(_funcname);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WindowDef
    {
        internal NodeTag type;
        private IntPtr _name;			/* window's own name */
        private IntPtr _refname;		/* referenced window name, if any */
        internal List* partitionClause;	/* PARTITION BY expression list */
        internal List* orderClause;	/* ORDER BY (list of SortBy) */
        internal int frameOptions;	/* frame_clause options, see below */
        internal Node* startOffset;	/* expression for starting bound, if any */
        internal Node* endOffset;		/* expression for ending bound, if any */
        internal int location;		/* parse location, or -1 if none/unknown */

        internal String name {
            get {
                return Marshal.PtrToStringAuto(_name);
            }
        }
        internal String refname {
            get {
                return Marshal.PtrToStringAuto(_refname);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct A_Const
    {
        internal NodeTag type;
        internal Value val;			/* value (includes type info, see value.h) */
        internal int location;		/* token location, or -1 if unknown */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TypeCast
    {
        internal NodeTag type;
        internal Node* arg;			/* the expression being casted */
        internal TypeName* typeName;		/* the target type */
        internal int location;		/* token location, or -1 if unknown */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TypeName
    {
        internal NodeTag type;
        internal List* names;			/* qualified name (list of Value strings) */
        internal int typeOid;		/* type identified by OID */
        internal bool setof;			/* is a set? */
        internal bool pct_type;		/* %TYPE specified? */
        internal List* typmods;		/* type modifier expression(s) */
        internal int typemod;		/* prespecified type modifier */
        internal List* arrayBounds;	/* array bounds */
        internal List* generics;		/* Type list of generic type */
        internal int location;		/* token location, or -1 if unknown */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct A_Expr
    {
        internal NodeTag type;
        internal A_Expr_Kind kind;			/* see above */
        internal List* name;			/* possibly-qualified name of operator */
        internal Node* lexpr;			/* left argument, or NULL if none */
        internal Node* rexpr;			/* right argument, or NULL if none */
        internal int location;		/* token location, or -1 if unknown */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SortBy
    {
        internal NodeTag type;
        internal Node* node;			/* expression to sort on */
        internal SortByDir sortby_dir;		/* ASC/DESC/USING/default */
        internal SortByNulls sortby_nulls;	/* NULLS FIRST/LAST */
        internal List* useOp;			/* name of op to use, if SORTBY_USING */
        internal int location;		/* operator location, or -1 if none/unknown */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct LimitOffset
    {
        internal NodeTag type;
        internal Node* limitCount;     /* # of result tuples to return */
        internal Node* limitOffset;    /* # of result tuples to skip */
        internal Node* limitOffsetkey;  /* or index value to start */
        internal int location;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ParamRef
    {
        internal NodeTag type;
        internal int number;			/* the number of the parameter */
        internal int location;		/* token location, or -1 if unknown */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct A_Indirection
    {
        internal NodeTag type;
        internal Node* arg;			/* the thing being selected from */
        internal List* indirection;	/* subscripts and/or field names and/or * */
    }

    #endregion
}
