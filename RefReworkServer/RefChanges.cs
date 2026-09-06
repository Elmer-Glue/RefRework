using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using System.Reflection;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Eft.Game;
using SPTarkov.Reflection.Patching;

namespace RefReworkServer;

public static class RefChanges
{
    public static readonly string REF_TRADER_ID = "6617beeaa9cfa777ca915b7c";
    internal static readonly MongoId GP_COIN_ID = new MongoId("5d235b4d86f7742e017bc88a");
    private const int LEGA_MEDAL_BARTER_LOYALTY_LEVEL = 1;

    public static bool Apply(Context context)
    {
        ChangeRefPurchasingOptions(context);

        AddLegaMedalBarterToRefAssort(context);

        if (context.config.blockDogtagSalesToOtherTraders)
        {
            BlockDogtagSalesToOtherTraders(context);
        }

        BlockLegaMedalSalesToOtherTraders(context);

        new GPCurrencyCoursePatch().Enable();

        return true;
    }

    private static void BlockDogtagSalesToOtherTraders(Context context)
    {
        HashSet<MongoId> dogtags = Utils.GetDogtagsList(context);

        foreach (var kvp in context.traders)
        {
            if (kvp.Key == REF_TRADER_ID)
            {
                continue;
            }

            TraderBase? traderBase = kvp.Value?.Base;
            if (traderBase == null)
            {
                continue;
            }

            traderBase.ItemsBuyProhibited ??= new ItemBuyData
            {
                Category = new HashSet<MongoId>(),
                IdList = new HashSet<MongoId>()
            };

            traderBase.ItemsBuyProhibited.IdList ??= new HashSet<MongoId>();
            traderBase.ItemsBuyProhibited.IdList.UnionWith(dogtags);
        }
    }

    private static void BlockLegaMedalSalesToOtherTraders(Context context)
    {
        foreach (var kvp in context.traders)
        {
            if (kvp.Key == REF_TRADER_ID)
            {
                continue;
            }

            TraderBase? traderBase = kvp.Value?.Base;
            if (traderBase == null)
            {
                continue;
            }

            traderBase.ItemsBuyProhibited ??= new ItemBuyData
            {
                Category = new HashSet<MongoId>(),
                IdList = new HashSet<MongoId>()
            };

            traderBase.ItemsBuyProhibited.IdList ??= new HashSet<MongoId>();
            traderBase.ItemsBuyProhibited.IdList.Add(ItemTpl.BARTER_LEGA_MEDAL);
        }
    }

    private static void ChangeRefPurchasingOptions(Context context)
    {
        Trader refTrader = context.traders[REF_TRADER_ID];

        refTrader.Base!.Currency = CurrencyType.GP;

        refTrader.Base!.ItemsBuy!.Category = new HashSet<MongoId>();
        refTrader.Base.ItemsBuy.IdList = Utils.GetDogtagsList(context);
    }

    private static void AddLegaMedalBarterToRefAssort(Context context)
    {
        Trader refTrader = context.traders[REF_TRADER_ID];
        RefGPConfig config = context.config;
        TraderAssort assort = refTrader.Assort!;

        MongoId assortItemId = new MongoId();

        assort.Items ??= new List<Item>();
        assort.Items.Add(new Item
        {
            Id = assortItemId,
            Template = ItemTpl.BARTER_LEGA_MEDAL,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 999999,
                BuyRestrictionMax = 1,
                BuyRestrictionCurrent = 0
            }
        });

        assort.BarterScheme ??= new Dictionary<MongoId, List<List<BarterScheme>>>();
        assort.BarterScheme[assortItemId] = new List<List<BarterScheme>>
        {
            new List<BarterScheme>
            {
                new BarterScheme
                {
                    Count = config.legaMedalBarterGpCost,
                    Template = GP_COIN_ID
                }
            }
        };

        assort.LoyalLevelItems ??= new Dictionary<MongoId, int>();
        assort.LoyalLevelItems[assortItemId] = LEGA_MEDAL_BARTER_LOYALTY_LEVEL;
    }
}

/// <summary>
/// Ref trades in GP coins, but the base game doesn't define a rouble exchange course for GP coin.
/// Without one, the trading UI can fail to price GP-currency items correctly and hide them
/// from the sell screen. This gives GP coin a fixed, working course.
/// </summary>
public class GPCurrencyCoursePatch : AbstractPatch
{
    private const int GP_COIN_ROUBLE_VALUE = 2500;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(TraderController).GetMethod(nameof(TraderController.GetItemPrices))!;
    }

    [PatchPostfix]
    static void Postfix(ref GetItemPricesResponse __result)
    {
        if (__result?.CurrencyCourses == null)
        {
            return;
        }

        __result.CurrencyCourses[RefChanges.GP_COIN_ID] = GP_COIN_ROUBLE_VALUE;
    }
}
