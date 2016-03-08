using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcounter;

namespace tpcc
{
  public class TerminalEmulator
  {
    private IEnumerable<TransactionDefinition> transactions;

    public TerminalEmulator(TpccValuesGenerator gen, int w_id, int d_id, int sets_number)
    {
            transactions = LoadEmulation.CreateDeck(gen, w_id, d_id, sets_number);
    }

    public async Task Run(byte scheduler)
    {
      string app_name = Starcounter.Internal.StarcounterEnvironment.AppName;
      await Task.Yield();

      Scheduling.ScheduleTask(() =>
      {
          Starcounter.Internal.StarcounterEnvironment.RunWithinApplication(app_name, () => {
              foreach (var transaction in transactions) {
                  transaction.self_execute();
              }
          });
      }, true, scheduler);

      await Task.Yield();
    }
  }
}
