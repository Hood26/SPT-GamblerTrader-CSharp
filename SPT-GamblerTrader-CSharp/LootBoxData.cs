namespace SPT_GamblerTrader_CSharp;

public class LootBoxData
{
    public required Dictionary<string, LootBoxDataProps> Containers { get; set; }

    public class LootBoxDataProps
    {
        public required List<string> Rarities { get; set; }
        public required List<int> RewardsAmount { get; set; }
        public required List<List<Rewards>> Rewards { get; set; }
    }

    public class Rewards {
        public required int Amount { get; set; }
        public required List<string> Items { get; set; }
    }
}