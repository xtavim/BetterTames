using HarmonyLib;

namespace BetterTames.Scripts.Portals
{
    [HarmonyPatch]
    public static class PortalPatches
    {
        // Runs after vanilla decided the teleport goes ahead, so every refusal it makes - no-portal
        // worlds, an active boss, carrying ore - keeps the tames where they are.
        [HarmonyPostfix, HarmonyPatch(typeof(TeleportWorld), "Teleport")]
        private static void TeleportWorld_Teleport_Postfix(TeleportWorld __instance, Player player)
        {
            Portals.OnTeleport(__instance, player);
        }
    }
}
