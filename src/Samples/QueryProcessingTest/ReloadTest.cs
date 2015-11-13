﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using System.Diagnostics;
using System.IO;



namespace QueryProcessingTest
{

  class ReloadTest
  {
    public static void Run()
    {
      HelpMethods.LogEvent("Test Db.Unload/Reload");

      const string dump1_path = @"s\QueryProcessingTest\dump1.sql";
      const string dump2_path = @"s\QueryProcessingTest\dump2.sql";

      //no arrange 

      //act
      int nrUnloaded1 = Starcounter.Db.Unload(dump1_path);

      Starcounter.Reload.DeleteAll();

      int nrLoaded = Starcounter.Db.Reload(dump1_path);

      int nrUnloaded2 = Starcounter.Db.Unload(dump2_path);

      //check
      Trace.Assert(nrUnloaded1 == nrLoaded);
      Trace.Assert(nrUnloaded1 == nrUnloaded2);

      // !!! This check may fail if order of recrods in dumps are unstable. 
      // It's stabe for time being. Once this has changed, we should adapt test appropriately
      Trace.Assert(File.ReadLines(dump1_path).SequenceEqual(File.ReadLines(dump2_path)));

      HelpMethods.LogEvent("Db.Unload/Reload test finished");
    }
  }
}