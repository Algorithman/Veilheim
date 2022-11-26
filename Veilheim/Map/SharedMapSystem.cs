﻿// Veilheim
// a Valheim mod
// 
// File:    SharedMapSystem.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zlib;
using Jotunn.Utils;
using UnityEngine;
using Veilheim.Extensions;
using Veilheim.Utils;
using CompressionLevel = Ionic.Zlib.CompressionLevel;
using Logger = Jotunn.Logger;

namespace Veilheim.Map
{
    public class SharedMapPatches
    {
        private static bool isInSetMapData;
        private static readonly List<int> explorationQueue = new List<int>();

        private static bool playerIsOnShip = false;
        private static Ship shipWithPlayer = null;

        [PatchInit(0)]
        public static void InitializePatches()
        {
            On.Ship.OnTriggerExit += RemovePlayerFromBoatingList;
            On.Ship.OnTriggerEnter += AddPlayerToBoatingList;
            On.Minimap.UpdateExplore += SendQueuedExploreData;
            On.Minimap.Explore_int_int += EnqueueExploreData;
            On.Minimap.SetMapData += InitialSendRequest;
            On.Game.Start += Register_RPC_MapSharing;
            On.ZNet.Shutdown += SaveExplorationData;
            On.Minimap.Awake += LoadExplorationData;
        }

        /// <summary>
        ///     Apply other player's locations as own exploration
        /// </summary>
        private static void GetSharedExploration(On.Minimap.orig_UpdateExplore orig, Minimap self, float dt, Player player)
        {

            orig(self, dt, player);
        }

        /// <summary>
        ///     On server, load saved data on minimap awake
        /// </summary>
        private static void LoadExplorationData(On.Minimap.orig_Awake orig, Minimap self)
        {
            orig(self);

            if (ConfigUtil.Get<bool>("MapServer", "shareMapProgression"))
            {
                if (ZNet.instance.IsServerInstance())
                {
                    Minimap.instance.m_explored = new bool[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize];
                    if (File.Exists(Path.Combine(ConfigUtil.GetConfigPath(), ZNet.instance.GetWorldUID().ToString(), "Explorationdata.bin")))
                    {
                        var mapData = ZPackageExtension.ReadFromFile(Path.Combine(ConfigUtil.GetConfigPath(), ZNet.instance.GetWorldUID().ToString(),
                            "Explorationdata.bin"));
                        ApplyMapData(mapData);
                    }
                    else
                    {
                        for (var i = 0; i < Minimap.instance.m_explored.Length; i++)
                        {
                            Minimap.instance.m_explored[i] = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Before ZNet destroy, save data to file on server
        /// </summary>
        private static void SaveExplorationData(On.ZNet.orig_Shutdown orig, ZNet self)
        {
            // Save exploration data only on the server
            if (ZNet.instance.IsServerInstance() && ConfigUtil.Get<bool>("MapServer", "shareMapProgression"))
            {
                Logger.LogInfo($"Saving shared exploration data");
                var mapData = new ZPackage(CreateExplorationData().ToArray());
                mapData.WriteToFile(Path.Combine(ConfigUtil.GetConfigPath(), ZNet.instance.GetWorldUID().ToString(), "Explorationdata.bin"));
            }

            orig(self);
        }

        /// <summary>
        ///     Register needed RPC's
        /// </summary>
        private static void Register_RPC_MapSharing(On.Game.orig_Start orig, Game self)
        {
            // Map data Receive
            try
            {
                ZRoutedRpc.instance.Register(nameof(RPC_Veilheim_ReceiveExploration),
                    new Action<long, ZPackage>(RPC_Veilheim_ReceiveExploration));
            }
            catch (Exception ex)
            {
                Logger.LogInfo(nameof(RPC_Veilheim_ReceiveExploration) + " was already added.");
            }

            try
            {
                ZRoutedRpc.instance.Register(nameof(RPC_Veilheim_ReceiveExploration_OnExplore),
                    new Action<long, ZPackage>(RPC_Veilheim_ReceiveExploration_OnExplore));
            }
            catch (Exception ex)
            {
                Logger.LogInfo(nameof(RPC_Veilheim_ReceiveExploration_OnExplore) + " was already added.");
            }


            // Map data Receive
            try
            {
                ZRoutedRpc.instance.Register(nameof(RPC_Veilheim_ReceiveExploration_ToServer),
                    new Action<long, ZPackage>(RPC_Veilheim_ReceiveExploration_ToServer));
            }
            catch (Exception ex)
            {
                Logger.LogInfo(nameof(RPC_Veilheim_ReceiveExploration_ToServer) + " was already added.");
            }

            try
            {
                ZRoutedRpc.instance.Register(nameof(RPC_Veilheim_ReceiveExploration_OnExplore_ToServer),
                    new Action<long, ZPackage>(RPC_Veilheim_ReceiveExploration_OnExplore_ToServer));
            }
            catch (Exception ex)
            {
                Logger.LogInfo(nameof(RPC_Veilheim_ReceiveExploration_OnExplore_ToServer) + " was already added.");
            }
            orig(self);
        }

        /// <summary>
        ///     After SetMapData is done, send it to the server
        ///     TODO: Check if configuration is loaded already, data should not be sent if map sharing is disabled
        /// </summary>
        private static void InitialSendRequest(On.Minimap.orig_SetMapData orig, Minimap self, byte[] data)
        {
            // Prevent queueing up loaded data
            isInSetMapData = true;

            orig(self, data);

            if (ConfigUtil.Get<bool>("MapServer", "shareMapProgression"))
            {
                Logger.LogInfo("Sending Map data initially to server");
                // After login, send map data to server (and get new map data back)
                var pkg = new ZPackage(CreateExplorationData().ToArray());
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_Veilheim_ReceiveExploration_ToServer), pkg);
            }

            isInSetMapData = false;
        }

        /// <summary>
        ///     Enqueue new exploration data if not added from SetMapData
        /// </summary>
        private static bool EnqueueExploreData(On.Minimap.orig_Explore_int_int orig, Minimap self, int x, int y)
        {
            // Do not explore if we're in the intro phase
            if (Player.m_localPlayer?.InIntro() == true)
            {
                return false;
            }

            bool result = orig(self, x, y);

            if (result && !isInSetMapData)
            {
                lock (explorationQueue)
                {
                    explorationQueue.Add(x + y * Minimap.instance.m_textureSize);
                }
            }

            return result;
        }

        /// <summary>
        ///     Send queued exploration data to server
        /// </summary>
        private static void SendQueuedExploreData(On.Minimap.orig_UpdateExplore orig, Minimap self, float dt, Player player)
        {
            bool doPayload = self.m_exploreTimer + Time.deltaTime > self.m_exploreInterval;

            if (doPayload)
            {
                if (ConfigUtil.Get<bool>("MapServer", "shareMapProgression"))
                {
                    if (doPayload)
                    {
                        var tempPlayerInfos = new List<ZNet.PlayerInfo>();
                        ZNet.instance.GetOtherPublicPlayers(tempPlayerInfos);

                        foreach (var tempPlayer in tempPlayerInfos)
                        {
                            ExploreLocal(tempPlayer.m_position);
                        }
                    }
                }

                if (playerIsOnShip && ConfigUtil.Get<float>("MapServer", "exploreRadiusSailing") > ConfigUtil.Get<float>("MapServer", "exploreRadius"))
                {
                    self.Explore(Player.m_localPlayer.transform.position, ConfigUtil.Get<float>("MapServer", "exploreRadiusSailing"));
                }
                else
                {
                    self.Explore(Player.m_localPlayer.transform.position, ConfigUtil.Get<float>("MapServer", "exploreRadius"));
                }
                self.m_exploreTimer = 0f;
            }
            else
            {
                orig(self, dt, player);
            }

            // disregard mini changes for now, lets build up some first
            if (explorationQueue.Count >= 10 && doPayload)
            {
                Logger.LogDebug($"UpdateExplore - sending newly explored locations to server ({explorationQueue.Count})");

                var toSend = new List<int>();
                lock (explorationQueue)
                {
                    toSend.AddRange(explorationQueue.Distinct());
                    explorationQueue.Clear();
                }

                var queueData = new ZPackage();
                queueData.Write(toSend.Count);
                foreach (var data in toSend)
                {
                    queueData.Write(data);
                }

                // Invoke RPC on server and send data
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_Veilheim_ReceiveExploration_OnExplore_ToServer), queueData);
            }
        }

        private static void AddPlayerToBoatingList(On.Ship.orig_OnTriggerEnter orig, Ship self, Collider collider)
        {
            orig(self, collider);

            if (self.m_players.Contains(Player.m_localPlayer))
            {
                Logger.LogDebug("Player entered ship");
                playerIsOnShip |= self.m_players.Contains(Player.m_localPlayer);
                shipWithPlayer = self;
            }
        }

        private static void RemovePlayerFromBoatingList(On.Ship.orig_OnTriggerExit orig, Ship self, Collider collider)
        {
            orig(self, collider);

            if (self == shipWithPlayer)
            {
                Logger.LogDebug("Player exited ship");
                playerIsOnShip = false;
                shipWithPlayer = null;
            }
        }

        /// <summary>
        ///     Apply sent exploration data to local map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mapData"></param>
        public static void RPC_Veilheim_ReceiveExploration(long sender, ZPackage mapData)
        {
            if (mapData == null)
            {
                return;
            }

            if (ZNet.instance.IsClientInstance())
            {
                Logger.LogInfo("Received map data from server");

                // Set flag to prevent enqueuing again for sending, since it can be new
                ApplyMapData(mapData);
            }
        }

        /// <summary>
        ///     Apply sent exploration data to local map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mapData"></param>
        public static void RPC_Veilheim_ReceiveExploration_ToServer(long sender, ZPackage mapData)
        {
            if (mapData == null)
            {
                return;
            }

            if (ZNet.instance.IsServerInstance())
            {
                Logger.LogInfo($"Received map data from client #{sender}");
                ApplyMapData(mapData);

                var tempPlayerInfos = new List<ZNet.PlayerInfo>();
                ZNet.instance.GetOtherPublicPlayers(tempPlayerInfos);

                var newMapData = new ZPackage(CreateExplorationData());

                foreach (var player in tempPlayerInfos)
                {
                    long playerUid = ZNet.instance.GetPeerByPlayerName(player.m_name).m_uid;
                    if (playerUid != sender)
                    {
                        Logger.LogInfo($"Sending map data to player {player.m_name} #{ZNet.instance.GetPeerByPlayerName(player.m_name)}");
                        ZRoutedRpc.instance.InvokeRoutedRPC(playerUid, nameof(RPC_Veilheim_ReceiveExploration), newMapData);
                    }
                }
            }
        }

        /// <summary>
        ///     Apply exploration data to local map (server and client)
        /// </summary>
        /// <param name="mapData"></param>
        public static void ApplyMapData(ZPackage mapData)
        {
            try
            {
                var isServer = ZNet.instance.IsServerInstance();
                mapData.SetPos(0);
                using (var gz = new ZlibStream(mapData.m_stream, CompressionMode.Decompress))
                {
                    using (var br = new BinaryReader(gz))
                    {
                        var state = br.ReadBoolean();
                        var idx = 0;

                        bool applyFog = false;

                        while (idx < Minimap.instance.m_explored.Length)
                        {
                            var count = br.ReadInt32();
                            while (count > 0)
                            {
                                if (state && !isServer)
                                {
                                    // Use local helper to prevent enqueuing again for sending
                                    ExploreLocal(idx % Minimap.instance.m_textureSize, idx / Minimap.instance.m_textureSize);
                                }
                                else if (state)
                                {
                                    applyFog |= state && !Minimap.instance.m_explored[idx];
                                    Minimap.instance.m_explored[idx] |= state;
                                }

                                idx++;
                                count--;
                            }

                            state = !state;
                        }

                        if (!isServer)
                        {
                            Minimap.instance.m_fogTexture.Apply();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Application of mapdata gone wrong.{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                Logger.LogError("Texture size: " + Minimap.instance.m_textureSize);
            }
        }

        /// <summary>
        ///     RPC to receive new exploration data from client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mapData"></param>
        public static void RPC_Veilheim_ReceiveExploration_OnExplore(long sender, ZPackage mapData)
        {
            if (mapData == null)
            {
                return;
            }

            if (ZNet.instance.IsServerInstance())
            {
                var numberOfEntries = mapData.ReadInt();
                Logger.LogInfo($"Received exploration diff data from client #{sender}, {numberOfEntries} items");

                while (numberOfEntries > 0)
                {
                    var toExplore = mapData.ReadInt();
                    Minimap.instance.m_explored[toExplore] = true;
                    numberOfEntries--;
                }
            }
            else
            {
                var numberOfEntries = mapData.ReadInt();
                Logger.LogInfo($"Received exploration diff data from server #{sender}, {numberOfEntries} items");

                bool applyFog = false;

                while (numberOfEntries > 0)
                {
                    var toExplore = mapData.ReadInt();
                    applyFog |= !Minimap.instance.m_explored[toExplore];
                    Minimap.instance.m_explored[toExplore] = true;

                    int x = toExplore % Minimap.instance.m_textureSize;
                    int y = toExplore / Minimap.instance.m_textureSize;

                    Color pixel = Minimap.instance.m_fogTexture.GetPixel(x, y);
                    pixel.r = 0f;
                    Minimap.instance.m_fogTexture.SetPixel(x, y, pixel);
                    
                    numberOfEntries--;
                }

                if (applyFog)
                {
                    Logger.LogInfo("Applying FOG");
                    Minimap.instance.m_fogTexture.Apply();
                }
            }
        }

        /// <summary>
        ///     RPC to receive new exploration data from client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mapData"></param>
        public static void RPC_Veilheim_ReceiveExploration_OnExplore_ToServer(long sender, ZPackage mapData)
        {
            if (mapData == null)
            {
                return;
            }

            if (ZNet.instance.IsServerInstance())
            {
                var numberOfEntries = mapData.ReadInt();
                Logger.LogInfo($"Received exploration diff data from client #{sender}, {numberOfEntries} items");

                while (numberOfEntries > 0)
                {
                    var toExplore = mapData.ReadInt();
                    Minimap.instance.m_explored[toExplore] = true;
                    numberOfEntries--;
                }

                mapData.SetPos(0);

                var tempPlayerInfos = new List<ZNet.PlayerInfo>();
                ZNet.instance.GetOtherPublicPlayers(tempPlayerInfos);


                foreach (var player in tempPlayerInfos)
                {
                    long playerUid = ZNet.instance.GetPeerByPlayerName(player.m_name).m_uid;
                    if (playerUid != sender)
                    {
                        Logger.LogInfo($"Sending map data to player {player.m_name} #{ZNet.instance.GetPeerByPlayerName(player.m_name)}");
                        ZRoutedRpc.instance.InvokeRoutedRPC(playerUid, nameof(RPC_Veilheim_ReceiveExploration_OnExplore), mapData);
                    }
                }

            }
        }

        // Helpers (copied from original assembly) to prevent enqueuing unneeded exploration data
        public static void ExploreLocal(Vector3 position)
        {
            var num = (int)Mathf.Ceil(ConfigUtil.Get<float>("MapServer", "exploreRadius") / Minimap.instance.m_pixelSize);
            var flag = false;
            int num2;
            int num3;
            Minimap.instance.WorldToPixel(position, out num2, out num3);
            for (var i = num3 - num; i <= num3 + num; i++)
            {
                for (var j = num2 - num; j <= num2 + num; j++)
                {
                    if (j >= 0 && i >= 0 && j < Minimap.instance.m_textureSize && i < Minimap.instance.m_textureSize &&
                        new Vector2(j - num2, i - num3).magnitude <= num && ExploreLocal(j, i))
                    {
                        flag = true;
                    }
                }
            }

            if (flag)
            {
                Minimap.instance.m_fogTexture.Apply();
            }
        }

        // Second helper
        public static bool ExploreLocal(int x, int y)
        {
            if (Minimap.instance.m_explored[y * Minimap.instance.m_textureSize + x])
            {
                return false;
            }

            Minimap.instance.m_fogTexture.SetPixel(x, y, new Color32(0, 0, 0, 0));
            Minimap.instance.m_explored[y * Minimap.instance.m_textureSize + x] = true;
            return true;
        }

        /// <summary>
        ///     Create compressed byte array
        /// </summary>
        /// <returns>compressed data</returns>
        public static byte[] CreateExplorationData()
        {
            var result = new MemoryStream();
            using (var gz = new ZlibStream(result, CompressionMode.Compress, CompressionLevel.BestCompression))
            {
                using (var binaryWriter = new BinaryWriter(gz))
                {
                    var idx = 0;

                    var state = Minimap.instance.m_explored[0];

                    binaryWriter.Write(state);
                    var count = 0;


                    var length = Minimap.instance.m_explored.Length;
                    while (idx < length)
                    {
                        while (idx < length && state == Minimap.instance.m_explored[idx])
                        {
                            count++;
                            idx++;
                        }

                        state = !state;
                        binaryWriter.Write(count);
                        count = 0;
                    }

                    binaryWriter.Flush();
                    //  gz.Flush();
                }

                return result.ToArray();
            }
        }
    }
}