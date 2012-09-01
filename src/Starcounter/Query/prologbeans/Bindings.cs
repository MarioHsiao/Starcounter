// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;
using System.Globalization;
/// <summary> <c>Bindings</c> handles the variable bindings in the
/// communication with the prolog server. Using variable bindings
/// ensures that the values are properly quoted when sent to the
/// prolog server.
/// </summary>
public class Bindings
{

    private System.Collections.Hashtable bindings;

    /// <summary> Creates a new <c>Bindings</c> instance with no variable bindings.
    /// </summary>
    public Bindings()
    {
    }

    /// <summary> Creates a new <c>Bindings</c> instance and copies all existing
    /// variable bindings from the specified bindings.
    /// </summary>
    /// <param name="binds">the variable bindings to copy
    ///
    /// </param>
    public Bindings(Bindings binds)
    {
        if (binds != null && binds.bindings != null)
        {
            bindings = (System.Collections.Hashtable) binds.bindings.Clone();
        }
    }

    /// <summary> Adds the specified variable binding. The variable name must start
    /// with an upper case letter or '_'.
    /// </summary>
    /// <param name="name">a prolog variable name
    /// </param>
    /// <param name="bvalue">the value to bind to the variable
    /// </param>
    /// <returns> a reference to this <c>Bindings</c> object
    /// </returns>
    /// <exception cref="System.ArgumentException"> if the name is not a
    /// valid prolog variable name
    /// </exception>
    public virtual Bindings bind(System.String name, int bvalue)
    {
        checkVar(name);
        // [PD] 3.12.5 SPRM 9325
        //      Use the culture-independent NumberFormatInfo to make sure
        //      fastrw can parse the number.
        String bvalueString = String.Format(NumberFormatInfo.InvariantInfo,
                                            "{0}",
                                            bvalue);
        SupportClass.PutElement(bindings, name,
                                new PBAtomic(bvalueString,
                                             PBAtomic.INTEGER));
        return this;
    }

    /// <summary> Adds the specified variable binding. The variable name must start
    /// with an upper case letter or '_'.
    /// </summary>
    /// <param name="name">a prolog variable name
    /// </param>
    /// <param name="bvalue">the value to bind to the variable
    /// </param>
    /// <returns> a reference to this <c>Bindings</c> object
    /// </returns>
    /// <exception cref="System.ArgumentException"> if the name is not a
    /// valid prolog variable name
    /// </exception>
    public virtual Bindings bind(System.String name, long bvalue)
    {
        checkVar(name);
        // [PD] 3.12.5 SPRM 9325
        //      Use the culture-independent NumberFormatInfo to make sure
        //      fastrw can parse the number.
        String bvalueString = String.Format(NumberFormatInfo.InvariantInfo,
                                            "{0}",
                                            bvalue);
        SupportClass.PutElement(bindings, name,
                                new PBAtomic(bvalueString,
                                             PBAtomic.INTEGER));
        return this;
    }

    /// <summary> Adds the specified variable binding. The variable name must start
    /// with an upper case letter or '_'.
    /// </summary>
    /// <param name="name">a prolog variable name
    /// </param>
    /// <param name="bvalue">the value to bind to the variable
    /// </param>
    /// <returns> a reference to this <c>Bindings</c> object
    /// </returns>
    /// <exception cref="System.ArgumentException"> if the name is not a
    /// valid prolog variable name
    /// </exception>
    public virtual Bindings bind(System.String name, float bvalue)
    {
        checkVar(name);
        // [PD] 3.12.5 SPRM 9325
        //      Use the culture-independent NumberFormatInfo to make sure
        //      fastrw can parse the number.
        String bvalueString = String.Format(NumberFormatInfo.InvariantInfo,
                                            "{0}",
                                            bvalue);
        SupportClass.PutElement(bindings, name,
                                new PBAtomic(bvalueString,
                                             PBAtomic.FLOAT));
        return this;
    }

    /// <summary> Adds the specified variable binding. The variable name must start
    /// with an upper case letter or '_'.
    /// </summary>
    /// <param name="name">a prolog variable name
    /// </param>
    /// <param name="bvalue">the value to bind to the variable
    /// </param>
    /// <returns> a reference to this <c>Bindings</c> object
    /// </returns>
    /// <exception cref="System.ArgumentException"> if the name is not a
    /// valid prolog variable name
    /// </exception>
    public virtual Bindings bind(System.String name, double bvalue)
    {
        checkVar(name);
        // [PD] 3.12.5 SPRM 9325
        //      Use the culture-independent NumberFormatInfo to make sure
        //   fastrw can parse the number.
        String bvalueString = String.Format(NumberFormatInfo.InvariantInfo,
                                            "{0}",
                                            bvalue);
        SupportClass.PutElement(bindings, name,
                                new PBAtomic(bvalueString,
                                             PBAtomic.FLOAT));
        return this;
    }

    /// <summary> Adds the specified variable binding. The variable name must start
    /// with an upper case letter or '_'.
    /// </summary>
    /// <param name="name">a prolog variable name
    /// </param>
    /// <param name="bvalue">the value to bind to the variable
    /// </param>
    /// <returns> a reference to this <c>Bindings</c> object
    /// </returns>
    /// <exception cref="System.ArgumentException"> if the name is not a
    /// valid prolog variable name
    /// </exception>
    public virtual Bindings bind(System.String name, System.String bvalue)
    {
        checkVar(name);
        SupportClass.PutElement(bindings, name, new PBString(bvalue));
        return this;
    }

    /// <summary> Adds the specified variable binding. The variable name must start
    /// with an upper case letter or '_'.
    /// </summary>
    /// <param name="name">a prolog variable name
    /// </param>
    /// <param name="bvalue">the value to bind to the variable
    /// </param>
    /// <returns> a reference to this <c>Bindings</c> object
    /// </returns>
    /// <exception cref="System.ArgumentException"> if the name is not a
    /// valid prolog variable name
    /// </exception>
    public virtual Bindings bind(System.String name, Term bvalue)
    {
        checkVar(name);
        SupportClass.PutElement(bindings, name, bvalue);
        return this;
    }

    /// <summary> Adds the specified variable binding. The variable name must start
    /// with an upper case letter or '_'. The value will be bound as an
    /// atom.
    /// </summary>
    /// <param name="name">a prolog variable name
    /// </param>
    /// <param name="bvalue">the value to bind to the variable as an atom
    /// </param>
    /// <returns> a reference to this <c>Bindings</c> object
    /// </returns>
    /// <exception cref="System.ArgumentException"> if the name is not a
    /// valid prolog variable name
    /// </exception>
    public virtual Bindings bindAtom(System.String name, System.String bvalue)
    {
        checkVar(name);
        SupportClass.PutElement(bindings, name, new PBAtomic(bvalue, PBAtomic.ATOM));
        return this;
    }

    private void  checkVar(System.String name)
    {
        char c = name[0];
        if (!System.Char.IsUpper(c) && c != '_' || "_".Equals(name))
        {
            throw new System.ArgumentException("Variable names must start with uppercase letter or '_' : " + name);
        }
        if (bindings == null)
        {
            bindings = new System.Collections.Hashtable();
        }
    }

    /// <summary> Returns the value for the specified variable or <c>null</c>
    /// if the variable is not bound.
    /// </summary>
    /// <param name="name">the name of the variable
    /// </param>
    /// <returns> the value of the variable as a <c>Term</c> or
    /// <c>null</c> if the variable is not bound
    ///
    /// </returns>
    public virtual Term getValue(System.String name)
    {
        if (bindings != null)
        {
            return (Term) bindings[name];
        }
        return null;
    }

    /// <summary>
    /// </summary>
    public override System.String ToString()
    {
        System.Collections.IEnumerator keys = bindings.Keys.GetEnumerator();
        System.Text.StringBuilder buffer = new System.Text.StringBuilder();
        buffer.Append('[');
        System.String key = null;
        //UPGRADE_TODO: Method 'java.util.Enumeration.hasMoreElements' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1073"'
        while (keys.MoveNext())
        {
            if (key != null)
            {
                buffer.Append(',');
            }
            //UPGRADE_TODO: Method 'java.util.Enumeration.nextElement' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1073"'
            key = (System.String) keys.Current;
            Term bvalue = (Term) bindings[key];
            buffer.Append(key).Append('=').Append(bvalue.toPrologString());
        }
        buffer.Append(']');
        return buffer.ToString();
    }

    internal virtual void  fastWrite(FastWriter writer)
    {
        System.Collections.IEnumerator keys = bindings.Keys.GetEnumerator();
        System.Text.StringBuilder buffer = new System.Text.StringBuilder();
        System.String key = null;
        //UPGRADE_TODO: Method 'java.util.Enumeration.hasMoreElements' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1073"'
        while (keys.MoveNext())
        {
            //UPGRADE_TODO: Method 'java.util.Enumeration.nextElement' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1073"'
            key = (System.String) keys.Current;
            Term bvalue = (Term) bindings[key];
            writer.writeList();
            writer.writeCompound("=", 2);
            //       stream.write(FastParser.LIST);
            //       stream.write(FastParser.COMPOUND);
            //       stream.write('=');
            //       stream.write(0);
            //       stream.write(2);
            // Arg 1
            writer.writeAtom(key);
            //       stream.write(FastParser.ATOM);
            //       stream.write(key.getBytes());
            //       stream.write(0);
            // Arg 2
            bvalue.fastWrite(writer);
        }
        writer.writeNIL();
        //     stream.write(FastParser.NIL);
    }

    //   public static void main(String[] args) {
    //     try {
    //       Bindings b = new Bindings().bind("T", "pest");
    //       b.bind("Int",4711);
    //       b.bind("List", new PBList(new Term[] {new Atom("e1",Atom.ATOM)}));
    //       java.io.OutputStream out =
    //  new java.io.FileOutputStream("testbind.txt");
    //       out.write(FastParser.VERSION);
    //       b.fastrwWrite(out);
    //       System.out.flush();
    //     } catch (IOException e) {
    //       e.printStackTrace();
    //     }
    //   }
}
}
