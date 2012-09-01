// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
/// <summary> <c>PBList</c> is the .NET representation of Prolog lists.
/// </summary>
[Serializable] //Made serializable [PI070502].
public class PBList: PBCompound
{
    /// <summary>
    /// </summary>
    override public bool List
    {
        get
        {
            return true;
        }
    }
    /// <summary>
    /// </summary>
    override public int Arity
    {
        get
        {
            return arguments == null ? 0 : 2;
        }
    }
    /// <summary> Returns the length of this list.
    /// Please note: this does not correspond to the predicate <code>length/1</code>.
    /// </summary>
    virtual public int Length
    {
        get
        {
            return arguments == null ? 0 : arguments.Length;
        }
    }
    /// <summary> Returns the end of this list. For closed lists this element is
    /// the empty list.
    /// </summary>
    virtual public Term End
    {
        get
        {
            return nextTerm;
        }
    }

    //UPGRADE_NOTE: Final was removed from the declaration of 'NIL '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
    internal static readonly Term NIL = new PBList(); //Field.

    //UPGRADE_NOTE: Final was removed from the declaration of 'nextTerm '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
    /// <summary>
    /// </summary>
    protected internal Term nextTerm; //Field.

    private PBList(): base("[]", null)
    {
        nextTerm = this;
    }

    internal PBList(Term[] terms): this(terms, null)
    {
    }

    internal PBList(Term[] terms, Term nextTerm): this(terms == null ? "[]" : ".", terms, nextTerm)
    {
    }

    internal PBList(System.String name, Term[] terms, Term nextTerm): base(name, terms)
    {
        this.nextTerm = nextTerm == null ? NIL : nextTerm;
    }


    /// <summary> Returns the first or second argument of this list node. Only
    /// non-empty lists have arguments. Note: the arguments are indexed
    /// from 1 to 2.
    /// Due to performance reasons this should be avoided if not 100%
    /// necessary. Please use <c>getTermAt(int index)</c> for
    /// accessing the elements of lists.
    /// </summary>
    /// <param name="index">the (one based) index of the argument
    /// </param>
    /// <returns> the argument as a Term
    /// </returns>
    /// <exception cref="System.SystemException"> if this term is not compound
    /// </exception>
    /// <seealso cref="se.sics.prologbeans.PBList.getTermAt(int)"/>
    public override Term getArgument(int index)
    {
        if (arguments == null)
        {
            //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
            throw new System.SystemException("not a compound term: " + ToString());
        }
        if (index == 1)
        {
            return arguments[0];
        }
        if (index == 2)
        {
            if (arguments.Length > 1)
            {
                Term[] newTerms = new Term[arguments.Length - 1];
                Array.Copy(arguments, 1, newTerms, 0, newTerms.Length);
                return new PBList(newTerms, nextTerm);
            }
            else
            {
                return nextTerm;
            }
        }
        throw new System.IndexOutOfRangeException("Index: " + index + ", needs to be between 1 and " + Arity);
    }



    /// <summary> Returns the element at the specified index in this list. Note:
    /// the elements are indexed from 1 to length.
    /// </summary>
    /// <param name="index">the (one based) index of the element in this list
    /// </param>
    /// <returns> the element as a Term
    /// </returns>
    /// <seealso cref="se.sics.prologbeans.PBList.Length"/>
    // [PM] should document as one-based
    public virtual Term getTermAt(int index)
    {
        if (arguments == null)
        {
            //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
            throw new System.SystemException("not a compound term: " + ToString());
        }
        if (0 < index && index <= arguments.Length)
        {
            return arguments[index - 1];
        }
        else
        {
            throw new System.IndexOutOfRangeException("Index: " + index + ", needs to be between 1 and " + arguments.Length);
        }
    }


    internal override System.String toPrologString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder().Append('[');
        if (arguments != null)
        {
            for (int i = 0, n = arguments.Length; i < n; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(arguments[i].toPrologString());
            }
        }
        // Only show last term if not nil!
        if (nextTerm != NIL)
        {
            sb.Append('|').Append(nextTerm.toPrologString());
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// </summary>
    public override System.String ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder().Append('[');
        if (arguments != null)
        {
            for (int i = 0, n = arguments.Length; i < n; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
                sb.Append(arguments[i].ToString());
            }
        }
        // Only show last term if not nil!
        if (nextTerm != NIL)
        {
            //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
            sb.Append('|').Append(nextTerm.ToString());
        }
        sb.Append(']');
        return sb.ToString();
    }

    internal override void  fastWrite(FastWriter writer)
    {
        if (arguments != null)
        {
            for (int i = 0, n = arguments.Length; i < n; i++)
            {
                writer.writeList();
                arguments[i].fastWrite(writer);
            }
        }
        if (nextTerm != NIL)
        {
            nextTerm.fastWrite(writer);
        }
        else
        {
            writer.writeNIL();
        }
    }
}
// PBList
}
