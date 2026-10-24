using Larje.Core.Editor.Defines;
using UnityEditor;

namespace Larje.Purchases.UnityIap.Editor.Defines
{
    [InitializeOnLoad]
    internal static class UnityIapDefines
    {
        static UnityIapDefines()
        {
            OptionalDependencyDefines.Register("Unity.Purchasing", "LARJE_UNITY_IAP");
        }
    }
}
