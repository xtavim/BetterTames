using HarmonyLib;

namespace BetterTames.Scripts.Feeding
{
    [HarmonyPatch]
    public static class FeedingPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Container), "Awake")]
        private static void Container_Awake_Postfix(Container __instance)
        {
            Feeding.Register(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MonsterAI), "FindClosestConsumableItem")]
        private static void MonsterAI_FindClosestConsumableItem_Postfix(MonsterAI __instance, ItemDrop __result)
        {
            if (__result == null) Feeding.TryEatFromChest(__instance);
        }
    }
}
