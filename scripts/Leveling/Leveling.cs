using System;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BetterTames.Scripts.Leveling
{
    public static class Leveling
    {
        private const int MaxLevel = 3;

        private static readonly AccessTools.FieldRef<Character, HitData> LastHit =
            AccessTools.FieldRefAccess<Character, HitData>("m_lastHit");

        private static readonly Func<Character, float> GetMaxHealth =
            AccessTools.MethodDelegate<Func<Character, float>>(AccessTools.Method(typeof(Character), "GetMaxHealth"));

        private static readonly Action<Character, float> SetHealth =
            AccessTools.MethodDelegate<Action<Character, float>>(AccessTools.Method(typeof(Character), "SetHealth"));

        public static void OnDeath(Character victim)
        {
            if (!Plugin.starUpOnKill.Value) return;

            var killer = LastHit(victim)?.GetAttacker();
            if (killer == null || !killer.IsTamed() || killer.GetLevel() >= MaxLevel) return;

            var nview = killer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || !nview.IsOwner()) return;

            var difference = Mathf.Max(0, victim.GetLevel() - killer.GetLevel());
            var chance = Plugin.starUpBaseChance.Value + difference * Plugin.starUpBonusPerStar.Value;

            if (Random.Range(0f, 100f) >= chance) return;

            killer.SetLevel(killer.GetLevel() + 1);
            if (Plugin.healOnStarUp.Value) SetHealth(killer, GetMaxHealth(killer));
        }
    }
}
