# Larje Purchases Unity IAP

`com.larje.iap-unity`: Unity IAP 5 provider for the `IPurchasesService` from `com.larje.core`, built on `StoreController`: catalog from `PurchasesConfig`, localized prices, purchases with pending order confirmation, ownership from fetched orders, restore and discount products.

## Install

UPM cannot resolve git dependencies, so add core, UniTask and this package to `Packages/manifest.json`:

```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10",
"com.larje.core": "https://github.com/danil777lar/unity-gameplay-core-services.git",
"com.larje.zenject": "https://github.com/danil777lar/unity-zenject-upm.git",
"com.larje.iap-unity": "https://github.com/danil777lar/unity-iap-purchases-service.git"
```

Runtime code compiles only when `LARJE_UNITY_IAP` is defined, so the package is safe to install before the SDK. The Zenject installer additionally needs `com.larje.zenject` (`LARJE_ZENJECT`).

### SDK

`com.unity.purchasing` 5.x from the Unity registry (verified with 5.4.4): `"com.unity.purchasing": "5.4.4"`. `LARJE_UNITY_IAP` comes from `versionDefines`. Create the products in Google Play Console / App Store Connect with ids equal to `ProductKey` (and `DiscountProductKey` for discounted variants).

## Setup

1. Fill `PurchasesConfig` from core with `ProductConfig` assets (product key, type, group, optional discount product key).
2. Create `UnityPurchasesInstaller` (`Create > Larje > Purchases > Unity IAP Installer`) and add it to the `ProjectContext` next to `LarjeCoreInstaller`. Assign `PurchasesConfig` there, or leave it empty to use the one from `LarjeCoreInstaller`.
3. Use `IPurchasesService` as with the simulated service: `Buy`, `GetInfo`, `IsPurchased`, `Restore`, `Purchased` / `PurchaseFailed`.

## Config

| Group | Fields |
|---|---|
| PurchasesConfig | Shared with core. Every `ProductKey` and `DiscountProductKey` is fetched from the store. |

## Behavior

- The store connects in the background; `Initialized` becomes `true` after products are fetched.
- A pending order grants the product (`ProductConfig.OnPurchased`, `Purchased` event, save) and is then confirmed.
- Non-consumables and subscriptions found in fetched confirmed orders are marked owned and `OnPurchased` runs once per session for them, which restores entitlements after reinstall.
- `Buy` uses the discount product key while a discount is active (`DiscountQueue` from core works unchanged).
- `Restore` calls `RestoreTransactions` and fetches purchases again.

## Status

Compiles against com.unity.purchasing 5.4.4 in Unity 6000.4.9f1. Not tested against a store yet.
