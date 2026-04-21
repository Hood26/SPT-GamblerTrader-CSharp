namespace SPT_GamblerTrader_CSharp;

public class LootBoxInfo
{
    public required Dictionary<string, LootBoxProps> Items { get; set; }

    public void DescEvaluation(Config config, GamblerData gamblerData)
    {
        foreach (var container in Items.Values)
        {
            string current = container.desc;
            int start = -1;
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] == '{')
                {
                    start = i;
                    continue;
                }
                else if (current[i] == '}')
                {
                    string oddsWithBrackets = current.Substring(start, i - start + 1);
                    gamblerData.logger.Info(oddsWithBrackets);
                    string oddsString = oddsWithBrackets.Substring(1, oddsWithBrackets.Length - 2);
                    Items[container._name].desc = Items[container._name].desc.Replace(oddsWithBrackets, config.Items[container._name].odds[oddsString].ToString());
                }
            }   
        }
    }
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