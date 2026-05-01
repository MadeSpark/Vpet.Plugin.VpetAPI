using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.VpetAPI
{
    public static class SleepController
    {
        public static async Task<bool> TrySetSleepAsync(IMainWindow mw, bool isSleeping, CancellationToken token)
        {
            await mw.Dispatcher.InvokeAsync(() =>
            {
                if (isSleeping)
                {
                    mw.Main.State = Main.WorkingState.Sleep;
                    mw.Main.DisplaySleep(true);
                }
                else
                {
                    mw.Main.State = Main.WorkingState.Nomal;
                    mw.Main.DisplayCEndtoNomal("sleep");
                }
            });

            return true;
        }
    }
}
