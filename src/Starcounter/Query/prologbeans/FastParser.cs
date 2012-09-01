// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
/// <summary> <c>FastParser</c>
/// Documentation of the fast_write/read "protocol"
/// Each "string" looks like:
/// [PM] 3.10.2 Updated specification
/// D TYPE [type-specific data]
/// D -> start of fastrw Term (D is also the version number)
/// TYPE, one byte for type:
/// I - integer - argument is a arbitrary long, nonempty string of
/// ASCII decimal digits, prefixed by a minus sign if negative
/// There should be no plus sign prefixed. There is no size restriction.
/// F - float - argument is an ASCII string of digits with a decimal
/// point preceeded by at least one digit, optionally using
/// E-notation (capital E) with exponent optionally prefixed by
/// minus sign. The floating point number is prefixed by minus if
/// negative
/// A - atom - argument is an ATOMNAME (*)
/// Make UTF-8 explicit (QP loses here). (Luckily this should be exactly
/// the Java String.getbytes bytes.)
/// _ - variable (followed by DATA which is a string of digits). The
/// variables are numbered 0..n in depth first traversal
/// increasing argument order. This is the change in version 'D'
/// compared to version 'C'. The variable numbering is strictly defined.
/// S ATOMNAME n - compound with n terms [followed by n terms] - n is
/// an (unsigned) byte
/// " - a list prefix consisting of only integers in [1..255] (*)
/// the sequence is terminated by zero byte followed by the tail
/// term. Note that this is just a compact representation of
/// nested compound terms with functor ./2.
/// Example "ab", i.e., .(97, .(98, [])) becomes (using C
/// char-constant notation) '"' 'a' 'b' '\0' ']'; [x,97,98|y]
/// becomes '[' 'A' 'x' '\0' '"' 'a' 'b' '\0' 'A' 'y' '\0'
/// > Clarified that this encoding is used for any "list"-prefix with
/// > suitable elements. In particular it is not always followed by ']'.
/// > Also note that the elements of this kind of list should *not* be
/// > treated by the reader as individual bytes of an UTF-8 encoded
/// > string. If a list is to be treated as a string then each element
/// > should be treated as a single UNICODE character, this holds for
/// > lists transmitted using any of the three ('"', '[' or 'S' '.' '\0'
/// > '\2') possible serialization-formats.
/// >
/// [ - a list cell, i.e, a shortcut for 'S' '.' '\0' '\2' (*)
/// ] - empty list, i.e, a shortcut for 'A' '[' ']' '\0' (*)
/// DATA - a byte sequence terminated by '\0'
/// NOTE: As of verson 'D' the numbering must be sequential from
/// _0.._n, the prolog side fast_read will most likely
/// crash if * this invariant is not maintained when
/// sending data to * prolog.
/// ATOMNAME is DATA where the bytes make up the UTF-8 name of the
/// atom.
/// (*) These could be optional in the writer since there are longer
/// equivalent forms from which the reader could produce the same term.
/// </summary>

class FastParser
{

    internal const sbyte VERSION = (sbyte) 'D';
    internal const sbyte INTEGER = (sbyte) 'I';
    internal const sbyte FLOAT = (sbyte) 'F';
    internal const sbyte ATOM = (sbyte) 'A';
    internal const sbyte COMPOUND = (sbyte) 'S';
    internal const sbyte VARIABLE = (sbyte) '_';
    internal const sbyte STRING = (sbyte) '"';
    internal const sbyte LIST = (sbyte) '[';
    internal const sbyte NIL = (sbyte) ']';

    private System.Collections.Hashtable variableTable = null;

    public FastParser()
    {
    }

    public virtual Term parseProlog(System.IO.Stream stream)
    {
        int c;
        try
        {
            if ((c = stream.ReadByte()) == VERSION)
            {
                return parseTerm(- 1, stream);
            }
            else
            {
                System.Console.Out.WriteLine("Not a fast prolog expression" + c);
            }
        }
        finally
        {
            if (variableTable != null)
            {
                variableTable.Clear();
            }
        }
        return null;
    }

    private Term parseTerm(int pch, System.IO.Stream stream)
    {
        System.String val;
        int chr = pch == - 1 ? stream.ReadByte() : pch;
        switch (chr)
        {
            case INTEGER:
                val = getString(stream);
                return new PBAtomic(val, PBAtomic.INTEGER);
            case FLOAT:
                val = getString(stream);
                return new PBAtomic(val, PBAtomic.FLOAT);
            case ATOM:
                val = getString(stream);
                return new PBAtomic(val, PBAtomic.ATOM);
            case VARIABLE:
                val = "" + '_' + getString(stream);
                if (variableTable == null)
                {
                    variableTable = new System.Collections.Hashtable();
                }
                PBAtomic atomic = (PBAtomic) variableTable[val];
                if (atomic == null)
                {
                    atomic = new PBAtomic(val, PBAtomic.VARIABLE);
                    SupportClass.PutElement(variableTable, val, atomic);
                }
                return atomic;
            case STRING:
                // [PD] 3.12.3
                //  val = getString(stream);
                val = getBytes(stream);
                Term t = parseTerm(- 1, stream);
                return new PBString(val, t);
            case LIST:
                return parseList(stream);
            case NIL:
                return PBList.NIL;
            case COMPOUND:
                val = getString(stream);
                int noTerms = stream.ReadByte();
                Term[] terms = new Term[noTerms];
                for (int i = 0; i < noTerms; i++)
                {
                    terms[i] = parseTerm(- 1, stream);
                }
                return new PBCompound(val, terms);
            default:
                throw new System.IO.IOException("Parse error: illegal character " + (char) chr);
        }
    }

    /* [PD] 3.12.1 Bogus
    private System.String getString(System.IO.Stream stream)
    {
      int c;
      System.Text.StringBuilder sBuff = new System.Text.StringBuilder();
      while ((c = stream.ReadByte()) != '\x0000')
      {
        sBuff.Append((char) c);
      }
      return sBuff.ToString();
    }*/

    // [PD] 3.12.1 Correct version, which handles UTF-8.
    // FIXME: For Java 5 (a.k.a. 1.5) we have to revise this.
    private System.String getString(System.IO.Stream stream)
    {
        int c;
        System.IO.MemoryStream byteBuff = new System.IO.MemoryStream();
        while ((c = stream.ReadByte()) != '\x0000')
        {
            byteBuff.WriteByte((System.Byte) c);
        }
        System.Text.Encoding encoding = System.Text.Encoding.UTF8;
        return encoding.GetString(byteBuff.ToArray());
    }

    // [PD] 3.12.3
    private String getBytes(System.IO.Stream stream)
    {
        int c;
        System.IO.MemoryStream byteBuff = new System.IO.MemoryStream();
        while ((c = stream.ReadByte()) != '\x0000')
        {
            byteBuff.WriteByte((System.Byte) c);
        }
        System.Text.Encoding encoding = System.Text.Encoding.GetEncoding("ISO-8859-1");
        return encoding.GetString(byteBuff.ToArray());
    }

    private PBList parseList(System.IO.Stream stream)
    {
        int index = 0;
        int c;
        // First read a term, then check "end of list" or cont...
        Term[] list = addTerm(new Term[5], index++, parseTerm(- 1, stream));
        while ((c = stream.ReadByte()) == LIST)
        {
            list = addTerm(list, index++, parseTerm(- 1, stream));
        }
        if (index < list.Length)
        {
            Term[] newList = new Term[index];
            Array.Copy(list, 0, newList, 0, index);
            list = newList;
        }
        Term endTerm = parseTerm(c, stream);
        return new PBList(list, endTerm);
    }

    private Term[] addTerm(Term[] list, int index, Term term)
    {
        if (index >= list.Length)
        {
            // [PM] in fact index == list.length
            Term[] newList = new Term[list.Length * 2];
            Array.Copy(list, 0, newList, 0, list.Length);
            list = newList;
        }
        list[index] = term;
        return list;
    }

    //   public static void main(String[] args) throws IOException {
    //     byte[] data = "CSa\0\2Aa\0Aa\0".getBytes();
    //     FastParser parser = new FastParser();
    //     Term t;
    //     if (args.length == 1) {
    //       t = parser.parseProlog(new java.io.FileInputStream(args[0]));
    //     } else {
    //       t = parser.parseProlog(new java.io.ByteArrayInputStream(data));
    //     }
    //     // Print to see what is read in...
    //     print("", t);
    //   }

    //   private static void print(String tab, Term term) {
    //     String type = term.isAtom() ? "A " : term.isVariable() ? "V " : term.isCompound() ? "C " : term.isInteger() ? "I " : "? ";
    //     System.out.println(tab + type + term.getName());
    //     tab = tab + " ";
    //     if (term.isCompound()) {
    //       for (int i = 1, n = term.getArity(); i <= n; i++) {
    //  System.out.print(tab + "" + i + " ");
    //  print(tab , term.getArgument(i));
    //       }
    //     }
    //   }
}
// FastParser
}
