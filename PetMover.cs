using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet.Plugin.VpetAPI
{
    public sealed class PetMover
    {
        private readonly IMainWindow mw;
        private readonly object gate = new object();
        private CancellationTokenSource? currentMoveCts;

        public PetMover(IMainWindow mw)
        {
            this.mw = mw ?? throw new ArgumentNullException(nameof(mw));
        }

        public void Cancel()
        {
            lock (gate)
            {
                try { currentMoveCts?.Cancel(); } catch { }
                try { currentMoveCts?.Dispose(); } catch { }
                currentMoveCts = null;
            }
        }

        public Task MoveToAsync(int x, int y, bool isCreeping, CancellationToken token)
        {
            CancellationTokenSource linked;
            lock (gate)
            {
                try { currentMoveCts?.Cancel(); } catch { }
                currentMoveCts?.Dispose();
                currentMoveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                linked = currentMoveCts;
            }

            return Task.Run(() => MoveLoopAsync(x, y, isCreeping, linked.Token), linked.Token);
        }

        private async Task MoveLoopAsync(int targetX, int targetY, bool isCreeping, CancellationToken token)
        {
            const int tickMs = 16;
            const int i = 1;
            const double maxStepX = i + 4;
            const double maxStepY = i + 2;
            const double offscreenMarginLeftX = 75;
            const double offscreenMarginRightX = - (250 / 2 + 30);

            var startState = await GetStateAsync(token).ConfigureAwait(false);
            var clamped = ClampTarget(targetX, targetY, startState);

            if (Math.Abs(clamped.x - startState.curX) < 60 && Math.Abs(clamped.y - startState.curY) < 10)
                return;

            var distLeft = startState.curX + offscreenMarginLeftX;
            var distRight = (startState.screenW + offscreenMarginRightX) - startState.curX;
            var edgeSide = distLeft <= distRight ? EdgeSide.Left : EdgeSide.Right;
            var edgeX = edgeSide == EdgeSide.Left ? -offscreenMarginLeftX : startState.screenW + offscreenMarginRightX;

            if (Math.Abs(clamped.x - startState.curX) >= 10)
            {
                var toEdge = edgeSide == EdgeSide.Left
                    ? (isCreeping ? "crawl.left" : "walk.left.faster")
                    : (isCreeping ? "crawl.right" : "walk.right.faster");
                await MoveWithForcedAnimationAsync(
                    toEdge,
                    () => MoveXToAsync(edgeX, maxStepX, tickMs, token, minX: -offscreenMarginLeftX, maxX: startState.screenW + offscreenMarginRightX),
                    token).ConfigureAwait(false);
            }

            var climb = edgeSide == EdgeSide.Left ? "climb.left" : "climb.right";
            await MoveWithForcedAnimationAsync(
                climb,
                () => MoveYToAsync(clamped.y, maxStepY, tickMs, token),
                token).ConfigureAwait(false);

            var afterY = await GetStateAsync(token).ConfigureAwait(false);
            var directionToTarget = clamped.x >= afterY.curX ? "right" : "left";
            var walkToTarget = $"walk.{directionToTarget}.faster";
            await MoveWithForcedAnimationAsync(
                walkToTarget,
                () => MoveXToAsync(clamped.x, maxStepX, tickMs, token),
                token).ConfigureAwait(false);
        }

        private enum EdgeSide
        {
            Left = 0,
            Right = 1,
        }

        private readonly record struct StateSnapshot(double curX, double curY, double maxX, double maxY, double screenW, double screenH);

        private readonly record struct ClampedTarget(double x, double y);

        private async Task<StateSnapshot> GetStateAsync(CancellationToken token)
        {
            return await mw.Dispatcher.InvokeAsync(() =>
            {
                var curX = mw.Core.Controller.GetWindowsDistanceLeft();
                var curY = mw.Core.Controller.GetWindowsDistanceUp();
                var w = mw.Main.ActualWidth;
                var h = mw.Main.ActualHeight;

                var screenW = SystemParameters.PrimaryScreenWidth;
                var screenH = SystemParameters.PrimaryScreenHeight;

                var maxX = Math.Max(0, screenW - w);
                var maxY = Math.Max(0, screenH - h);

                return new StateSnapshot(curX, curY, maxX, maxY, screenW, screenH);
            }, System.Windows.Threading.DispatcherPriority.Send, token);
        }

        private static ClampedTarget ClampTarget(int x, int y, StateSnapshot state)
        {
            var cx = Math.Clamp(x, 0, (int)Math.Floor(state.maxX));
            var cy = Math.Clamp(y, 0, (int)Math.Floor(state.maxY));
            return new ClampedTarget(cx, cy);
        }

        private async Task MoveWithForcedAnimationAsync(string baseGraphName, Func<Task> moveBody, CancellationToken token)
        {
            string? activeGraph = null;
            try
            {
                activeGraph = await StartForcedLoopAsync(baseGraphName, token).ConfigureAwait(false);
                await moveBody().ConfigureAwait(false);
            }
            finally
            {
                if (activeGraph != null)
                    await EndForcedLoopAsync(activeGraph, token).ConfigureAwait(false);
            }
        }

        private async Task<string> StartForcedLoopAsync(string baseGraphName, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            await mw.Dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(token);
                    return;
                }

                var hasStart = TryFindGraph(baseGraphName, GraphInfo.AnimatType.A_Start);
                if (hasStart)
                {
                    try
                    {
                        mw.Main.Display(baseGraphName, AnimatType.A_Start, (graphname) =>
                        {
                            try { mw.Main.DisplayBLoopingForce(graphname); } catch { }
                            tcs.TrySetResult(graphname);
                        });
                    }
                    catch
                    {
                        try { mw.Main.DisplayBLoopingForce(baseGraphName); } catch { }
                        tcs.TrySetResult(baseGraphName);
                    }
                }
                else
                {
                    try { mw.Main.DisplayBLoopingForce(baseGraphName); } catch { }
                    tcs.TrySetResult(baseGraphName);
                }
            }, System.Windows.Threading.DispatcherPriority.Send, token);

            return await tcs.Task.ConfigureAwait(false);
        }

        private async Task EndForcedLoopAsync(string baseGraphName, CancellationToken token)
        {
            await mw.Dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                var hasEnd = TryFindGraph(baseGraphName, GraphInfo.AnimatType.C_End);
                if (hasEnd)
                {
                    try
                    {
                        mw.Main.Display(baseGraphName, AnimatType.C_End, (graphname) =>
                        {
                            try { mw.Main.DisplayCEndtoNomal(graphname); } catch { }
                        });
                    }
                    catch
                    {
                        try { mw.Main.DisplayCEndtoNomal(baseGraphName); } catch { }
                    }
                }
                else
                {
                    try { mw.Main.DisplayCEndtoNomal(baseGraphName); } catch { }
                }
            }, System.Windows.Threading.DispatcherPriority.Send, token);
        }

        private bool TryFindGraph(string baseGraphName, GraphInfo.AnimatType animatType)
        {
            try
            {
                return mw.Core.Graph.FindGraph(baseGraphName, animatType, mw.GameSavesData.GameSave.Mode) != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task MoveXToAsync(double targetX, double maxStepX, int tickMs, CancellationToken token, double? minX = null, double? maxX = null)
        {
            while (!token.IsCancellationRequested)
            {
                var state = await GetStateAsync(token).ConfigureAwait(false);
                var min = minX ?? 0;
                var max = maxX ?? state.maxX;
                var clampedTargetX = Math.Clamp(targetX, min, max);
                var dx = clampedTargetX - state.curX;
                if (Math.Abs(dx) <= 1.0)
                    return;

                var step = Math.Clamp(dx, -maxStepX, maxStepX);
                await mw.Dispatcher.InvokeAsync(() => mw.Core.Controller.MoveWindows(step, 0), System.Windows.Threading.DispatcherPriority.Send, token);
                await Task.Delay(tickMs, token).ConfigureAwait(false);
            }
        }

        private async Task MoveYToAsync(double targetY, double maxStepY, int tickMs, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var state = await GetStateAsync(token).ConfigureAwait(false);
                var clampedTargetY = Math.Clamp(targetY, 0, state.maxY);
                var dy = clampedTargetY - state.curY;
                if (Math.Abs(dy) <= 1.0)
                    return;

                var step = Math.Clamp(dy, -maxStepY, maxStepY);
                await mw.Dispatcher.InvokeAsync(() => mw.Core.Controller.MoveWindows(0, step), System.Windows.Threading.DispatcherPriority.Send, token);
                await Task.Delay(tickMs, token).ConfigureAwait(false);
            }
        }
    }
}
