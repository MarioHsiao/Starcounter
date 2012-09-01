// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
/// <summary> <c>FastWriter</c>
/// </summary>
class FastWriter
{

    // [PD] 3.12.1 UTF-8 is required to make SICStus happy.
    private System.Text.Encoding UTF8encoder = System.Text.Encoding.GetEncoding("UTF-8");
    // [PD] 3.12.3 We need a Latin1 encoder in some cases.
    private System.Text.Encoding Latin1encoder = System.Text.Encoding.GetEncoding("ISO-8859-1");

    private System.IO.Stream output;
    private bool isWritingTerm = false;
    private System.Collections.Hashtable variableTable = null;

    public FastWriter(System.IO.Stream output)
    {
        this.output = output;
    }
    // FastWriter constructor

    private void  initOutput()
    {
        if (!isWritingTerm)
        {
            isWritingTerm = true;
            output.WriteByte((byte) FastParser.VERSION);
        }
    }

    public virtual void  writeCompound(System.String name, int arguments)
    {
        initOutput();
        output.WriteByte((byte) FastParser.COMPOUND);
        byte[] byteArray = UTF8encoder.GetBytes(name);
        output.Write(byteArray, 0, byteArray.Length);
        output.WriteByte((System.Byte) 0);
        output.WriteByte((System.Byte) arguments);
    }

    public virtual void  writeList()
    {
        initOutput();
        output.WriteByte((byte) FastParser.LIST);
    }

    public virtual void  writeNIL()
    {
        initOutput();
        output.WriteByte((byte) FastParser.NIL);
    }

    // [PD] 3.12.3 Bogus
    //    public virtual void  writeString(System.String value_Renamed)
    //    {
    //      initOutput();
    //      output.WriteByte((byte) FastParser.STRING);
    // // [PD] 3.12.3 Strings should not be UTF-8-encoded
    // //     byte[] byteArray = UTF8encoder.GetBytes(value_Renamed);
    //      byte[] byteArray = Latin1encoder.GetBytes(value_Renamed);
    //      output.Write(byteArray, 0, byteArray.Length);
    //      output.WriteByte((System.Byte) 0);
    //      output.WriteByte((byte) FastParser.NIL);
    //    }

    // [PD] 3.12.3 Correct version which handles character codes > 255
    public virtual void  writeString(System.String value_Renamed)
    {
        initOutput();
        writeCharList(value_Renamed, false);
        output.WriteByte((System.Byte) 0);
        output.WriteByte((byte) FastParser.NIL);
    }

    private void writeCharList(System.String value_Renamed, bool in_compact_list)
    {
        if (value_Renamed.Length > 0)
        {
            Char car = value_Renamed[0];
            String cdr = value_Renamed.Substring(1);
            if (0 < car && car < 256)
            {
                if (!in_compact_list)
                {
                    output.WriteByte((byte)FastParser.STRING);
                }
                output.WriteByte((byte)car);
                writeCharList(cdr, true);
            }
            else
            {
                if (in_compact_list)
                {
                    output.WriteByte((System.Byte) 0);
                }
                output.WriteByte((byte)FastParser.LIST);
                output.WriteByte((byte)FastParser.INTEGER);
                byte[] ba = Latin1encoder.GetBytes(((int)car).ToString());
                output.Write(ba, 0, ba.Length);
                output.WriteByte((System.Byte) 0);
                writeCharList(cdr, false);
            }
        }
    }

    // [PD] 3.12.3 Bogus
    //    public virtual void  writeString(System.String value_Renamed, Term nextTerm)
    //    {
    //      initOutput();
    //      output.WriteByte((byte) FastParser.STRING);
    // // [PD] 3.12.3 Strings should not be UTF-8-encoded
    // //     byte[] byteArray = UTF8encoder.GetBytes(value_Renamed);
    //      byte[] byteArray = Latin1encoder.GetBytes(value_Renamed);
    //      output.Write(byteArray, 0, byteArray.Length);
    //      output.WriteByte((System.Byte) 0);
    //      if (nextTerm != PBList.NIL)
    //      {
    //        nextTerm.fastWrite(this);
    //      }
    //      else
    //      {
    //        output.WriteByte((byte) FastParser.NIL);
    //      }
    //    }

    // [PD] 3.12.3 Correct version which handles character codes > 255
    public virtual void  writeString(System.String value_Renamed, Term nextTerm)
    {
        initOutput();
        writeCharList(value_Renamed, false);
        output.WriteByte((System.Byte) 0);
        if (nextTerm != PBList.NIL)
        {
            nextTerm.fastWrite(this);
        }
        else
        {
            output.WriteByte((byte) FastParser.NIL);
        }
    }

    public virtual void  writeAtom(System.String value_Renamed)
    {
        initOutput();
        output.WriteByte((byte) FastParser.ATOM);
        byte[] byteArray = UTF8encoder.GetBytes(value_Renamed);
        output.Write(byteArray, 0, byteArray.Length);
        output.WriteByte((System.Byte) 0);
    }

    public virtual void  writeAtomic(PBAtomic atomic)
    {
        initOutput();
        int type = atomic.Type;
        switch (type)
        {
            case PBAtomic.ATOM:
                output.WriteByte((byte) FastParser.ATOM);
                break;
            case PBAtomic.FLOAT:
                output.WriteByte((byte) FastParser.FLOAT);
                break;
            case PBAtomic.INTEGER:
                output.WriteByte((byte) FastParser.INTEGER);
                break;
            case PBAtomic.VARIABLE:
                output.WriteByte((byte) FastParser.VARIABLE);
                break;
        }
        if (type != PBAtomic.VARIABLE)
        {
            byte[] byteArray = UTF8encoder.GetBytes(atomic.Name);
            output.Write(byteArray, 0, byteArray.Length);
            output.WriteByte((System.Byte) 0);
        }
        else
        {
            if (variableTable == null)
            {
                variableTable = new System.Collections.Hashtable();
            }
            System.String variableName = (System.String) variableTable[atomic];
            if (variableName == null)
            {
                variableName = "" + '_' + variableTable.Count;
                SupportClass.PutElement(variableTable, atomic, variableName);
            }
            byte[] byteArray2 = UTF8encoder.GetBytes(variableName);
            output.Write(byteArray2, 0, byteArray2.Length);
            output.WriteByte((System.Byte) 0);
        }
    }

    public virtual void  commit()
    {
        output.Flush();
        isWritingTerm = false;
        if (variableTable != null)
        {
            variableTable.Clear();
        }
    }

    public virtual void  close()
    {
        commit();
        this.output.Close();
    }
}
// FastWriter
}
