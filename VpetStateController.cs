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
                Description = food.Description,
            };
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
