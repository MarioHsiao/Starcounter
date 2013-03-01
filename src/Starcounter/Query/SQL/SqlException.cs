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
    readonly String unexpectedToken;
    readonly Int32 beginPosition;
    readonly Int32 endPosition;
    readonly String queryString;

    internal SqlException(String message)
    : base(message)
    {
        unexpectedToken = null;
        beginPosition = -1;
        endPosition = -1;
        queryString = null;
    }

    internal SqlException(String message, Exception innerException)
    : base(message, innerException)
    {
        unexpectedToken = null;
        beginPosition = -1;
        endPosition = -1;
        queryString = null;
    }

    internal SqlException(String message, String token)
        : base(message) {
        unexpectedToken = token;
        beginPosition = -1;
        endPosition = -1;
        queryString = null;
    }

    internal SqlException(String message, String token, String query)
        : base(message) {
        unexpectedToken = token;
        beginPosition = -1;
        endPosition = -1;
        queryString = query;
    }

    internal SqlException(String message, String token, Int32 position)
    : base(message)
    {
        unexpectedToken = token;
        beginPosition = position;
        endPosition = -1;
        queryString = null;
    }

    internal SqlException(String message, String token, Int32 position, String query)
        : base(message) {
        unexpectedToken = token;
        beginPosition = position;
        endPosition = -1;
        queryString = query;
    }

    internal SqlException(String message, String token, Int32 beginPosition, Int32 endPosition)
    : base(message)
    {
        this.unexpectedToken = token;
        this.beginPosition = beginPosition;
        this.endPosition = endPosition;
        queryString = null;
    }

    internal SqlException(String message, String token, Int32 beginPosition, Int32 endPosition, String query)
        : base(message) {
        this.unexpectedToken = token;
        this.beginPosition = beginPosition;
        this.endPosition = endPosition;
        queryString = query;
    }

    internal String Token
    {
        get
        {
            return unexpectedToken;
        }
    }

    internal Int32 BeginPosition
    {
        get
        {
            return beginPosition;
        }
    }

    internal Int32 EndPosition {
        get {
            if (endPosition == -1)
                if (Token == null || Token == "")
                    return beginPosition;
                else
                    return beginPosition + Token.Length - 1;
            else
                return endPosition;
        }
    }

    internal String Query {
        get { return queryString; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("tokenList", unexpectedToken);
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
        this.unexpectedToken = info.GetString("token");
        this.beginPosition = info.GetInt32("beginPosition");
        this.endPosition = info.GetInt32("endPosition");
        this.queryString = info.GetString("query");
    }

    public override String ToString() {
        StringBuilder str = new StringBuilder();
        str.AppendLine(Message);
        if (Query != null)
            str.AppendLine("Query: " + Query);
        if (Token != null && unexpectedToken != "")
            str.AppendLine("Token: " + Token);
        if (BeginPosition > -1)
            str.AppendLine("Begin position: " + BeginPosition);
        if (EndPosition >= BeginPosition)
            str.AppendLine("End position: " + EndPosition);
        return str.ToString();
    }
}
}
