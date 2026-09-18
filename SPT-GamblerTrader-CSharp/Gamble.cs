using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Inventory;
namespace SPT_GamblerTrader_CSharp;

public class Gamble(GamblerData gamblerData, string containerName)
{
    private readonly GamblerData _gamblerData = gamblerData;
    public readonly PresetCreator presetCreator = new(gamblerData);
    public string containerName = containerName;
    public string? previousItemId;
    public AddItemsDirectRequest newItemsRequest = new()
    {
        ItemsWithModsToAdd = [],
        FoundInRaid = true,
        UseSortingTable = true
    };
    public List<List<Item>> itemsWithModsToAdd = new();

    public void NewGamble()
    {
        List<Item> items = [];

        if (containerName.Contains("loadout"))
        {
            //OpenLoadout();
            Loadout loadout = new Loadout(_gamblerData, this);
            loadout.GenerateLoadout();
        }
        else
        {
            OpenReward();
        }
        Console.WriteLine(itemsWithModsToAdd);
        newItemsRequest.ItemsWithModsToAdd = itemsWithModsToAdd;
    }


    // Opens a singular randomly chosen reward from a lootbox
    public void OpenReward()
    {
        var containerProps = _gamblerData.LootBoxData.Containers[containerName];
        int rewardIndex = GetIndex();
        if (rewardIndex == -1) return;
        var reward = GetReward(rewardIndex);
        MongoId newItemId = new(); 

        if (reward is null)
        {
            _gamblerData.logger.Error($"[Gambler Trader] OpenReward() No valid reward found for index '{rewardIndex}'");
            return;
        }
        if (containerProps.RewardType == "Preset")
        {
            var preset = presetCreator.CreatePreset(reward);
            itemsWithModsToAdd.Add(preset);
        }
        else if (reward.Item is not null)
        {
            if (IsStackable(reward.Item))
            {
                itemsWithModsToAdd.Add(new List<Item> { NewItemFormatter(reward.Item, newItemId, reward.Amount) });
            }
            else
            {
                for (var i = 0; i < reward.Amount; i++)
                {
                    itemsWithModsToAdd.Add(new List<Item> { NewItemFormatter(reward.Item, newItemId, 1) });
                }
            }
        }
    }

    // Returns only one rewards from a containers list of possible rewards
    public LootBoxData.Reward? GetReward(int index)
    {
        var containers = _gamblerData.LootBoxData.Containers;
        var rewards = containers[containerName].Rewards[index];
        int maxAttempts = 50;

        // Reward can be from a mod that may not be installed and must be validated
        for(var i = 0; i < maxAttempts; i++)
        {
            int randomRewardIndex = Random.Shared.Next(0, rewards.Count);
            _gamblerData.logger.Info($"[Gambler Trader] GetReward() chosen index = {randomRewardIndex} from rarity index {index} from container {containerName} has {rewards.Count} possible rewards");


            if (rewards[randomRewardIndex] is null) 
            {
                _gamblerData.logger.Error($"[Gambler Trader] OpenReward() No valid reward found for index '{randomRewardIndex}'");
                return null;
            }

            else if(containers[containerName].RewardType == "Preset") return rewards[randomRewardIndex];

            else if(IsValidItem(rewards[randomRewardIndex].Item))
            {
                var reward = rewards[randomRewardIndex];
                _gamblerData.logger.Info($"[Gambler Trader] GetReward() Container Name = {containerName}");
                var amountGenerated = _gamblerData.config.Items[containerName].amount_generated;
                if (amountGenerated is not null)
                {
                    var randomIndex = Random.Shared.Next(0, amountGenerated.Length);
                    reward.Amount = amountGenerated[randomIndex];
                    _gamblerData.logger.Info($"[Gambler Trader] GetReward() New amount = {reward.Amount}");
                }
                return reward;
            }
            _gamblerData.logger.Info($"[Gambler Trader] GetReward() Invalid Item = {rewards[randomRewardIndex].Item}");
        }
        return null;
    }

    // Returns the rewarding index in a container for a randomized roll
    // Default return -1 if Index could not be found.
    public int GetIndex()
    {
        float roll = RandomRoll();
        var itemProps = _gamblerData.config.Items[containerName].odds;
        float sum = 0;
        for (int i = 0; i < itemProps.Count; i++)
        {
            var item = itemProps.ElementAt(i);
            sum += item.Value;
            if (roll <= sum)
            {
                var rewardContainer = _gamblerData.LootBoxData.Containers[containerName].RewardContainer;
                // Handles if RewardContainer is not the current container
                // Returns odds index of the RewardContainer
                if (rewardContainer is null) return i;
                containerName = rewardContainer; // Set container to rewarding container
                var rewardContainerOdds = _gamblerData.config.Items[rewardContainer].odds;
                for (int j = 0; j < rewardContainerOdds.Count; j++)
                {
                    var currentOdds = rewardContainerOdds.ElementAt(j);
                    if (currentOdds.Key == item.Key)
                    {
                        //_gamblerData.logger.Info($"[Gambler Trader] GetIndex() returning {j} for {containerName}");
                        return j;
                        
                    }
                }
            }
        }
        _gamblerData.logger.Error($"[Gambler Trader] GetIndex() Could not find index returned -1 for container {containerName}");
        return -1;
    }


    public bool IsValidItem(MongoId? id) 
    {
        if (id is null) return true; // null items like coinflips are valid
        var db = _gamblerData.db;
        var tables = db.GetTables();
        if (tables.Templates.Items.TryGetValue((MongoId)id, out _)) return true;
        return false;
    }

    public bool IsStackable(MongoId id)
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

    public Item NewItemFormatter(MongoId tpl, MongoId id, int amount = 1, string parentId = "hideout", string slotId = "hideout")
    {
        Item item;
        if(tpl == "5cc70093e4a949033c734312") // p90 50rd Magazine is Vertical
        {
            item = new()
            {
                Template = tpl,
                Id = id,
                ParentId = parentId,
                SlotId = slotId,
                Location = new Location
                {
                  X = 0,
                  Y = 0,
                  R = "Vertical"  
                },
                Upd = new() { StackObjectsCount = amount > 0 ? amount : 1 }
            };
        }
        else
        {
            item = new()
            {
                Template = tpl,
                Id = id,
                ParentId = parentId,
                SlotId = slotId,
                Upd = new() { StackObjectsCount = amount > 0 ? amount : 1 }
            };
            
        }

        //_gamblerData.logger.Info($"tpl = {tpl}\n id = {id}\n parentId = {"hideout"}\n slotId = {slotId}\n\n ");
        return item;
    }
}
