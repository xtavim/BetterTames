using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterTames.Scripts.Protection
{
    public static class Protection
    {
        private static readonly int RecoverUntilKey = "BetterTames_RecoverUntil".GetStableHashCode();

        // 0 = follow the Protect All Tames default, 1 = protected, -1 = not protected.
        private static readonly int ProtectionKey = "BetterTames_Protection".GetStableHashCode();

        private static readonly AccessTools.FieldRef<Character, ZNetView> NView =
            AccessTools.FieldRefAccess<Character, ZNetView>("m_nview");

        private static readonly AccessTools.FieldRef<Character, HitData> LastHit =
            AccessTools.FieldRefAccess<Character, HitData>("m_lastHit");

        private static readonly AccessTools.FieldRef<Player, Character> HoveringCreature =
            AccessTools.FieldRefAccess<Player, Character>("m_hoveringCreature");

        private static readonly Func<Character, float> GetHealth =
            AccessTools.MethodDelegate<Func<Character, float>>(AccessTools.Method(typeof(Character), "GetHealth"));

        private static readonly Func<Character, float> GetMaxHealth =
            AccessTools.MethodDelegate<Func<Character, float>>(AccessTools.Method(typeof(Character), "GetMaxHealth"));

        private static readonly Action<Character, float> SetHealth =
            AccessTools.MethodDelegate<Action<Character, float>>(AccessTools.Method(typeof(Character), "SetHealth"));

        private static readonly Func<Humanoid, ItemDrop.ItemData> GetCurrentWeapon =
            AccessTools.MethodDelegate<Func<Humanoid, ItemDrop.ItemData>>(AccessTools.Method(typeof(Humanoid), "GetCurrentWeapon"));

        private static readonly Action<MonsterAI> Sleep =
            AccessTools.MethodDelegate<Action<MonsterAI>>(AccessTools.Method(typeof(MonsterAI), "Sleep"));

        private static readonly Action<MonsterAI> Wakeup =
            AccessTools.MethodDelegate<Action<MonsterAI>>(AccessTools.Method(typeof(MonsterAI), "Wakeup"));

        private static readonly Action<BaseAI> StopMoving =
            AccessTools.MethodDelegate<Action<BaseAI>>(AccessTools.Method(typeof(BaseAI), "StopMoving"));

        private static readonly Action<MonsterAI, bool> SetAlerted =
            AccessTools.MethodDelegate<Action<MonsterAI, bool>>(AccessTools.Method(typeof(MonsterAI), "SetAlerted"));

        private static readonly Action<BaseAI, ZDOID> SetTargetInfo =
            AccessTools.MethodDelegate<Action<BaseAI, ZDOID>>(AccessTools.Method(typeof(BaseAI), "SetTargetInfo"));

        private static readonly Func<MonsterAI, bool> IsSleeping =
            AccessTools.MethodDelegate<Func<MonsterAI, bool>>(AccessTools.Method(typeof(MonsterAI), "IsSleeping"));

        private static readonly Action<ZNetView> ClaimOwnership =
            AccessTools.MethodDelegate<Action<ZNetView>>(AccessTools.Method(typeof(ZNetView), "ClaimOwnership"));

        private static readonly Func<Player, bool> TakeInput =
            AccessTools.MethodDelegate<Func<Player, bool>>(AccessTools.Method(typeof(Player), "TakeInput"));

        private static long Now => ZNet.instance.GetTime().Ticks;

        private static readonly AccessTools.FieldRef<Character, Vector3> MoveDir =
            AccessTools.FieldRefAccess<Character, Vector3>("m_moveDir");

        private static readonly AccessTools.FieldRef<Character, Vector3> CurrentVel =
            AccessTools.FieldRefAccess<Character, Vector3>("m_currentVel");

        private static readonly AccessTools.FieldRef<Character, Rigidbody> Body =
            AccessTools.FieldRefAccess<Character, Rigidbody>("m_body");

        private static readonly AccessTools.FieldRef<Character, ZSyncAnimation> ZAnim =
            AccessTools.FieldRefAccess<Character, ZSyncAnimation>("m_zanim");

        private static readonly Action<ZSyncAnimation, int, float> SetAnimFloat =
            AccessTools.MethodDelegate<Action<ZSyncAnimation, int, float>>(
                AccessTools.Method(typeof(ZSyncAnimation), "SetFloat", new[] { typeof(int), typeof(float) }));

        private static readonly int ForwardSpeed = ZSyncAnimation.GetHash("forward_speed");
        private static readonly int SidewaySpeed = ZSyncAnimation.GetHash("sideway_speed");
        private static readonly int TurnSpeed = ZSyncAnimation.GetHash("turn_speed");

        private static readonly AccessTools.FieldRef<Player, int> InteractMask =
            AccessTools.FieldRefAccess<Player, int>("m_interactMask");

        // Downed tames currently loaded, so the extra hover raycast only runs while one is around.
        private static readonly HashSet<Character> Downed = new();
        private static readonly Predicate<Character> IsGone = c => c == null;
        private static readonly RaycastHit[] HoverHits = new RaycastHit[32];

        private static float lastDebugLog;

        private static void DebugLog(Character character, MonsterAI monsterAI, string when)
        {
            if (!Plugin.debugMode.Value) return;

            var animator = character.GetComponentInChildren<Animator>();
            var hasSleep = false;
            if (animator != null)
            {
                foreach (var parameter in animator.parameters)
                {
                    if (parameter.name == "sleeping") hasSleep = true;
                }
            }

            Plugin.Logger.LogInfo(
                $"[Protection] {when} {character.m_name}: sleepParam={hasSleep} sleeping={IsSleeping(monsterAI)} " +
                $"moveDir={MoveDir(character)} currentVel={CurrentVel(character)} " +
                $"forward_speed={(animator != null ? animator.GetFloat("forward_speed") : -1f):0.00} " +
                $"animSleeping={(animator != null && hasSleep && animator.GetBool("sleeping"))}");
        }

        // For Tames Fight Their Own Kind: a wild creature only counts as an enemy of its tamed kin while it is
        // alerted (actually in a fight) and not partway through being tamed, so taming several of the same
        // kind at once doesn't turn the first one tamed against the rest.
        public static bool WildWillFight(Character wild)
        {
            var ai = wild.GetBaseAI();
            if (ai == null || !ai.IsAlerted()) return false;

            if (!wild.TryGetComponent(out Tameable tameable)) return true;
            var nview = NView(wild);
            if (nview == null || !nview.IsValid()) return true;
            return nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft, tameable.m_tamingTime) >= tameable.m_tamingTime;
        }

        public static bool IsDowned(Character? character)
        {
            if (character == null || !character.IsTamed()) return false;
            var nview = NView(character);
            return nview != null && nview.IsValid() && nview.GetZDO().GetLong(RecoverUntilKey, 0L) != 0L;
        }

        // Called in place of death. Returns true when the tame was put down to recover instead.
        public static bool TryDown(Character character)
        {
            if (!character.IsTamed() || GetHealth(character) > 0f) return false;
            if (!character.TryGetComponent(out MonsterAI monsterAI)) return false;

            var nview = NView(character);
            if (nview == null || !nview.IsValid() || !nview.IsOwner()) return false;

            var zdo = nview.GetZDO();
            if (!IsProtected(zdo) || KilledWithButcherKnife(character)) return false;

            zdo.Set(RecoverUntilKey, Now + TimeSpan.FromSeconds(Plugin.recoveryTime.Value).Ticks);
            SetHealth(character, 1f);
            Calm(monsterAI, character);
            Sleep(monsterAI);
            Downed.Add(character);
            DebugLog(character, monsterAI, "downed");
            return true;
        }

        // Runs in place of vanilla's sleep update while recovering, which would otherwise wake the tame
        // as soon as a player comes near or makes noise. Healing is worked out from world time, so a tame
        // left in an unloaded area or through a logout still gets up on schedule.
        public static bool UpdateRecovery(MonsterAI monsterAI)
        {
            if (!monsterAI.TryGetComponent(out Character character)) return false;

            var nview = NView(character);
            if (nview == null || !nview.IsValid()) return false;

            var zdo = nview.GetZDO();
            var until = zdo.GetLong(RecoverUntilKey, 0L);
            if (until == 0L) return false;
            Downed.Add(character);
            if (!nview.IsOwner()) return true;

            Calm(monsterAI, character);
            if (!IsSleeping(monsterAI)) Sleep(monsterAI);

            if (Time.time - lastDebugLog > 2f)
            {
                lastDebugLog = Time.time;
                DebugLog(character, monsterAI, "recovering");
            }

            var now = Now;
            var max = GetMaxHealth(character);
            if (now >= until)
            {
                zdo.Set(RecoverUntilKey, 0L);
                SetHealth(character, max);
                Wakeup(monsterAI);
                Downed.Remove(character);
                return true;
            }

            var total = TimeSpan.FromSeconds(Plugin.recoveryTime.Value).Ticks;
            var healed = max * (1f - (float)(until - now) / total);
            if (healed > GetHealth(character)) SetHealth(character, healed);
            return true;
        }

        // Character.UpdateMotion skips walking entirely while the AI is asleep, and walking is the only place
        // that slows the velocity down and writes the run speed into the animator. Vanilla sleepers are spawned
        // standing still so it never matters; a tame put to sleep mid-run would stay frozen running, so its
        // velocity and animation speeds are zeroed here. The saved "has a target" flag behind the nameplate's
        // warning icon is likewise only ever updated by targeting, which doesn't run while asleep.
        private static void Calm(MonsterAI monsterAI, Character character)
        {
            StopMoving(monsterAI);
            SetAlerted(monsterAI, false);
            SetTargetInfo(monsterAI, ZDOID.None);

            CurrentVel(character) = Vector3.zero;
            var body = Body(character);
            if (body != null) body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);

            var zanim = ZAnim(character);
            if (zanim == null) return;
            SetAnimFloat(zanim, ForwardSpeed, 0f);
            SetAnimFloat(zanim, SidewaySpeed, 0f);
            SetAnimFloat(zanim, TurnSpeed, 0f);
        }

        public static string? HoverLine(Character character, ZDO zdo)
        {
            if (!character.IsTamed() || !character.TryGetComponent(out MonsterAI _)) return null;

            var until = zdo.GetLong(RecoverUntilKey, 0L);
            if (until == 0L) return null;
            return "Recovering " + Mathf.Max(0, Mathf.CeilToInt((until - Now) / (float)TimeSpan.TicksPerSecond)) + "s";
        }

        private const string ProtectedColor = "#4FA3FF";
        private const string RecoveringColor = "#FFD24A";

        // HudData is a nested game type, so it's reached through reflection rather than named in a signature.
        private static readonly FieldInfo HudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
        private static readonly FieldInfo HudNameField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_name");

        // Added after vanilla's own [E] Pet / [Shift + E] Rename prompts, in the same style.
        public static string AddPrompt(Character character, string hoverText)
        {
            if (hoverText.Length == 0 || !character.IsTamed() || !character.TryGetComponent(out MonsterAI _)) return hoverText;

            var nview = NView(character);
            if (nview == null || !nview.IsValid()) return hoverText;

            var state = IsProtected(nview.GetZDO()) ? "On" : "Off";
            return hoverText + $"\n[<color=yellow><b>{KeyText(Plugin.toggleProtectionKey.Value)}</b></color>] Protection: {state}";
        }

        // The nameplate above the health bar: blue when protected, yellow while recovering. Vanilla rewrites
        // the name from GetHoverName every frame, but only while the plate is showing, so a hidden plate is
        // skipped or the color tags would pile up.
        public static void TintNameplates(EnemyHud enemyHud)
        {
            Downed.RemoveWhere(IsGone);
            if (HudsField.GetValue(enemyHud) is not IDictionary huds) return;

            foreach (DictionaryEntry entry in huds)
            {
                if (entry.Key is not Character character || character == null) continue;

                // Checked for every plate, hidden or not, since it also keeps the downed set current.
                var color = NameColor(character);
                if (color == null) continue;

                if (HudNameField.GetValue(entry.Value) is TMP_Text name && name.gameObject.activeInHierarchy)
                    name.text = $"<color={color}>{name.text}</color>";
            }
        }

        private static string? NameColor(Character character)
        {
            if (!character.IsTamed() || !character.TryGetComponent(out MonsterAI _)) return null;

            var nview = NView(character);
            if (nview == null || !nview.IsValid()) return null;

            var zdo = nview.GetZDO();
            if (zdo.GetLong(RecoverUntilKey, 0L) != 0L)
            {
                Downed.Add(character);
                return RecoveringColor;
            }

            Downed.Remove(character);
            return IsProtected(zdo) ? ProtectedColor : null;
        }

        // Vanilla's hover skips sleeping creatures so sleeping monsters don't give themselves away, which
        // also hides a downed tame: no nameplate once its old one is gone (after a logout or a teleport),
        // and the protection toggle can't reach it. When nothing else is under the crosshair, the same 50m
        // raycast is repeated and a downed tame at the front is picked up.
        public static void FindDownedHover(Player player, ref Character hoverCreature)
        {
            if (hoverCreature != null || Downed.Count == 0 || !Plugin.showDownedTames.Value || GameCamera.instance == null) return;

            var camera = GameCamera.instance.transform;
            var count = Physics.RaycastNonAlloc(camera.position, camera.forward, HoverHits, 50f, InteractMask(player));

            Character? closest = null;
            var closestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = HoverHits[i];
                var character = hit.collider.attachedRigidbody != null
                    ? hit.collider.attachedRigidbody.GetComponent<Character>()
                    : hit.collider.GetComponent<Character>();
                if (character == null || character == player || hit.distance >= closestDistance) continue;

                closest = character;
                closestDistance = hit.distance;
            }

            if (closest != null && Downed.Contains(closest)) hoverCreature = closest;
        }

        // Holding the toggle's modifiers turns E into our action on a tame, so vanilla must not also
        // pet or command it with the same press.
        public static bool SwallowsInteract(Tameable tameable)
        {
            return Plugin.toggleProtectionKey.Value.IsPressed() &&
                   tameable.TryGetComponent(out Character character) && character.IsTamed();
        }

        public static void Tick()
        {
            var player = Player.m_localPlayer;
            if (player == null || !TakeInput(player) || !Plugin.toggleProtectionKey.Value.IsDown()) return;

            var target = HoveringCreature(player);
            if (target == null || !target.IsTamed() || !target.TryGetComponent(out MonsterAI _)) return;

            var nview = NView(target);
            if (nview == null || !nview.IsValid()) return;

            ClaimOwnership(nview);
            var zdo = nview.GetZDO();
            var protect = !IsProtected(zdo);
            zdo.Set(ProtectionKey, protect ? 1 : -1);
            player.Message(MessageHud.MessageType.Center, $"{target.GetHoverName()} {(protect ? "protected" : "not protected")}");
        }

        private static string KeyText(KeyboardShortcut shortcut)
        {
            var text = "";
            foreach (var modifier in shortcut.Modifiers) text += KeyName(modifier) + " + ";
            return text + KeyName(shortcut.MainKey);
        }

        private static string KeyName(KeyCode key) => key switch
        {
            KeyCode.LeftAlt or KeyCode.RightAlt => "Alt",
            KeyCode.LeftShift or KeyCode.RightShift => "Shift",
            KeyCode.LeftControl or KeyCode.RightControl => "Ctrl",
            _ => key.ToString()
        };

        private static bool IsProtected(ZDO zdo) => zdo.GetInt(ProtectionKey, 0) switch
        {
            1 => true,
            -1 => false,
            _ => Plugin.protectAllTames.Value
        };

        private static bool KilledWithButcherKnife(Character character)
        {
            return LastHit(character)?.GetAttacker() is Humanoid attacker && attacker.IsPlayer() &&
                   GetCurrentWeapon(attacker)?.m_shared.m_tamedOnly == true;
        }
    }
}
