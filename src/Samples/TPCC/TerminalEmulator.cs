using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcounter;

namespace tpcc
{
  public class TerminalEmulator
  {
    private DeckPerTerminal deck;

    public TerminalEmulator(TpccValuesGenerator gen, int w_id, int d_id, int sets_number)
    {
      deck = new DeckPerTerminal(gen, w_id, d_id, sets_number);
    }

    public async Task Run(byte scheduler)
    {
      string app_name = Starcounter.Internal.StarcounterEnvironment.AppName;
      await Task.Yield();

      new DbSession().RunSync(() =>
      {
        Starcounter.Internal.StarcounterEnvironment.RunWithinApplication(app_name,
          () =>
          {
            foreach (var transaction in deck.trasactions)
            {
              transaction();
            }
          });
      }, scheduler);

      await Task.Yield();
    }
  }
}
