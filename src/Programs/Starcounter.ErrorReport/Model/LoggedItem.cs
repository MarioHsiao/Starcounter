using System;
using Sc.Tools.Logging;

namespace Starcounter.Applications.ErrorReport.Model {
	/// <summary>
	/// 
	/// </summary>
	[Database]
	public class LoggedItem {
		/// <summary>
		/// The main report this item belongs to.
		/// </summary>
		public StarcounterReport Report;
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
		public Severity Severity;
		/// <summary>
		/// 
		/// </summary>
		public string Source;
	}
}
