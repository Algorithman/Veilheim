// Veilheim
// a Valheim mod
// 
// File:    Veilheim.cs
// Project: Veilheim

using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Utils;
using UnityEngine;
using Veilheim.AssetManagers;
using Veilheim.Patches;

namespace Veilheim
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class VeilheimPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.valheim.veilheim";
        public const string PluginName = "Veilheim";
        public const string PluginVersion = "0.4.0";

        // Static instance needed for Coroutines
        public static VeilheimPlugin Instance = null;

        // Unity GameObject as a root to all managers
        internal static GameObject RootObject;

        // Load order for managers
        private readonly List<Type> managerTypes = new List<Type>()
        {
            typeof(GUIManager),
        };

        // List of all managers
        private readonly List<Manager> managers = new List<Manager>();

        private void Awake()
        {
            Instance = this;

            CreateConfigBindings();

            // Root GameObject for all plugin components
            RootObject = new GameObject("_VeilheimPlugin");
            DontDestroyOnLoad(RootObject);

            // Create and initialize all managers
            foreach (Type managerType in managerTypes)
            {
                managers.Add((Manager)RootObject.AddComponent(managerType));
            }

            foreach (Manager manager in managers)
            {
                manager.Init();
            }

            On.ZNet.RPC_ClientHandshake += ZNet_RPC_ClientHandshake;

            GhostRotationTranspiler.PatchUpdatePlacementGhost();

            // Done
            Jotunn.Logger.LogInfo($"{PluginName} v{PluginVersion} loaded");
        }

        private void ZNet_RPC_ClientHandshake(On.ZNet.orig_RPC_ClientHandshake orig, ZNet self, ZRpc rpc, bool needPassword)
        {
            if (Environment.GetCommandLineArgs().Any(x => x.ToLower() == "+password"))
            {
                var args = Environment.GetCommandLineArgs();

                // find password argument index
                var index = 0;
                while (index < args.Length && args[index].ToLower() != "+password")
                {
                    index++;
                }

                index++;

                // is there a password after +password?
                if (index < args.Length)
                {
                    // do normal handshake
                    self.m_connectingDialog.gameObject.SetActive(false);
                    self.SendPeerInfo(rpc, args[index]);
                    return;
                }
            }

            orig(self, rpc, needPassword);
        }

        private void CreateConfigBindings()
        {
            string section;

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
