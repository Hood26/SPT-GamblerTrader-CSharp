using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace SPT_GamblerTrader_CSharp;

public class LootBoxData
{
    public required Dictionary<string, LootBoxDataProps> Containers { get; set; }

    public class LootBoxDataProps
    {
        //public required List<string> Rarities { get; set; }
        //public required List<int> RewardsAmount { get; set; }
        public required List<List<Reward>> Rewards { get; set; }
    }

    public class Reward {
        public required string? Item { get; set; }
        public int Amount { get; set; } = 1;
    }

    // Returns only one rewards from a containers list of possible rewards
    public Reward? GetReward(GamblerData gamblerData, string containerName, int index)
    {
        var rewards = Containers[containerName].Rewards[index];
        var test = rewards.Count() - 1;
        int randomRewardIndex = Random.Shared.Next(0, rewards.Count() - 1);
        if (rewards[randomRewardIndex].Item == null) return null;
        return rewards[randomRewardIndex];
    }

    // Returns all possible rewards from a container
    public List<Reward> GetRewards(string containerName, int index)
    {
        return Containers[containerName].Rewards[index];
    }
}