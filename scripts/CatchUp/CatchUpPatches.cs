using HarmonyLib;

namespace BetterTames.Scripts.CatchUp
{
    [HarmonyPatch]
    public static class CatchUpPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static void Player_OnSpawned_Postfix(Player __instance)
        {
            CatchUp.OnRespawn(__instance);
        }
    }
}
