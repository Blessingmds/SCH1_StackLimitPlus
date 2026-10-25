using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using SCH1_StackLimitPlus.Game;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;

namespace SCH1_StackLimitPlus
{
public class Core : MelonMod
{
    private const int CHECK_INTERVAL = 60;

    private static readonly HashSet<string> _loggedErrors = new();
    private static SettingsStore _store = null!;

    private bool _sceneLoaded;
    private bool _playerReady;
    private int _frameCounter;

    public static ModSettings Settings { get; private set; } = ModSettings.CreateDefault();
    public static ItemCatalog Catalog { get; } = new();
    public static bool PlayerReady { get; private set; }

    public override void OnInitializeMelon()
    {
        var dir = MelonEnvironment.UserDataDirectory;
        _store = new SettingsStore(Path.Combine(dir, "StackLimitPlus.json"), Path.Combine(dir, "StackLimitPlus.txt"));
        _store.Error += msg => MelonLogger.Error(msg);
        Settings = _store.Load();
        MelonLogger.Msg($"StackLimitPlus loaded. Global limit: {Settings.Global?.ToString() ?? "vanilla"}, hotkey: {Settings.Hotkey}");

        new HarmonyLib.Harmony("com.sch1.stacklimitplus").PatchAll();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        _sceneLoaded = sceneName == "Main";
        _playerReady = false;
        PlayerReady = false;
        _frameCounter = 0;
        PlaceResolver.ClearCache();
    }

    public override void OnUpdate()
    {
        if (_sceneLoaded && !_playerReady && ++_frameCounter >= CHECK_INTERVAL)
        {
            _frameCounter = 0;
            if (IsPlayerLoaded())
            {
                _playerReady = true;
                PlayerReady = true;
                Catalog.Rescan();
                ApplyAll();
                MelonLogger.Msg($"Local player detected. Applied limits to {Catalog.Items.Count} items.");
            }
        }
    }

    // Call after any change to Settings: applies to the game and saves.
    public static void SettingsChanged()
    {
        ApplyAll();
        _store.Save(Settings);
    }

    public static void ReplaceSettings(ModSettings s)
    {
        Settings = s;
        SettingsChanged();
    }

    private static void ApplyAll()
    {
        Catalog.Apply(Settings);
    }

    public static void ErrorOnce(string message)
    {
        if (_loggedErrors.Add(message)) MelonLogger.Error(message);
    }

    private static bool IsPlayerLoaded()
    {
        try
        {
            var players = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<Il2CppScheduleOne.PlayerScripts.Player>());
            if (players != null && players.Length > 0) return true;

            var localPlayerObj = GameObject.Find("Player_Local");
            return localPlayerObj != null && localPlayerObj.activeInHierarchy;
        }
        catch (Exception ex)
        {
            ErrorOnce($"Error checking if player is loaded: {ex.Message}");
            return false;
        }
    }
}
}
