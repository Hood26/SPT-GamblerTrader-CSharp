using SPTarkov.Server.Core.Models.Eft.Inventory;

namespace SPT_GamblerTrader_CSharp;

public class Gamble(GamblerData gamblerData, String containerName)
{
    private readonly GamblerData gamblerData = gamblerData;
    private readonly String containerName = containerName;
    public AddItemDirectRequest addItemRequest = new()
    {
        ItemWithModsToAdd = null,
        FoundInRaid = true,
        Callback = null,
        UseSortingTable = true
    };

    public void NewGamble()
    {
        // Determine loot box type
        OpenReward();
    }

    public void OpenReward()
    {
        float roll = RandomRoll();
        var itemProps = gamblerData.config.Items[containerName].odds;

        for (int i = 0; i < itemProps.Count; i++)
        {
            var item = itemProps.ElementAt(i);
            //if(roll <= item.Value)
        }


        foreach (var item in gamblerData.config.Items[containerName].odds)
        {
            if(roll <= item.Value) continue;

        }




    }

    // Returns a randon float between 0-100
    public float RandomRoll()
    {
        return MathF.Round(Random.Shared.NextSingle() * (100.0f - 0.0f) + 0.0f, 2);
    }
}