using SPTarkov.Server.Core.Models.Eft.Inventory;

namespace SPT_GamblerTrader_CSharp;

public class Gamble(GamblerData gamblerData)
{
    private readonly GamblerData gamblerData = gamblerData;
    public AddItemDirectRequest addItemRequest = new()
    {
        ItemWithModsToAdd = null,
        FoundInRaid = true,
        Callback = null,
        UseSortingTable = true
    };

    public void NewGamble()
    {
        OpenReward();
    }

    public void OpenReward()
    {
        
    }
}