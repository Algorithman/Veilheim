﻿// Veilheim
// a Valheim mod
// 
// File:    AssetCreator.cs
// Project: Veilheim

using System.Collections.Generic;
using UnityEngine;

namespace Veilheim.AssetUtils
{
    public static class AssetCreator
    {
        public static Dictionary<string, CraftingStation> CraftingStations;

        public static T RequireComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
            }

            return c;
        }

        public static GameObject CreatePiece(string name)
        {
            var ret = new GameObject(name);
            Object.DontDestroyOnLoad(ret);

            // Create basic Components
            var znet = RequireComponent<ZNetView>(ret);
            znet.m_persistent = true;
            znet.m_type = ZDO.ObjectType.Default;

            var piece = RequireComponent<Piece>(ret);

            return ret;
        }

        public static GameObject ClonePiece(string name, string item)
        {
            var orig = ObjectDB.instance.GetItemPrefab(item);
            if (orig == null)
            {
                Logger.LogWarning($"Could not find item prefab ({item})");
                return null;
            }

            var clone = new GameObject(name);
            Object.DontDestroyOnLoad(clone);

            //TODO: clone all needed values
            var origPiece = orig.GetComponent<Piece>();
            var newPiece = RequireComponent<Piece>(clone);


            return clone;
        }
    }
}