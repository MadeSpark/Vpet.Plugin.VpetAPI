using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Windows.Interface;

namespace YourModNamespace
{
    /// <summary>
    /// VpetAPI 依赖调用例子
    /// 只演示依赖检测与状态轮询逻辑
    /// </summary>
    public class DependencyExamplePlugin : MainPlugin
    {
        private const string DependencyWorkshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=3666818950";
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

        public DependencyExamplePlugin(IMainWindow mainwin) : base(mainwin)
        {
        }

        public override string PluginName => "依赖调用例子";

        public override void LoadPlugin()
        {
            _ = WaitForVpetApiAsync();
        }

        private async Task WaitForVpetApiAsync()
        {
            try
            {
                var loaded = await WaitUntilAsync(IsVpetApiLoaded, Timeout, PollInterval);
                if (!loaded)
                {
                    ShowDependencyMissingDialog();
                    return;
                }

                var running = await WaitUntilAsync(IsVpetApiRunning, Timeout, PollInterval);
                if (!running)
                {
                    var error = GetVpetApiError();
                    ShowMessage(
                        string.IsNullOrWhiteSpace(error)
                            ? "VpetAPI 的 HTTP 服务初始化异常，请联系开发者。"
                            : $"VpetAPI 的 HTTP 服务初始化异常，请联系开发者。\n\n错误信息：{error}");
                    return;
                }

                var url = GetVpetApiUrl();
                ShowMessage($"VpetAPI 依赖已载入，功能正常运行中。\n服务地址：{url}");
            }
            catch (Exception ex)
            {
                ShowMessage($"依赖检测过程中发生异常：{ex.Message}");
            }
        }

        private async Task<bool> WaitUntilAsync(Func<bool> predicate, TimeSpan timeout, TimeSpan interval)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                if (predicate())
                    return true;

                await Task.Delay(interval);
            }

            return false;
        }

        private bool IsVpetApiLoaded()
        {
            try
            {
                var serviceType = GetVpetApiServiceType();
                var isLoadedProperty = serviceType?.GetProperty(
                    "IsLoaded",
                    BindingFlags.Public | BindingFlags.Static);

                return (bool?)(isLoadedProperty?.GetValue(null)) == true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsVpetApiRunning()
        {
            try
            {
                var serviceType = GetVpetApiServiceType();
                var isRunningProperty = serviceType?.GetProperty(
                    "IsRunning",
                    BindingFlags.Public | BindingFlags.Static);

                return (bool?)(isRunningProperty?.GetValue(null)) == true;
            }
            catch
            {
                return false;
            }
        }

        private string? GetVpetApiUrl()
        {
            try
            {
                var serviceType = GetVpetApiServiceType();
                var urlProperty = serviceType?.GetProperty(
                    "ServiceUrl",
                    BindingFlags.Public | BindingFlags.Static);

                return urlProperty?.GetValue(null) as string;
            }
            catch
            {
                return null;
            }
        }

        private string? GetVpetApiError()
        {
            try
            {
                var serviceType = GetVpetApiServiceType();
                var errorProperty = serviceType?.GetProperty(
                    "ErrorMessage",
                    BindingFlags.Public | BindingFlags.Static);

                return errorProperty?.GetValue(null) as string;
            }
            catch
            {
                return null;
            }
        }

        private Type? GetVpetApiServiceType()
        {
            var assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "VPet.Plugin.VpetAPI");

            return assembly?.GetType("VPet.Plugin.VpetAPI.VpetApiService");
        }

        private void ShowDependencyMissingDialog()
        {
            try
            {
                MW.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        "未正确订阅或启用 VpetAPI 依赖，请检查依赖是否正常。\n\n点击“是”可跳转到依赖创意工坊页面。",
                        "依赖缺失",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = DependencyWorkshopUrl,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"打开创意工坊链接失败：{ex.Message}\n\n请手动访问：{DependencyWorkshopUrl}",
                                "打开失败",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                });
            }
            catch
            {
            }
        }

        private void ShowMessage(string text)
        {
            try
            {
                MW.Dispatcher.Invoke(() =>
                {
                    MW.Main.LabelDisplayShow(text, 5000);
                });
            }
            catch
            {
            }
        }
    }
}
