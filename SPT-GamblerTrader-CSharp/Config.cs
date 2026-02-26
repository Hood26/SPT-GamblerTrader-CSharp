namespace SPT_GamblerTrader_CSharp;

public class Config
{
    public required int trader_update_min_time { get; set; }
    public required int trader_update_max_time { get; set; }
    public required Dictionary<string, ConfigProps> Items { get; set; }
}

public class ConfigProps
{
    public bool enable { get; set; }
    public bool manual_pricing { get; set; }
    public bool sold_by_trader { get; set; }
    public bool flea_banned { get; set; }
    public int trader_price_roubles { get; set; }
    public int flea_price_roubles { get; set; }
    public int handbook_price_roubles { get; set; }
    public int trader_stock { get; set; }
    public int loyalty_level { get; set; }
    public float profit_percentage { get; set; }
    public required LootboxOdds odds { get; set; }
}

public class LootboxOdds
{
    public required Dictionary<string, float> odds { get; set;}
}