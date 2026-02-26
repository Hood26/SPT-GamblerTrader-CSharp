namespace SPT_GamblerTrader_CSharp;

public class LootBoxData
{
    public required Dictionary<string, LootBoxProps> Items { get; set; }
}

public class LootBoxProps
{
    public string _id { get; set; }
    public string _name { get; set; }
    public string prefab { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string name { get; set; }
    public string shortName { get; set; }
    public string desc { get; set; }

    public BarterProps? barter { get; set; }
}

public class BarterProps
{
    public Dictionary<string, int>? Items { get; set; }
}