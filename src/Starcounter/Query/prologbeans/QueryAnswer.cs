// Copyright (c) 2004 SICS AB. All rights reserved.
//
namespace se.sics.prologbeans
{
using System;

/// <summary> <c>QueryAnswer</c> is the .NET representation of an answer
/// from the Prolog server. The <c>QueryAnswer</c> is returned by
/// <see cref="se.sics.prologbeans.PrologSession"/> in response to a query
/// and contains variable bindings, errors, and success/failure
/// information. It also contains the variable bindings specified in
/// the query.
/// </summary>
public class QueryAnswer: Bindings
{
    /// <summary> Returns <c>true</c> if an error occurred while processing
    /// the query and <c>false</c> otherwise.
    /// </summary>
    virtual public bool IsError
    {
        get
        {
            return !hasValues && answer.Name.Equals("error");
        }
    }
    /// <summary> Returns the error reason or <c>null</c> if an error has not
    /// occurred or if no error reason is known.
    /// </summary>
    virtual public System.String Error
    {
        get
        {
            if (answer.Name.Equals("error"))
            {
                //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
                return answer.getArgument(1).ToString();
            }
            return null;
        }
    }

    // This term is on one of these forms:
    // - A list of the form "['='(VariableNameAsAtom,Value), ...]" (variable bindings)
    // - The atom "no" (the prolog responded with 'no')
    // - The functor "error(ErrorReason)" (an error occurred)
    private Term answer;
    private bool hasValues = false;
    private bool bound = false;

    /// <summary> Creates a new <c>QueryAnswer</c> instance with the
    /// specified information.
    /// </summary>
    /// <param name="answer">a <c>Term</c> value representing the Prolog response
    /// </param>
    /// <param name="bindings">the variable bindings for the query to which this
    /// is an answer
    ///
    /// </param>
    public QueryAnswer(Term answer, Bindings bindings): base(bindings)
    {
        this.answer = answer;
        hasValues = answer.List && answer is PBList;
    }
    // QueryAnswer constructor

    /// <summary> Returns the value of the specified variable or <c>null</c>
    /// if the variable is not bound.
    /// </summary>
    /// <param name="variable">the name of the variable
    /// </param>
    /// <returns> the value of the variable as a <c>Term</c> or
    /// <c>null</c> if the variable is not bound
    ///
    /// </returns>
    public override Term getValue(System.String variable)
    {
        if (!bound)
        {
            if (hasValues)
            {
                // copy all the new bindings into Bindings
                PBList list = (PBList) answer;
                for (int i = 1, n = list.Length; i <= n; i++)
                {
                    Term Bind = list.getTermAt(i);
                    if (Bind.Name.Equals("="))
                    {
                        bind(Bind.getArgument(1).Name, Bind.getArgument(2));
                    }
                }
            }
            bound = true;
        }
        return base.getValue(variable);
    }

    /// <summary> Returns <c>true</c> if the query failed (i.e. the Prolog
    /// responded with 'no') and <c>false</c> otherwise.
    /// </summary>
    public virtual bool queryFailed()
    {
        return !hasValues && answer.Name.Equals("no");
    }



    /// <summary> Returns a string description of this answer.
    /// </summary>
    public override System.String ToString()
    {
        //UPGRADE_TODO: The equivalent in .NET for method 'java.Object.toString' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043"'
        return answer.ToString();
    }
}
// QueryAnswer
}
