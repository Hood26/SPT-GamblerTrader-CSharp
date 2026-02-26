using SPT_GamblerTrader_CSharp;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Mod;
namespace HoodsEnergyDrinks_CSharp;
class ItemCreator(GamblerData gamblerData)
{
    private readonly GamblerData gamblerData = gamblerData;

    public void BuildItems(CustomItemService customItemService)
    {
        foreach (var (name, props) in gamblerData.lootBoxData.Items)
        {
            var itemConfig = gamblerData.config.Items[name];
            var newItem = new NewItemFromCloneDetails
            {
                ItemTplToClone = "66582972ac60f009f270d2aa",
                OverrideProperties = new TemplateItemProperties
                {
                    Prefab = new Prefab
                    {
                        Path = props.prefab
                    },
                    Name = props._name,
                    Width = props.width,
                    Height = props.height,
                    ExaminedByDefault = true
                },
                NewId = props._id,
                ParentId = "62f109593b54472778797866",
                FleaPriceRoubles = itemConfig.flea_price_roubles,
                HandbookPriceRoubles = itemConfig.handbook_price_roubles,
                HandbookParentId = "5b5f6fa186f77409407a7eb7",
                Locales = new Dictionary<string, LocaleDetails>
                {
                    {
                        "en",
                        new LocaleDetails
                        {
                            Name = props.name,
                            ShortName = props.shortName,
                            Description = props.desc
                        }
                    }
                }
            };
            customItemService.CreateItemFromClone(newItem);
        }
    }
}