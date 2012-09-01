// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
using System.Globalization;

/// <summary> <c>PBAtomic</c> is the .NET representation of Prolog constants
/// and variables.
/// </summary>
[Serializable] //Made serializable [PI070502].
public class PBAtomic : Term
{
    virtual internal int Type
    {
        get
        {
            return type;
        }
    }
    /// <summary>
    /// </summary>
    override public bool Atom
    {
        get
        {
            return type == ATOM;
        }
    }
    /// <summary>
    /// </summary>
    override public bool Integer
    {
        get
        {
            return type == INTEGER;
        }
    }
    /// <summary>
    /// </summary>
    override public bool Float
    {
        get
        {
            return type == FLOAT;
        }
    }
    /// <summary>
    /// </summary>
    override public bool Atomic
    {
        get
        {
            return type != VARIABLE;
        }
    }
    /// <summary>
    /// </summary>
    override public bool Variable
    {
        get
        {
            return type == VARIABLE;
        }
    }

    internal const int ATOM = 1;
    internal const int INTEGER = 2;
    internal const int FLOAT = 3;
    internal const int VARIABLE = 4;

    //UPGRADE_NOTE: Final was removed from the declaration of 'type '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
    private int type; //Field.

    /// <summary> Creates a new <c>PBAtomic</c> instance with the specified name
    /// and of the specified Prolog type (integer, floating-point number,
    /// atom, or variable).
    /// </summary>
    internal PBAtomic(System.String name, int type): base(name)
    {
        this.type = type;
    }







    /// <summary>
    /// </summary>
    public override int intValue()
    {
        if (type == INTEGER)
        {
            // [PD] 3.12.5 SPRM 9325
            //      Use the culture-independent NumberFormatInfo to make sure
            //      we can parse the number.
            return System.Int32.Parse(name,
                                      NumberFormatInfo.InvariantInfo);
        }
        else
        {
            throw new System.SystemException("not an integer: " + name);
        }
    }

    /// <summary>
    /// </summary>
    public override long longValue()
    {
        if (type == INTEGER)
        {
            // [PD] 3.12.5 SPRM 9325
            //      Use the culture-independent NumberFormatInfo to make sure
            //      we can parse the number.
            return System.Int64.Parse(name,
                                      NumberFormatInfo.InvariantInfo);
        }
        else
        {
            throw new System.SystemException("not an integer: " + name);
        }
    }

    /// <summary>
    /// </summary>
    public override float floatValue()
    {
        if (type == FLOAT || type == INTEGER)
        {
            // [PD] 3.12.5 SPRM 9325
            //      Use the culture-independent NumberFormatInfo to make sure
            //      we can parse the number.
            return System.Single.Parse(name,
                                       NumberFormatInfo.InvariantInfo);
        }
        else
        {
            throw new System.SystemException("not a number: " + name);
        }
    }

    /// <summary>
    /// </summary>
    public override double doubleValue()
    {
        if (type == FLOAT || type == INTEGER)
        {
            // [PD] 3.12.5 SPRM 9325
            //      Use the culture-independent NumberFormatInfo to make sure
            //      we can parse the number.
            return System.Double.Parse(name,
                                       NumberFormatInfo.InvariantInfo);
        }
        else
        {
            throw new System.SystemException("not a number: " + name);
        }
    }

    internal override System.String toPrologString()
    {
        if (type != ATOM)
        {
            return name;
        }
        return stuffAtom(name);
    }

    /// <summary>
    /// </summary>
    public override System.String ToString()
    {
        return name;
    }

    internal override void  fastWrite(FastWriter writer)
    {
        writer.writeAtomic(this);
    }
}
// PBAtomic
}
