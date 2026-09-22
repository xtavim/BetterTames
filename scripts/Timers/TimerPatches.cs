using HarmonyLib;

namespace BetterTames.Scripts.Timers
{
    [HarmonyPatch]
    public static class TimerPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
        private static void Character_GetHoverText_Postfix(Character __instance, ref string __result)
        {
            __result = Timers.AddTo(__instance, __result);
        }
    }
}
