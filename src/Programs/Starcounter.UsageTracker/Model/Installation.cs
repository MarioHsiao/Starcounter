using System;
using System.Collections;
using Starcounter;
using Starcounter.Applications.UsageTrackerApp.Model;
using Starcounter.Applications.UsageTrackerApp.API.Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// Installation
    /// One installation per Installer start /the enduser clicked the installer exe(.
    /// </summary>
    [Database]
    public class Installation {

        /// <summary>
        /// Serial ID (Unique id that identifies a specific build)
        /// </summary>
        public string Serial;

        /// <summary>
        /// Previous Installation number
        /// -1 if there was no previous detected installation
        /// </summary>
        public Int64 PreviousInstallationNo;

        /// <summary>
        /// Installation Number
        /// One number per installation start
        /// </summary>
        public Int64 InstallationNo;

        /// <summary>
        /// Date when the installation was detected
        /// </summary>
        public DateTime Date;

        /// <summary>
        /// Starcounter usages
        /// </summary>
        public IEnumerable Usages {
            get {
                return Db.SlowSQL("SELECT O FROM StarcounterUsage WHERE O.Installation = {?}", this);
            }
        }

        /// <summary>
        /// Installer starts
        /// List of when the user executed the installer executable.
        /// </summary>
        public IEnumerable InstallerStart {
            get {
                return Db.SlowSQL("SELECT O FROM InstallerStart WHERE O.Installation = {?}", this);
            }
        }

        /// <summary>
        /// Installer Executing
        /// List when the installer is executing, installing/Uninstalling some software
        /// </summary>
        public IEnumerable InstallerExecuting {
            get {
                return Db.SlowSQL("SELECT O FROM InstallerExecuting WHERE O.Installation = {?}", this);
            }
        }

        /// <summary>
        /// Installer Aborts
        /// List of user aborts
        /// </summary>
        public IEnumerable InstallerAbort {
            get {
                return Db.SlowSQL("SELECT O FROM InstallerAbort WHERE O.Installation = {?}", this);
            }
        }

        /// <summary>
        /// Installer Finish
        /// List of finished installation executes
        /// </summary>
        public IEnumerable InstallerFinish {
            get {
                return Db.SlowSQL("SELECT O FROM InstallerFinish WHERE O.Installation = {?}", this);
            }
        }

        /// <summary>
        /// Installer End
        /// List of when the installer end's (Process ends)
        /// </summary>
        public IEnumerable InstallerEnd {
            get {
                return Db.SlowSQL("SELECT O FROM InstallerEnd WHERE O.Installation = {?}", this);
            }
        }

        /// <summary>
        /// Starcounter Usage
        /// </summary>
        public IEnumerable StarcounterGeneral {
            get {
                return Db.SlowSQL("SELECT O FROM StarcounterGeneral WHERE O.Installation = {?}", this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serial"></param>
        public Installation(string serial) {

            this.Serial = serial;

            DateTime d = new DateTime(2000, 1, 1);

            this.Date = DateTime.UtcNow;
            this.InstallationNo = DateTime.UtcNow.Ticks - d.Ticks;
            this.PreviousInstallationNo = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="previousInstallationNo"></param>
        public Installation(string serial, Int64 previousInstallationNo) {

            this.Serial = serial;
            this.Date = DateTime.UtcNow;

            DateTime d = new DateTime(2000, 1, 1);

            this.InstallationNo = DateTime.UtcNow.Ticks - d.Ticks;
            this.PreviousInstallationNo = previousInstallationNo;
        }

    }


}
