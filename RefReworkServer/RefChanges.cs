using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using System.Reflection;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Eft.Game;
using SPTarkov.Reflection.Patching;

namespace RefReworkServer;

public static class RefChanges
{
    public static readonly string REF_TRADER_ID = "6617beeaa9cfa777ca915b7c";

    public static bool Apply(Context context)
    {
        ChangeRefPurchasingOptions(context);

        if (context.config.blockDogtagSalesToOtherTraders)
        {
            BlockDogtagSalesToOtherTraders(context);
        }

        if (context.config.blockLegaMedalSalesToOtherTraders)
        {
            BlockLegaMedalSalesToOtherTraders(context);
        }

        GPPriceConversionPatch.gpCoinRoubleValue = context.config.gpCoinRoubleValue;
        GPPriceConversionPatch.legaMedalGpCoinRoubleValue = context.config.legaMedalGpCoinRoubleValue;
        new GPPriceConversionPatch().Enable();

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
        RefGPConfig config = context.config;

        if (config.refBuysInGPCoins)
        {
            refTrader.Base!.Currency = CurrencyType.GP;
        }

        if (config.refOnlyBuysDogtags)
        {
            refTrader.Base!.ItemsBuy!.Category = new HashSet<MongoId>();
            refTrader.Base.ItemsBuy.IdList = Utils.GetDogtagsList(context);
        }

        if (config.refAlsoBuysLegaMedals)
        {
            refTrader.Base!.ItemsBuy!.IdList.Add(ItemTpl.BARTER_LEGA_MEDAL);
        }
    }
}

public class GPPriceConversionPatch : AbstractPatch
{
    public static int gpCoinRoubleValue = 2500;
    public static int legaMedalGpCoinRoubleValue = 9000;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(TraderController).GetMethod(nameof(TraderController.GetItemPrices))!;
    }

    [PatchPostfix]
    static void Postfix(MongoId traderId, ref GetItemPricesResponse __result)
    {
        if (__result?.CurrencyCourses == null)
        {
            return;
        }

        __result.CurrencyCourses["5d235b4d86f7742e017bc88a"] = gpCoinRoubleValue;

        if (traderId != RefChanges.REF_TRADER_ID)
        {
            return;
        }

        if (__result.Prices != null &&
            __result.Prices.TryGetValue(ItemTpl.BARTER_LEGA_MEDAL, out var legaMedalRoublePrice))
        {
            var prices = new Dictionary<MongoId, double>(__result.Prices)
            {
                [ItemTpl.BARTER_LEGA_MEDAL] = legaMedalRoublePrice * gpCoinRoubleValue / legaMedalGpCoinRoubleValue
            };
            __result.Prices = prices;
        }
    }
}
