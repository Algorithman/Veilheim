﻿// Veilheim
// a Valheim mod
// 
// File:    FlattenTerrain.cs
// Project: Veilheim

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Veilheim.Blueprints
{
    internal class FlattenTerrain
    {
        public static void Flatten(Transform transform, Vector2 floorSize, List<PieceEntry> pieces)
        {
            Logger.LogDebug($"Entered FlattenTerrain {transform} / {floorSize} with {pieces.Count}");

            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                var lowestY = pieces.Min(x => x.posY);
                var startPosition = transform.position + Vector3.down * 0.5f;
                var rotation = transform.rotation;

                var forward = 0f;

                while (forward < floorSize.y)
                {
                    var right = 0f;
                    while (right < floorSize.x)
                    {
                        var lowestAtPosition = pieces.OrderBy(x => x.posY)
                            .FirstOrDefault(x => Math.Abs(x.posX - forward) < 4f && Math.Abs(x.posZ - right) < 4f);
                        if (lowestAtPosition != null)
                        {
                            Debug.Log("Lowest: " + lowestAtPosition.posY);

                            Object.Instantiate(groundPrefab,
                                startPosition + transform.forward * forward + transform.right * right + new Vector3(0, lowestAtPosition.posY, 0), rotation);
                        }

                        right++;
                    }

                    forward++;
                }
            }
        }

        public static void FlattenForBlueprint(Transform transform, float maxX, float maxZ)
        {
            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                var forward = -0.5f;
                while (forward < maxZ+1f)
                {
                    var right = -0.5f;
                    while (right < maxX+1f)
                    {
                        Object.Instantiate(groundPrefab, transform.position + transform.forward * forward + transform.right * right + new Vector3(0, -0.5f, 0), transform.rotation);
                        right++;
                    }

                    forward++;

                }
            }
        }
    }
}