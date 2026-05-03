using HarmonyLib;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private const string CustomLabelName = "VpetAPI_CustomTypeLabel";
        private static Harmony? _harmony;
        private static int? _fakeLevel;
        private static double? _fakeMoney;
        private static string _customType = "穷逼系统ProMax";
        private static bool _smartMoneyMode = false;
        private static Func<double>? _getRealMoney;

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

        public static string CustomType
        {
            get => _customType;
            set => _customType = value ?? "穷逼系统ProMax";
        }

        public static bool SmartMoneyMode
        {
            get => _smartMoneyMode;
            set => _smartMoneyMode = value;
        }

        public static Func<double>? GetRealMoneyFunc
        {
            get => _getRealMoney;
            set => _getRealMoney = value;
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
            _customType = "穷逼系统ProMax";
            _smartMoneyMode = false;
            _getRealMoney = null;
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
            // 智能模式：动态计算绝对值
            if (_smartMoneyMode && _getRealMoney != null)
            {
                var realMoney = _getRealMoney();
                if (realMoney < 0)
                {
                    _fakeMoney = Math.Abs(realMoney);
                }
                else
                {
                    _fakeMoney = null;
                }
            }

            var levelTextBlock = __instance.FindName("Tlv") as TextBlock;
            var moneyTextBlock = __instance.FindName("tMoney") as TextBlock;
            
            // 判断是否需要显示自定义标签（有等级篡改或金钱篡改）
            bool shouldShowLabel = _fakeLevel.HasValue || _fakeMoney.HasValue;
            
            if (shouldShowLabel && levelTextBlock != null)
            {
                // 更新等级文本
                if (_fakeLevel.HasValue)
                {
                    levelTextBlock.Text = "Lv " + _fakeLevel.Value;
                }
                
                // 创建或更新自定义类型标签
                var panel = levelTextBlock.Parent as Panel;
                if (panel != null)
                {
                    var customLabel = panel.FindName(CustomLabelName) as TextBlock;
                    if (customLabel == null)
                    {
                        customLabel = new TextBlock
                        {
                            Name = CustomLabelName,
                            FontSize = levelTextBlock.FontSize,
                            Foreground = new SolidColorBrush(Color.FromRgb(255, 204, 76)),
                            VerticalAlignment = levelTextBlock.VerticalAlignment,
                            Margin = new Thickness(5, 0, 0, 0)
                        };
                        
                        panel.RegisterName(CustomLabelName, customLabel);
                        
                        // 如果是 Grid，添加到同一行
                        if (panel is Grid grid)
                        {
                            var row = Grid.GetRow(levelTextBlock);
                            var col = Grid.GetColumn(levelTextBlock);
                            Grid.SetRow(customLabel, row);
                            Grid.SetColumn(customLabel, col);
                        }
                        
                        panel.Children.Add(customLabel);
                    }
                    
                    customLabel.Text = _customType;
                    customLabel.Visibility = Visibility.Visible;
                }
            }
            else if (levelTextBlock != null)
            {
                // 隐藏自定义标签
                var panel = levelTextBlock.Parent as Panel;
                if (panel != null)
                {
                    var customLabel = panel.FindName(CustomLabelName) as TextBlock;
                    if (customLabel != null)
                    {
                        customLabel.Visibility = Visibility.Collapsed;
                    }
                }
            }

            // 更新金钱文本
            if (_fakeMoney.HasValue && moneyTextBlock != null)
                moneyTextBlock.Text = "$ " + _fakeMoney.Value.ToString("N2");
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
