using System;
using System.Collections.Generic;

namespace Starcounter.Applications.UsageTrackerApp.Model {
	/// <summary>
	/// 
	/// </summary>
	[Database]
	public class ErrorReport {
		/// <summary>
		/// The installation object this report comes from.
		/// </summary>
		public Installation Installation;

        /// <summary>
        /// IP Address of the reporter
        /// </summary>
        public string IP;

		/// <summary>
		/// 
		/// </summary>
		public IEnumerable<ErrorReportItem> Items {
			get {
				return Db.SQL<ErrorReportItem>("SELECT l FROM LoggedItem l WHERE Report=?", this);
			}
		}
	}
}
