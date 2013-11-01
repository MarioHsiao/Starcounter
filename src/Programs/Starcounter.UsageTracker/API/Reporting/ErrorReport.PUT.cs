using System;
using System.Collections.Generic;
using Starcounter.ErrorReporting;
using Starcounter.Applications.UsageTrackerApp.Model;
using Starcounter.Advanced;
using System.Net;

namespace Starcounter.Applications.UsageTrackerApp.API {
    internal static class ErrorReportHandler {
        public static void Setup_PUT(ushort port) {
            Handle.PUT(port, "/api/usage/errorreport", (Report report, Request request) => {
				Db.Transaction(() => {

					var sr = new ErrorReport();
					sr.Installation = Db.SQL<Installation>("SELECT i FROM Installation i WHERE i.InstallationNo=?", report.InstallationNo).First;
                    sr.IP = request.GetClientIpAddress().ToString();

					foreach (Report.LoggedItemsElementJson item in report.LoggedItems) {
						var dbItem = new ErrorReportItem();
						dbItem.Report = sr;
                        dbItem.ReportDate = DateTime.Parse(item.Date);
                        dbItem.Date = DateTime.UtcNow;
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
