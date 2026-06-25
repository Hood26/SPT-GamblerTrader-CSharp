using SPT_GamblerTrader_CSharp;
using SPTarkov.Server.Core.Models.Enums;

namespace HoodsEnergyDrinks_CSharp;

class GamblerTraderHelper(GamblerData gamblerData)
{
    private readonly GamblerData gamblerData = gamblerData;

    public void AddSingleItemToTrader(string traderId)
    {
        foreach (var (name, props) in gamblerData.lootBoxInfo.Items)
        {
            gamblerData.logger.Info($"Adding {name} to trader with id {traderId}");
            var itemProps = gamblerData.config.Items[name];
            if (itemProps.sold_by_trader)
            {
                var newItem = gamblerData.assortCreator.CreateSingleAssortItem(props._id)
                    .AddUnlimitedStackCount()
                    .AddBuyRestriction(itemProps.trader_stock)
                    .AddLoyaltyLevel(itemProps.loyalty_level);
                if (itemProps.barter is not null)
                {
                    foreach (var (item, amount) in itemProps.barter)
                    {
                        newItem.AddBarterCost(item, amount);
                    }
                }
                else
                {
                    newItem.AddMoneyCost(Money.ROUBLES, itemProps.trader_price_roubles);
                }
                newItem.Export(traderId);
            }
        }


        /*
        List<string> lootboxes = [
                                    "665888282c4a1b73af576b77", // Unlocked weapon crate (Common)
                                    "665829424de4820934746ce6", // Unlocked weapon crate (Epic)
                                    "665732e7ac60f009f270d1ef", // Unlocked weapon crate Rare
                                    "64898e9db18e646e992aba47", // Sealed weapon case
                                    "665829a6efd94e2d665b14a8", // Unlocked valuables crate (Epic)
                                    "66573310a1657263d816a139", // Unlocked valuables crate (Rare)
                                    "665886abdaadd1069736c539", // Unlocked valuables crate (Common)
                                    "665730fa4de4820934746c48", // Unlocked equipment crate (Rare)
                                 ];

        foreach (var lootbox in lootboxes)
        {
            gamblerData.assortCreator.CreateSingleAssortItem(lootbox)
                .AddUnlimitedStackCount()
                .AddBuyRestriction(999)
                .AddMoneyCost(Money.ROUBLES, 10)
                .AddLoyaltyLevel(1)
                .Export(traderId);
        }
        */

    }


}