using System;
using System.Collections.Generic;
using Sc.Tools.Logging;
using Starcounter.Applications.UsageTrackerApp.Model;

namespace Starcounter.Applications.ErrorReport.Model {
	/// <summary>
	/// 
	/// </summary>
	[Database]
	public class StarcounterReport {
		/// <summary>
		/// 
		/// </summary>
		public Installation Installation;

		/// <summary>
		/// 
		/// </summary>
		public IEnumerable<LoggedItem> Items {
			get {
				return Db.SQL<LoggedItem>("SELECT l FROM LoggedItem l WHERE Report=?", this);
			}
		}
	}
}
