using HarmonyLib;

namespace BetterTames.Scripts.Tuning
{
    [HarmonyPatch]
    public static class TuningPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Tameable), "Awake")]
        private static void Tameable_Awake_Postfix(Tameable __instance)
        {
            Tuning.Apply(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Procreation), "Awake")]
        private static void Procreation_Awake_Postfix(Procreation __instance)
        {
            Tuning.Apply(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Growup), "Start")]
        private static void Growup_Start_Postfix(Growup __instance)
        {
            Tuning.Apply(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Character), "RPC_SetTamed")]
        private static void Character_RPC_SetTamed_Postfix(Character __instance, bool tamed)
        {
            if (tamed) Tuning.Apply(__instance);
        }

        // RPC_SetTamed only fires the moment a creature becomes tamed, not on every subsequent load of
        // one that already was, so an already-tamed creature needs this too, the same as the other
        // Tuning values already reapply on every Awake/Start.
        [HarmonyPostfix, HarmonyPatch(typeof(Character), "Awake")]
        private static void Character_Awake_Postfix(Character __instance)
        {
            if (__instance.IsTamed()) Tuning.Apply(__instance);
        }
    }
}
