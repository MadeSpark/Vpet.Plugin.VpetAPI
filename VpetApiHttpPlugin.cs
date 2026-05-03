using System;
using System.Windows;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.VpetAPI
{
    public sealed class VpetApiHttpPlugin : MainPlugin
    {
        public override string PluginName => "VpetAPI";

        private HttpControlServer? server;
        private PetMover? mover;
        private WorkCatalog? workCatalog;
        private VpetStateController? stateController;
        private LevelLimitAdjuster? levelLimitAdjuster;

        public VpetApiHttpPlugin(IMainWindow mainwin) : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            // 标记插件已加载
            VpetApiService.MarkLoaded();
            
            // 初始化 UI 数据篡改器（Harmony Hooks）
            UIDataFaker.Initialize();

            mover = new PetMover(MW);
            workCatalog = new WorkCatalog(MW);
            levelLimitAdjuster = new LevelLimitAdjuster(MW);
            stateController = new VpetStateController(MW, workCatalog, mover, levelLimitAdjuster);

            try
            {
                server = new HttpControlServer(stateController);
                server.Start();
                
                // 标记服务已启动
                VpetApiService.Start(server.Port);
                
                TryNotify($"VpetAPI HTTP 服务已启动：127.0.0.1:{server.Port}");
            }
            catch (Exception ex)
            {
                // 标记服务启动失败
                VpetApiService.MarkFailed(ex.Message);
                
                TryNotify($"VpetAPI HTTP 服务启动失败：{ex.Message}");
                try
                {
                    MW.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"VpetAPI HTTP 服务启动失败：{ex.Message}\n\n" +
                            "常见原因：端口被占用，或 HttpListener 未配置 URLACL。\n" +
                            "如需 URLACL，可用管理员执行：\n" +
                            "netsh http add urlacl url=http://127.0.0.1:52814/ user=当前用户名",
                            "VpetAPI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
                catch
                {
                }
            }
        }

        ~VpetApiHttpPlugin()
        {
            try { server?.Stop(); } catch { }
            try { UIDataFaker.Uninitialize(); } catch { }
            try { VpetApiService.Stop(); } catch { }
        }

        private void TryNotify(string text)
        {
            try { MW.Dispatcher.BeginInvoke(() => MW.Main.LabelDisplayShow(text, 5000)); } catch { }
        }
    }
}

