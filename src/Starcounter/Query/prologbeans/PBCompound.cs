// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
/// <summary> <c>PBCompound</c> is the .NET representation of Prolog compound
/// terms and atoms (such as the empty list).
/// </summary>
[Serializable] //Made serializable [PI070502].
public class PBCompound: Term
{
    /// <summary>
    /// </summary>
    override public bool Atom
    {
        // PBCompound constructor
        get
        {
            return Arity == 0;
        }
    }
    /// <summary>
    /// </summary>
    override public bool Atomic
    {
        get
        {
            return Arity == 0;
        }
    }
    /// <summary>
    /// </summary>
    override public int Arity
    {
        get
        {
            return arguments == null ? 0 : arguments.Length;
        }
    }

    //UPGRADE_NOTE: Final was removed from the declaration of 'arguments '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
    /// <summary>
    /// </summary>
    protected internal Term[] arguments; //Field.

    internal PBCompound(System.String name, Term[] args): base(name)
    {
        this.arguments = args;
    }


    /// <summary>
    /// </summary>
    public override Term getArgument(int index)
    {
        if (arguments == null)
        {
            //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
            throw new System.SystemException("not a compound term: " + ToString());
        }
        if (index < 1 || index > arguments.Length)
        {
            throw new System.IndexOutOfRangeException("Index: " + index + ", needs to be between 1 and " + Arity);
        }
        return arguments[index - 1];
    }


    internal override System.String toPrologString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder().Append(stuffAtom(name));
        if (arguments != null)
        {
            sb.Append('(');
            for (int i = 0, n = arguments.Length; i < n; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(arguments[i].toPrologString());
            }
            sb.Append(')');
        }
        return sb.ToString();
    }

    /// <summary>
    /// </summary>
    public override System.String ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder().Append(name);
        if (arguments != null)
        {
            sb.Append('(');
            for (int i = 0, n = arguments.Length; i < n; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
                sb.Append(arguments[i].ToString());
            }
            sb.Append(')');
        }
        return sb.ToString();
    }

    internal override void  fastWrite(FastWriter writer)
    {
        if (arguments != null)
        {
            writer.writeCompound(name, arguments.Length);
            for (int i = 0, n = arguments.Length; i < n; i++)
            {
                arguments[i].fastWrite(writer);
            }
        }
        else
        {
            //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
            throw new System.SystemException("not a compound term: " + ToString());
        }
    }
}
// PBCompound
}
