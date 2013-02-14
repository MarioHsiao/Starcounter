
using Sc.Tools.Logging;
using System;
using System.Text;
using System.Threading;
using System.IO;

namespace ServerLogTail
{

public static class Program
{
    public static void Main(String[] args) { }

#if false
	private static ConsoleColor _originalColor;

    public static void Main(String[] args)
    {
        String directory;
        LogFilter lf;
        LogReader lr;
        Thread th;

		_originalColor = Console.ForegroundColor;
        directory = ".";
        if (args.Length > 0)
        {
            directory = args[0];
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Specified directory does not exist.");
                return;
            }
        }
        lf = null;
#if false
        lf = new LogFilter();
        //      lf.Type = EntryType.Error;
        //      lf.FromDateTime = DateTime.Today;
        lf.ToDateTime = DateTime.Today.AddDays(1);
#endif
        lr = new LogReader(directory, lf, (4096 * 256));
        lr.Open();
        th = new Thread(new ParameterizedThreadStart(ThreadProc));
        th.Start(lr);
#if false
        Console.ReadLine();
        lr.Close();
#endif
#if false
        Thread.Sleep(30000);
        lr.CancelRead();
#endif
#if false
        Thread.Sleep(1000);
        lr.Reset();
        Thread.Sleep(1000);
        lr.Reset();
#endif
        th.Join();
		Console.ForegroundColor = _originalColor;
    }

    private static void ThreadProc(Object arg)
    {
        LogReader lr;
        LogEntry le;
        lr = (LogReader)arg;
        for (;;)
        {
            le = lr.Read(true);
            if (le == null)
            {
                break;
            }
			Colorize(le);
            Dump(le);
        }
    }

    private static void Dump(LogEntry le)
    {
        StringBuilder sb;
        String value;
#if false
        sb = new StringBuilder();
        sb.AppendLine("BEGIN");
        sb.Append("TYPE=");
        sb.AppendLine(le.Type.ToString());
        sb.Append("DATE_TIME=");
        sb.AppendLine(le.DateTime.ToString("o"));
        sb.Append("ACTIVITY_ID=");
        sb.AppendLine(le.ActivityID.ToString());
        value = le.MachineName;
        if (value != null)
        {
            sb.Append("MACHINE_NAME=");
            sb.AppendLine(value);
        }
        value = le.ServerName;
        if (value != null)
        {
            sb.Append("SERVER_NAME=");
            sb.AppendLine(value);
        }
        value = le.Source;
        if (value != null)
        {
            sb.Append("SOURCE=");
            sb.AppendLine(value);
        }
        value = le.Category;
        if (value != null)
        {
            sb.Append("CATEGORY=");
            sb.AppendLine(value);
        }
        value = le.UserName;
        if (value != null)
        {
            sb.Append("USER_NAME=");
            sb.AppendLine(value);
        }
        value = le.Message;
        if (value != null)
        {
            sb.Append("MESSAGE=");
            sb.AppendLine(value);
        }
        sb.AppendLine("END");
#endif
        sb = new StringBuilder();
        sb.Append(le.DateTime.ToString("s"));
        sb.Append(" ");
        sb.Append(le.Source);
        sb.Append(" ");
        sb.Append(le.Type.ToString());
        value = le.Message;
        if (value != null)
        {
            sb.Append(":");
            sb.Append(value);
        }
        sb.AppendLine();
        Console.Write(sb.ToString());
    }

	private static void Colorize(LogEntry le)
	{
		ConsoleColor color = ConsoleColor.White;
		ConsoleColor foregroundColor = Console.ForegroundColor;
		switch (le.Type)
		{
			case EntryType.Debug:
				color = ConsoleColor.Gray;
				break;

			case EntryType.Notice:
				if (foregroundColor != ConsoleColor.Green)
				{
					color = ConsoleColor.Green;
					break;
				}
				color = ConsoleColor.DarkGreen;
				break;

			case EntryType.Warning:
				color = ConsoleColor.Yellow;
				break;

			case EntryType.Error:
			case EntryType.Critical:
				color = ConsoleColor.Red;
				break;
		}
		Console.ForegroundColor = color;
	}
#endif
}
}
