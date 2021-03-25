﻿// Veilheim
// a Valheim mod
// 
// File:    RecipeDef.cs
// Project: Veilheim

using System.Collections.Generic;
using UnityEngine;
using Veilheim.AssetManagers;

namespace Veilheim.AssetEntities
{
    /// <summary>
    ///     A wrapper class representing certain references to Valheim objects and attributes of
    ///     <see cref="Recipe" />s as primitives. Must be instantiated for every item prefab that 
    ///     you want to register. The actual objects are instantiated and referenced at runtime.
    /// </summary>
    internal class RecipeDef
    {
        public string Name { get; set; } = null;
        public string Item { get; set; } = null;
        public int Amount { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public string CraftingStation { get; set; } = null;
        public string RepairStation { get; set; } = null;
        public int MinStationLevel { get; set; }
        public RequirementDef[] Requirements { get; set; } = new RequirementDef[0];

        public Recipe GetRecipe()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Requirements[i].GetPieceRequirement();
            }

            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();

            if (Item == null)
            {
                Logger.LogError("Error, recipe contains no item");
                return null;
            }

            GameObject itemPrefab = PrefabManager.Instance.GetPrefab(Item);

            if (itemPrefab == null)
            {
                Logger.LogError("Error, recipe contained null item prefab for item: " + Item);
                return null;
            }

            if (string.IsNullOrEmpty(Name))
            {
                Name = "Recipe_" + Item;
            }

            recipe.name = Name;
            recipe.m_item = itemPrefab.GetComponent<ItemDrop>();
            recipe.m_amount = Amount;
            recipe.m_enabled = Enabled;

            if (CraftingStation != null)
            {
                GameObject craftingStationPrefab = PrefabManager.Instance.GetPrefab(CraftingStation);
                CraftingStation craftingStation = craftingStationPrefab.GetComponent<CraftingStation>();

                if (craftingStationPrefab == null || craftingStation == null)
                {
                    Logger.LogError("Crafting station is not valid: " + CraftingStation);
                    return null;
                }

                recipe.m_craftingStation = craftingStation;
            }

            if (RepairStation != null)
            {
                GameObject repairStationPrefab = PrefabManager.Instance.GetPrefab(RepairStation);
                CraftingStation repairStation = repairStationPrefab.GetComponent<CraftingStation>();

                if (repairStationPrefab == null || repairStation == null)
                {
                    Logger.LogError("Repair station is not valid: " + RepairStation);
                    return null;
                }

                recipe.m_craftingStation = repairStation;
            }

            recipe.m_minStationLevel = MinStationLevel;
            recipe.m_resources = reqs;

            return recipe;
        }
    }
}