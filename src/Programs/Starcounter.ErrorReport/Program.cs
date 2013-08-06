using System;
using Starcounter.Internal;
using Starcounter.Applications.ErrorReport.Model;
using Starcounter.Applications.UsageTrackerApp.Model;
using Sc.Tools.Logging;

namespace Starcounter.Applications.ErrorReport {
    class Program {
        static void Main(string[] args) {
            AppsBootstrapper.Bootstrap(null, 8585);

			Handle.PUT("/api/errorreport", (Report report) => {
				Db.Transaction(() => {
					var sr = new StarcounterReport();
					sr.Installation = Db.SQL<Installation>("SELECT i FROM Installation i WHERE i.InstallationNo=?", report.InstallationNo).First;

					foreach (Report.LoggedItemsObj item in report.LoggedItems) {
						var dbItem = new LoggedItem();
						dbItem.Report = sr;
						dbItem.Date = DateTime.Parse(item.Date);
						dbItem.Errorcode = (uint)item.Errorcode;
						dbItem.Hostname = item.Hostname;
						dbItem.Message = item.Message;
						dbItem.Severity = (Severity)item.Severity;
						dbItem.Source = item.Source;
					}
				});
				return 200;
			});
        }
    }
}