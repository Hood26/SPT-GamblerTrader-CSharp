using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace SPT_GamblerTrader_CSharp;

public class LootBoxData
{
    public required Dictionary<string, LootBoxDataProps> Containers { get; set; }

    public class LootBoxDataProps
    {
        public required List<List<Reward>> Rewards { get; set; }
        public string RewardType { get; set; } = "Item";
        public string? RewardContainer { get; set; } = null;
    }

    public class Reward {
        public string? Item { get; set; }
        public int Amount { get; set; } = 1;
        public string? Id { get; set; } 
        public string? Name { get; set; } 
        public string? Root { get; set; } 
        public List<Attachments>? Items { get; set; } 
    }

    public class Attachments
    {
        public string? _id { get; set; }
        public string? _tpl { get; set; }
        public string? slotId { get; set; }
        public string? parentId { get; set; }
    }

    /*
        // Returns only one rewards from a containers list of possible rewards
        public Reward? GetReward(GamblerData gamblerData, string containerName, int index)
        {
            //string? rewardContainer = Containers[containerName].RewardContainer;
            //List<Reward>? rewards;
            //if (rewardContainer is null)
            var rewards = Containers[containerName].Rewards[index];
            gamblerData.logger.Info($"Rewards = {rewards}");
            int randomRewardIndex = Random.Shared.Next(0, rewards.Count() - 1);
            if (rewards[randomRewardIndex].Item == null) return null;
            return rewards[randomRewardIndex];
        }

        // Returns a specific index given a randomized roll in a certain container
        private int GetIndex(GamblerData gamblerData, string containerName, float roll)
        {
            var itemProps = gamblerData.config.Items[containerName].odds;
            float sum = 0;

            for (int i = 0; i < itemProps.Count; i++)
            {
                var item = itemProps.ElementAt(i);
                sum += item.Value;
                if (roll <= sum) return i;
            }
            return -1;
        }

        // Returns a randon float between 0-100
        private float RandomRoll()
        {
            return MathF.Round(Random.Shared.NextSingle() * (100.0f - 0.0f) + 0.0f, 2);
        }
    */
    // Returns all possible rewards from a container
    public List<Reward> GetRewards(string containerName, int index)
    {
        return Containers[containerName].Rewards[index];
    }
}