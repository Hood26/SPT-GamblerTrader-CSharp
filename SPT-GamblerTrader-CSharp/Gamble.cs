using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Utils;

namespace SPT_GamblerTrader_CSharp;

public class Gamble(GamblerData gamblerData, string containerName)
{
    private readonly GamblerData gamblerData = gamblerData;
    private string containerName = containerName;
    public AddItemsDirectRequest newItemsRequest = new()
    {
        ItemsWithModsToAdd = [],
        FoundInRaid = true,
        UseSortingTable = true
    };
    private readonly List<List<Item>> _itemsWithModsToAdd = new();
    //private int count = 0;

    public void NewGamble()
    {
        // Determine loot box type...
        OpenReward();
        newItemsRequest.ItemsWithModsToAdd = _itemsWithModsToAdd;
    }

    // Opens a singular randomly chosen reward from a lootbox
    public void OpenReward()
    {
        var containerProps = gamblerData.LootBoxData.Containers[containerName];
        int index = GetIndex();
        var rewardContainer = containerProps.RewardContainer;
        if (rewardContainer is not null)
        {
            containerName = rewardContainer;
        }
        var reward = GetReward(index);
        if (reward is not null)
        {
            if (containerProps.RewardType == "preset")
            {
                PresetCreator presetCreator = new(gamblerData, containerName);
                var preset = presetCreator.CreatePreset(reward);
                _itemsWithModsToAdd.Add(preset);
            }
            else
            {
                if (reward.Item is not null)
                {
                    _itemsWithModsToAdd.Add(new List<Item> { NewItemFormatter(reward.Item, reward.Amount) });
                }
            }
            return;
        }
        gamblerData.logger.Error($"OpenReward() reward is NULL!!");
    }

    // Returns only one rewards from a containers list of possible rewards
    public LootBoxData.Reward? GetReward(int index)
    {
        var containers = gamblerData.LootBoxData.Containers;
        var rewards = containers[containerName].Rewards[index];
        int randomRewardIndex = Random.Shared.Next(0, rewards.Count - 1);
        return rewards[randomRewardIndex];
    }

    // Returns the rewarding index in a container for a randomized roll
    // Default return -1 if Index could not be found.
    private int GetIndex()
    {
        float roll = RandomRoll();
        var itemProps = gamblerData.config.Items[containerName].odds;
        float sum = 0;

        for (int i = 0; i < itemProps.Count; i++)
        {
            var item = itemProps.ElementAt(i);
            sum += item.Value;
            if (roll <= sum)
            {
                var rewardContainer = gamblerData.LootBoxData.Containers[containerName].RewardContainer;
                if (rewardContainer is null) return i;
                // Handles if RewardContainer is not the current container
                // Returns odds index of the RewardContainer
                var rewardContainerOdds = gamblerData.config.Items[rewardContainer].odds;
                for (int j = 0; j < rewardContainerOdds.Count; j++)
                {
                    var currentOdds = rewardContainerOdds.ElementAt(j);
                    if (currentOdds.Key == item.Key) return j;
                }
            }
        }
        gamblerData.logger.Error("[Gambler Trader] GetIndex() Could not find index returned -1");
        return -1;
    }

    // Returns a random float between 0-100
    private float RandomRoll()
    {
        return MathF.Round(Random.Shared.NextSingle() * (100.0f - 0.0f) + 0.0f, 2);
    }

    private Item NewItemFormatter(MongoId tpl, int amount)
    {
        Item item = new()
        {
           Id = new MongoId(),
           Template = tpl,
           ParentId = "hideout",
           SlotId = "hideout",
           Upd = new() { StackObjectsCount = amount > 0 ? amount : 1 }
        };
        return item;
    }
}
