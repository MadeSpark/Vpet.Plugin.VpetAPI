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
}
