using System;

namespace Starcounter.Applications.UsageTrackerApp.Model {
	/// <summary>
	/// 
	/// </summary>
	[Database]
	public class ErrorReportItem {
		/// <summary>
		/// The main report this item belongs to.
		/// </summary>
		public ErrorReport Report;

		/// <summary>
		/// 
		/// </summary>
		public DateTime Date;

		/// <summary>
		/// 
		/// </summary>
		public uint Errorcode;

		/// <summary>
		/// 
		/// </summary>
		public string Hostname;

		/// <summary>
		/// 
		/// </summary>
		public string Message;

		/// <summary>
		/// 
		/// </summary>
		public int Severity;

		/// <summary>
		/// 
		/// </summary>
		public string Source;
	}
}
