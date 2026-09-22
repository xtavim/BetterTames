using HarmonyLib;

namespace BetterTames.Scripts.Protection
{
    [HarmonyPatch]
    public static class ProtectionPatches
    {
        // CheckDeath is where every death is decided, whatever caused it, so hooking it catches falls,
        // fire, poison and drowning as well as hits.
        [HarmonyPrefix, HarmonyPatch(typeof(Character), "CheckDeath")]
        private static bool Character_CheckDeath_Prefix(Character __instance)
        {
            return !Protection.TryDown(__instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Character), "ApplyDamage")]
        private static bool Character_ApplyDamage_Prefix(Character __instance)
        {
            return !Protection.IsDowned(__instance);
        }

        // Breeding runs on its own timer, apart from the AI, and never checks for sleep.
        [HarmonyPrefix, HarmonyPatch(typeof(Procreation), "Procreate")]
        private static bool Procreation_Procreate_Prefix(Procreation __instance)
        {
            return !Protection.IsDowned(__instance.GetComponent<Character>());
        }

        // Partner counting skips any animal not ready to procreate (the way pregnant ones stop counting),
        // so a downed tame doesn't stand in as a partner for the tames around it.
        [HarmonyPostfix, HarmonyPatch(typeof(Procreation), "ReadyForProcreation")]
        private static void Procreation_ReadyForProcreation_Postfix(Procreation __instance, ref bool __result)
        {
            if (__result && Protection.IsDowned(__instance.GetComponent<Character>())) __result = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MonsterAI), "UpdateSleep")]
        private static bool MonsterAI_UpdateSleep_Prefix(MonsterAI __instance)
        {
            return !Protection.UpdateRecovery(__instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Tameable), "Interact")]
        private static bool Tameable_Interact_Prefix(Tameable __instance, ref bool __result)
        {
            if (!Protection.SwallowsInteract(__instance)) return true;
            __result = true;
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Character), "GetHoverText")]
        private static void Character_GetHoverText_Postfix(Character __instance, ref string __result)
        {
            __result = Protection.AddPrompt(__instance, __result);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
        private static void EnemyHud_UpdateHuds_Postfix(EnemyHud __instance)
        {
            Protection.TintNameplates(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "FindHoverObject")]
        private static void Player_FindHoverObject_Postfix(Player __instance, ref Character hoverCreature)
        {
            Protection.FindDownedHover(__instance, ref hoverCreature);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BaseAI), "IsEnemy", typeof(Character), typeof(Character))]
        private static void BaseAI_IsEnemy_Postfix(Character a, Character b, ref bool __result)
        {
            // Vanilla's first check is "same group, never enemies", which keeps a tamed wolf on the wild wolves'
            // side. With one tamed and one wild, vanilla's own tamed rules would otherwise make them enemies.
            if (!__result && Plugin.tamesFightOwnKind.Value && a != null && b != null && a != b &&
                a.IsTamed() != b.IsTamed() && !a.IsPlayer() && !b.IsPlayer())
            {
                var group = a.GetGroup();
                if (group.Length > 0 && group == b.GetGroup() && Protection.WildWillFight(a.IsTamed() ? b : a))
                    __result = true;
            }

            // An enemy already attacking a downed tame drops it on its next AI tick, because vanilla clears any
            // target that is no longer an enemy, and none picks it as a new target.
            if (__result && (Protection.IsDowned(a) || Protection.IsDowned(b))) __result = false;
        }
    }
}
