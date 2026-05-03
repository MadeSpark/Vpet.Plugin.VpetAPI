using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LinePutScript.Localization.WPF;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.VpetAPI
{
    public sealed class VpetStateController
    {
        private readonly IMainWindow mw;
        private readonly WorkCatalog workCatalog;
        private readonly PetMover mover;
        private readonly MenuManager menuManager;
        private readonly LevelLimitAdjuster levelLimitAdjuster;
        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public VpetStateController(IMainWindow mw, WorkCatalog workCatalog, PetMover mover, LevelLimitAdjuster levelLimitAdjuster)
        {
            this.mw = mw ?? throw new ArgumentNullException(nameof(mw));
            this.workCatalog = workCatalog ?? throw new ArgumentNullException(nameof(workCatalog));
            this.mover = mover ?? throw new ArgumentNullException(nameof(mover));
            this.levelLimitAdjuster = levelLimitAdjuster ?? throw new ArgumentNullException(nameof(levelLimitAdjuster));
            menuManager = new MenuManager(mw);
        }

        public async Task<(int statusCode, object payload)> HandleAsync(string path, string bodyText, CancellationToken token)
        {
            switch (path)
            {
                case "/move_to":
                    return await MoveToAsync(bodyText, token).ConfigureAwait(false);
                case "/say":
                    return await SayAsync(bodyText, token, rnd: false).ConfigureAwait(false);
                case "/say_rnd":
                    return await SayAsync(bodyText, token, rnd: true).ConfigureAwait(false);
                case "/set_sleep":
                    return await SetSleepAsync(bodyText, token).ConfigureAwait(false);
                case "/set_work":
                    return await StartWorkAsync(bodyText, token, WorkCategory.Work).ConfigureAwait(false);
                case "/set_study":
                    return await StartWorkAsync(bodyText, token, WorkCategory.Study).ConfigureAwait(false);
                case "/set_play":
                    return await StartWorkAsync(bodyText, token, WorkCategory.Play).ConfigureAwait(false);
                case "/get_work_list":
                    return await GetListAsync(token, WorkCategory.Work).ConfigureAwait(false);
                case "/get_study_list":
                    return await GetListAsync(token, WorkCategory.Study).ConfigureAwait(false);
                case "/get_play_list":
                    return await GetListAsync(token, WorkCategory.Play).ConfigureAwait(false);
                case "/get_favorite_food_list":
                    return await GetFoodListAsync(token, FoodListCategory.Star).ConfigureAwait(false);
                case "/get_meal_list":
                    return await GetFoodListAsync(token, FoodListCategory.Meal).ConfigureAwait(false);
                case "/get_snack_list":
                    return await GetFoodListAsync(token, FoodListCategory.Snack).ConfigureAwait(false);
                case "/get_drink_list":
                    return await GetFoodListAsync(token, FoodListCategory.Drink).ConfigureAwait(false);
                case "/get_functional_list":
                    return await GetFoodListAsync(token, FoodListCategory.Functional).ConfigureAwait(false);
                case "/get_drug_list":
                    return await GetFoodListAsync(token, FoodListCategory.Drug).ConfigureAwait(false);
                case "/get_gift_list":
                    return await GetFoodListAsync(token, FoodListCategory.Gift).ConfigureAwait(false);
                case "/buy_item":
                    return await BuyItemAsync(bodyText, token).ConfigureAwait(false);
                case "/get_pet_info":
                    return await GetPetInfoAsync(token).ConfigureAwait(false);
                case "/set_fake_level":
                    return await SetFakeLevelAsync(bodyText, token).ConfigureAwait(false);
                case "/set_fake_money":
                    return await SetFakeMoneyAsync(bodyText, token).ConfigureAwait(false);
                case "/set_fake_money_smart":
                    return await SetFakeMoneySmartAsync(bodyText, token).ConfigureAwait(false);
                case "/reset_fake_data":
                    return await ResetFakeDataAsync(token).ConfigureAwait(false);
                case "/set_menu":
                    return await SetMenuAsync(bodyText, token).ConfigureAwait(false);
                case "/reset_status":
                    return await ResetStatusAsync(token).ConfigureAwait(false);
                default:
                    return (404, new { error = $"未知接口: {path}" });
            }
        }

        private async Task<(int, object)> MoveToAsync(string bodyText, CancellationToken token)
        {
            var req = TryDeserialize<MoveToRequest>(bodyText);
            if (req == null)
                return (400, new { error = "请求体需要包含 x,y" });

            await mover.MoveToAsync(req.X, req.Y, req.IsCreeping, token).ConfigureAwait(false);
            return (200, new { });
        }

        private async Task<(int, object)> SayAsync(string bodyText, CancellationToken token, bool rnd)
        {
            var req = TryDeserialize<SayRequest>(bodyText);
            var text = req?.Text?.Trim();
            if (string.IsNullOrEmpty(text))
                return (400, new { error = "请求体需要包含 text" });

            if (text.Length > 500)
                text = text[..500];

            await mw.Dispatcher.InvokeAsync(() =>
            {
                if (rnd)
                    mw.Main.SayRnd(text, true);
                else
                    mw.Main.Say(text);
            });

            return (200, new { });
        }

        private async Task<(int, object)> SetSleepAsync(string bodyText, CancellationToken token)
        {
            var req = TryDeserialize<SetSleepRequest>(bodyText);
            if (req == null)
                return (400, new { error = "请求体需要包含 isSleeping" });

            var ok = await SleepController.TrySetSleepAsync(mw, req.IsSleeping, token).ConfigureAwait(false);
            if (!ok)
                return (500, new { error = "未找到可用的睡觉/唤醒接口（可能与当前版本不兼容）" });

            return (200, new { });
        }

        private async Task<(int, object)> StartWorkAsync(string bodyText, CancellationToken token, WorkCategory category)
        {
            var req = TryDeserialize<SetWorkRequest>(bodyText) ?? new SetWorkRequest { Id = string.Empty };
            var ok = await workCatalog.TryStartAsync(category, req.Id, token, levelLimitAdjuster).ConfigureAwait(false);
            if (!ok)
                return (400, new { error = "未找到对应条目（id 可为索引或名称；为空则随机）" });
            return (200, new { });
        }

        private async Task<(int, object)> GetListAsync(CancellationToken token, WorkCategory category)
        {
            var list = await workCatalog.GetNameListAsync(category, token).ConfigureAwait(false);
            return (200, new { data = list });
        }

        private async Task<(int, object)> GetFoodListAsync(CancellationToken token, FoodListCategory category)
        {
            token.ThrowIfCancellationRequested();

            var foods = await mw.Dispatcher.InvokeAsync(() =>
            {
                IEnumerable<Food> source = category switch
                {
                    FoodListCategory.Star => mw.Foods.Where(food => food.Star || mw.Set["betterbuy"]["star"].GetInfos().Contains(food.Name)),
                    FoodListCategory.Meal => mw.Foods.Where(food => food.Type == Food.FoodType.Meal),
                    FoodListCategory.Snack => mw.Foods.Where(food => food.Type == Food.FoodType.Snack),
                    FoodListCategory.Drink => mw.Foods.Where(food => food.Type == Food.FoodType.Drink),
                    FoodListCategory.Functional => mw.Foods.Where(food => food.Type == Food.FoodType.Functional),
                    FoodListCategory.Drug => mw.Foods.Where(food => food.Type == Food.FoodType.Drug),
                    FoodListCategory.Gift => mw.Foods.Where(food => food.Type == Food.FoodType.Gift),
                    _ => Enumerable.Empty<Food>(),
                };

                return source
                    .OrderBy(food => food.TranslateName, StringComparer.CurrentCulture)
                    .Select(ToFoodInfo)
                    .ToList();
            });

            return (200, new { data = foods });
        }

        private static FoodInfoResponse ToFoodInfo(Food food)
        {
            return new FoodInfoResponse
            {
                Name = food.TranslateName,
                Id = food.Name,
                Price = food.Price,
                Exp = food.Exp,
                StrengthFood = food.StrengthFood,
                StrengthDrink = food.StrengthDrink,
                Strength = food.Strength,
                Feeling = food.Feeling,
                Health = food.Health,
                Likability = food.Likability,
                LikabilityPercent = "100%", // 默认100%，实际值需要从游戏内部状态获取
                Description = LocalizeCore.Translate(food.Desc ?? string.Empty),
            };
        }

        private async Task<(int, object)> BuyItemAsync(string bodyText, CancellationToken token)
        {
            var req = TryDeserialize<BuyItemRequest>(bodyText) ?? new BuyItemRequest();
            
            if (req.Count <= 0)
                req.Count = 1;

            Food? targetFood = null;

            await mw.Dispatcher.InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(req.Id))
                {
                    // 随机购买
                    if (mw.Foods.Count > 0)
                    {
                        var random = new Random();
                        targetFood = mw.Foods[random.Next(mw.Foods.Count)];
                    }
                }
                else
                {
                    // 按名称或ID查找
                    targetFood = mw.Foods.FirstOrDefault(f => 
                        string.Equals(f.Name, req.Id, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(f.TranslateName, req.Id, StringComparison.OrdinalIgnoreCase));
                }
            });

            if (targetFood == null)
                return (404, new { error = "未找到指定物品" });

            // 执行购买逻辑（参考 winBetterBuy.xaml.cs 的 BtnBuy_Click）
            bool success = false;
            string? errorMsg = null;

            await mw.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    for (int i = 0; i < req.Count; i++)
                    {
                        // 检查金钱（$1000以内允许赊账）
                        if (targetFood.Price >= 1000 && targetFood.Price > mw.Core.Save.Money)
                        {
                            errorMsg = $"金钱不足，需要 {targetFood.Price:f2}，当前拥有 {mw.Core.Save.Money:f2}";
                            return;
                        }

                        // 检查超模（如果启用HashCheck）
                        if (mw.HashCheck && targetFood.IsOverLoad())
                        {
                            mw.HashCheckOff();
                        }

                        // 扣除金钱
                        mw.Core.Save.Money -= targetFood.Price;
                        
                        // 使用物品
                        mw.TakeItem(targetFood);
                    }

                    // 显示动画
                    mw.DisplayFoodAnimation(targetFood.GetGraph(), targetFood.ImageSource);
                    
                    success = true;
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                }
            });

            if (!success)
                return (400, new { error = errorMsg ?? "购买失败" });

            return (200, new 
            { 
                message = "购买成功",
                item = targetFood.TranslateName,
                count = req.Count,
                totalPrice = targetFood.Price * req.Count,
                remainingMoney = mw.Core.Save.Money
            });
        }

        private async Task<(int, object)> ResetStatusAsync(CancellationToken token)
        {
            await StatusResetter.ResetAsync(mw, mover, token).ConfigureAwait(false);
            return (200, new { });
        }

        private async Task<(int, object)> SetMenuAsync(string bodyText, CancellationToken token)
        {
            var req = TryDeserialize<Dictionary<string, Dictionary<string, SetMenuItemRequest>>>(bodyText);
            if (req == null || req.Count == 0)
                return (400, new { error = "请求体需要包含菜单定义" });

            foreach (var rootPair in req)
            {
                var rootName = rootPair.Key?.Trim();
                var rootSpec = rootPair.Value;
                if (string.IsNullOrEmpty(rootName) || rootSpec == null || rootSpec.Count == 0)
                    continue;

                foreach (var itemPair in rootSpec)
                {
                    var menuKey = itemPair.Key?.Trim();
                    var item = itemPair.Value;
                    if (string.IsNullOrEmpty(menuKey))
                        continue;

                    if (string.IsNullOrWhiteSpace(item?.Name) || string.IsNullOrWhiteSpace(item?.CallbackUrl))
                        return (400, new { error = $"菜单定义缺少 name/callbackUrl: {rootName}/{menuKey}" });
                }
            }

            await mw.Dispatcher.InvokeAsync(() =>
            {
                menuManager.Apply(req);
            });

            return (200, new { });
        }

        private async Task<(int, object)> GetPetInfoAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            PetInfoResponse info = await mw.Dispatcher.InvokeAsync(() =>
            {
                var save = mw.Core.Save;
                return new PetInfoResponse
                {
                    Name = save.Name,
                    Level = save.Level,
                    Money = save.Money,
                    Exp = save.Exp,
                    LevelUpNeed = save.LevelUpNeed(),
                    Strength = save.Strength,
                    StrengthMax = save.StrengthMax,
                    Feeling = save.Feeling,
                    StrengthFood = save.StrengthFood,
                    StrengthDrink = save.StrengthDrink,
                    Likability = save.Likability,
                    Health = save.Health,
                };
            });

            return (200, new { data = info });
        }

        private async Task<(int, object)> SetFakeLevelAsync(string bodyText, CancellationToken token)
        {
            var req = TryDeserialize<FakeDataRequest>(bodyText);
            if (req?.Level == null)
                return (400, new { error = "请求体需要包含 level" });

            UIDataFaker.FakeLevel = Math.Max(1, req.Level.Value);
            UIDataFaker.CustomType = string.IsNullOrWhiteSpace(req.Type) ? "穷逼系统ProMax" : req.Type;

            return (200, new 
            { 
                message = "等级篡改成功（UI显示已更新）",
                fakeLevel = UIDataFaker.FakeLevel.Value,
                realLevel = await GetRealLevelAsync()
            });
        }

        private async Task<(int, object)> SetFakeMoneyAsync(string bodyText, CancellationToken token)
        {
            var req = TryDeserialize<FakeDataRequest>(bodyText);
            if (req?.Money == null)
                return (400, new { error = "请求体需要包含 money" });

            UIDataFaker.FakeMoney = Math.Max(0, req.Money.Value);
            UIDataFaker.CustomType = string.IsNullOrWhiteSpace(req.Type) ? "穷逼系统ProMax" : req.Type;

            return (200, new 
            { 
                message = "金钱篡改成功（UI显示已更新）",
                fakeMoney = UIDataFaker.FakeMoney.Value,
                realMoney = await GetRealMoneyAsync()
            });
        }

        private async Task<(int, object)> SetFakeMoneySmartAsync(string bodyText, CancellationToken token)
        {
            var req = TryDeserialize<FakeDataRequest>(bodyText);
            var customType = string.IsNullOrWhiteSpace(req?.Type) ? "穷逼系统ProMax" : req.Type;
            
            var realMoney = await GetRealMoneyAsync();
            
            // 设置智能模式和真实金钱获取函数
            UIDataFaker.SmartMoneyMode = true;
            UIDataFaker.GetRealMoneyFunc = () => mw.Core.Save.Money;
            UIDataFaker.CustomType = customType;
            
            // 如果是正数，不做处理
            if (realMoney >= 0)
            {
                UIDataFaker.FakeMoney = null;
                
                return (200, new 
                { 
                    message = "金钱为正数，无需篡改",
                    realMoney = realMoney,
                    changed = false
                });
            }
            
            // 负数显示为绝对值
            UIDataFaker.FakeMoney = Math.Abs(realMoney);
            
            return (200, new 
            { 
                message = "智能篡改成功（负数已转为正数显示）",
                fakeMoney = UIDataFaker.FakeMoney.Value,
                realMoney = realMoney,
                changed = true
            });
        }

        private async Task<(int, object)> ResetFakeDataAsync(CancellationToken token)
        {
            UIDataFaker.Reset();

            return (200, new 
            { 
                message = "已恢复真实数据",
                level = await GetRealLevelAsync(),
                money = await GetRealMoneyAsync()
            });
        }

        // 获取真实等级
        private async Task<int> GetRealLevelAsync()
        {
            return await mw.Dispatcher.InvokeAsync(() => mw.Core.Save.Level);
        }

        // 获取真实金钱
        private async Task<double> GetRealMoneyAsync()
        {
            return await mw.Dispatcher.InvokeAsync(() => mw.Core.Save.Money);
        }

        private T? TryDeserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(json, jsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
