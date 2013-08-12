using System;

namespace Starcounter.ErrorReporting {
	internal class LoggedErrorItem {
		internal int FileNumber;
		internal long StartFilePosition;
		internal long EndFilePosition;
		internal DateTime Date;
	}
}
