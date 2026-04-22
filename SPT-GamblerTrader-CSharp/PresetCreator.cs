using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
namespace SPT_GamblerTrader_CSharp;

public class PresetCreator(GamblerData gamblerData, string containerName)
{
    private readonly GamblerData gamblerData = gamblerData;
    private readonly string containerName = containerName;

    public List<Item> CreatePreset(LootBoxData.Reward reward)
    {
        // I don't think we need this but idk
        string rewardType = gamblerData.LootBoxData.Containers[containerName].RewardType;
        return GenerateItem(reward);
    }


    private List<Item> GenerateItem(LootBoxData.Reward reward)
    {
        List<Item> item = [];
        Dictionary<string, string> parentIdMap = []; // Map new id to original to stop collisions
        var build = reward.Items;
        MongoId randomId = new(); // New Item baseId;
        MongoId baseId = new(); // Id of the base attachment that all other attachments apply to

        for (int i = 0; i < build.Count; i++)
        {
            var currentItem = build[i];
            if (i == 0)
            {
                baseId = currentItem._id;
                parentIdMap.Add(baseId, randomId);

                item.Add(new Item
                {
                    Id = randomId,
                    Template = currentItem._tpl
                });

            }
            else
            {
                MongoId newId = new();
                // Every _id is mapped to a newly generated _id, so every item is unique and doesn"t _id collide
                if (!parentIdMap.TryGetValue(currentItem._id, out _))
                {
                    parentIdMap.Add(currentItem._id, newId);
                }
                // Attachments with parents that are not the base Item
                if (currentItem.parentId != baseId)
                {
                    item.Add(new Item
                    {
                        Id = newId,
                        Template = currentItem._tpl,
                        ParentId = parentIdMap[currentItem.parentId],
                        SlotId = currentItem.slotId
                    });
                }
                else
                {
                    item.Add(new Item
                    {
                        Id = newId,
                        Template = currentItem._tpl,
                        ParentId = randomId,
                        SlotId = currentItem.slotId
                    });
                }
            }
        }
        return item;
    }
}