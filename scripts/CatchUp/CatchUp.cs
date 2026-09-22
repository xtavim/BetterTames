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

        private const float CheckInterval = 2f;
        private static float lastCheck;

        public static void Tick()
        {
            if (!Plugin.catchUpTeleport.Value) return;

            var localPlayer = Player.m_localPlayer;
            if (localPlayer != null && TakeInput(localPlayer) && Plugin.teleportFollowersKey.Value.IsDown())
                TeleportFollowers(localPlayer);

            if (Time.time - lastCheck < CheckInterval) return;
            lastCheck = Time.time;

            foreach (var character in Character.GetAllCharacters())
            {
                if (!character.IsTamed() || !character.TryGetComponent(out Tameable tameable)) continue;

                var nview = TameView(tameable);
                if (nview == null || !nview.IsValid() || !nview.IsOwner()) continue;

                var name = nview.GetZDO().GetString(ZDOVars.s_follow);
                if (name.Length == 0) continue;

                var owner = FindPlayer(name);
                if (owner == null) continue;

                if (Vector3.Distance(character.transform.position, owner.transform.position) > Plugin.catchUpDistance.Value)
                    MoveTo(character, owner);
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
                MoveTo(character, player);
                count++;
            }

            var text = count == 0
                ? "No tames following you"
                : $"{count} {(count == 1 ? "tame" : "tames")} teleported to you";
            player.Message(MessageHud.MessageType.Center, text);
        }

        private static IEnumerable<Character> FollowersOf(string playerName)
        {
            foreach (var character in Character.GetAllCharacters())
            {
                if (!character.IsTamed() || !character.TryGetComponent(out Tameable tameable)) continue;

                var nview = TameView(tameable);
                if (nview == null || !nview.IsValid() || !nview.IsOwner()) continue;
                if (nview.GetZDO().GetString(ZDOVars.s_follow) != playerName) continue;

                yield return character;
            }
        }

        private static Player? FindPlayer(string name)
        {
            foreach (var player in Player.GetAllPlayers())
            {
                if (player.GetPlayerName() == name) return player;
            }
            return null;
        }

        private static void MoveTo(Character character, Player player)
        {
            var position = player.transform.position + RandomOffset();

            character.transform.position = position;
            var body = CharacterBody(character);
            if (body != null) body.position = position;
        }

        private static Vector3 RandomOffset()
        {
            var offset = UnityEngine.Random.insideUnitCircle * 2f;
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}
