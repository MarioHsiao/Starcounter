// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
/// <summary> <c>Term</c> is the base for .NET representations of Prolog terms.
/// </summary>
[Serializable] //Made serializable [PI070502].
public abstract class Term
{
    /// <summary> Returns <c>true</c> if this term is an atom and
    /// <c>false</c> otherwise.
    /// </summary>
    virtual public bool Atom
    {
        get
        {
            return false;
        }
    }
    /// <summary> Returns <c>true</c> if this term is an integer and
    /// <c>false</c> otherwise.
    /// </summary>
    virtual public bool Integer
    {
        get
        {
            return false;
        }
    }
    /// <summary> Returns <c>true</c> if this term is a floating-point number
    /// and <c>false</c> otherwise.
    /// </summary>
    virtual public bool Float
    {
        get
        {
            return false;
        }
    }
    /// <summary> Returns <c>true</c> if this term is a compund term and
    /// <c>false</c> otherwise.
    /// </summary>
    virtual public bool Compound
    {
        get
        {
            return Arity > 0;
        }
    }
    /// <summary> Returns <c>true</c> if this term is a list and
    /// <c>false</c> otherwise.
    /// </summary>
    virtual public bool List
    {
        get
        {
            return Arity == 2 && ".".Equals(Name);
        }
    }
    /// <summary> Returns <c>true</c> if this term is an instance
    /// of <c>PBString</c> (which can be used for fast string access) and
    /// <c>false</c> otherwise.
    /// </summary>
    //    virtual public bool String
    virtual public bool IsString
    {
        // [PM] FIXME: this is not true. For Terms received from
        // library(fastrw) it is true only if the character codes are
        // [1..255], i.e., not for NUL nor for non-Latin-1 UNICODE but for
        // Terms created with Bindings.bind(String,String) it is true for
        // all characters. Arguably the documentation is right and the code
        // is wrong but it may be too expensive to implement isString to
        // work not only for PBString but also for PBList and Compund (and
        // perhaps Atom if the atom "[]" can ever result in an Atom instead
        // of PBList.NIL. On the other hand, the common case is likely to
        // be when it is a PBString so the extra cost for other classes may
        // not be an issue.
        // [PM] FIXME: This also affects prologbeans.texi
        // [JE] Documentation updated. Should probably try to find another
        //      solution to this.
        //
        get
        {
            return false;
        }
    }
    /// <summary> Returns <c>true</c> if this term is a constant
    /// (e.g. integer, floating-point number, or atom) and
    /// <c>false</c> if this term is a compound term or variable.
    /// </summary>
    virtual public bool Atomic
    {
        get
        {
            return false;
        }
    }
    /// <summary> Returns <c>true</c> if this term is a variable and
    /// <c>false</c> otherwise.
    /// </summary>
    virtual public bool Variable
    {
        get
        {
            return false;
        }
    }
    /// <summary> Returns the name of this constant or compound term.
    /// </summary>
    virtual public System.String Name
    {
        get
        {
            return name;
        }
    }
    /// <summary> Returns the number of arguments of this compound term or 0 if this
    /// term is not a compound term.
    /// </summary>
    virtual public int Arity
    {
        get
        {
            return 0;
        }
    }

    //UPGRADE_NOTE: Final was removed from the declaration of 'name '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
    /// <summary>
    /// </summary>
    protected internal System.String name; //Field.

    /// <summary> Creates a new <c>Term</c> instance with the specified name.
    /// </summary>
    // [PM] FIXME: Should there not be any (static) methods for creating instances of (classes derived from) Term??
    // [JE] Possibly (even probably) in later versions.
    internal Term(System.String name)
    {
        if (name == null)
        {
            throw new System.NullReferenceException();
        }
        this.name = name;
    }
    // Term constructor




    /// <summary> Returns the argument at the specified index. Only compound terms
    /// have arguments. Note: the arguments are indexed from 1 to arity.
    /// </summary>
    /// <param name="index">the (one based) index of the argument
    /// </param>
    /// <returns> the argument as a Term
    /// </returns>
    /// <exception cref="System.SystemException"> if this term is not compound
    /// </exception>
    public virtual Term getArgument(int index)
    {
        //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
        throw new System.SystemException("not a compound term: " + ToString());
    }


    /// <summary> Returns the integer value of this term.
    /// <exception cref="System.SystemException"> if this term is not an integer
    /// </exception>
    /// </summary>
    public virtual int intValue()
    {
        //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
        throw new System.SystemException("not an integer: " + ToString());
    }

    /// <summary> Returns the integer value of this term.
    /// <exception cref="System.SystemException"> if this term is not an integer
    /// </exception>
    /// </summary>
    public virtual long longValue()
    {
        //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
        throw new System.SystemException("not an integer: " + ToString());
    }

    /// <summary> Returns the floating-point value of this term.
    /// <exception cref="System.SystemException"> if this term is not a number
    /// </exception>
    /// </summary>
    public virtual float floatValue()
    {
        //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
        throw new System.SystemException("not a number: " + ToString());
    }

    /// <summary> Returns the floating-point value of this term.
    /// <exception cref="System.SystemException"> if this term is not a number
    /// </exception>
    /// </summary>
    public virtual double doubleValue()
    {
        //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
        throw new System.SystemException("not a number: " + ToString());
    }

    /// <summary> For internal use by PrologBeans.
    /// Returns a string representation of this term in a format that can be
    /// parsed by a Prolog parser.
    /// </summary>

    // [PM] FIXME: Should use fastrw-format to send data to Prolog as
    // well. It will be a nightmare trying to produce properly quoted
    // terms in a way that can be read correctly by Prolog. If that is
    // not hard enough try making it work with non-8-bit characters and
    // then start flipping the prolog-flags 'language' (ISO have
    // different quoting rules than SICStus), 'double_quotes' and
    // 'character_escapes'. (Did I mention that I think relying on the
    // prolog reader is a bad idea for the release version :-).
    // [JE] Fixed using writeFast() (toPrologString() not used anymore)

    internal abstract System.String toPrologString();

    internal abstract void  fastWrite(FastWriter writer);

    /// <summary> Returns a string description of this term.
    /// </summary>
    //    public override System.String ToString();
    public override abstract System.String ToString();


    // -------------------------------------------------------------------
    // Internal utilities
    // -------------------------------------------------------------------

    private int getFirstStuffing(System.String atom)
    {
        int len = atom.Length;
        if (len == 0)
        {
            return 0;
        }
        char c = atom[0];
        if (c < 'a' || c > 'z')
        {
            return 0;
        }
        for (int i = 1; i < len; i++)
        {
            c = atom[i];
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c == '_')))
            {
                return i;
            }
        }
        return - 1;
    }

    /// <summary>
    /// </summary>
    protected internal virtual System.String stuffAtom(System.String atom)
    {
        int start = getFirstStuffing(atom);
        if (start < 0)
        {
            // No stuffing needed
            return atom;
        }
        int len = atom.Length;
        char[] buf = new char[start + (len - start) * 2 + 2];
        int index = 1;
        buf[0] = '\'';
        if (start > 0)
        {
            SupportClass.GetCharsFromString(atom, 0, start, ref buf, 1);
            index += start;
        }
        for (int i = start; i < len; i++)
        {
            char c = atom[i];
            if (c == '\'')
            {
                buf[index++] = '\\';
            }
            buf[index++] = c;
        }
        buf[index++] = '\'';
        return new System.String(buf, 0, index);
    }
}
// Term
}
