using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

namespace SPT_GamblerTrader_CSharp;

public class Gamble(GamblerData gamblerData, string containerName)
{
    private readonly GamblerData _gamblerData = gamblerData;
    private string _containerName = containerName;
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
        var containerProps = _gamblerData.LootBoxData.Containers[_containerName];
        int rewardIndex = GetIndex();
        if (rewardIndex == -1) return;

        _containerName = containerProps.RewardContainer ?? _containerName;
        var reward = GetReward(rewardIndex);

        if (reward is null)
        {
            _gamblerData.logger.Error($"[Gambler] No valid reward found for index '{rewardIndex}'");
            return;
        }
        if (containerProps.RewardType == "Preset")
        {
            PresetCreator presetCreator = new(_gamblerData, _containerName);
            var preset = presetCreator.CreatePreset(reward);
            _itemsWithModsToAdd.Add(preset);
        }
        else if (reward.Item is not null)
        {
            if (IsStackable(reward.Item))
            {
                _itemsWithModsToAdd.Add(new List<Item> { NewItemFormatter(reward.Item, reward.Amount) });
            }
            else
            {
                for (var i = 0; i < reward.Amount; i++)
                {
                    _itemsWithModsToAdd.Add(new List<Item> { NewItemFormatter(reward.Item, 1) });
                }
            }
        }
    }

    // Returns only one rewards from a containers list of possible rewards
    public LootBoxData.Reward? GetReward(int index)
    {
        var containers = _gamblerData.LootBoxData.Containers;
        var rewards = containers[_containerName].Rewards[index];
        int randomRewardIndex = Random.Shared.Next(0, rewards.Count);
        _gamblerData.logger.Info($"index = {randomRewardIndex}");
        return rewards[randomRewardIndex];
    }

    // Returns the rewarding index in a container for a randomized roll
    // Default return -1 if Index could not be found.
    private int GetIndex()
    {
        float roll = RandomRoll();
        var itemProps = _gamblerData.config.Items[_containerName].odds;
        float sum = 0;

        for (int i = 0; i < itemProps.Count; i++)
        {
            var item = itemProps.ElementAt(i);
            sum += item.Value;
            if (roll <= sum)
            {
                var rewardContainer = _gamblerData.LootBoxData.Containers[_containerName].RewardContainer;
                if (rewardContainer is null) return i;
                // Handles if RewardContainer is not the current container
                // Returns odds index of the RewardContainer
                var rewardContainerOdds = _gamblerData.config.Items[rewardContainer].odds;
                for (int j = 0; j < rewardContainerOdds.Count; j++)
                {
                    var currentOdds = rewardContainerOdds.ElementAt(j);
                    if (currentOdds.Key == item.Key) return j;
                }
            }
        }
        _gamblerData.logger.Error($"[Gambler Trader] GetIndex() Could not find index returned -1 for container {_containerName}");
        return -1;
    }

    private bool IsStackable(MongoId id)
    {
        var db = _gamblerData.db;
        var tables = db.GetTables();
        var item = tables.Templates.Items[id];
        if (item.Properties?.StackMaxSize > 1) return true;
        return false;
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
