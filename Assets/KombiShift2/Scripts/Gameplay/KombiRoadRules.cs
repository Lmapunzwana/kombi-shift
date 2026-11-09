using System.Collections.Generic;

namespace KombiShift2.Gameplay
{
    public static class KombiRoadRules
    {
        public static readonly HashSet<KombiRoadRuleZone> Active = new();
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Active.Clear();
    }
}
