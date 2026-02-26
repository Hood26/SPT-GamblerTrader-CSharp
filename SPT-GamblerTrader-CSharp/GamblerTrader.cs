using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils.Cloners;
using _13._1AddTraderWithDynamicAssorts;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using Path = System.IO.Path;
using HoodsEnergyDrinks_CSharp;
using SPTarkov.Server.Core.Services.Mod;

namespace SPT_GamblerTrader_CSharp;


[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class OpenRandomLootContainerPatch : AbstractPatch
{
    protected override MethodBase? GetTargetMethod()
    {
        var inventoryController = typeof(InventoryController);
        return inventoryController.GetMethod(
            "OpenRandomLootContainer",
            [
                typeof(PmcData),
                typeof(OpenRandomLootContainerRequestData),
                typeof(MongoId),
                typeof(ItemEventRouterResponse)
            ]
        );
    }

    [PatchPrefix]
    public static bool Prefix(
        InventoryController __instance,
        PmcData pmcData,
        OpenRandomLootContainerRequestData request,
        MongoId sessionId,
        ItemEventRouterResponse output
    )
    {
        ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>().Success("OpenRandomLootContainer intercepted by GamblerTrader");
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
        var openedItem = pmcData.Inventory.Items.Find(x => x.Id == request.Item);
        var containerDetails = itemHelper.GetItem(openedItem.Template);
        var isGamblingContainer = containerDetails.Value.Properties.Name;

        if (isGamblingContainer.Contains("gambling_"))
        {
            // Prevent original OpenRandomLootContainer from running 
            ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>().Success("Is a Gambler Item...");
            return false;
        }
        // run original OpenRandomLootContainer
        return true;
    }
}


[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class GamblerTrader(
    ISptLogger<GamblerTrader> logger,
    ModHelper modHelper,
    DatabaseService databaseService,
    ImageRouter imageRouter,
    ConfigServer configServer,
    TimeUtil timeUtil,
    ICloner cloner,
    FluentTraderAssortCreator assortCreator,
    AddCustomTraderHelper addCustomTraderHelper,
    CustomItemService customItemService
) : IOnLoad
{

    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();

    public Task OnLoad()
    {
        new OpenRandomLootContainerPatch().Enable();
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var configPath = Path.GetFullPath(Path.Combine(pathToMod, "config"));
        var lootBoxPath = Path.GetFullPath(Path.Combine(pathToMod, "lootbox-data"));
        var config = modHelper.GetJsonDataFromFile<Config>(configPath, "config.jsonc");
        var lootBoxData = modHelper.GetJsonDataFromFile<LootBoxData>(lootBoxPath, "lootboxData.json");
        var traderImagePath = Path.Combine(pathToMod, "res/gambler.jpg");
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
        imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), traderImagePath);
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase, config.trader_update_min_time, config.trader_update_max_time);
        _ragfairConfig.Traders.TryAdd(traderBase.Id, true);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase);
        addCustomTraderHelper.AddTraderToLocales(traderBase, "Gambler", "Welcome warrior! I have many mystery boxes for sale if you wish to try your luck.");
        var gamblerData = new GamblerData(assortCreator, config, lootBoxData, logger);
        var itemCreator = new ItemCreator(gamblerData);
        itemCreator.BuildItems(customItemService);
        var gamblerTraderHelper = new GamblerTraderHelper(gamblerData);
        gamblerTraderHelper.addSingleItemToTrader("67b7b52a4767af842e0521d0");
        logger.Info("Gambler Trader Loaded Successfully!");
        return Task.CompletedTask;
    }
}
