using HarmonyLib;
using UnityEngine;

namespace BetterTames.Scripts.Portals
{
    public static class Portals
    {
        private static readonly AccessTools.FieldRef<TeleportWorld, ZNetView> PortalView =
            AccessTools.FieldRefAccess<TeleportWorld, ZNetView>("m_nview");

        private static readonly AccessTools.FieldRef<TeleportWorld, float> ExitDistance =
            AccessTools.FieldRefAccess<TeleportWorld, float>("m_exitDistance");

        public static void OnTeleport(TeleportWorld portal, Player player)
        {
            if (!Plugin.portalTeleport.Value) return;
            if (player != Player.m_localPlayer || !player.IsTeleporting()) return;

            if (!TryGetExit(portal, out var exit)) return;

            var range = Plugin.massCommandRange.Value;
            foreach (var character in CatchUp.CatchUp.FollowersOf(player.GetPlayerName()))
            {
                if (Vector3.Distance(character.transform.position, player.transform.position) > range) continue;

                var position = CatchUp.CatchUp.MoveTo(character, exit);

                // The tame's zone unloads seconds before the destination loads, so its position is written
                // straight to the ZDO as well rather than relying on the move reaching it before then.
                var nview = character.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid() && nview.IsOwner()) nview.GetZDO().SetPosition(position);
            }
        }

        // Same exit point vanilla teleports the player to: the far portal, pushed out its own exit
        // distance and lifted a meter.
        private static bool TryGetExit(TeleportWorld portal, out Vector3 exit)
        {
            exit = Vector3.zero;

            var nview = PortalView(portal);
            if (nview == null || !nview.IsValid()) return false;

            var target = ZDOMan.instance.GetZDO(nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal));
            if (target == null) return false;

            var rotation = target.GetRotation();
            exit = target.GetPosition() + rotation * Vector3.forward * ExitDistance(portal) + Vector3.up;
            return true;
        }
    }
}
