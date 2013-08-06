using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Codeplex.Data;
using Sc.Tools.Logging;
using Starcounter.Internal;

namespace Starcounter.ErrorReporting {
	public class ErrorReporter {
		private const string NotifyFile = "starcounter.notify";
		private string logDirectoryPath;

		private static ErrorReporter instance;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logDirectoryPath"></param>
		internal ErrorReporter(string logDirectoryPath) {
			this.logDirectoryPath = logDirectoryPath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logDirectoryPath"></param>
		public static void Setup(string logDirectoryPath) {
			instance = new ErrorReporter(logDirectoryPath);
		}

		/// <summary>
		/// 
		/// </summary>
		public static void CheckAndSendErrorReports() {
			List<LoggedErrorItem> errors;
			List<LogEntry> logEntries;

			if (instance == null)
				throw new Exception("TODO");

			errors = instance.GetNotifications();
			foreach (var error in errors) {
				logEntries = instance.GetLogEntries(error, 10);
				if (logEntries.Count == 0)
					continue;

				instance.SendErrorReport(logEntries);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logEntries"></param>
		internal void SendErrorReport(List<LogEntry> logEntries) {
			int installationNo = Starcounter.Tracking.Environment.GetInstallationNo();

			// TODO:
			// Should really use the same typed json object as in Starcounter.ErrorReport (the server app).
			dynamic json = new DynamicJson();
			json.InstallationNo = installationNo;

			foreach (var logEntry in logEntries) {
				dynamic loggedError = new DynamicJson();
				loggedError.Date = logEntry.DateTime.ToString("u");
				loggedError.Errorcode = logEntry.ErrorCode;
				loggedError.Hostname = logEntry.HostName;
				loggedError.Message = logEntry.Message;
				loggedError.Severity = (int)logEntry.Severity;
				loggedError.Source = logEntry.Source;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		internal List<LoggedErrorItem> GetNotifications() {
			string fullPath = Path.Combine(logDirectoryPath, NotifyFile);
			List<LoggedErrorItem> errors = new List<LoggedErrorItem>();

			if (!File.Exists(fullPath))
				return errors;

			LoggedErrorItem ei;
			string[] items = GetNotifyContent(fullPath);
			foreach (string item in items) {
				string[] parts = item.Split(' ');
				ei = new LoggedErrorItem();
				ei.FileNumber = Int32.Parse(parts[0]);
				ei.StartFilePosition = Int64.Parse(parts[1]);
				ei.Date = DateTime.ParseExact(parts[2], "yyyyMMddTHHmmss", System.Globalization.CultureInfo.CurrentCulture);

				if (!GetEndPosition(ei)) {
					continue;
				}

				errors.Add(ei);
			}

			return errors;
		}

		/// <summary>
		/// Returns a list containging the logentries starting with the entry that is read
		/// from the notify file and including a number of earlier logs.
		/// </summary>
		/// <remarks>
		/// If the number of existing logs is lower then the specified count, the number of 
		/// available entries are returned.
		/// </remarks>
		/// <param name="item"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		internal List<LogEntry> GetLogEntries(LoggedErrorItem item, int count) {
			LogEntry entry;
			List<LogEntry> logEntries = new List<LogEntry>();
			LogReader reader = new LogReader();

			reader.Open(logDirectoryPath, ReadDirection.Reverse, 24 * 1024, item.FileNumber, item.EndFilePosition);

			// Read the first entry which is the one that was logged in the notify file and 
			// validate it against the date in the notified item. If the date mismatches it
			// problably is because the log file have been recreated. In that case we ignore this 
			// error and return an empty list.
			entry = reader.Next();
			if (entry == null || entry.DateTime != item.Date) {
				return logEntries;
			}

			logEntries.Add(entry);
			count--;

			while (count-- > 0) {
				reader.Next();
				if (entry == null)
					break;
			}
			reader.Close();

			return logEntries;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ei"></param>
		/// <returns></returns>
		private bool GetEndPosition(LoggedErrorItem ei) {
			int read;
			string filePath = GetLogFilePath(ei);

			if (!File.Exists(filePath))
				return false;

			using (FileStream fs = File.OpenRead(filePath)) {
				if (fs.Length < ei.StartFilePosition)
					return false;

				fs.Position = ei.StartFilePosition;
				while (true) {
					read = fs.ReadByte();
					if (read < 0)
						return false;
					if (read == 10) {
						ei.EndFilePosition = fs.Position;
						break;
					}
				}

			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private string GetLogFilePath(LoggedErrorItem item) {
			string logPath = "starcounter.";
			string fileNumStr = item.FileNumber.ToString();
			for (int i = 10; i > fileNumStr.Length; i--) {
				logPath += '0';
			}
			logPath += item.FileNumber + ".log";
			logPath = Path.Combine(logDirectoryPath, logPath);
			return logPath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string[] GetNotifyContent(string path) {
			string[] content;
			string tempPath = path +".tmp";

			while (true) { // TODO: Change to max number of retries if file cannot be moved.
				try {
					File.Move(path, tempPath);
					break;
				} catch {
					Thread.Sleep(10);
				}
			}
			content = File.ReadAllLines(tempPath);
			File.Delete(tempPath);
			return content;
		}
	}
}
