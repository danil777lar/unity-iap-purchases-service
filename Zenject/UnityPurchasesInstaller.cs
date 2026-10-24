using Larje.Core.Monetization;
using Larje.Core.Zenject;
using UnityEngine;
using Zenject;

namespace Larje.Purchases.UnityIap.Zenject
{
    [CreateAssetMenu(fileName = "UnityPurchasesInstaller", menuName = "Larje/Purchases/Unity IAP Installer")]
    public sealed class UnityPurchasesInstaller : ScriptableObjectInstaller<UnityPurchasesInstaller>
    {
        [SerializeField] private PurchasesConfig purchases;

        public override void InstallBindings()
        {
            Container.BindLarjePurchasesProvider<UnityPurchasesService>(purchases);
        }
    }
}
