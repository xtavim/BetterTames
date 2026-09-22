using HarmonyLib;

namespace BetterTames.Scripts.Tuning
{
    public static class Tuning
    {
        private static readonly AccessTools.FieldRef<Tameable, ZNetView> TameView =
            AccessTools.FieldRefAccess<Tameable, ZNetView>("m_nview");

        private static readonly AccessTools.FieldRef<Character, float> RegenAllHpTime =
            AccessTools.FieldRefAccess<Character, float>("m_regenAllHPTime");

        public static void Apply(Tameable tameable)
        {
            tameable.m_tamingTime = Plugin.tamingTime.Value * 60f;

            // Progress is saved as time left, so a creature partway through a longer taming keeps too much.
            var nview = TameView(tameable);
            if (nview == null || !nview.IsValid() || !nview.IsOwner()) return;
            var zdo = nview.GetZDO();
            if (zdo.GetFloat(ZDOVars.s_tameTimeLeft, tameable.m_tamingTime) > tameable.m_tamingTime)
                zdo.Set(ZDOVars.s_tameTimeLeft, tameable.m_tamingTime);
        }

        public static void Apply(Procreation procreation)
        {
            procreation.m_requiredLovePoints = Plugin.lovePoints.Value;
            procreation.m_maxCreatures = Plugin.maxCreaturesNearby.Value;
            procreation.m_totalCheckRange = Plugin.maxCreaturesRange.Value;
        }

        public static void Apply(Growup growup)
        {
            growup.m_growTime = Plugin.growthTime.Value * 60f;
        }

        // Only tamed creatures are touched; a wild creature's own regen is left as the game made it.
        public static void Apply(Character character)
        {
            RegenAllHpTime(character) = Plugin.regenTime.Value;
        }

        public static void ApplyAll()
        {
            foreach (var character in Character.GetAllCharacters())
            {
                if (character.TryGetComponent(out Tameable tameable)) Apply(tameable);
                if (character.TryGetComponent(out Procreation procreation)) Apply(procreation);
                if (character.TryGetComponent(out Growup growup)) Apply(growup);
                if (character.IsTamed()) Apply(character);
            }
        }
    }
}
