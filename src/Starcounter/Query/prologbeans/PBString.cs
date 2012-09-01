// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
using System.Globalization;
/// <summary> <c>PBString</c> is the .NET representation of Prolog strings
/// (e.g. lists of integers that are interpretable as characters).
/// </summary>
[Serializable] //Made serializable [PI070502].
public class PBString : PBList
{
    /// <summary>
    /// </summary>
    override public bool IsString
    {
        get
        {
            return nextTerm == NIL;
        }
    }
    /// <summary>
    /// </summary>
    override public int Arity
    {
        get
        {
            return value_Renamed.Length == 0 ? 0 : 2;
        }
    }
    /// <summary> Returns the string value of this term.
    /// </summary>
    virtual public System.String String
    {
        get
        {
            return value_Renamed;
        }
    }
    /// <summary> Returns the number of characters in this string.
    /// Please note: this does not correspond to the predicate <code>length/1</code>.
    /// </summary>
    override public int Length
    {
        get
        {
            return value_Renamed.Length;
        }
    }
    // [PM] FIXME: Is the case where (value.length()==0 && nextTerm != NIL) treated correctly?
    // 1. Can this happen, and if not, why?
    //    - currently this can only happen if fastrw write...
    // 2. If it can happen then 'this' should behave as an alias for
    //    nextTerm so *all* Term-methods, not just those redefined
    //    here, should be passed to nextTerm.

    //UPGRADE_NOTE: Final was removed from the declaration of 'value '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
    private System.String value_Renamed; //Field.

    internal PBString(System.String value_Renamed): this(value_Renamed, NIL)
    {
    }

    internal PBString(System.String value_Renamed, Term nextTerm): base(value_Renamed.Length == 0 ? "[]" : ".", null, nextTerm)
    {
        if (value_Renamed.Length == 0 && nextTerm != null)
        {
            throw new System.ArgumentException("illegal string: [|" + nextTerm + "]");
        }
        this.value_Renamed = value_Renamed;
    }


    /// <summary> Returns the first or second argument of this list node. Only
    /// non-empty lists have arguments. Note: the arguments are indexed
    /// from 1 to 2.
    /// <para>
    /// Due to performance reasons this should be avoided if not 100%
    /// necessary. Please use <c>getCharAt(int index)</c> or
    /// <c>getString()</c> for accessing the value of strings.
    /// </para>
    /// </summary>
    /// <param name="index">the (one based) index of the argument
    /// </param>
    /// <returns> the argument as a Term
    /// </returns>
    /// <exception cref="System.SystemException"> if this term is not compound
    /// </exception>
    public override Term getArgument(int index)
    {
        if (value_Renamed.Length == 0)
        {
            //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
            throw new System.SystemException("not a compound term: " + ToString());
        }
        if (index == 1)
        {
            // [PD] 3.12.5 SPRM 9325
            //      Use the culture-independent NumberFormatInfo to make sure
            //      fastrw can parse the number.
            String valueArgString = String.Format(NumberFormatInfo.InvariantInfo,
                                                  "{0}",
                                                  (int)value_Renamed[0]);
            return new PBAtomic(valueArgString,
                                PBAtomic.INTEGER);
        }
        if (index == 2)
        {
            if (value_Renamed.Length > 1)
            {
                return new PBString(value_Renamed.Substring(1), nextTerm);
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
    /// <para>
    /// Due to performance reasons this should be avoided if not 100%
    /// necessary. Please use <c>getCharAt(int index)</c> or
    /// <c>getString()</c> for accessing the value of strings.
    /// </para>
    /// </summary>
    /// <param name="index">the (one based) index of the element in this list
    /// </param>
    /// <returns> the element as a Term
    /// </returns>
    /// <seealso cref="Length" />
    /// <seealso cref="getCharAt" />
    /// <seealso cref="String" />
    public override Term getTermAt(int index)
    {
        // [PD] 3.12.5 SPRM 9325
        //      Use the culture-independent NumberFormatInfo to make sure
        //      fastrw can parse the number.
        String charString = String.Format(NumberFormatInfo.InvariantInfo,
                                          "{0}",
                                          (int)getCharAt(index));
        return new PBAtomic(charString,
                            PBAtomic.INTEGER);
    }

    /// <summary> Returns the character at the specified index in this
    /// string. Note: characters are indexed from 1 to length
    /// </summary>
    /// <param name="index">the (one based) index of the character in this string
    /// </param>
    /// <returns> the character
    /// </returns>
    /// <seealso cref="se.sics.prologbeans.PBString.Length"/>
    public virtual char getCharAt(int index)
    {
        int len = value_Renamed.Length;
        if (0 < index && index <= len)
        {
            return value_Renamed[index - 1];
        }
        else if (len == 0)
        {
            //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
            throw new System.SystemException("not a compound term: " + ToString());
        }
        else
        {
            throw new System.IndexOutOfRangeException("Index: " + index + ", needs to be between 1 and " + len);
        }
    }

    internal override System.String toPrologString()
    {
        System.Text.StringBuilder stuffed = new System.Text.StringBuilder();
        if (nextTerm == NIL)
        {
            stuffed.Append('\"');
            for (int i = 0, n = value_Renamed.Length; i < n; i++)
            {
                char c = value_Renamed[i];
                if (c == '\"')
                {
                    stuffed.Append('\\');
                }
                stuffed.Append(c);
            }
            stuffed.Append('\"');
        }
        else
        {
            stuffed.Append('[');
            for (int i = 0, n = value_Renamed.Length; i < n; i++)
            {
                if (i > 0)
                {
                    stuffed.Append(',');
                }
                stuffed.Append((int) value_Renamed[i]);
            }
            stuffed.Append('|');
            stuffed.Append(nextTerm.toPrologString());
            stuffed.Append(']');
        }
        return stuffed.ToString();
    }

    /// <summary>
    /// </summary>
    public override System.String ToString()
    {
        if (nextTerm == NIL)
        {
            // [PD] 3.12.2 SPRM 8740
            bool printable = true;
            for (int i = 0, n = value_Renamed.Length; i < n && printable; i++)
            {
                char c = value_Renamed[i];
                // is c in ASCII?
                if (c < 32 || c > 126)
                {
                    printable = false;
                }
            }
            if (printable)
            {
                return value_Renamed;
            }
        }
        System.Text.StringBuilder stuffed = new System.Text.StringBuilder();
        stuffed.Append('[');
        for (int i = 0, n = value_Renamed.Length; i < n; i++)
        {
            if (i > 0)
            {
                stuffed.Append(',');
            }
            stuffed.Append((int) value_Renamed[i]);
        }
        stuffed.Append('|');
        //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
        stuffed.Append(nextTerm.ToString());
        stuffed.Append(']');
        return stuffed.ToString();
    }

    internal override void  fastWrite(FastWriter writer)
    {
        writer.writeString(value_Renamed, nextTerm);
    }

    //       // [PD] 3.12.3 Has to be public to be used in PBTest.
    //       //      Something of a kludge, since it may perhaps crash in some cases.
    //       /// <summary>
    //       /// </summary>
    //       public bool equals(String str) {
    //  Term pbs = this;
    //  int i;
    //  for (i = 0; i < str.Length && pbs != NIL; i++) {
    //    int c = pbs.getArgument(1).intValue();
    //    if (c != str[i]) {
    //      return false;
    //    } else {
    //      pbs = pbs.getArgument(2);
    //    }
    //  }
    //  if (i < str.Length || pbs != NIL) {
    //    return false;
    //  }
    //  return true;
    //       }

}

}
