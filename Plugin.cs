using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using BetterTames.Scripts.CatchUp;
using BetterTames.Scripts.Commands;
using BetterTames.Scripts.Leveling;
using BetterTames.Scripts.Tuning;
using UnityEngine;

namespace BetterTames
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> showTamingTimer;
        public static ConfigEntry<bool> showBreedingTimer;
        public static ConfigEntry<bool> showGrowthTimer;

        public static ConfigEntry<int> tamingTime;

        public static ConfigEntry<int> lovePoints;
        public static ConfigEntry<int> growthTime;
        public static ConfigEntry<int> maxCreaturesNearby;
        public static ConfigEntry<float> maxCreaturesRange;
        public static ConfigEntry<int> offspringStarChance;

        public static ConfigEntry<bool> commandAllTames;
        public static ConfigEntry<KeyboardShortcut> massFollowKey;
        public static ConfigEntry<KeyboardShortcut> massStayKey;
        public static ConfigEntry<float> massCommandRange;
        public static ConfigEntry<bool> tamesFightOwnKind;

        public static ConfigEntry<bool> catchUpTeleport;
        public static ConfigEntry<float> catchUpDistance;
        public static ConfigEntry<KeyboardShortcut> teleportFollowersKey;
        public static ConfigEntry<bool> bigTamesEnterDungeons;

        public static ConfigEntry<bool> starUpOnKill;
        public static ConfigEntry<int> starUpBaseChance;
        public static ConfigEntry<int> starUpBonusPerStar;
        public static ConfigEntry<bool> healOnStarUp;

        public static ConfigEntry<int> regenTime;
        public static ConfigEntry<bool> protectAllTames;
        public static ConfigEntry<int> recoveryTime;
        public static ConfigEntry<KeyboardShortcut> toggleProtectionKey;
        public static ConfigEntry<bool> showDownedTames;

        public static ConfigEntry<bool> wildEatFromChests;
        public static ConfigEntry<bool> tamesEatFromChests;
        public static ConfigEntry<float> chestRange;
        public static ConfigEntry<bool> chestRequireOnlyFood;

        public static ConfigEntry<bool> debugMode;

        public new static readonly ManualLogSource Logger =
            BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

        private static readonly ConfigSync configSync = new(PluginInfo.PLUGIN_GUID)
        {
            DisplayName = PluginInfo.PLUGIN_NAME,
            CurrentVersion = PluginInfo.PLUGIN_VERSION,
            MinimumRequiredVersion = PluginInfo.PLUGIN_VERSION
        };

        private void Awake()
        {
            InitializeConfig();
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_VERSION} loaded");
        }

        private void Update()
        {
            Commands.Tick();
            CatchUp.Tick();
            Scripts.Protection.Protection.Tick();
        }

        private ConfigEntry<T> ConfigSync<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            var configDescription = new ConfigDescription(
                description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
            var configEntry = Config.Bind(group, name, value, configDescription);
            configSync.AddConfigEntry(configEntry).SynchronizedConfig = synchronizedSetting;
            return configEntry;
        }

        private void InitializeConfig()
        {
            Config.SaveOnConfigSet = false;

            var serverConfigLocked = ConfigSync("1 - General", "Lock Configuration", true,
                new ConfigDescription(
                    "If enabled, the configuration is locked and can be changed by server admins only."));
            configSync.AddLockingConfigEntry(serverConfigLocked);

            showTamingTimer = ConfigSync("2 - Timers", "Show Taming Timer", true,
                new ConfigDescription(
                    "Show how long a creature you are taming has left, counting any taming boost from players nearby. The timer is marked paused while the creature is hungry or frightened, since taming stops then."), false);

            showBreedingTimer = ConfigSync("2 - Timers", "Show Breeding Timer", true,
                new ConfigDescription(
                    "Show how long a pregnant tame has left before giving birth. A tame that is not pregnant shows its love points instead: it gains one now and then while fed, calm and next to a partner, and gets pregnant when they are full."), false);

            showGrowthTimer = ConfigSync("2 - Timers", "Show Growth Timer", true,
                new ConfigDescription(
                    "Show how long offspring have left before growing up."), false);

            tamingTime = ConfigSync("3 - Taming", "Taming Time", 30,
                new ConfigDescription(
                    "Minutes of feeding it takes to tame any creature.",
                    new AcceptableValueRange<int>(1, 180)));

            lovePoints = ConfigSync("4 - Breeding", "Love Points Needed", 3,
                new ConfigDescription(
                    "Love points a tame needs before it gets pregnant. It gains one now and then while fed, calm and next to a partner.",
                    new AcceptableValueRange<int>(1, 20)));

            growthTime = ConfigSync("4 - Breeding", "Growth Time", 30,
                new ConfigDescription(
                    "Minutes offspring take to grow up.",
                    new AcceptableValueRange<int>(1, 180)));

            maxCreaturesNearby = ConfigSync("4 - Breeding", "Crowd Limit", 6,
                new ConfigDescription(
                    "How many creatures of the same kind can be nearby before breeding stops.",
                    new AcceptableValueRange<int>(1, 20)));

            maxCreaturesRange = ConfigSync("4 - Breeding", "Crowd Range", 10f,
                new ConfigDescription(
                    "How far, in meters, Crowd Limit counts around a breeding pair.",
                    new AcceptableValueRange<float>(1f, 200f)));

            offspringStarChance = ConfigSync("4 - Breeding", "Offspring Star Chance", 25,
                new ConfigDescription(
                    "Percent chance a newborn is one star above the tame giving birth, up to 2 stars. Eggs are not affected.",
                    new AcceptableValueRange<int>(0, 100)));

            tamingTime.SettingChanged += (_, _) => Tuning.ApplyAll();
            lovePoints.SettingChanged += (_, _) => Tuning.ApplyAll();
            growthTime.SettingChanged += (_, _) => Tuning.ApplyAll();
            maxCreaturesNearby.SettingChanged += (_, _) => Tuning.ApplyAll();
            maxCreaturesRange.SettingChanged += (_, _) => Tuning.ApplyAll();

            commandAllTames = ConfigSync("5 - Commands", "Command Any Tame", true,
                new ConfigDescription(
                    "Press E on any tame to make it follow you or stay, not only on wolves, lox and the others the game allows. Off leaves each creature as the game or its mod made it."));

            massFollowKey = ConfigSync("5 - Commands", "Follow All Key", new KeyboardShortcut(KeyCode.F, KeyCode.LeftAlt),
                new ConfigDescription(
                    "Every tame in range that is not following anyone starts following you."), false);

            massStayKey = ConfigSync("5 - Commands", "Stay All Key", new KeyboardShortcut(KeyCode.G, KeyCode.LeftAlt),
                new ConfigDescription(
                    "Every tame in range that is following you stays where it is."), false);

            massCommandRange = ConfigSync("5 - Commands", "Follow/Stay All Range", 30f,
                new ConfigDescription(
                    "How far, in meters, the Follow All and Stay All keys reach.",
                    new AcceptableValueRange<float>(5f, 100f)));

            tamesFightOwnKind = ConfigSync("1 - General", "Tames Fight Their Own Kind", true,
                new ConfigDescription(
                    "A tame fights wild creatures of its own kind that are in a fight, like a tamed wolf defending you from wild wolves. Calm ones, and ones you are partway through taming, are left alone. The game normally keeps them on the same side."));

            commandAllTames.SettingChanged += (_, _) => Commands.ApplyAll();

            catchUpTeleport = ConfigSync("6 - Teleport", "Teleport Lost Tames", true,
                new ConfigDescription(
                    "A tame that is following you teleports to you if it falls too far behind, and after you die and respawn. Vanilla following has no distance limit, so a tame stuck on terrain or left behind at your death is otherwise stuck or lost for good."));

            catchUpDistance = ConfigSync("6 - Teleport", "Teleport Distance", 30f,
                new ConfigDescription(
                    "How far, in meters, a following tame can fall behind before it teleports to you.",
                    new AcceptableValueRange<float>(20f, 50f)));

            teleportFollowersKey = ConfigSync("6 - Teleport", "Teleport Tames Key", new KeyboardShortcut(KeyCode.H, KeyCode.LeftAlt),
                new ConfigDescription(
                    "Every tame following you teleports to you right away."), false);

            bigTamesEnterDungeons = ConfigSync("6 - Teleport", "Big Tames Enter Dungeons", false,
                new ConfigDescription(
                    "Following tames come with you into dungeons and back out. Turn off to leave big tames, like lox, outside, since they get stuck in narrow corridors."));

            starUpOnKill = ConfigSync("7 - Leveling", "Gain Stars From Kills", true,
                new ConfigDescription(
                    "A tame has a chance to gain a star whenever it kills an enemy, up to 2 stars. The chance is higher the more stars the enemy has over the tame; killing something the same star or lower gives no bonus."));

            starUpBaseChance = ConfigSync("7 - Leveling", "Star Chance", 10,
                new ConfigDescription(
                    "Base percent chance to gain a star on a kill.",
                    new AcceptableValueRange<int>(0, 100)));

            starUpBonusPerStar = ConfigSync("7 - Leveling", "Bonus Chance Per Star", 5,
                new ConfigDescription(
                    "Percent added per star the killed enemy has over the tame.",
                    new AcceptableValueRange<int>(0, 100)));

            healOnStarUp = ConfigSync("7 - Leveling", "Heal On New Star", true,
                new ConfigDescription(
                    "A tame that gains a star from a kill is healed to full."));

            regenTime = ConfigSync("8 - Health", "Full Heal Time", 3600,
                new ConfigDescription(
                    "Seconds a tame takes to heal from empty to full. Lower is faster. Regeneration pauses while a tame is hungry, the same as the game's own creatures.",
                    new AcceptableValueRange<int>(60, 7200)));

            regenTime.SettingChanged += (_, _) => Tuning.ApplyAll();

            protectAllTames = ConfigSync("8 - Health", "Protect All Tames", true,
                new ConfigDescription(
                    "A tame that would die lies down to recover instead. Enemies leave it alone while it is down. A butcher knife still kills it. Individual tames can be switched with the Protection Key."));

            recoveryTime = ConfigSync("8 - Health", "Recovery Time", 60,
                new ConfigDescription(
                    "Seconds a recovering tame rests, healing back to full, before it gets up.",
                    new AcceptableValueRange<int>(10, 600)));

            toggleProtectionKey = ConfigSync("8 - Health", "Protection Key", new KeyboardShortcut(KeyCode.E, KeyCode.LeftAlt),
                new ConfigDescription(
                    "Switch protection on or off for the tame you are looking at, overriding Protect All Tames for that tame."), false);

            showDownedTames = ConfigSync("8 - Health", "Show Recovering Tames", true,
                new ConfigDescription(
                    "Show the name and health bar of a recovering tame when you look at it. The game hides sleeping creatures from the crosshair, so with this off a recovering tame shows no name or health bar and the Protection Key can't reach it."), false);

            wildEatFromChests = ConfigSync("9 - Feeding", "Wild Creatures Eat From Chests", true,
                new ConfigDescription(
                    "A hungry creature you are taming eats from a nearby chest when there is no food on the ground, so taming keeps going without dropping food by hand."));

            tamesEatFromChests = ConfigSync("9 - Feeding", "Tames Eat From Chests", true,
                new ConfigDescription(
                    "A hungry tame eats from a nearby chest when there is no food on the ground, which keeps it healing and breeding."));

            chestRange = ConfigSync("9 - Feeding", "Chest Range", 10f,
                new ConfigDescription(
                    "How far, in meters, a creature reaches for a chest. It eats in place and never walks to it. Only chests a player built count, not dungeon or other world loot.",
                    new AcceptableValueRange<float>(2f, 30f)));

            chestRequireOnlyFood = ConfigSync("9 - Feeding", "Only Food Chests", false,
                new ConfigDescription(
                    "Only eat from a chest that holds nothing but food this creature eats, so a feeding chest can sit next to your storage without being raided."));

            debugMode = ConfigSync("Debug", "Debug Mode", false,
                new ConfigDescription(
                    "Log what the mod is doing to tamed creatures."), false);

            Config.SaveOnConfigSet = true;
            Config.Save();
        }
    }
}
