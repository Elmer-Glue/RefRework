using System.Linq;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using EFT.Trading;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RefReworkClient;

[HarmonyPatch(typeof(TradingItemView), nameof(TradingItemView.SetPrice))]
public static class TradingItemViewSetPricePatch
{
    private static readonly MongoID GpId = new MongoID("5d235b4d86f7742e017bc88a");
    private static TMP_SpriteAsset? _currencySpriteAsset;
    private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("RefReworkClient");

    [HarmonyPostfix]
    private static void Postfix(TradingItemView __instance, Trader.ItemPrice? __0)
    {
        var price = __0;

        if (!price.HasValue || price.Value.CurrencyId != GpId)
        {
            return;
        }

        var currency = __instance._currency;
        if (currency == null)
        {
            return;
        }

        if (currency.spriteAsset != null)
        {
            if (_currencySpriteAsset == null)
            {
                Log.LogInfo($"Found currency sprite asset from a working instance: '{currency.spriteAsset.name}'");
            }
            _currencySpriteAsset = currency.spriteAsset;
            return;
        }

        var asset = _currencySpriteAsset ?? FindCurrencySpriteAsset();
        if (asset == null)
        {
            Log.LogWarning("GP price shown, but no currency sprite asset found (cached or searched) to fix the icon with.");
            return;
        }

        if (_currencySpriteAsset == null)
        {
            Log.LogInfo($"Located currency sprite asset via search: '{asset.name}'. Applying it to player item view.");
        }

        _currencySpriteAsset = asset;
        currency.spriteAsset = asset;

        var text = currency.text;
        currency.text = string.Empty;
        currency.text = text;

        currency.gameObject.SetActive(true);
        if (__instance._schemeIcon != null)
        {
            __instance._schemeIcon.gameObject.SetActive(false);
        }
    }

    private static TMP_SpriteAsset? FindCurrencySpriteAsset()
    {
        var candidates = Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>();
        return candidates.FirstOrDefault(a =>
            a.name.IndexOf("currency", System.StringComparison.OrdinalIgnoreCase) >= 0
            || a.name.IndexOf("gp", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
