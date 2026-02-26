using SPT_GamblerTrader_CSharp;
using SPTarkov.Server.Core.Models.Enums;

namespace HoodsEnergyDrinks_CSharp;

class GamblerTraderHelper(GamblerData gamblerData)
{
    private readonly GamblerData gamblerData = gamblerData;

    public void addSingleItemToTrader(string traderId)
    {
        foreach (var (name, props) in gamblerData.lootBoxData.Items)
        {
            if (gamblerData.config.Items[name].sold_by_trader)
            {
                gamblerData.assortCreator.CreateSingleAssortItem(props._id)
                    .AddUnlimitedStackCount()
                    .AddBuyRestriction(gamblerData.config.Items[name].trader_stock)
                    .AddMoneyCost(Money.ROUBLES, gamblerData.config.Items[name].trader_price_roubles)
                    .AddLoyaltyLevel(gamblerData.config.Items[name].loyalty_level)
                    .Export(traderId);
            }
        }
    }


}