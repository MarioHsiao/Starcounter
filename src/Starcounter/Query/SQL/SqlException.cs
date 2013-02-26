// ***********************************************************************
// <copyright file="SqlException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter
{
    /// <summary>
    /// Class SqlException
    /// </summary>
[Serializable]
public class SqlException : Exception
{
    readonly List<String> tokenList;
    readonly Int32 beginPosition;
    readonly Int32 endPosition;

    internal SqlException(String message)
    : base(message)
    {
        tokenList = null;
        beginPosition = -1;
        endPosition = -1;
    }

    internal SqlException(String message, Exception innerException)
    : base(message, innerException)
    {
        tokenList = null;
        beginPosition = -1;
        endPosition = -1;
    }

    internal SqlException(String message, List<String> tokenList, Int32 position)
    : base(message)
    {
        this.tokenList = tokenList;
        beginPosition = position;
        endPosition = position;
    }

    internal SqlException(String message, List<String> tokenList, Int32 beginPosition, Int32 endPosition)
    : base(message)
    {
        this.tokenList = tokenList;
        this.beginPosition = beginPosition;
        this.endPosition = endPosition;
    }

    internal List<String> TokenList
    {
        get
        {
            return tokenList;
        }
    }

    internal Int32 BeginPosition
    {
        get
        {
            return beginPosition;
        }
    }

    internal Int32 EndPosition
    {
        get
        {
            return endPosition;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("tokenList", tokenList);
        info.AddValue("beginPosition", beginPosition);
        info.AddValue("endPosition", endPosition);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected SqlException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
    : base(info, context)
    {
        this.tokenList = (List<String>)info.GetValue("tokenList", typeof(List<String>));
        this.beginPosition = info.GetInt32("beginPosition");
        this.endPosition = info.GetInt32("endPosition");
    }

    public override String ToString() {
        StringBuilder str = new StringBuilder(Message);
        if (tokenList != null && tokenList.Count > 0) {
            str.AppendLine();
            str.Append("Tokens: ");
            foreach (String token in tokenList)
                str.Append(token);
            str.AppendLine();
        }
        if (beginPosition > -1)
            str.AppendLine("Begin position: " + beginPosition);
        if (endPosition > beginPosition)
            str.AppendLine("End position: " + endPosition);
        return str.ToString();
    }
}
}
