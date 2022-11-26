// Veilheim
// a Valheim mod
// 
// File:    PortalSelectionGUI.cs
// Project: Veilheim

using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Veilheim.AssetManagers;
using Logger = Jotunn.Logger;

namespace Veilheim.Map
{
    /// <summary>
    ///     When renaming/tagging a portal read all tags from unconnected portals in the world and make a list of them to tag
    ///     it.
    ///     Coded by https://github.com/Algorithman
    /// </summary>
    internal class PortalSelectionGUI
    {
        private static readonly List<GameObject> teleporterButtons = new List<GameObject>();

        private static GameObject GUIRoot;

        private static void CreatePortalGUI(string currentTag)
        {
            if (GUIRoot == null)
            {

                if (GUIManager.Instance == null)
                {
                    Logger.LogError("GUIManager instance is null");
                    return;
                }

                if (!GUIManager.CustomGUIFront)
                {
                    Logger.LogError("GUIManager CustomGUI is null");
                    return;
                }


                GUIRoot = Object.Instantiate(Jotunn.Managers.PrefabManager.Instance.GetPrefab("PortalButtonBox"));
                GUIRoot.transform.SetParent(Jotunn.Managers.GUIManager.CustomGUIFront.transform, false);
                GUIRoot.GetComponentInChildren<Image>().sprite = Jotunn.Managers.GUIManager.Instance.GetSprite("woodpanel_trophys");
            }

            foreach (var button in teleporterButtons)
            {
                Object.Destroy(button);

            }

            teleporterButtons.Clear();

            IEnumerable<Portal> singlePortals;

            // Generate list of unconnected portals from ZDOMan
            if (ZNet.instance.IsLocalInstance())
            {
                singlePortals = PortalList.GetPortals().Where(x => !x.m_con);
            }

            // or from PortalsOnMap.portalsFromServer, if it is a real client
            else
            {
                singlePortals = PortalsOnMap.portalsFromServer.Where(x => !x.m_con);
            }

            var idx = 0;

            var lines = singlePortals.Count() / 3;

            foreach (var portal in singlePortals)
            {
                // Skip if it is the selected teleporter
                if (portal.m_tag == currentTag || currentTag == "<unnamed>" && string.IsNullOrEmpty(portal.m_tag))
                {
                    continue;
                }

                var newButton = Jotunn.Managers.GUIManager.Instance.CreateButton(portal.m_tag, GUIRoot.transform.Find("Image/Scroll View/Viewport/Content"), new Vector2(0, 1),
                    new Vector2(0, 1), new Vector2(0, 0));

                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // Set input field text to new name
                    TextInput.instance.m_textField.text = portal.m_tag;

                    // simulate enter key
                    TextInput.instance.OnEnter();

                    // hide textinput
                    TextInput.instance.Hide();

                    // Reset visibility state
                    GUIRoot.SetActive(false);
                });

                newButton.name = "TP" + teleporterButtons.Count;
                newButton.SetActive(true);

                newButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(95f + idx % 3 * (180f + 20f), -(idx / 3) * 50f - 25f);
                teleporterButtons.Add(newButton);
                idx++;
            }

            GUIRoot.transform.Find("Image/Scroll View/Viewport/Content").GetComponent<RectTransform>()
                .SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lines * 50f + 50f);
            GUIRoot.transform.localPosition = new Vector3(25, -100);
            Logger.LogInfo(GUIRoot.transform.localPosition.ToString());
            Logger.LogInfo(GUIRoot.transform.position.ToString());


            GUIRoot.SetActive(teleporterButtons.Count > 0);
        }

        public static void OpenPortalSelection()
        {
            if (TextInput.instance.m_panel.activeSelf)
            {
                Logger.LogInfo("Generating portal selection");

                // set position of textinput (a bit higher)
                TextInput.instance.m_panel.transform.localPosition = new Vector3(0, 270.0f, 0);

                // Get name of portal
                var currentTag = TextInput.instance.m_textField.text;
                if (string.IsNullOrEmpty(currentTag))
                {
                    currentTag = "<unnamed>";
                    TextInput.instance.m_textField.text = currentTag;
                }

                CreatePortalGUI(currentTag);

                // release mouselock
                GameCamera.instance.m_mouseCapture = false;
                GameCamera.instance.UpdateMouseCapture();
            }
        }

        public static bool IsVisible()
        {
            if (GUIRoot == null)
            {
                return false;
            }

            return GUIRoot.activeSelf;
        }

        public static void Hide()
        {
            if (GUIRoot != null)
            {
                GUIRoot.SetActive(false);
            }
        }
    }
}