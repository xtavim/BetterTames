using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterTames.Scripts.CatchUp
{
    public static class CatchUp
    {
        private static readonly AccessTools.FieldRef<Tameable, ZNetView> TameView =
            AccessTools.FieldRefAccess<Tameable, ZNetView>("m_nview");

        private static readonly AccessTools.FieldRef<Character, Rigidbody> CharacterBody =
            AccessTools.FieldRefAccess<Character, Rigidbody>("m_body");

        private static readonly Func<Player, bool> TakeInput =
            AccessTools.MethodDelegate<Func<Player, bool>>(AccessTools.Method(typeof(Player), "TakeInput"));

        private static readonly AccessTools.FieldRef<ZDOMan, Dictionary<ZDOID, ZDO>> AllZDOs =
            AccessTools.FieldRefAccess<ZDOMan, Dictionary<ZDOID, ZDO>>("m_objectsByID");

        private static readonly AccessTools.FieldRef<BaseAI, Pathfinding.AgentType> PathAgent =
            AccessTools.FieldRefAccess<BaseAI, Pathfinding.AgentType>("m_pathAgentType");

        private static readonly Action<ZNetView> ClaimOwnership =
            AccessTools.MethodDelegate<Action<ZNetView>>(AccessTools.Method(typeof(ZNetView), "ClaimOwnership"));

        private const float CheckInterval = 2f;
        private static float lastCheck;

        public static void Tick()
        {
            if (!Plugin.catchUpTeleport.Value) return;

            var localPlayer = Player.m_localPlayer;
            if (localPlayer != null && TakeInput(localPlayer) && Plugin.teleportFollowersKey.Value.IsDown())
                TeleportFollowers(localPlayer);

            if (localPlayer == null || Time.time - lastCheck < CheckInterval) return;
            lastCheck = Time.time;

            // Only our own followers: another player's tames are swept by their own game, so two
            // clients never move the same creature.
            foreach (var character in FollowersOf(localPlayer.GetPlayerName()))
            {
                if (Vector3.Distance(character.transform.position, localPlayer.transform.position) > Plugin.catchUpDistance.Value &&
                    CanReach(character, localPlayer))
                    MoveTo(character, localPlayer);
            }
        }

        public static void OnRespawn(Player player)
        {
            if (!Plugin.catchUpTeleport.Value) return;
            var name = player.GetPlayerName();

            // Following tames whose zone unloaded (you can respawn far from where you died) have no
            // live Character to move, so they are reached directly through their ZDO instead. Anything
            // still loaded is moved through its Character below, which is the smoother of the two paths.
            var handled = new HashSet<ZDOID>();
            foreach (var character in FollowersOf(name))
            {
                MoveTo(character, player);
                var nview = TameView(character.GetComponent<Tameable>());
                handled.Add(nview.GetZDO().m_uid);
            }

            foreach (var zdo in AllZDOs(ZDOMan.instance).Values)
            {
                if (handled.Contains(zdo.m_uid)) continue;
                if (!zdo.IsOwner() || !zdo.GetBool(ZDOVars.s_tamed)) continue;
                if (zdo.GetString(ZDOVars.s_follow) != name) continue;

                zdo.SetPosition(player.transform.position + RandomOffset());
            }
        }

        private static void TeleportFollowers(Player player)
        {
            var count = 0;
            foreach (var character in FollowersOf(player.GetPlayerName()))
            {
                if (!CanReach(character, player)) continue;
                MoveTo(character, player);
                count++;
            }

            var text = count == 0
                ? "No tames following you"
                : $"{count} {(count == 1 ? "tame" : "tames")} teleported to you";
            player.Message(MessageHud.MessageType.Center, text);
        }

        public static IEnumerable<Character> FollowersOf(string playerName)
        {
            foreach (var character in Character.GetAllCharacters())
            {
                if (!character.IsTamed() || !character.TryGetComponent(out Tameable tameable)) continue;

                var nview = TameView(tameable);
                if (nview == null || !nview.IsValid()) continue;
                if (nview.GetZDO().GetString(ZDOVars.s_follow) != playerName) continue;

                yield return character;
            }
        }

        // Dungeon interiors sit 5000m above their entrance, so the distance check is what carries a
        // following tame through the door. Big tames are kept on their own side when that's turned off.
        private static bool CanReach(Character character, Player player)
        {
            if (Plugin.bigTamesEnterDungeons.Value) return true;
            if (!IsBig(character)) return true;
            return Character.InInterior(character.transform.position) == Character.InInterior(player.transform.position);
        }

        private static bool IsBig(Character character)
        {
            var ai = character.GetBaseAI();
            if (ai == null) return false;

            switch (PathAgent(ai))
            {
                case Pathfinding.AgentType.Humanoid:
                case Pathfinding.AgentType.HumanoidNoSwim:
                case Pathfinding.AgentType.HumanoidAvoidWater:
                    return false;
                default:
                    return true;
            }
        }

        private static void MoveTo(Character character, Player player) =>
            MoveTo(character, player.transform.position);

        public static Vector3 MoveTo(Character character, Vector3 target)
        {
            var position = target + RandomOffset();

            // A tame near another player can still be run by them, and only its owner's move sticks.
            var nview = character.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid() && !nview.IsOwner()) ClaimOwnership(nview);

            character.transform.position = position;
            var body = CharacterBody(character);
            if (body != null) body.position = position;
            return position;
        }

        private static Vector3 RandomOffset()
        {
            var offset = UnityEngine.Random.insideUnitCircle * 2f;
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}
