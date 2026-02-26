using SPTarkov.Server.Core.Models.Utils;

namespace SPT_GamblerTrader_CSharp;

public class GamblerData(FluentTraderAssortCreator assortCreator, Config config, LootBoxData lootBoxData, ISptLogger<GamblerTrader> logger)
{
    public readonly FluentTraderAssortCreator assortCreator = assortCreator;
    public readonly Config config = config;
    public readonly LootBoxData lootBoxData = lootBoxData;
    public readonly ISptLogger<GamblerTrader> logger = logger;
}