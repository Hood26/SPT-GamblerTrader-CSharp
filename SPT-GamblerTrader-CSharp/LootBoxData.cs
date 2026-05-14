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
        public string? Item { get; set; } = null;
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
        public AttachmentsUpd? upd { get; set; }
    }

    public class AttachmentsUpd
    {
        public Togglable? Togglable { get; set; }
    }

    public class Togglable
    {
        public bool On { get; set; }
    }

    // Returns all possible rewards from a container
    public List<Reward> GetRewards(string containerName, int index)
    {
        return Containers[containerName].Rewards[index];
    }
}