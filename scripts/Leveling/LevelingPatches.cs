using HarmonyLib;

namespace BetterTames.Scripts.Leveling
{
    [HarmonyPatch]
    public static class LevelingPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static void Character_OnDeath_Postfix(Character __instance)
        {
            Leveling.OnDeath(__instance);
        }
    }
}
