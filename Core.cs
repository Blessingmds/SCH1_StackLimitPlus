using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SCH1_StackLimitPlus
{
    public class Core : MelonMod
    {
        private static int _globalStackLimit = 40;
        private static bool _menuVisible = false;
        private static Rect _menuRect = new Rect(20, 20, 310, 170);
        private static bool _sceneLoaded = false;
        private static bool _hasInitialized = false;
        private static int _frameCounter = 0;
        private const int CHECK_INTERVAL = 60; 

        private static readonly Color _greenColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        private static readonly Color _yellowColor = new Color(1f, 0.9f, 0.2f, 1f);
        private static Texture2D? _whiteTexture;

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            MelonLogger.Msg("StackLimitPlus Loaded");

            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();

            var harmony = new HarmonyLib.Harmony("com.sch1.stacklimitplus");
            harmony.PatchAll();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                MelonLogger.Msg("Main scene loaded. Waiting for westville object...");
                _sceneLoaded = true;
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                _menuVisible = !_menuVisible;
            }

            if (_sceneLoaded && !_hasInitialized)
            {
                _frameCounter++;
                if (_frameCounter >= CHECK_INTERVAL)
                {
                    _frameCounter = 0;
                    if (GameObject.Find("westville") != null)
                    {
                        InitializeAllItems();
                        _hasInitialized = true;
                    }
                }
            }
        }

        public override void OnGUI()
        {
            if (!_menuVisible) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _menuRect = GUI.Window(0, _menuRect, (GUI.WindowFunction)DrawMenuWindow, "");
        }

        private static void DrawMenuWindow(int windowId)
        {
            GUI.color = _greenColor;
            GUI.Label(new Rect(10, 10, 230, 25), "StackLimit", new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            });

            GUI.color = _yellowColor;
            GUI.Label(new Rect(110, 10, 50, 25), "Plus", new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            });

            GUI.color = Color.white;

            DrawLine(new Rect(10, 35, _menuRect.width - 20, 1), Color.gray);

            GUI.Label(new Rect(10, 45, 150, 20), "Current Stack Limit:");
            GUI.Label(new Rect(150, 45, 100, 20), _globalStackLimit.ToString(), new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight
            });

            _globalStackLimit = (int)GUI.HorizontalSlider(new Rect(10, 70, _menuRect.width - 20, 20), _globalStackLimit, 1, 9999);

            GUI.Label(new Rect(10, 95, 70, 20), "Presets:");
            if (GUI.Button(new Rect(70, 95, 35, 20), "10")) _globalStackLimit = 10;
            if (GUI.Button(new Rect(110, 95, 35, 20), "40")) _globalStackLimit = 40;
            if (GUI.Button(new Rect(150, 95, 35, 20), "99")) _globalStackLimit = 99;
            if (GUI.Button(new Rect(190, 95, 45, 20), "999")) _globalStackLimit = 999;

            if (GUI.Button(new Rect(10, 125, 30, 30), "-"))
            {
                if (_globalStackLimit > 1)
                    _globalStackLimit--;
            }

            if (GUI.Button(new Rect(45, 125, 30, 30), "+"))
            {
                if (_globalStackLimit < 9999)
                    _globalStackLimit++;
            }

            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 1f);
            if (GUI.Button(new Rect(95, 125, 100, 30), "Update"))
            {
                FindAndUpdateAllItems();
                MelonLogger.Msg($"Stack limit updated to {_globalStackLimit}");
            }

            GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            if (GUI.Button(new Rect(205, 125, 85, 30), "Close"))
            {
                _menuVisible = false;
            }

            GUI.backgroundColor = Color.white;

            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(0, _menuRect.height - 22, _menuRect.width, 20),
                $"F6 to toggle • {ItemDefinitionConstructorPatch.AllItems.Count} items",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 });

            GUI.color = Color.white;

            GUI.DragWindow();
        }

        private static void DrawLine(Rect rect, Color color)
        {
            Color savedColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = savedColor;
        }

        private static void InitializeAllItems()
        {
            FindAndUpdateAllItems();
        }

        private static void FindAndUpdateAllItems()
        {
            var resourceItems = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            MelonLogger.Msg($"Found {resourceItems.Length} items");

            int newItemsAdded = 0;
            foreach (var item in resourceItems)
            {
                if (item == null) continue;

                if (!ItemDefinitionConstructorPatch.AllItems.Contains(item))
                {
                    ItemDefinitionConstructorPatch.AllItems.Add(item);
                    newItemsAdded++;
                    UpdateStackLimit(item);
                }
            }

            MelonLogger.Msg($"Added {newItemsAdded} new items");
            UpdateAllDefinitions();
        }

        public static void UpdateAllDefinitions()
        {
            int totalUpdated = 0;
            foreach (var item in ItemDefinitionConstructorPatch.AllItems)
            {
                if (item == null) continue;

                try
                {
                    UpdateStackLimit(item);
                    totalUpdated++;
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Error($"Update failed: {ex.Message}");
                }
            }
            MelonLogger.Msg($"Updated {totalUpdated} items");
        }

        private static void UpdateStackLimit(ItemDefinition item)
        {
            if (item == null) return;

            try
            {
                item.StackLimit = _globalStackLimit;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Update failed: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(ItemDefinition))]
        public class ItemDefinitionConstructorPatch
        {
            public static HashSet<ItemDefinition> AllItems = new HashSet<ItemDefinition>();

            [HarmonyPostfix]
            [HarmonyPatch(MethodType.Constructor)]
            static void OnItemDefinitionCreated(ItemDefinition __instance)
            {
                if (__instance == null) return;

                if (AllItems.Add(__instance))
                {
                    try
                    {
                        __instance.StackLimit = Core._globalStackLimit;
                    }
                    catch (System.Exception ex)
                    {
                        MelonLogger.Error($"Init failed: {ex.Message}");
                    }
                }
            }
        }
    }
}