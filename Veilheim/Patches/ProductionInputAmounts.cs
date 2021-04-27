﻿// Veilheim
// a Valheim mod
// 
// File:    ProductionInputAmounts.cs
// Project: Veilheim

using Jotunn.Utils;
using Veilheim.Configurations;
using Veilheim.PatchEvents;

namespace Veilheim.Patches
{
    public class ProductionInputAmounts : IPatchEventConsumer
    {
        [PatchInit(0)]
        public static void InitializePatches()
        {
            On.Smelter.Awake += SetSmelterInputAmounts;
        }

        private static void SetSmelterInputAmounts(On.Smelter.orig_Awake orig, Smelter self)
        {
            orig(self);

            if (Configuration.Current.ProductionInputAmounts.IsEnabled)
            {
                var prefab = self.m_nview.GetPrefabName();
                if (prefab == "piece_spinningwheel")
                {
                    self.m_maxOre = Configuration.Current.ProductionInputAmounts.spinningWheelFlachsAmount;
                }
                else if (prefab == "charcoal_kiln")
                {
                    self.m_maxOre = Configuration.Current.ProductionInputAmounts.kilnWoodAmount;
                }
                else if (prefab == "blastfurnace")
                {
                    self.m_maxOre = Configuration.Current.ProductionInputAmounts.blastfurnaceOreAmount;
                    self.m_maxFuel = Configuration.Current.ProductionInputAmounts.blastfurnaceCoalAmount;
                }
                else if (prefab == "smelter")
                {
                    self.m_maxOre = Configuration.Current.ProductionInputAmounts.furnaceOreAmount;
                    self.m_maxFuel = Configuration.Current.ProductionInputAmounts.furnaceCoalAmount;
                }
                else if (prefab == "windmill")
                {
                    self.m_maxOre = Configuration.Current.ProductionInputAmounts.windmillBarleyAmount;
                }
            }
        }
    }
}