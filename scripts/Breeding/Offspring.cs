using UnityEngine;

namespace BetterTames.Scripts.Breeding
{
    public static class Offspring
    {
        private const int MaxLevel = 3;

        // The parent whose Procreate is running. The newborn only exists as a local inside that method, but
        // its one SetLevel call happens while this is set, which is how the newborn is reached.
        private static Character? birthingParent;

        public static void BeginProcreate(Procreation procreation)
        {
            birthingParent = procreation.GetComponent<Character>();
        }

        public static void EndProcreate()
        {
            birthingParent = null;
        }

        public static void AdjustNewbornLevel(Character character, ref int level)
        {
            if (birthingParent == null || character == birthingParent) return;
            birthingParent = null;

            if (level >= MaxLevel) return;
            if (Random.Range(0f, 100f) < Plugin.offspringStarChance.Value) level++;
        }
    }
}
