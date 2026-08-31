using EFT.InventoryLogic;
using EFT.Trading;
using HarmonyLib;

namespace RefReworkClient;

[HarmonyPatch(typeof(Trader), nameof(Trader.GetUserItemPrice))]
public static class DogtagValuePatch
{
    private const string RefTraderId = "6617beeaa9cfa777ca915b7c";

    [HarmonyPostfix]
    private static void Postfix(Trader __instance, Item __0, ref Trader.ItemPrice? __result)
    {
        if (__instance.Id != RefTraderId || !__result.HasValue)
        {
            return;
        }

        if (!__0.TryGetItemComponent<DogtagComponent>(out var dogtag))
        {
            return;
        }

        var amount = Plugin.ValueTable.GetAmountForLevel(dogtag.Level);
        var price = __result.Value;
        __result = new Trader.ItemPrice(price.CurrencyId, amount);
    }
}
