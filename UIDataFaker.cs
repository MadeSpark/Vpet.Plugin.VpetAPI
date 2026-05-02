using HarmonyLib;
using System;
using VPet_Simulator.Core;

namespace VPet.Plugin.VpetAPI
{
    /// <summary>
    /// UI 数据篡改管理器
    /// 使用 Harmony 拦截游戏的 UI 更新，返回假数据
    /// </summary>
    public static class UIDataFaker
    {
        private static Harmony? _harmony;
        private static int? _fakeLevel = null;
        private static double? _fakeMoney = null;

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

            _harmony = new Harmony("com.madespark.vpetapi.datafaker");
            
            try
            {
                // Patch IGameSave.Level 属性的 getter
                var levelGetter = AccessTools.PropertyGetter(typeof(IGameSave), nameof(IGameSave.Level));
                if (levelGetter != null)
                {
                    _harmony.Patch(levelGetter,
                        postfix: new HarmonyMethod(typeof(UIDataFaker), nameof(Level_Postfix)));
                }

                // Patch IGameSave.Money 属性的 getter
                var moneyGetter = AccessTools.PropertyGetter(typeof(IGameSave), nameof(IGameSave.Money));
                if (moneyGetter != null)
                {
                    _harmony.Patch(moneyGetter,
                        postfix: new HarmonyMethod(typeof(UIDataFaker), nameof(Money_Postfix)));
                }

                System.Diagnostics.Debug.WriteLine("[UIDataFaker] Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIDataFaker] Failed to apply patches: {ex.Message}");
            }
        }

        public static void Reset()
        {
            _fakeLevel = null;
            _fakeMoney = null;
        }

        // Harmony Postfix: 拦截 Level 属性的返回值
        private static void Level_Postfix(ref int __result)
        {
            if (_fakeLevel.HasValue)
            {
                __result = _fakeLevel.Value;
            }
        }

        // Harmony Postfix: 拦截 Money 属性的返回值
        private static void Money_Postfix(ref double __result)
        {
            if (_fakeMoney.HasValue)
            {
                __result = _fakeMoney.Value;
            }
        }

        public static void Uninitialize()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll("com.madespark.vpetapi.datafaker");
                _harmony = null;
            }
        }
    }
}
