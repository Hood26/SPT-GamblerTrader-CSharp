using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Utils;

namespace SPT_GamblerTrader_CSharp;

public class Gamble(GamblerData gamblerData, string containerName)
{
    private readonly GamblerData gamblerData = gamblerData;
    private readonly string containerName = containerName;
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
        float roll = RandomRoll();
        int index = GetIndex(roll);
        var reward = gamblerData.LootBoxData.GetReward(gamblerData, containerName, index);
        if (reward is not null)
        {
            _itemsWithModsToAdd.Add(new List<Item> { NewItemFormater(reward.Item, reward.Amount) });
        }
    }

    // Returns a randon float between 0-100
    private float RandomRoll()
    {
        return MathF.Round(Random.Shared.NextSingle() * (100.0f - 0.0f) + 0.0f, 2);
    }

    // Returns a specific index given a randomized roll in a certain container
    private int GetIndex(float roll)
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

    private Item NewItemFormater(MongoId tpl, int amount)
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