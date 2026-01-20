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
    public static bool Prefix(InventoryController __instance,
                              PmcData pmcData,
                              OpenRandomLootContainerRequestData request,
                              MongoId sessionId,
                              ItemEventRouterResponse output)
    {
        ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>().Success("OpenRandomLootContainer intercepted by GamblerTrader");

        // Prevent original method from running 
        return false;
    }
}


[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class GamblerTrader(
    ISptLogger<GamblerTrader> logger,
    ConfigServer configServer
    ) : IOnLoad
{
    public Task OnLoad()
    {

        new OpenRandomLootContainerPatch().Enable();

        logger.Info("Gambler Trader Loaded Successfully!");
        return Task.CompletedTask;
    }
}
