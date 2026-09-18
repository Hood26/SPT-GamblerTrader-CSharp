using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
namespace SPT_GamblerTrader_CSharp;

public class PresetCreator(GamblerData gamblerData)
{
    private readonly GamblerData gamblerData = gamblerData;
    public bool isDrumMagazine, isBadMagazine;
    public int magazineCapacity;
    public string caliber, magazineTpl, magazineId = "";
    public MongoId baseTpl, baseId;


    public List<Item> CreatePreset(LootBoxData.Reward reward)
    {
        return GenerateItem(reward);
    }


    private List<Item> GenerateItem(LootBoxData.Reward reward)
    {
        List<Item> item = [];
        Dictionary<string, string> parentIdMap = []; // Map new id to original to stop collisions
        var build = reward.Items;
        MongoId randomId = new(); // New Item baseId;
        baseId = new(); // Id of the base attachment that all other attachments apply to
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();

        for (int i = 0; i < build.Count; i++)
        {
            var currentItem = build[i];
            if (i == 0)
            {
                baseTpl = currentItem._tpl;
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
                        SlotId = currentItem.slotId,
                        Upd = currentItem.upd != null
                            ? new Upd { Togglable = new UpdTogglable { On = true }}
                            : null
                    });
                }
                else
                {
                    if(currentItem.slotId == "mod_magazine")
                    {
                        magazineTpl = currentItem._tpl;
                        magazineId = newId;
                        isDrumMagazine = IsDrumMagazine(magazineTpl);
                        isBadMagazine = IsBadMagazine(magazineTpl);
                        var magazineInfo =  itemHelper?.GetItem(magazineTpl);
                        magazineCapacity = (int)magazineInfo?.Value?.Properties?.Cartridges?.ElementAt(0).MaxCount;
                    }

                    item.Add(new Item
                    {
                        Id = newId,
                        Template = currentItem._tpl,
                        ParentId = randomId,
                        SlotId = currentItem.slotId,
                        Upd = currentItem.upd != null
                            ? new Upd { Togglable = new UpdTogglable { On = true }}
                            : null
                    });
                }
            }
        }
        var itemInfo = itemHelper?.GetItem(item[0].Template);
        var bsgCaliber = itemInfo?.Value?.Properties?.AmmoCaliber;
        if (bsgCaliber is not null)
        {
        caliber = CaliberConverter(bsgCaliber);
        }
        return item;
    }

    private bool IsDrumMagazine(MongoId id)
    {
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
        var itemInfo = itemHelper?.GetItem(id);
        if(itemInfo?.Value?.Properties?.Height == 2 && itemInfo?.Value?.Properties?.Width == 2) return true;
        return false;
    }

    private bool IsBadMagazine(MongoId id)
    {
        MongoId[] badMagazines =
        [
            "633ec6ee025b096d320a3b15", // RSh-12 12.7x55 5-round cylinder
            "5ae0973a5acfc4001562206c", // Mosin Rifle 7.62x54R 5-round magazine
            "587df3a12459772c28142567", // SKS 7.62x39 10-round internal box magazine
            "67c5424826265106dd0697a4", // MXLR .308 ME 5-round magazine
            "5a78830bc5856700137e4c90", // M870 12ga 7-shell magazine
            "5a7882dcc5856700177af662", // M870 12ga 4-shell magazine cap
            "5a78832ec5856700155a6ca3", // M870 M870 12ga 10-shell magazine
        ];
        if(badMagazines.Contains(id)) return true;
        return false;
    }

    private string? CaliberConverter(string currentCaliber)
    {
        gamblerData.logger.Info($"[Gambler Trader] CaliberConverter() Caliber to input = {currentCaliber}");
        
        // There are more calibers that need to be inserted...
        Dictionary<string, string> calibers = new()
        {
            {"Caliber762x25TT", "7.62x25"},
            {"Caliber9x18PM", "9x18"},
            {"Caliber9x18PMM", "9x18"},
            {"Caliber9x19PARA", "9x19"},
            {"Caliber9x21", "9x21"},
            {"Caliber9x33R", ".357"},
            {"Caliber1143x23ACP", ".45"},
            {"Caliber46x30", "4.6x30"},
            {"Caliber57x28", "5.7x28"},
            {"Caliber545x39", "5.45x39"},
            {"Caliber556x45NATO", "5.56x45"},
            {"Caliber762x35", ".300"},
            {"Caliber784x49", ".308"},
            {"Caliber68x51", "6.8x51"},
            {"Caliber762x39", "7.62x39"},
            {"Caliber762x51", "7.62x51"},
            {"Caliber762x54R", "7.62x54"},
            {"Caliber86x70", ".338"},
            {"Caliber9x39", "9x39"},
            {"Caliber366TKM", ".366"},
            {"Caliber127x55", "12.7x55"},
            {"Caliber12g", "12/70"},
            {"Caliber20g", "20/70"},
            {"Caliber23x75", "23x75"},
            {"Caliber127x33", ".50_Action"},
        };

        calibers.TryGetValue(currentCaliber, out string? value);
        if(value is null)
        {
            gamblerData.logger.Error($"[Gambler Trader] PresetCreator.CaliberConverter() failed to find ammo in conversion!");
        }
        return value;

        //return Calibers.TryGetValue(caliber, out string? value) ? value : null;
    }






}