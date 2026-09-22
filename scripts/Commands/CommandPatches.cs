using HarmonyLib;

namespace BetterTames.Scripts.Commands
{
    [HarmonyPatch]
    public static class CommandPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Tameable), "Awake")]
        private static void Tameable_Awake_Postfix(Tameable __instance)
        {
            Commands.Apply(__instance);
        }
    }
}
