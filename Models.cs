namespace VPet.Plugin.VpetAPI
{
    public sealed class MoveToRequest
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsCreeping { get; set; }
    }

    public sealed class SayRequest
    {
        public string? Text { get; set; }
    }

    public sealed class SetSleepRequest
    {
        public bool IsSleeping { get; set; }
    }

    public sealed class SetWorkRequest
    {
        public string? Id { get; set; }
    }

    public sealed class SetMenuItemRequest
    {
        public string? Name { get; set; }
        public string? CallbackUrl { get; set; }
    }

    public enum WorkCategory
    {
        Work = 0,
        Study = 1,
        Play = 2,
    }

    public enum FoodListCategory
    {
        Star = 0,
        Meal = 1,
        Snack = 2,
        Drink = 3,
        Functional = 4,
        Drug = 5,
        Gift = 6,
    }

    public sealed class FoodInfoResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Exp { get; set; }
        public double StrengthFood { get; set; }
        public double StrengthDrink { get; set; }
        public double Strength { get; set; }
        public double Feeling { get; set; }
        public double Health { get; set; }
        public double Likability { get; set; }
        public string LikabilityPercent { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class BuyItemRequest
    {
        public string? Id { get; set; }
        public int Count { get; set; } = 1;
    }
}
