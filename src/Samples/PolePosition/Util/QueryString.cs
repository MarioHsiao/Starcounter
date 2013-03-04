using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Poleposition.Framework;

namespace Starcounter.Poleposition.Util
{
public class QueryString
{
    private readonly string original;
    private Dictionary<string, string> values = new Dictionary<string, string>();

    public QueryString(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException();
        }
        original = s;
        ParseInput();
    }

    private void ParseInput()
    {
        int pos = 0;
        int nextEquals = original.IndexOf('&', pos);
        while (nextEquals != -1)
        {
            HandlePair(pos, nextEquals);
            pos = nextEquals + 1;
            nextEquals = original.IndexOf('&', pos);
        }
        HandlePair(pos, original.Length);
    }

    private void HandlePair(int start, int end)
    {
        int len = end - start;
        int delim = original.IndexOf('=', start, len);
        if (delim == -1)
        {
            throw new PolePositionException("Bad key/value pair: " + original.Substring(start, len));
        }
        values.Add(original.Substring(start, delim - start), original.Substring(delim + 1, end - delim - 1));
    }

    public string this[string index]
    {
        get
        {
            return values[index];
        }
    }

    public bool TryGetValue(string key, out string value)
    {
        return values.TryGetValue(key, out value);
    }

    public bool ContainsKey(string key)
    {
        return values.ContainsKey(key);
    }

    public override string ToString()
    {
        StringBuilder buf = new StringBuilder();
        foreach (KeyValuePair<string, string> pair in values)
        {
            if (buf.Length != 0)
                buf.Append('&');

            buf.Append(pair.Key).Append('=').Append(pair.Value);
        }
        return buf.ToString();
    }

}
}
