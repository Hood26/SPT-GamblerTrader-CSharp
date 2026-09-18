using SPT_GamblerTrader_CSharp;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

public class Loadout(GamblerData gamblerData, Gamble gamble)
{
    private readonly GamblerData gamblerData = gamblerData;
    private readonly Gamble gamble = gamble;
    private int[]? storageItemFilledSlots;

    public void GenerateLoadout()
    {
        int rewardIndex = gamble.GetIndex();
        if (rewardIndex == -1) return;
        var containerProps = gamblerData.LootBoxData.Containers[gamble.containerName];
        gamble.containerName = containerProps.RewardContainer ?? gamble.containerName;
        var amountOfRewardContainers = gamblerData.LootBoxData.Containers[gamble.containerName].Rewards[rewardIndex].Count();
        string originalContainer = gamble.containerName;
        int vestIndex = 0;
        bool isArmoredRig = false;
        string armorContainer = "", vestContainer = "";
        for (int i = 0; i < amountOfRewardContainers; i++)
        {
            gamble.containerName = gamblerData.LootBoxData.Containers[originalContainer].Rewards[rewardIndex].ElementAt(i).Item;
            gamble.OpenReward();

            if (gamble.containerName.Contains("armor"))
            {
                if (IsArmoredRig(gamble.presetCreator.baseTpl))
                {
                    isArmoredRig = true;
                    armorContainer = gamble.containerName;
                    vestIndex = i;
                    i++; // Skip vest generation
                }
                else
                {
                    vestContainer = gamblerData.LootBoxData.Containers[originalContainer].Rewards[rewardIndex].ElementAt(i+1).Item;
                    vestIndex = i + 1;
                }
            }
        }

        var caliberName = gamble.presetCreator.caliber;;
        var magazineTpl = gamble.presetCreator.magazineTpl;
        var magazineCapacity = gamble.presetCreator.magazineCapacity;
        var vestTpl = gamble.itemsWithModsToAdd[vestIndex][0].Template;
        var vestId = gamble.itemsWithModsToAdd[vestIndex][0].Id;
        // Used to keep information of what slots are filled
        var rigProps = GetItemProps(vestTpl);
        storageItemFilledSlots = new int[rigProps.Properties.Grids.Count()];
        // Determine ammunition from weapon calibor
        gamble.containerName = "gambling_" + caliberName;
        int ammoIndex = LoadoutAmmoMap(rewardIndex);
        var chosenAmmo = gamble.GetReward(ammoIndex);
        var ammoTpl = chosenAmmo?.Item;

        // Store ammo in rig if magazine can only be chambered
        if (gamble.presetCreator.isBadMagazine)
        {
            MongoId firstAmmoId = new();
            int ammoSlotId = StorageItemIndex(ammoTpl, vestTpl);
            storageItemFilledSlots[ammoSlotId] = 1; // index is filled with an item
            ammoSlotId += 1; // vests count from 1 instead of 0 for some reason, so we must add 1 to result
            Item vestAmmo = gamble.NewItemFormatter(ammoTpl, firstAmmoId, magazineCapacity * 4, vestId, ammoSlotId.ToString());
            gamble.itemsWithModsToAdd[vestIndex].Add(vestAmmo);

        }
        else // Store magazines in rig
        {
            // Reroll armored rig or vest if the magazine doesn't fit
            while (StorageItemIndex(magazineTpl, vestTpl) == -1)
            {
                gamble.itemsWithModsToAdd[vestIndex].Clear();
                if (isArmoredRig)
                {
                    gamble.containerName = armorContainer;
                }
                else
                {
                    gamble.containerName = vestContainer;
                }
                var currentIndex = gamble.GetIndex();
                var reward = gamble.GetReward(currentIndex);
                if (isArmoredRig)
                {
                    var newPresetCreator = new PresetCreator(gamblerData);
                    var armoredRigPreset = newPresetCreator.CreatePreset(reward);
                    gamble.itemsWithModsToAdd[vestIndex] = armoredRigPreset;
                    
                }
                else
                {
                    MongoId newVestId = new();
                    var newVest = new List<Item> { gamble.NewItemFormatter(reward.Item, newVestId, 1) };
                    gamble.itemsWithModsToAdd[vestIndex] = newVest;
                }

                vestTpl = gamble.itemsWithModsToAdd[vestIndex][0].Template;
                vestId = gamble.itemsWithModsToAdd[vestIndex][0].Id;
                rigProps = GetItemProps(vestTpl);
                storageItemFilledSlots = new int[rigProps.Properties.Grids.Count()];
            }

            AddMagazineToRig(vestTpl, vestId, vestIndex, ammoTpl, magazineTpl, magazineCapacity);
            if (!gamble.presetCreator.isDrumMagazine)
            {
                AddMagazineToRig(vestTpl, vestId, vestIndex, ammoTpl, magazineTpl, magazineCapacity);
            }
        }
        // Add ammo to weapon
        MongoId weaponMagazineAmmoId = new();
        Item weaponMagazineAmmo = gamble.NewItemFormatter(ammoTpl, weaponMagazineAmmoId, magazineCapacity, gamble.presetCreator.magazineId, "cartridges");
        gamble.itemsWithModsToAdd[0].Add(weaponMagazineAmmo);
    }

    private void AddMagazineToRig(MongoId vestTpl, MongoId vestId, int vestIndex, MongoId ammoTpl, MongoId magazineTpl, int magazineCapacity)
    {
        int currentSlotId = StorageItemIndex(magazineTpl, vestTpl);
        string slot = SetSlotId(vestTpl, currentSlotId + 1);
        storageItemFilledSlots[currentSlotId] = 1;
        MongoId magazineId = new(), ammoId = new();
        int amountGenerated = 1;
        Item magazine = gamble.NewItemFormatter(magazineTpl, magazineId, amountGenerated, vestId, slot);
        Item magazineAmmo = gamble.NewItemFormatter(ammoTpl, ammoId, magazineCapacity, magazineId, "cartridges");
        gamble.itemsWithModsToAdd[vestIndex].Add(magazine);
        gamble.itemsWithModsToAdd[vestIndex].Add(magazineAmmo);
    }

    //  Returns the index that an item can fit inside of storage container (Rig)
    //  Returns -1 when an item cannot fit inside a storage container
    private int StorageItemIndex(MongoId itemTpl, MongoId storageItemTpl)
    {
        if (!gamble.IsValidItem(itemTpl) || !gamble.IsValidItem(storageItemTpl))
        {
            throw new InvalidOperationException("[Gambler Trader] StorageItemIndex() arguments contain an invalid item tpl!");
        }
        var db = gamblerData.db;
        var tables = db.GetTables();
        var itemProps = tables.Templates.Items[itemTpl];
        int? itemWidth, itemHeight;
        if (itemTpl == "5cc70093e4a949033c734312") // p90 50rd Magazine is Vertical
        {
            itemWidth = itemProps?.Properties?.Height;
            itemHeight = itemProps?.Properties?.Width;
        }
        else
        {
            itemWidth = itemProps?.Properties?.Width;
            itemHeight = itemProps?.Properties?.Height;
        }
        var storageItemProps = tables.Templates.Items[storageItemTpl];
        var storageItemGrids = storageItemProps?.Properties?.Grids;

        for (var i = 0; i < storageItemGrids?.Count(); i++)
        {
            var currentIndex = storageItemGrids.ElementAt(i);
            var currentIndexHeight = currentIndex?.Properties?.CellsV;
            var currentIndexWidth = currentIndex?.Properties?.CellsH;

            if (itemHeight <= currentIndexHeight && itemWidth <= currentIndexWidth && storageItemFilledSlots[i] != 1)
            {
                return i;
            }
        }
        return -1;
    }

    private int LoadoutAmmoMap(int index)
    {
        Dictionary<int, int> ammo = new()
        {
            {0, 0}, // scav Loadout -> scav ammo
            {1, 0}, // Early Game Loadout - > Early Game ammo
            {2, 1}, // Mid Game Loadout -> Mid Game ammo
            {3, 2}, // Late Game Loadout -> Late Game ammo
            {4, 1}  // Cursed Loadout -> Mid Game Ammo (probably change this in the future to add randomness)
                    //   - Might want to have this depend on the weapon generated. Late game weapon should have
                    //     Late game ammo.
        };
        bool isFound = ammo.TryGetValue(index, out int value);
        if (!isFound)
        {
            gamblerData.logger.Error($"[Gambler Trader] LoadoutAmmoMap() Invalid index value! Index = {index}");
        }
        return value;
    }

    private static string SetSlotId(MongoId vestTpl, int slotId)
    {
        if (IsWttBackportCustomSlotVest(vestTpl))
        {
            return $"GridView ({slotId})";
        }
        return slotId.ToString();
    }

    private static bool IsWttBackportCustomSlotVest(MongoId itemTpl)
    {
        MongoId[] wttItems =
        [
           "693fd13aa490096a05028cc8", // Tac-Kek JayPC plate carrier (OD Green)
           "693fd1200ec97e98040bd3f9", // Tac-Kek JayPC plate carrier (Black)
           "693fd0e9deee848f70054999", // Crye Precision JPC Plate Carrier (MultiCam)
           "68948ad72c87773b9f06d73f", // 6B45 armored rig (General Purpose)
           "689479cb47e5acd1e10be986", // Ferro Concepts FCPC V5 Plate Carrier (Black Division)
           "68947a4be4bf255d1b0ca746", // First Spear Siege-R Optimized M.A.S.S. Plate Carrier (Black Division)
           "689479eb30cc5ba7be00f5ff", // Spiritus Systems LV-119 Plate Carrier (Black Division V2)
           "689479a4a733b1602007e2eb", // Spiritus Systems LV-119 Plate Carrier (Black Division V1)
           "68948b118c57a8a52301d7ae", // 6B45 armored rig (Assault)
           "68948aebd8f2b85fb705e2b0", // 6B45 armored rig (Medic)
        ];

        if (wttItems.Contains(itemTpl))
        {
            return true;
        }
        return false;
    }
    public bool IsArmoredRig(MongoId id)
    {
        var db = gamblerData.db;
        var tables = db.GetTables();
        bool isFound = tables.Templates.Items.TryGetValue(id, out var item);
        if (isFound)
        {
            if (item.Properties?.Grids.Count() != 0)
            {
                return true;
            }
        }
        return false;
    }
    private TemplateItem? GetItemProps(MongoId id)
    {
        if (!gamble.IsValidItem(id)) return null;
        var db = gamblerData.db;
        var tables = db.GetTables();
        var item = tables.Templates.Items[id];
        return item;
    }
}