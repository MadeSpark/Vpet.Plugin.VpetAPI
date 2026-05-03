using HarmonyLib;
using System;
using System.Windows.Controls;
using VPet_Simulator.Core;

namespace VPet.Plugin.VpetAPI
{
    /// <summary>
    /// UI 数据篡改管理器。
    /// 只拦截桌宠面板 UI 刷新函数，不修改存档，也不影响购买/扣钱等游戏逻辑。
    /// </summary>
    public static class UIDataFaker
    {
        private const string HarmonyId = "com.madespark.vpetapi.datafaker";
        private static Harmony? _harmony;
        private static int? _fakeLevel;
        private static double? _fakeMoney;

        public static int? FakeLevel
        {
            get => _fakeLevel;
            set => _fakeLevel = value;
        }

        public static double? FakeMoney
        {
            get => _fakeMoney;
            set => _fakeMoney = value;
        }

        public static void Initialize()
        {
            if (_harmony != null)
                return;

            _harmony = new Harmony(HarmonyId);

            try
            {
                var refreshMethod = AccessTools.Method(typeof(VPet_Simulator.Core.ToolBar), nameof(VPet_Simulator.Core.ToolBar.M_TimeUIHandle));
                if (refreshMethod != null)
                {
                    _harmony.Patch(
                        refreshMethod,
                        postfix: new HarmonyMethod(typeof(UIDataFaker), nameof(ToolBar_M_TimeUIHandle_Postfix)));
                }

                System.Diagnostics.Debug.WriteLine("[UIDataFaker] ToolBar UI patch applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIDataFaker] Failed to apply UI patch: {ex.Message}");
            }
        }

        public static void Reset()
        {
            _fakeLevel = null;
            _fakeMoney = null;
        }

        public static int GetDisplayLevel(int realLevel)
        {
            return _fakeLevel ?? realLevel;
        }

        public static double GetDisplayMoney(double realMoney)
        {
            return _fakeMoney ?? realMoney;
        }

        private static void ToolBar_M_TimeUIHandle_Postfix(VPet_Simulator.Core.ToolBar __instance)
        {
            if (_fakeLevel.HasValue && __instance.FindName("Tlv") is TextBlock levelText)
                levelText.Text = "Lv " + _fakeLevel.Value;

            if (_fakeMoney.HasValue && __instance.FindName("tMoney") is TextBlock moneyText)
                moneyText.Text = "$ " + _fakeMoney.Value.ToString("N2");
        }

        public static void Uninitialize()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(HarmonyId);
                _harmony = null;
            }
        }
    }
}
