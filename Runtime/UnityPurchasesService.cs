using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Larje.Core;
using Larje.Core.Monetization;
using Larje.Core.Saves;
using UnityEngine;
using UnityEngine.Purchasing;
using LarjeProductType = Larje.Core.Monetization.ProductType;
using StoreProduct = UnityEngine.Purchasing.Product;
using StoreProductType = UnityEngine.Purchasing.ProductType;

namespace Larje.Purchases.UnityIap
{
    public sealed class UnityPurchasesService : PurchasesServiceBase, IInitializableService, IDisposable
    {
        private bool _initialized;
        private bool _disposed;
        private readonly HashSet<string> _owned = new HashSet<string>();
        private readonly Dictionary<string, ProductConfig> _byStoreId = new Dictionary<string, ProductConfig>();
        private StoreController _store;

        public override bool Initialized => _initialized;

        public UnityPurchasesService(PurchasesConfig config, ISaveService saves, IServiceProvider services = null) : base(config, saves, services)
        {
            foreach (ProductConfig product in config.Products)
            {
                if (product == null)
                {
                    Debug.LogWarning("UnityPurchasesService: PurchasesConfig contains an empty product");
                }
                else
                {
                    Register(product.ProductKey, product);
                    Register(product.DiscountProductKey, product);
                }
            }
        }

        public void Initialize()
        {
            ConnectAsync().Forget();
        }

        public void Dispose()
        {
            _disposed = true;
            if (_store != null)
            {
                _store.OnProductsFetched -= OnProductsFetched;
                _store.OnProductsFetchFailed -= OnProductsFetchFailed;
                _store.OnPurchasesFetched -= OnPurchasesFetched;
                _store.OnPurchasePending -= OnPurchasePending;
                _store.OnPurchaseFailed -= OnPurchaseFailed;
                _store.OnPurchaseDeferred -= OnPurchaseDeferred;
                _store.OnStoreDisconnected -= OnStoreDisconnected;
            }
        }

        public override bool IsPurchased(ProductConfig product)
        {
            return _owned.Contains(product.ProductKey) || (product.HasDiscountProduct && _owned.Contains(product.DiscountProductKey));
        }

        public override ProductInfo GetInfo(ProductConfig product)
        {
            StoreProduct storeProduct = _initialized ? _store.GetProductById(GetStoreKey(product)) : null;
            if (storeProduct == null || storeProduct.metadata == null)
            {
                return new ProductInfo(false, string.Empty, product.name, string.Empty, string.Empty, 0m);
            }

            ProductMetadata metadata = storeProduct.metadata;
            return new ProductInfo(storeProduct.availableToPurchase, metadata.localizedPriceString, metadata.localizedTitle,
                metadata.localizedDescription, metadata.isoCurrencyCode, metadata.localizedPrice);
        }

        public override void Buy(ProductConfig product)
        {
            if (!_initialized)
            {
                Debug.LogWarning($"UnityPurchasesService: store is not ready, cannot buy '{product.name}'");
                FailPurchase(product);
                return;
            }

            _store.PurchaseProduct(GetStoreKey(product));
        }

        public override void Restore()
        {
            if (!_initialized)
            {
                return;
            }

            _store.RestoreTransactions(OnRestoreFinished);
        }

        private async UniTaskVoid ConnectAsync()
        {
            _store = UnityIAPServices.StoreController();
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
            _store.OnStoreDisconnected += OnStoreDisconnected;

            try
            {
                await _store.Connect().AsUniTask();
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogWarning($"UnityPurchasesService: store connection failed: {exception.Message}");
                return;
            }

            await UniTask.SwitchToMainThread();
            if (!_disposed)
            {
                _store.FetchProducts(BuildDefinitions());
            }
        }

        private List<ProductDefinition> BuildDefinitions()
        {
            List<ProductDefinition> definitions = new List<ProductDefinition>();
            foreach (KeyValuePair<string, ProductConfig> pair in _byStoreId)
            {
                definitions.Add(new ProductDefinition(pair.Key, ToStoreType(pair.Value.ProductType)));
            }

            return definitions;
        }

        private void Register(string storeId, ProductConfig product)
        {
            if (!string.IsNullOrEmpty(storeId) && !_byStoreId.ContainsKey(storeId))
            {
                _byStoreId.Add(storeId, product);
            }
        }

        private void OnProductsFetched(List<StoreProduct> products)
        {
            _initialized = true;
            _store.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"UnityPurchasesService: products fetch failed: {failure.FailureReason}");
            if (_store.GetProducts().Count > 0)
            {
                _initialized = true;
                _store.FetchPurchases();
            }
        }

        private void OnPurchasesFetched(Orders orders)
        {
            foreach (ConfirmedOrder order in orders.ConfirmedOrders)
            {
                foreach (CartItem item in order.CartOrdered.Items())
                {
                    ProductConfig product = FindProduct(item);
                    if (product != null && product.ProductType != LarjeProductType.Consumable && _owned.Add(item.Product.definition.id))
                    {
                        product.OnPurchased(_context);
                    }
                }
            }
        }

        private void OnPurchasePending(PendingOrder order)
        {
            foreach (CartItem item in order.CartOrdered.Items())
            {
                ProductConfig product = FindProduct(item);
                if (product == null)
                {
                    Debug.LogWarning($"UnityPurchasesService: pending order for unknown product '{item.Product?.definition?.id}'");
                }
                else
                {
                    if (product.ProductType != LarjeProductType.Consumable)
                    {
                        _owned.Add(item.Product.definition.id);
                    }

                    CompletePurchase(product);
                }
            }

            _store.ConfirmPurchase(order);
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            Debug.LogWarning($"UnityPurchasesService: purchase failed: {order.FailureReason} {order.Details}");
            foreach (CartItem item in order.CartOrdered.Items())
            {
                ProductConfig product = FindProduct(item);
                if (product != null)
                {
                    FailPurchase(product);
                }
            }
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            Debug.Log("UnityPurchasesService: purchase deferred, waiting for approval");
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogWarning($"UnityPurchasesService: store disconnected: {failure.Message}");
        }

        private void OnRestoreFinished(bool success, string error)
        {
            if (!success)
            {
                Debug.LogWarning($"UnityPurchasesService: restore failed: {error}");
            }

            _store.FetchPurchases();
        }

        private ProductConfig FindProduct(CartItem item)
        {
            string id = item.Product?.definition?.id;
            if (id != null && _byStoreId.TryGetValue(id, out ProductConfig product))
            {
                return product;
            }

            return null;
        }

        private StoreProductType ToStoreType(LarjeProductType type)
        {
            switch (type)
            {
                case LarjeProductType.NonConsumable:
                    return StoreProductType.NonConsumable;
                case LarjeProductType.Subscription:
                    return StoreProductType.Subscription;
                default:
                    return StoreProductType.Consumable;
            }
        }
    }
}
