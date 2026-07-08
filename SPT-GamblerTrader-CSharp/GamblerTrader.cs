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
using AddTraderWithDynamicAssorts;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using Path = System.IO.Path;
using HoodsEnergyDrinks_CSharp;
using SPTarkov.Server.Core.Services.Mod;

namespace SPT_GamblerTrader_CSharp;


[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 25)]
public class GamblerTrader(
    ISptLogger<GamblerTrader> logger,
    ModHelper modHelper,
    DatabaseService databaseService,
    DatabaseServer db,
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
    private static GamblerData? gamblerData;

    public Task OnLoad()
    {
        new OpenRandomLootContainerPatch().Enable();
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var configPath = Path.GetFullPath(Path.Combine(pathToMod, "config"));
        var lootBoxPath = Path.GetFullPath(Path.Combine(pathToMod, "lootbox-data"));
        var config = modHelper.GetJsonDataFromFile<Config>(configPath, "config.jsonc");
        var lootBoxInfo = modHelper.GetJsonDataFromFile<LootBoxInfo>(lootBoxPath, "lootBoxInfo.json");
        var lootBoxData = modHelper.GetJsonDataFromFile<LootBoxData>(lootBoxPath, "lootBoxData.jsonc");
        var traderImagePath = Path.Combine(pathToMod, "res/gambler.jpg");
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
        imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), traderImagePath);
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase, config.trader_update_min_time, config.trader_update_max_time);
        _ragfairConfig.Traders.TryAdd(traderBase.Id, true);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase);
        addCustomTraderHelper.AddTraderToLocales(traderBase, "Gambler", "Welcome warrior! I have many loot boxes for sale if you wish to try your luck.");
        GamblerTrader.gamblerData = new GamblerData(db, assortCreator, config, lootBoxInfo, lootBoxData, logger);
        lootBoxInfo.DescEvaluation(config, gamblerData);
        var itemCreator = new ItemCreator(gamblerData);
        itemCreator.BuildItems(customItemService);
        var gamblerTraderHelper = new GamblerTraderHelper(gamblerData);
        gamblerTraderHelper.AddSingleItemToTrader("67b7b52a4767af842e0521d0");
        logger.Info("Gambler Trader Loaded Successfully!");


        // Thicc Case changes for preset generation...
        // Must be removed in production...
        var tables = db.GetTables();
        var thiccCase = tables.Templates.Items["5c0a840b86f7742ffa4f2482"];
        var grid = thiccCase.Properties.Grids?.FirstOrDefault();
        if (grid != null)
        {
            var filterEntry = grid.Properties.Filters?.FirstOrDefault();
            if (filterEntry?.Filter != null && !filterEntry.Filter.Contains("5c0a840b86f7742ffa4f2482"))
                filterEntry.Filter.Add("5c0a840b86f7742ffa4f2482");
        }

        return Task.CompletedTask;
    }

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
            var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>();
            var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
            var inventoryHelper = ServiceLocator.ServiceProvider.GetService<InventoryHelper>();
            var openedItem = pmcData.Inventory.Items.Find(x => x.Id == request.Item);
            var containerDetails = itemHelper.GetItem(openedItem.Template);
            var isGamblingContainer = containerDetails.Value.Properties.Name;

            logger.Success("[Gambler Trader] OpenRandomLootContainer intercepted by Gambler Trader");
            if (isGamblingContainer.Contains("gambling_"))
            {
                AddItemsDirectRequest newItemsRequest = new()
                {
                    ItemsWithModsToAdd = [],
                    FoundInRaid = true,
                    UseSortingTable = true
                };

                // Prevent original OpenRandomLootContainer from running 
                logger.Success("[Gambler Trader] This container is a Gambler Item...");
                if (gamblerData is null) return false;
                Gamble gamble = new Gamble(GamblerTrader.gamblerData, isGamblingContainer);
                gamble.NewGamble();

                if (gamble.newItemsRequest?.ItemsWithModsToAdd?.Count() != 0)
                {
                    newItemsRequest.ItemsWithModsToAdd = gamble.newItemsRequest?.ItemsWithModsToAdd;
                    newItemsRequest.FoundInRaid = gamble.newItemsRequest?.FoundInRaid;

                    if (inventoryHelper.CanPlaceItemsInInventory(sessionId, newItemsRequest.ItemsWithModsToAdd))
                    {
                        inventoryHelper.RemoveItem(pmcData, request.Item, sessionId, output);
                        inventoryHelper.AddItemsToStash(sessionId, newItemsRequest, pmcData, output);
                    }
                    else
                    {
                        logger.Error("[Gambler Trader] Cannot Open Container! Inventory Is Full!");
                    }
                }
                else
                {
                inventoryHelper.RemoveItem(pmcData, request.Item, sessionId, output);
                }
                return false;
            }
            // run original OpenRandomLootContainer
            return true;
        }
    }
}
