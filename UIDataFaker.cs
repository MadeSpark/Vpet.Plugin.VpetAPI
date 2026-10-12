using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core;

namespace VPet.Plugin.VpetAPI
{
    /// <summary>
    /// UI 数据篡改管理器。
    /// 不修改存档，也不影响购买/扣钱等游戏逻辑。
    ///
    /// 实现：不使用 Harmony 方法补丁——
    /// 1) 宿主已升级 .NET 10，旧版 Harmony 无法打补丁，新版又与其他 mod 自带的 0Harmony 版本冲突；
    /// 2) 面板刷新路径除定时事件外还有鼠标悬停直调，事件订阅无法覆盖，方法补丁又过于脆弱。
    /// 因此用 DispatcherTimer（100ms）独立同步面板 UI：篡改时覆盖文本/维护称号标签，
    /// 清除时恢复真实文本并同步宿主的 Tag 数值缓存。
    /// </summary>
    public static class UIDataFaker
    {
        private const string CustomLabelName = "VpetAPI_CustomTypeLabel";
        private static Func<VPet_Simulator.Core.ToolBar?>? _toolBarGetter;
        private static DispatcherTimer? _syncTimer;
        private static int? _fakeLevel;
        private static double? _fakeMoney;
        private static string _customType = "穷逼系统ProMax";
        private static bool _smartMoneyMode = false;
        private static Func<double>? _getRealMoney;
        private static Func<int>? _getRealLevel;
        // 上一轮是否处于篡改状态，用于检测"篡改→恢复"的切换
        private static bool _levelWasFaked;
        private static bool _moneyWasFaked;

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

        public static Func<int>? GetRealLevelFunc
        {
            get => _getRealLevel;
            set => _getRealLevel = value;
        }

        // 诊断用：当前篡改状态标记
        public static bool LevelWasFaked => _levelWasFaked;
        public static bool MoneyWasFaked => _moneyWasFaked;
        public static bool SyncTimerRunning => _syncTimer?.IsEnabled == true;

        /// <summary>
        /// 启动 UI 同步定时器。toolBarGetter 每次取当前 ToolBar 实例，
        /// 宿主重建面板后自动跟随，无需重新挂接。
        /// </summary>
        public static void Initialize(Func<VPet_Simulator.Core.ToolBar?> toolBarGetter, Dispatcher dispatcher)
        {
            _toolBarGetter = toolBarGetter;

            if (_syncTimer != null)
                return;

            _syncTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _syncTimer.Tick += (_, _) => SyncToolBar();
            _syncTimer.Start();
        }

        public static void Reset()
        {
            _fakeLevel = null;
            _fakeMoney = null;
            _customType = "穷逼系统ProMax";
            _smartMoneyMode = false;
            // 保留真实值获取函数，下一轮同步时用它恢复真实文本
        }

        public static int GetDisplayLevel(int realLevel)
        {
            return _fakeLevel ?? realLevel;
        }

        public static double GetDisplayMoney(double realMoney)
        {
            return _fakeMoney ?? realMoney;
        }

        private static void SyncToolBar()
        {
            try
            {
                var toolBar = _toolBarGetter?.Invoke();
                if (toolBar == null)
                    return;

                // 智能模式：动态计算绝对值
                if (_smartMoneyMode && _getRealMoney != null)
                {
                    var realMoney = _getRealMoney();
                    _fakeMoney = realMoney < 0 ? Math.Abs(realMoney) : null;
                }

                // 无篡改且无需恢复时跳过，避免常驻开销
                if (!_fakeLevel.HasValue && !_fakeMoney.HasValue
                    && !_levelWasFaked && !_moneyWasFaked)
                    return;

                var levelTextBlock = toolBar.FindName("Tlv") as TextBlock;
                var moneyTextBlock = toolBar.FindName("tMoney") as TextBlock;

                // 更新等级文本与自定义标签（标题）
                if (levelTextBlock != null)
                {
                    if (_fakeLevel is int fakeLevelValue)
                    {
                        levelTextBlock.Text = "Lv " + fakeLevelValue;
                        _levelWasFaked = true;
                    }
                    else if (_levelWasFaked)
                    {
                        // 宿主更新后 M_TimeUIHandle 会把上次渲染值缓存进 Tag，数值未变时跳过重绘，
                        // 假数据会一直残留，必须主动恢复真实文本并同步宿主缓存
                        RestoreText(levelTextBlock, SafeGet(_getRealLevel), "Lv {0:0}");
                        _levelWasFaked = false;
                    }

                    // 标签放在面板网格右上角（右对齐），而不是跟在等级文本后面
                    var labelGrid = FindLabelGrid(levelTextBlock);
                    if (labelGrid != null)
                        EnsureCustomLabel(labelGrid, levelTextBlock, _fakeLevel.HasValue || _fakeMoney.HasValue);
                }

                // 更新金钱文本
                if (moneyTextBlock != null)
                {
                    if (_fakeMoney is double fakeMoneyValue)
                    {
                        moneyTextBlock.Text = "$ " + fakeMoneyValue.ToString("N2");
                        _moneyWasFaked = true;
                        SyncBetterBuyMoney(fakeMoneyValue, null);
                    }
                    else if (_moneyWasFaked)
                    {
                        var realMoney = SafeGet(_getRealMoney);
                        RestoreText(moneyTextBlock, realMoney, "$ {0:N2}");
                        SyncBetterBuyMoney(null, realMoney);
                        _moneyWasFaked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIDataFaker] sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// 定位放置称号标签的面板网格。
        /// 宿主运行时会把 Tlv 移进水平 StackPanel 再放回 gdPanel，
        /// 因此标签需要放在外层 Grid（gdPanel）才能独立右对齐。
        /// </summary>
        private static Grid? FindLabelGrid(TextBlock levelTextBlock)
        {
            if (levelTextBlock.Parent is Grid grid)
                return grid;
            if (levelTextBlock.Parent is Panel panel && panel.Parent is Grid outerGrid)
                return outerGrid;
            return null;
        }

        /// <summary>
        /// 创建/更新/隐藏自定义类型标签（标题）。
        /// 标签固定放在 gdPanel 等级所在行、从金钱列跨到最右列并右对齐（面板右上角），
        /// 不跟随 Tlv 所在的 StackPanel，避免挤在 "Lv N" 文本后面。
        /// </summary>
        private static void EnsureCustomLabel(Grid grid, TextBlock levelTextBlock, bool shouldShowLabel)
        {
            var customLabel = grid.FindName(CustomLabelName) as TextBlock;

            // 标签挂在旧父容器上（宿主重新布局导致），先摘下来重新放置
            if (customLabel != null && !ReferenceEquals(customLabel.Parent, grid))
            {
                if (customLabel.Parent is Panel oldParent)
                    oldParent.Children.Remove(customLabel);
                customLabel = null; // 名称注册仍在宿主 NameScope 中，无需重复 RegisterName
            }

            if (!shouldShowLabel)
            {
                if (customLabel != null)
                    customLabel.Visibility = Visibility.Collapsed;
                return;
            }

            if (customLabel == null)
            {
                customLabel = new TextBlock
                {
                    Name = CustomLabelName,
                    FontSize = levelTextBlock.FontSize,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 204, 76)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(5, 0, 10, 0)
                };

                // 行取等级容器的实际行（StackPanel 或 Tlv 自身都在第 0 行）；
                // 只占金钱列（第 2 列）并右对齐，右缘与右侧数据列（x1.00 等）起点对齐，
                // 避开右上角的桌宠头像，也不遮挡最右列内容
                var rowHost = levelTextBlock.Parent is StackPanel sp ? (UIElement)sp : levelTextBlock;
                Grid.SetRow(customLabel, Grid.GetRow(rowHost));
                Grid.SetColumn(customLabel, 2);

                grid.Children.Add(customLabel);
                grid.RegisterName(CustomLabelName, customLabel);
            }

            customLabel.Text = _customType;
            customLabel.Visibility = Visibility.Visible;
        }

        // 宿主 winBetterBuy 的金钱 Run 私有字段（rMoney），按名反射解析，兼容宿主更新
        private static FieldInfo? _betterBuyMoneyField;

        /// <summary>
        /// 同步"更好买"商店标题栏的金钱显示。
        /// fakeMoney 非 null 时覆盖为假值；为 null 且传入真实值时恢复真实显示。
        /// </summary>
        private static void SyncBetterBuyMoney(double? fakeMoney, double? realMoney)
        {
            try
            {
                var windows = Application.Current?.Windows;
                if (windows == null)
                    return;

                var value = fakeMoney ?? realMoney;
                if (!value.HasValue)
                    return;

                foreach (Window window in windows)
                {
                    // 不直接引用宿主程序集类型，按窗口类名匹配，反射读取 rMoney 字段
                    if (window.GetType().Name != "winBetterBuy")
                        continue;

                    _betterBuyMoneyField ??= window.GetType().GetField(
                        "rMoney", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (_betterBuyMoneyField?.GetValue(window) is Run moneyRun)
                        moneyRun.Text = value.Value.ToString("F2"); // 与宿主 ToString("f2") 格式一致
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIDataFaker] betterbuy sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复真实文本并同步宿主的 Tag 数值缓存；
        /// 拿不到真实值时清空缓存，强制宿主在下一轮刷新时重绘。
        /// </summary>
        private static void RestoreText(TextBlock text, double? realValue, string format)
        {
            if (realValue.HasValue)
            {
                text.Text = string.Format(format, realValue.Value);
                text.Tag = realValue.Value;
            }
            else
            {
                text.Tag = null;
            }
        }

        private static double? SafeGet(Func<int>? func)
        {
            if (func == null)
                return null;
            try { return func(); }
            catch { return null; }
        }

        private static double? SafeGet(Func<double>? func)
        {
            if (func == null)
                return null;
            try { return func(); }
            catch { return null; }
        }

        public static void Uninitialize()
        {
            _syncTimer?.Stop();
            _syncTimer = null;
            _toolBarGetter = null;
            _getRealMoney = null;
            _getRealLevel = null;
            _levelWasFaked = false;
            _moneyWasFaked = false;
        }
    }
}
