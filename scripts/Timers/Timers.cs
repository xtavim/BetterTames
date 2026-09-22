using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterTames.Scripts.Timers
{
    public static class Timers
    {
        private static readonly AccessTools.FieldRef<Character, ZNetView> NView =
            AccessTools.FieldRefAccess<Character, ZNetView>("m_nview");

        private static readonly List<Player> NearbyPlayers = new();
        private static readonly List<string> Lines = new();

        public static string AddTo(Character character, string hoverText)
        {
            var nview = NView(character);
            if (nview == null || !nview.IsValid() || ZNet.instance == null) return hoverText;
            var zdo = nview.GetZDO();

            Lines.Clear();
            Add(Taming(character, zdo));
            Add(Breeding(character, zdo));
            Add(Growth(character, zdo));
            Add(Protection.Protection.HoverLine(character, zdo));
            if (Lines.Count == 0) return hoverText;

            var extra = string.Join("\n", Lines);

            if (hoverText.Length == 0) return character.GetHoverName() + "\n" + extra;

            var end = hoverText.IndexOf('\n');
            return end < 0
                ? hoverText + "\n" + extra
                : hoverText.Substring(0, end) + "\n" + extra + hoverText.Substring(end);
        }

        private static void Add(string? line)
        {
            if (line != null) Lines.Add(line);
        }

        private static string? Taming(Character character, ZDO zdo)
        {
            if (!Plugin.showTamingTimer.Value || character.IsTamed()) return null;

            var tameable = character.GetComponent<Tameable>();
            if (tameable == null) return null;

            var left = zdo.GetFloat(ZDOVars.s_tameTimeLeft, tameable.m_tamingTime);
            if (left >= tameable.m_tamingTime) return null;

            var text = "Tamed in " + Format(left / TamingSpeed(tameable));

            var ai = character.GetBaseAI();
            if (tameable.IsHungry() || (ai != null && ai.IsAlerted())) text += " (paused)";

            return text;
        }

        private static float TamingSpeed(Tameable tameable)
        {
            var speed = 1f;
            NearbyPlayers.Clear();
            Player.GetPlayersInRange(tameable.transform.position, tameable.m_tamingSpeedMultiplierRange, NearbyPlayers);
            foreach (var player in NearbyPlayers)
            {
                if (player.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.TamingBoost))
                    speed *= tameable.m_tamingBoostMultiplier;
            }
            return speed;
        }

        private static string? Breeding(Character character, ZDO zdo)
        {
            if (!Plugin.showBreedingTimer.Value || !character.IsTamed()) return null;

            var procreation = character.GetComponent<Procreation>();
            if (procreation == null) return null;

            var conceived = zdo.GetLong(ZDOVars.s_pregnant, 0L);
            if (conceived == 0L)
                return $"Love {zdo.GetInt(ZDOVars.s_lovePoints)}/{procreation.m_requiredLovePoints}";

            var left = procreation.m_pregnancyDuration - SecondsSince(conceived);
            return left > 0 ? "Gives birth in " + Format(left) : "Giving birth";
        }

        private static string? Growth(Character character, ZDO zdo)
        {
            if (!Plugin.showGrowthTimer.Value) return null;

            var growup = character.GetComponent<Growup>();
            if (growup == null) return null;

            // BaseAI.GetTimeSinceSpawned writes the spawn time when missing, which only the owner should do.
            var spawned = zdo.GetLong(ZDOVars.s_spawnTime, 0L);
            if (spawned == 0L) return null;

            var left = growup.m_growTime - SecondsSince(spawned);
            return left > 0 ? "Grows up in " + Format(left) : "Growing up";
        }

        private static double SecondsSince(long ticks) =>
            (ZNet.instance.GetTime() - new DateTime(ticks)).TotalSeconds;

        private static string Format(double seconds)
        {
            var s = Mathf.CeilToInt((float)seconds);
            if (s >= 3600) return $"{s / 3600}h {s % 3600 / 60}m";
            if (s >= 60) return $"{s / 60}m {s % 60}s";
            return $"{s}s";
        }
    }
}
