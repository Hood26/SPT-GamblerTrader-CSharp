using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace SPT_GamblerTrader_CSharp;

public class GamblerData(
    DatabaseServer db, 
    FluentTraderAssortCreator assortCreator, 
    Config config, 
    LootBoxInfo lootBoxInfo, 
    LootBoxData lootBoxData, 
    ISptLogger<GamblerTrader> logger
    )
{
    public readonly DatabaseServer db = db;
    public readonly FluentTraderAssortCreator assortCreator = assortCreator;
    public readonly Config config = config;
    public readonly LootBoxInfo lootBoxInfo = lootBoxInfo;
    public readonly LootBoxData LootBoxData = lootBoxData;
    public readonly ISptLogger<GamblerTrader> logger = logger;
}