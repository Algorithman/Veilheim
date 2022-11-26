// Veilheim
// a Valheim mod
// 
// File:    Veilheim.cs
// Project: Veilheim

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Veilheim.AssetManagers;
using Veilheim.Map;
using Veilheim.Patches;


namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class VeilheimPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        public const string PluginName = "Veilheim";
        public const string PluginVersion = "0.50.4";

        // Static instance needed for Coroutines
        public static VeilheimPlugin Instance = null;

        // Unity GameObject as a root to all managers
        internal static GameObject RootObject;

        private void Awake()
        {
            Instance = this;

            CreateConfigBindings();

            ProductionInputAmounts.InitializePatches();
            Map_Patches.InitializePatches();
            NoMinimap.InitializePatches();
            PortalsOnMap.InitializePatches();
            PublicPostion_Patches.InitializePatches();
            SharedMapPatches.InitializePatches();


            // Load assets
            AssetBundle assetBundle;

            assetBundle = AssetUtils.LoadAssetBundleFromResources("configurationgui", typeof(VeilheimPlugin).Assembly);
            LoadGUIPrefab(assetBundle, "ConfigurationEntry");
            LoadGUIPrefab(assetBundle, "ConfigurationSection");
            LoadGUIPrefab(assetBundle, "ConfigurationGUIRoot");
            assetBundle.Unload(false);

            assetBundle = AssetUtils.LoadAssetBundleFromResources("portalselectiongui", typeof(VeilheimPlugin).Assembly);
            LoadGUIPrefab(assetBundle, "PortalButtonBox");
            assetBundle.Unload(false);



            // Root GameObject for all plugin components
            RootObject = new GameObject("_VeilheimPlugin");
            DontDestroyOnLoad(RootObject);

            // Done
            Jotunn.Logger.LogInfo($"{PluginName} v{PluginVersion} loaded");
        }


        /// <summary>
        ///     Load a GUI prefab from a bundle and register it in the <see cref="GUIManager" />.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="assetName"></param>
        private void LoadGUIPrefab(AssetBundle assetBundle, string assetName)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            Jotunn.Managers.PrefabManager.Instance.AddPrefab(prefab);
        }


        private void CreateConfigBindings()
        {
            string section;

            Config.SaveOnConfigSet = true;

            // Section Map
            section = "Map";
            Config.Bind(section, "showPortalsOnMap", false, "Show portals on map");
            Config.Bind(section, "showPortalSelection", false, "Show portal selection window on portal rename");
            Config.Bind(section, "showNoMinimap", false, "Play without minimap");

            // Section MapServer
            section = "MapServer";
            Config.Bind(section, "shareMapProgression", false,
                new ConfigDescription("With this enabled you will receive the same exploration progression as other players on the server", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "exploreRadius", 100f,
                new ConfigDescription("The radius of the map that you explore when moving", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "exploreRadiusSailing", 100f,
                new ConfigDescription("The radius of the map that you explore while sailing", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "playerPositionPublicOnJoin", false,
                new ConfigDescription("Automatically turn on the Map option to share your position when joining or starting a game", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "preventPlayerFromTurningOffPublicPosition", false,
                new ConfigDescription("Prevents you and other people on the server to turn off their map sharing option", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));

            // Section Blueprints
            section = "Blueprints";
            Config.Bind(section, "allowPlacementWithoutMaterial", true,
                new ConfigDescription("Allow placement of blueprints without materials", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));

            // Section ProductionInputAmount
            section = "ProductionInputAmounts";
            Config.Bind(section, "windmillBarleyAmount", 50,
                new ConfigDescription("Max windmill barley amount", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "kilnWoodAmount", 25,
                new ConfigDescription("Max wood amount for kiln", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "furnaceCoalAmount", 20,
                new ConfigDescription("Max coal amount for furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "furnaceOreAmount", 10,
                new ConfigDescription("Max ore amount for furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "blastfurnaceCoalAmount", 20,
                new ConfigDescription("Max coal amount for blast furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "blastfurnaceOreAmount", 10,
                new ConfigDescription("Max ore amount for blast furnace", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
            Config.Bind(section, "spinningWheelFlachsAmount", 40,
                new ConfigDescription("Max flachs amount for spinning wheel", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));
        }
    }
}
