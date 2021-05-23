﻿// Veilheim
// a Valheim mod
// 
// File:    PlanPiece.cs
// Project: Veilheim

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Veilheim.Blueprints
{
    internal class PlanPiece : MonoBehaviour, Interactable, Hoverable
    {
        public const string zdoPlanPiece = "PlanPiece";
        public const string zdoPlanResource = "PlanResource";

        private ZNetView m_nView;
        private WearNTear m_wearNTear;

        public string m_hoverText = "";
        public Piece originalPiece;

        public void Awake()
        {
            if (!originalPiece)
            {
                InvalidPlanPiece();
                return;
            }

            m_wearNTear = GetComponent<WearNTear>();
            m_nView = GetComponent<ZNetView>();
            if (m_nView.IsOwner())
            {
                m_nView.GetZDO().Set("support", 0f);
            }
            m_nView.Register<bool>("Refund", RPC_Refund);
            m_nView.Register<string, int>("AddResource", RPC_AddResource);
            m_nView.Register("SpawnPieceAndDestroy", RPC_SpawnPieceAndDestroy);
            UpdateHoverText();
            UpdateTextures();
        }

        private void RPC_Refund(long sender, bool all)
        {
            if (m_nView.IsOwner())
            {
                Refund(all);
            }
        }

        private bool hasSupport = false;

        public void Update()
        {
            if (m_nView.IsValid())
            {
                bool haveSupport = m_nView.GetZDO().GetFloat("support") >= m_wearNTear.GetMinSupport();
                if (haveSupport != hasSupport)
                {
                    hasSupport = haveSupport;
                    UpdateTextures();
                }
            }
        }

        public static int m_planLayer = LayerMask.NameToLayer("piece_nonsolid");
        public static int m_placeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");

        /// <summary>
        /// Destroy this gameObject because of invalid state detected
        /// </summary>
        private void InvalidPlanPiece()
        {
            Jotunn.Logger.LogWarning("Invalid PlanPiece , destroying self: " + name + " @ " + gameObject.transform.position);
            ZNetScene.instance.Destroy(base.gameObject);
            Destroy(this.gameObject);
        }

        internal void UpdateTextures()
        {
            ShaderHelper.UpdateTextures(gameObject, GetShaderState());
        }

        private ShaderHelper.ShaderState GetShaderState()
        {
            //TODO: bind key on rune?
            /*if (PlanBuild.showRealTextures)
            {
                return ShaderHelper.ShaderState.Skuld;
            }*/
            if (hasSupport)
            {
                return ShaderHelper.ShaderState.Supported;
            }
            return ShaderHelper.ShaderState.Floating;
        }

        public string GetHoverName()
        {
            return "Planned " + originalPiece.m_name;
        }

        private float m_lastLookedTime = -9999f;
        private float m_lastUseTime = -9999f;
        private float m_holdRepeatInterval = 1f;

        public string GetHoverText()
        {
            if (Time.time - m_lastLookedTime > 0.2f)
            {
                m_lastLookedTime = Time.time;
                SetupPieceInfo(originalPiece);
            }
            Hud.instance.m_buildHud.SetActive(true);
            if (!HasAllResources())
            {
                return Localization.instance.Localize("" +
                    "[<color=yellow>$KEY_Use</color>] [<color=yellow>1-8</color>] $plan_hover_add_material\n" +
                    "[$plan_hover_hold <color=yellow>$KEY_Use</color>] $plan_hover_add_all_materials");
            }
            return Localization.instance.Localize("[<color=yellow>$KEY_Use</color>] $plan_hover_build");
        }

        private void SetupPieceInfo(Piece piece)
        {
            Player localPlayer = Player.m_localPlayer;
            Hud.instance.m_buildSelection.text = Localization.instance.Localize(piece.m_name);
            Hud.instance.m_pieceDescription.text = Localization.instance.Localize(piece.m_description);
            Hud.instance.m_buildIcon.enabled = true;
            Hud.instance.m_buildIcon.sprite = piece.m_icon;
            GameObject[] uiRequirementPanels = Hud.instance.m_requirementItems;
            for (int j = 0; j < uiRequirementPanels.Length; j++)
            {
                if (j < piece.m_resources.Length)
                {
                    Piece.Requirement req = piece.m_resources[j];
                    uiRequirementPanels[j].SetActive(value: true);
                    SetupRequirement(uiRequirementPanels[j].transform, req, GetResourceCount(GetResourceName(req)));
                }
                else
                {
                    uiRequirementPanels[j].SetActive(value: false);
                }
            }
            if ((bool)piece.m_craftingStation)
            {
                CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, localPlayer.transform.position);
                GameObject obj = uiRequirementPanels[piece.m_resources.Length];
                obj.SetActive(value: true);
                Image component = obj.transform.Find("res_icon").GetComponent<Image>();
                Text component2 = obj.transform.Find("res_name").GetComponent<Text>();
                Text component3 = obj.transform.Find("res_amount").GetComponent<Text>();
                UITooltip component4 = obj.GetComponent<UITooltip>();
                component.sprite = piece.m_craftingStation.m_icon;
                component2.text = Localization.instance.Localize(piece.m_craftingStation.m_name);
                component4.m_text = piece.m_craftingStation.m_name;
                if (craftingStation != null)
                {
                    craftingStation.ShowAreaMarker();
                    component.color = Color.white;
                    component3.text = "";
                    component3.color = Color.white;
                }
                else
                {
                    component.color = Color.gray;
                    component3.text = "None";
                    component3.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
                }
            }
        }

        public bool SetupRequirement(Transform elementRoot, Piece.Requirement req, int currentAmount)
        {
            Image imageResIcon = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            Text textResName = elementRoot.transform.Find("res_name").GetComponent<Text>();
            Text textResAmount = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            UITooltip uiTooltip = elementRoot.GetComponent<UITooltip>();
            if (req.m_resItem != null)
            {
                imageResIcon.gameObject.SetActive(value: true);
                textResName.gameObject.SetActive(value: true);
                textResAmount.gameObject.SetActive(value: true);
                imageResIcon.sprite = req.m_resItem.m_itemData.GetIcon();
                imageResIcon.color = Color.white;

                uiTooltip.m_text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
                textResName.text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);

                int requiredAmount = req.GetAmount(0);

                int playerAmount = PlayerGetResourceCount(Player.m_localPlayer, req.m_resItem.m_itemData.m_shared.m_name);
                int remaining = requiredAmount - currentAmount;

                textResAmount.text = currentAmount + "/" + requiredAmount;
                if (remaining > 0 && playerAmount == 0)
                {
                    imageResIcon.color = Color.gray;
                    textResAmount.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
                }
                else
                {
                    imageResIcon.color = Color.white;
                    textResAmount.color = Color.white;
                }
            }
            return true;
        }

        //Hooks for Harmony patches
        public bool PlayerHaveResource(Humanoid player, string resourceName)
        {
            return player.GetInventory().HaveItem(resourceName);
        }

        public int PlayerGetResourceCount(Humanoid player, string resourceName)
        {
            return player.GetInventory().CountItems(resourceName);
        }

        public void PlayerRemoveResource(Humanoid player, string resourceName, int amount)
        {
            player.GetInventory().RemoveItem(resourceName, amount);
        }

        public bool Interact(Humanoid user, bool hold)
        {
            if (hold)
            {
                if (Time.time - m_lastUseTime < m_holdRepeatInterval)
                {
                    return false;
                }
                m_lastUseTime = Time.time;

                return AddAllMaterials(user);
            }

            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                if (!PlayerHaveResource(user, resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                if (currentCount < req.m_amount)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, 1);
                    user.GetInventory().RemoveItem(resourceName, 1);
                    UpdateHoverText();
                    return true;
                }
            }
            if (!HasAllResources())
            {
                user.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                return false;
            }
            if (user.GetInventory().GetItem("$item_blueprintrune") == null)
            {
                user.Message(MessageHud.MessageType.Center, "$plan_need_rune");
                return false;
            }
            if ((bool)originalPiece.m_craftingStation)
            {
                CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(originalPiece.m_craftingStation.m_name, user.transform.position);
                if (!craftingStation)
                {
                    user.Message(MessageHud.MessageType.Center, "$msg_missingstation");
                    return false;
                }
            }
            if (!hasSupport)
            {
                user.Message(MessageHud.MessageType.Center, "$plan_not_enough_support");
                return false;
            }
            m_nView.InvokeRPC("Refund", false);
            m_nView.InvokeRPC("SpawnPieceAndDestroy");
            return false;
        }

        private bool AddAllMaterials(Humanoid user)
        {
            bool added = false;
            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                if (!PlayerHaveResource(user, resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                int remaining = req.m_amount - currentCount;
                int amountToAdd = Math.Min(remaining, PlayerGetResourceCount(user, resourceName));
                if (amountToAdd > 0)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, amountToAdd);
                    PlayerRemoveResource(user, resourceName, amountToAdd);
                    UpdateHoverText();
                    added = true;

                }
            }
            return added;
        }

        private static string GetResourceName(Piece.Requirement req)
        {
            return req.m_resItem.m_itemData.m_shared.m_name;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                if (req.m_resItem.m_itemData.m_shared.m_name != item.m_shared.m_name)
                {
                    continue;
                }
                string resourceName = GetResourceName(req);
                if (!PlayerHaveResource(user, resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                int remaining = req.m_amount - currentCount;
                if (remaining > 0)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, 1);
                    PlayerRemoveResource(user, resourceName, 1);
                    UpdateHoverText();
                    return true;
                }
            }
            return false;
        }

        private void Refund(bool all)
        {
            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                int currentCount = GetResourceCount(resourceName);
                if (!all)
                {
                    currentCount -= req.m_amount;
                }


                while (currentCount > 0)
                {
                    ItemDrop.ItemData itemData = req.m_resItem.m_itemData.Clone();
                    int dropCount = Mathf.Min(currentCount, itemData.m_shared.m_maxStackSize);
                    itemData.m_stack = dropCount;
                    currentCount -= dropCount;

                    Instantiate(req.m_resItem.gameObject, base.transform.position + Vector3.up, Quaternion.identity)
                        .GetComponent<ItemDrop>().SetStack(dropCount);
                }
            }

        }

        private bool HasAllResources()
        {
            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                int currentCount = GetResourceCount(resourceName);
                if (currentCount < req.m_amount)
                {
                    return false;
                }
            }
            return true;
        }

        public void RPC_AddResource(long sender, string resource, int amount)
        {
            if (m_nView.IsOwner())
            {
                AddResource(resource, amount);
            }
        }

        private void AddResource(string resource, int amount)
        {
            int current = GetResourceCount(resource);
            SetResourceCount(resource, current + amount);
        }

        private void SetResourceCount(string resource, int count)
        {
            m_nView.GetZDO().Set(zdoPlanResource + "_" + resource, count);
        }

        private int GetResourceCount(string resource)
        {
            if (!m_nView.IsValid())
            {
                return 0;
            }
            return m_nView.GetZDO().GetInt(zdoPlanResource + "_" + resource);
        }

        public void UpdateHoverText()
        {
            StringBuilder builder = new StringBuilder();
            foreach (Piece.Requirement requirement in originalPiece.m_resources)
            {
                builder.Append(requirement.m_resItem.m_itemData.m_shared.m_name + ": " + GetResourceCount(requirement.m_resItem.m_itemData.m_shared.m_name) + "/" + requirement.m_amount + "\n");
            }
            m_hoverText = builder.ToString();
        }

        private void RPC_SpawnPieceAndDestroy(long sender)
        {
            if (!m_nView.IsOwner())
            {
                return;
            }
            GameObject actualPiece = Instantiate(originalPiece.gameObject, gameObject.transform.position, gameObject.transform.rotation);
            WearNTear wearNTear = actualPiece.GetComponent<WearNTear>();
            if (wearNTear)
            {
                wearNTear.OnPlaced();
            }
#if DEBUG
            Jotunn.Logger.LogDebug("Plan spawn actual piece: " + actualPiece + " -> Destroying self");
#endif
            ZNetScene.instance.Destroy(this.gameObject);
            Destroy(this.gameObject);
        }

        [HarmonyPatch(typeof(WearNTear), "Highlight")]
        [HarmonyPrefix]
        static bool WearNTear_Hightlight_Prefix(WearNTear __instance)
        {
            if (__instance.GetComponent<PlanPiece>())
            {
                foreach (MeshRenderer renderer in __instance.GetComponentsInChildren<MeshRenderer>())
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        material.SetColor("_EmissionColor", Color.black);
                    }
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WearNTear), "Damage")]
        [HarmonyPrefix]
        static bool WearNTear_Damage_Prefix(WearNTear __instance)
        {
            if (__instance.GetComponent<PlanPiece>())
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WearNTear), "GetSupport")]
        [HarmonyPrefix]
        static bool WearNTear_GetSupport_Prefix(WearNTear __instance, ref float __result)
        {
            if (__instance.GetComponent<PlanPiece>())
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WearNTear), "HaveSupport")]
        [HarmonyPrefix]
        static bool WearNTear_HaveSupport_Prefix(WearNTear __instance, ref bool __result)
        {
            if (__instance.GetComponent<PlanPiece>())
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WearNTear), "Destroy")]
        [HarmonyPrefix]
        static bool WearNTear_Destroy_Prefix(WearNTear __instance)
        {
            PlanPiece planPiece = __instance.GetComponent<PlanPiece>();
            if (planPiece && planPiece.m_nView.IsOwner())
            {
                //Don't
                // create noise
                // create fragments
                // play destroyed effects
                planPiece.Refund(all: true);
                ZNetScene.instance.Destroy(__instance.gameObject);
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(Player), "CheckCanRemovePiece")]
        static bool Player_CheckCanRemovePiece_Prefix(Piece piece, ref bool __result)
        {
            PlanPiece PlanPiece = piece.GetComponent<PlanPiece>();
            if (PlanPiece)
            {
                __result = true;
                return false;
            }
            return true;
        }

    }
}

