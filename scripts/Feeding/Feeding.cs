using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterTames.Scripts.Feeding
{
    public static class Feeding
    {
        private static readonly AccessTools.FieldRef<MonsterAI, List<ItemDrop>> ConsumeItems =
            AccessTools.FieldRefAccess<MonsterAI, List<ItemDrop>>("m_consumeItems");

        private static readonly AccessTools.FieldRef<MonsterAI, Action<ItemDrop>> OnConsumedItem =
            AccessTools.FieldRefAccess<MonsterAI, Action<ItemDrop>>("m_onConsumedItem");

        private static readonly AccessTools.FieldRef<BaseAI, ZSyncAnimation> Animator =
            AccessTools.FieldRefAccess<BaseAI, ZSyncAnimation>("m_animator");

        private static readonly AccessTools.FieldRef<Humanoid, EffectList> ConsumeEffects =
            AccessTools.FieldRefAccess<Humanoid, EffectList>("m_consumeItemEffects");

        private static readonly Func<Container, Inventory> GetInventory =
            AccessTools.MethodDelegate<Func<Container, Inventory>>(AccessTools.Method(typeof(Container), "GetInventory"));

        private static readonly List<Container> Chests = new();

        public static void Register(Container container)
        {
            if (!Chests.Contains(container)) Chests.Add(container);
        }

        // Called only when vanilla went looking for food on the ground and found none. That search already
        // runs on the creature's own interval, only while it is hungry, and never mid-fight.
        public static void TryEatFromChest(MonsterAI monsterAI)
        {
            if (!monsterAI.TryGetComponent(out Character character) || !monsterAI.TryGetComponent(out Tameable _)) return;
            if (!(character.IsTamed() ? Plugin.tamesEatFromChests.Value : Plugin.wildEatFromChests.Value)) return;

            var nview = monsterAI.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || !nview.IsOwner()) return;

            var consumeItems = ConsumeItems(monsterAI);
            if (consumeItems == null || consumeItems.Count == 0) return;

            var position = character.transform.position;
            var range = Plugin.chestRange.Value;

            for (var i = Chests.Count - 1; i >= 0; i--)
            {
                var chest = Chests[i];
                if (chest == null)
                {
                    Chests.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(chest.transform.position, position) > range) continue;
                if (!IsPlayerChest(chest, out var chestView) || !chestView.IsOwner()) continue;

                var inventory = GetInventory(chest);
                if (inventory == null) continue;

                if (!FindFood(inventory, consumeItems, out var food, out var foodPrefab)) continue;

                inventory.RemoveItem(food, 1);
                Animator(monsterAI)?.SetTrigger("consume");
                if (character is Humanoid humanoid) ConsumeEffects(humanoid)?.Create(position, Quaternion.identity);
                OnConsumedItem(monsterAI)?.Invoke(foodPrefab);
                return;
            }
        }

        // Only chests a player built: dungeon and other world loot chests have no creator.
        private static bool IsPlayerChest(Container chest, out ZNetView chestView)
        {
            chestView = chest.GetComponent<ZNetView>();
            return chestView != null && chestView.IsValid() && chestView.GetZDO().GetLong(ZDOVars.s_creator, 0L) != 0L;
        }

        private static bool FindFood(Inventory inventory, List<ItemDrop> consumeItems,
            out ItemDrop.ItemData food, out ItemDrop foodPrefab)
        {
            food = null!;
            foodPrefab = null!;

            foreach (var item in inventory.GetAllItems())
            {
                var prefab = Match(item, consumeItems);
                if (prefab == null)
                {
                    if (Plugin.chestRequireOnlyFood.Value) return false;
                    continue;
                }

                if (food == null)
                {
                    food = item;
                    foodPrefab = prefab;
                    if (!Plugin.chestRequireOnlyFood.Value) return true;
                }
            }

            return food != null;
        }

        private static ItemDrop? Match(ItemDrop.ItemData item, List<ItemDrop> consumeItems)
        {
            foreach (var consumeItem in consumeItems)
            {
                if (consumeItem.m_itemData.m_shared.m_name == item.m_shared.m_name) return consumeItem;
            }
            return null;
        }
    }
}
