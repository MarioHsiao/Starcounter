// ***********************************************************************
// <copyright file="_Tokenizer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Sql
{
internal static class Tokenizer
{
    static HashSet<Char> symbolHash = CreateSymbolHash();
    static HashSet<String> reservedWordHash = CreateReservedWordHash();

    private static HashSet<Char> CreateSymbolHash()
    {
        HashSet<Char> charHash = new HashSet<Char>();
        charHash.Add('!');
        charHash.Add('%');
        charHash.Add('(');
        charHash.Add(')');
        charHash.Add('*');
        charHash.Add('+');
        charHash.Add(',');
        charHash.Add('-');
        charHash.Add('.');
        charHash.Add('/');
        charHash.Add('<');
        charHash.Add('=');
        charHash.Add('>');
        charHash.Add('?');
        charHash.Add('@');
        charHash.Add('|');
        return charHash;
    }

    private static HashSet<String> CreateReservedWordHash()
    {
        HashSet<String> stringHash = new HashSet<String>();
        stringHash.Add("ALL");
        stringHash.Add("AND");
        stringHash.Add("AS");
        stringHash.Add("ASC");
        stringHash.Add("AVG");
        stringHash.Add("BINARY");
        stringHash.Add("BY");
        stringHash.Add("CAST");
        stringHash.Add("COUNT");
        stringHash.Add("CREATE");
        stringHash.Add("CROSS");
        stringHash.Add("DATE");
        stringHash.Add("DATETIME");
        stringHash.Add("DELETE");
        stringHash.Add("DESC");
        stringHash.Add("DISTINCT");
        stringHash.Add("DROP");
        stringHash.Add("ESCAPE");
        stringHash.Add("EXISTS");
        stringHash.Add("FALSE");
        stringHash.Add("FIXED");
        stringHash.Add("FORALL");
        stringHash.Add("FROM");
        stringHash.Add("FULL");
        stringHash.Add("GROUP");
        stringHash.Add("HAVING");
        stringHash.Add("IN");
        stringHash.Add("INDEX");
        stringHash.Add("INNER");
        stringHash.Add("INSERT");
        stringHash.Add("IS");
        stringHash.Add("JOIN");
        stringHash.Add("LEFT");
        stringHash.Add("LIKE");
        stringHash.Add("MAX");
        stringHash.Add("MIN");
        stringHash.Add("NOT");
        stringHash.Add("NULL");
        stringHash.Add("OBJ");
        stringHash.Add("OBJECT");
        stringHash.Add("ON");
        stringHash.Add("OPTION");
        stringHash.Add("OR");
        stringHash.Add("ORDER");
        stringHash.Add("OUTER");
        stringHash.Add("OUT");
        stringHash.Add("OUTPUT");
        stringHash.Add("PROC");
        stringHash.Add("PROCEDURE");
        stringHash.Add("RANDOM");
        stringHash.Add("RIGHT");
        stringHash.Add("SELECT");
        stringHash.Add("STARTS");
        stringHash.Add("SUM");
        stringHash.Add("TABLE");
        stringHash.Add("TIME");
        stringHash.Add("TIMESTAMP");
        stringHash.Add("TRUE");
        stringHash.Add("UNIQUE");
        stringHash.Add("UNKNOWN");
        stringHash.Add("UPDATE");
        stringHash.Add("VALUES");
        stringHash.Add("VAR");
        stringHash.Add("VARIABLE");
        stringHash.Add("WHERE");
        stringHash.Add("WITH");
        return stringHash;
    }

    // Internal format for a reserved-word is "$WORD".
    internal static Boolean InternalReservedWord(String token)
    {
        if (token == null || token.Length == 0)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect token.");
        }
        return (token[0] == '$');
    }

    private static Boolean Symbol(Char chr)
    {
        return symbolHash.Contains(chr);
    }

    private static Boolean WhiteSpace(Char chr)
    {
        return chr == ' ' || chr == '\t' || chr == '\n'; ;
    }

    internal static Boolean Digit(Char chr)
    {
        return chr >= '0' && chr <= '9';
    }

    internal static Boolean IdentifierSeparation(Char chr) {
        return chr == '.';
    }

    internal static Boolean Letter(Char chr)
    {
        return (chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z');
    }

    internal static Boolean Exponent(Char chr)
    {
        return chr == 'E' || chr == 'e';
    }

    internal static Boolean Quote(Char chr)
    {
        return chr == '\'';
    }

    internal static Boolean DoubleQuote(Char chr) {
        return chr == '\"';
    }

    internal static Boolean Underscore(Char chr)
    {
        return chr == '_';
    }

    public static List<String> Tokenize(String input)
    {
        if (input == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect input.");
        }
        List<String> tokenList = new List<String>();
        Char[] chrArr = input.ToCharArray();
        String token = "";
        Int32 i = 0;
        while (i < chrArr.Length)
        {
            // Skip white space.
            if (i < chrArr.Length && WhiteSpace(chrArr[i]))
            {
                while (i < chrArr.Length && WhiteSpace(chrArr[i]))
                {
                    i++;
                }
            }
            // Accumulate symbol token.
            else if (i < chrArr.Length && Symbol(chrArr[i]))
            {
                while (i < chrArr.Length && Symbol(chrArr[i]))
                {
                    token += chrArr[i].ToString();
                    i++;
                }
                tokenList.Add(token);
                token = "";
            }
            // Accumulate string token ('...').
            else if (i < chrArr.Length && Quote(chrArr[i]))
            {
                while (i < chrArr.Length && Quote(chrArr[i]))
                {
                    token += chrArr[i].ToString();
                    i++;
                    while (i < chrArr.Length && !Quote(chrArr[i]))
                    {
                        token += chrArr[i].ToString();
                        i++;
                    }
                    if (i < chrArr.Length)
                    {
                        token += chrArr[i].ToString();
                        i++;
                    }
                }
                tokenList.Add(token);
                token = "";
                // Accumulate double quoted identifier token
            } else if (i < chrArr.Length && DoubleQuote(chrArr[i])) {
                while (i < chrArr.Length && DoubleQuote(chrArr[i])) {
                    token += chrArr[i].ToString();
                    i++;
                    while (i < chrArr.Length && !DoubleQuote(chrArr[i])) {
                        token += chrArr[i].ToString();
                        i++;
                    }
                    if (i < chrArr.Length) {
                        token = token.Substring(1);
                        i++;
                    }
                }
                tokenList.Add(token);
                token = "";
            }
                // Accumulate numerical token.
            else if (i < chrArr.Length && Digit(chrArr[i]))
            {
                while (i < chrArr.Length && Digit(chrArr[i]))
                {
                    token += chrArr[i].ToString();
                    i++;
                }
                tokenList.Add(token);
                token = "";
                // Exponent symbol.
                if (i < chrArr.Length && Exponent(chrArr[i]))
                {
                    token += chrArr[i].ToString().ToUpper();
                    i++;
                    tokenList.Add(token);
                    token = "";
                }
            }
            // Accumulate other token.
            else
            {
                while (i < chrArr.Length && !WhiteSpace(chrArr[i]) && !Symbol(chrArr[i]) && !Quote(chrArr[i]))
                {
                    token += chrArr[i].ToString();
                    i++;
                }
                // Reserved word to internal format "$WORD".
                String tokenUpper = token.ToUpper();
                if (reservedWordHash.Contains(tokenUpper))
                {
                    token = "$" + tokenUpper;
                }
                // Replace "[identifier]" with "identifier".
                if (token.StartsWith("[") && token.EndsWith("]"))
                    token = token.Substring(1, token.Length - 2);
                // Add token.
                if (token != "")
                {
                    tokenList.Add(token);
                }
                token = "";
            }
        }
        return tokenList;
    }
}
}
