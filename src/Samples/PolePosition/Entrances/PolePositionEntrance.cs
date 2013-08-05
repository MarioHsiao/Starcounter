using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Starcounter.Poleposition.Framework;
using Starcounter.Poleposition.Circuits.Barcelona;
using Starcounter.Poleposition.Circuits.Bahrain;
using Starcounter.Poleposition.Circuits.Imola;
using Starcounter.Poleposition.Circuits.Melbourne;
using Starcounter.Poleposition.Circuits.Sepang;
using Starcounter.Poleposition.Util;
using Starcounter;
using System.IO;
using Starcounter.TestFramework;

namespace Starcounter.Poleposition.Entrances
{
public class PolePositionEntrance
{
    /// <summary>
    /// Indicates if library is started on client.
    /// </summary>
    Boolean startedOnClient = false;

    /// <summary>
    /// Information logger.
    /// </summary>
    static TestLogger logger = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PolePositionEntrance(Boolean startedOnClient)
    {
        logger = new TestLogger("PolePosition", startedOnClient);
        this.startedOnClient = startedOnClient;

        if (!startedOnClient)
        {
            // Reporting reference statistics.
            TestLogger.ReportStatistics("poleposition_sepang_write_reference", 520);
            TestLogger.ReportStatistics("poleposition_sepang_read_reference", 34);
            TestLogger.ReportStatistics("poleposition_sepang_read_hot_reference", 34);
            TestLogger.ReportStatistics("poleposition_sepang_delete_reference", 174);

            TestLogger.ReportStatistics("poleposition_melbourne_write_reference", 181);
            TestLogger.ReportStatistics("poleposition_melbourne_read_reference", 17);
            TestLogger.ReportStatistics("poleposition_melbourne_read_hot_reference", 17);
            TestLogger.ReportStatistics("poleposition_melbourne_delete_reference", 76);

            TestLogger.ReportStatistics("poleposition_imola_retrieve_reference", 50);

            TestLogger.ReportStatistics("poleposition_barcelona_write_reference", 26);
            TestLogger.ReportStatistics("poleposition_barcelona_read_reference", 4);
            TestLogger.ReportStatistics("poleposition_barcelona_query_reference", 12);
            TestLogger.ReportStatistics("poleposition_barcelona_delete_reference", 12);

            TestLogger.ReportStatistics("poleposition_bahrain_write_reference", 394);
            TestLogger.ReportStatistics("poleposition_bahrain_query_indexed_string_reference", 414);
            TestLogger.ReportStatistics("poleposition_bahrain_query_string_reference", 353532);
            TestLogger.ReportStatistics("poleposition_bahrain_query_indexed_int_reference", 137);
            TestLogger.ReportStatistics("poleposition_bahrain_query_int_reference", 332278);
            TestLogger.ReportStatistics("poleposition_bahrain_update_reference", 591);
            TestLogger.ReportStatistics("poleposition_bahrain_delete_reference", 175);
        }
    }

    /// <summary>
    /// Logs some important event both for client and server side execution.
    /// </summary>
    public static void LogEvent(String eventString)
    {
        logger.Log(eventString);
    }

    /// <summary>
    /// Same but no new line at the end.
    /// </summary>
    public static void LogEventNoNewLine(String eventString)
    {
        logger.Log(eventString, false);
    }

    // Flag determines if its a nightly build.
    Boolean NightlyBuild = false;

    // Determines if its a nightly build.
    public void CheckIfNightlyBuild()
    {
        // Checking if its a scheduled nightly build.
        if (TestLogger.IsNightlyBuild())
            NightlyBuild = true;
    }

    /// <summary>
    /// Entry point to run all tests.
    /// </summary>
    public void RunLaps(Object state)
    {
        // Checking if we need to skip the process.
        if ((!startedOnClient) && (TestLogger.SkipInProcessTests()))
        {
            // Creating file indicating finish of the work.
            logger.Log("PolePosition in-process test is skipped!", TestLogger.LogMsgType.MSG_SUCCESS);

            return;
        }

        // Checking if we are inside a nightly build.
        CheckIfNightlyBuild();

        // Running all laps.
        for (Int32 i = 0; i < 1; i++)
        {
            LogEvent("\n\n---------------------------------");
            LogEvent("Starting test suite " + i);
            LogEvent("\n\n Running Sepang...");
            RunSepang();
            LogEvent("\n\n Running Melbourne...");
            RunMelbourne();
            LogEvent("\n\n Running Imola...");
            RunImola();
            LogEvent("\n\n Running Barcelona...");
            RunBarcelona();
            LogEvent("\n\n Running Bahrain...");
            RunBahrain();
            LogEvent("\n\n Done test suite " + i);
        }

        // Creating file indicating finish of the work.
        logger.Log("PolePosition successfully finished!", TestLogger.LogMsgType.MSG_SUCCESS);
    }

    void RunSepang()
    {
        String name = "Sepang";

        Setup setup = new Setup();
        setup.TreeDepth = 18;

        LogEvent(" Tree depth: " + setup.TreeDepth);

        SepangDriver sepangDriver = new SepangDriver(setup);
        sepangDriver.TakeSeatIn();

        sepangDriver.Prepare();
        DoLap(sepangDriver.LapWrite, name);
        sepangDriver.BackToPit();

        sepangDriver.Prepare();
        DoLap(sepangDriver.LapRead, name);
        DoLap(sepangDriver.LapReadHot, name);
        sepangDriver.BackToPit();

        sepangDriver.Prepare();
        DoLap(sepangDriver.LapDelete, name);
        sepangDriver.BackToPit();
    }

    void RunMelbourne()
    {
        String name = "Melbourne";

        Setup setup = new Setup();
        setup.ObjectCount = 300000;
        setup.CommitInterval = 1000;

        LogEvent(" Number of objects: " + setup.ObjectCount);

        MelbourneDriver melbourneDriver = new MelbourneDriver(setup);
        melbourneDriver.TakeSeatIn();

        melbourneDriver.Prepare();
        DoLap(melbourneDriver.LapWrite, name);
        melbourneDriver.BackToPit();

        melbourneDriver.Prepare();
        DoLap(melbourneDriver.LapRead, name);
        DoLap(melbourneDriver.LapReadHot, name);
        melbourneDriver.BackToPit();

        melbourneDriver.Prepare();
        DoLap(melbourneDriver.LapDelete, name);
        melbourneDriver.BackToPit();
    }

    void RunImola()
    {
        String name = "Imola";

        Setup setup = new Setup();
        setup.ObjectCount = 300000;
        setup.SelectCount = 300000;
        setup.CommitInterval = 1000;

        LogEvent(" Number of objects: " + setup.ObjectCount);
        LogEvent(" Number of selects: " + setup.SelectCount);

        ImolaDriver imolaDriver = new ImolaDriver(setup);
        imolaDriver.TakeSeatIn();

        imolaDriver.Prepare();
        DoLap(imolaDriver.LapStore, name);
        imolaDriver.BackToPit();

        imolaDriver.Prepare();
        DoLap(imolaDriver.LapRetrieve, name);
        imolaDriver.BackToPit();
    }

    void RunBahrain()
    {
        String name = "Bahrain";

        Setup setup = new Setup();
        setup.ObjectCount = 50000;
        setup.SelectCount = 50000;
        setup.UpdateCount = 50000;
        setup.CommitInterval = 1000;

        LogEvent(" Number of objects: " + setup.ObjectCount);
        LogEvent(" Number of selects: " + setup.SelectCount);
        LogEvent(" Number of updates: " + setup.UpdateCount);

        BahrainDriver bahrainDriver = new BahrainDriver(setup);

        bahrainDriver.TakeSeatIn();
        bahrainDriver.Prepare();
        DoLap(bahrainDriver.LapWrite, name);
        bahrainDriver.BackToPit();

        if (NightlyBuild)
        {
            bahrainDriver.Prepare();
            DoLap(bahrainDriver.LapQueryInt, name);
            bahrainDriver.BackToPit();

            bahrainDriver.Prepare();
            DoLap(bahrainDriver.LapQueryString, name);
            bahrainDriver.BackToPit();
        }

        bahrainDriver.Prepare();
        DoLap(bahrainDriver.LapQueryIndexedString, name);
        bahrainDriver.BackToPit();

        bahrainDriver.Prepare();
        DoLap(bahrainDriver.LapQueryIndexedInt, name);
        bahrainDriver.BackToPit();

        bahrainDriver.Prepare();
        DoLap(bahrainDriver.LapUpdate, name);
        bahrainDriver.BackToPit();

        bahrainDriver.Prepare();
        DoLap(bahrainDriver.LapDelete, name);
        bahrainDriver.BackToPit();
    }

    void RunBarcelona()
    {
        String name = "Barcelona";

        Setup setup = new Setup();
        setup.SelectCount = 5000;
        setup.ObjectCount = 5000;

        LogEvent(" Number of objects: " + setup.ObjectCount);
        LogEvent(" Number of selects: " + setup.SelectCount);

        BarcelonaDriver barcelonaDriver = new BarcelonaDriver(setup);

        barcelonaDriver.TakeSeatIn();

        barcelonaDriver.Prepare();
        DoLap(barcelonaDriver.LapWrite, name);
        barcelonaDriver.BackToPit();

        barcelonaDriver.Prepare();
        DoLap(barcelonaDriver.LapRead, name);
        barcelonaDriver.BackToPit();

        barcelonaDriver.Prepare();
        DoLap(barcelonaDriver.LapQuery, name);
        barcelonaDriver.BackToPit();

        barcelonaDriver.Prepare();
        DoLap(barcelonaDriver.LapDelete, name);
        barcelonaDriver.BackToPit();
    }

    void ReportStatistics(String statName, Int32 value)
    {
        if (startedOnClient)
            TestLogger.ReportStatistics("poleposition_" + statName.ToLower() + "_client", value);
        else
            TestLogger.ReportStatistics("poleposition_" + statName.ToLower() + "_server", value);
    }

    void DoLap(Action lap, String name)
    {
        var lapAttr = Attributes.Find<LapAttribute>(lap.Method, false);

        if (lapAttr != null)
            LogEventNoNewLine("Lap " + lapAttr.Name + ", ");
        else
            LogEventNoNewLine("Unnamed lap, ");

        var stopwatch = Stopwatch.StartNew();
        lap();
        stopwatch.Stop();

        // Logging time spent.
        Int32 elapsedMs = (Int32) stopwatch.ElapsedMilliseconds;
        LogEvent("done in " + elapsedMs + " milliseconds. ");

        // Reporting statistics.
        if (lapAttr != null)
            ReportStatistics(name + lapAttr.Name, elapsedMs);
    }
}
}