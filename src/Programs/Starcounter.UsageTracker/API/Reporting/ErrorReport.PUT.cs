using System;
using System.Collections.Generic;
using Starcounter.Applications.UsageTrackerApp.Model;

namespace Starcounter.Applications.UsageTrackerApp.API {
    internal static class ErrorReportHandler {
        public static void Setup_PUT(ushort port) {
            Handle.PUT(port, "/api/usage/errorreport", (Report report) => {
				Db.Transaction(() => {
					var sr = new ErrorReport();
					sr.Installation = Db.SQL<Installation>("SELECT i FROM Installation i WHERE i.InstallationNo=?", report.InstallationNo).First;

					foreach (Report.LoggedItemsObj item in report.LoggedItems) {
						var dbItem = new ErrorReportItem();
						dbItem.Report = sr;
						dbItem.Date = DateTime.Parse(item.Date);
						dbItem.Errorcode = (uint)item.Errorcode;
						dbItem.Hostname = item.Hostname;
						dbItem.Message = item.Message;
						dbItem.Severity = (int)item.Severity;
						dbItem.Source = item.Source;
					}
				});
				return 200;
            });
        }
    }
}
