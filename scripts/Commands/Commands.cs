using System;
using HarmonyLib;
using UnityEngine;

namespace BetterTames.Scripts.Commands
{
    public static class Commands
    {
        private static readonly AccessTools.FieldRef<Tameable, ZNetView> TameView =
            AccessTools.FieldRefAccess<Tameable, ZNetView>("m_nview");

        private static readonly Func<Player, bool> TakeInput =
            AccessTools.MethodDelegate<Func<Player, bool>>(AccessTools.Method(typeof(Player), "TakeInput"));

        private static float lastMassCommand;

        public static void Apply(Tameable tameable)
        {
            if (Plugin.commandAllTames.Value && tameable.GetComponent<MonsterAI>() != null)
            {
                tameable.m_commandable = true;
                return;
            }

            var prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(tameable.gameObject)) : null;
            var own = prefab ? prefab.GetComponent<Tameable>() : null;
            if (own != null) tameable.m_commandable = own.m_commandable;
        }

        public static void ApplyAll()
        {
            foreach (var character in Character.GetAllCharacters())
            {
                if (character.TryGetComponent(out Tameable tameable)) Apply(tameable);
            }
        }

        public static void Tick()
        {
            var player = Player.m_localPlayer;
            if (player == null || !TakeInput(player)) return;

            if (Plugin.massFollowKey.Value.IsDown()) MassCommand(player, follow: true);
            else if (Plugin.massStayKey.Value.IsDown()) MassCommand(player, follow: false);
        }

        private static void MassCommand(Player player, bool follow)
        {
            // Command is a toggle and the follow state only comes back once the owner has handled it,
            // so a second press straight after would flip the same tames back.
            if (Time.time - lastMassCommand < 1f) return;
            lastMassCommand = Time.time;

            var name = player.GetPlayerName();
            var range = Plugin.massCommandRange.Value;
            var count = 0;

            foreach (var character in Character.GetAllCharacters())
            {
                if (!character.IsTamed() || !character.TryGetComponent(out Tameable tameable)) continue;
                if (!tameable.m_commandable || character.GetComponent<MonsterAI>() == null) continue;
                if (Vector3.Distance(character.transform.position, player.transform.position) > range) continue;

                var nview = TameView(tameable);
                if (nview == null || !nview.IsValid()) continue;

                var following = nview.GetZDO().GetString(ZDOVars.s_follow);
                if (follow ? following.Length > 0 : following != name) continue;

                tameable.Command(player, message: false);
                count++;
            }

            var text = count == 0
                ? (follow ? "No tames to follow you" : "No tames following you")
                : $"{count} {(count == 1 ? "tame" : "tames")} {(follow ? "following" : "staying")}";
            player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
