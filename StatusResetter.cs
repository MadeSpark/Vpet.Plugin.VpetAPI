using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.VpetAPI
{
    public static class StatusResetter
    {
        public static async Task ResetAsync(IMainWindow mw, PetMover? mover, CancellationToken token)
        {
            try { mover?.Cancel(); } catch { }

            await mw.Dispatcher.InvokeAsync(() =>
            {
                try { mw.Main.WorkTimer.Stop(); } catch { }
                try { mw.Main.WorkTimer.Visibility = Visibility.Collapsed; } catch { }

                if (mw.Main.State == Main.WorkingState.Sleep)
                {
                    try { mw.Main.DisplayCEndtoNomal("sleep"); } catch { }
                }

                mw.Main.State = Main.WorkingState.Nomal;
            });
        }
    }
}
