using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using SCH1_StackLimitPlus.Game;
using SCH1_StackLimitPlus.Settings;
using SCH1_StackLimitPlus.UI;
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
    private float _worldTimer;

    public static ModSettings Settings { get; private set; } = ModSettings.CreateDefault();
    public static ItemCatalog Catalog { get; } = new();
    public static bool PlayerReady { get; private set; }
    // true when playing as a multiplayer client: settings come from the host and are read-only
    public static bool IsClient { get; set; }
    public static OverlayHost Overlay { get; } = new(AddTabs);
    public static PhoneApp PhoneMenu { get; } = new(AddTabs);
    // set by the Settings tab while it waits for a new hotkey; receives the key instead of the menu
    public static Action<KeyCode>? HotkeyCapture { get; set; }

    private static bool AddTabs(MenuView menu)
    {
        menu.AddTab(new UI.Tabs.ObjectTab());
        menu.AddTab(new UI.Tabs.ItemsTab());
        menu.AddTab(new UI.Tabs.PlacesTab());
        menu.AddTab(new UI.Tabs.CashTab());
        menu.AddTab(new UI.Tabs.StationsTab());
        menu.AddTab(new UI.Tabs.SettingsTab());
        return true;
    }

    public static KeyCode HotkeyCode => Enum.TryParse<KeyCode>(Settings.Hotkey, out var k) ? k : KeyCode.F6;

    public override void OnInitializeMelon()
    {
        var dir = MelonEnvironment.UserDataDirectory;
        _store = new SettingsStore(Path.Combine(dir, "StackLimitPlus.json"), Path.Combine(dir, "StackLimitPlus.txt"));
        _store.Error += msg => MelonLogger.Error(msg);
        Settings = _store.Load();
        MelonLogger.Msg($"StackLimitPlus loaded. Global limit: {Settings.Global?.ToString() ?? "vanilla"}, hotkey: {Settings.Hotkey}");

        // MelonLoader applies [HarmonyPatch] classes in this assembly by itself
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        _sceneLoaded = sceneName == "Main";
        _playerReady = false;
        PlayerReady = false;
        _frameCounter = 0;
        PlaceResolver.ClearCache();
        InputBlocker.Reset();
        Theme.ResetCaches();
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
        if (!PlayerReady) return;

        // picks up newly placed racks/mixers without hooking their init during save loading
        _worldTimer += Time.unscaledDeltaTime;
        if (_worldTimer >= 10f)
        {
            _worldTimer = 0;
            try { WorldValues.Apply(Settings); }
            catch (Exception ex) { ErrorOnce($"Could not apply cash/rack/mixer values: {ex.Message}"); }
        }

        if (HotkeyCapture != null)
        {
            CaptureHotkey();
            return; // the captured key (e.g. Esc) must not also close the menu this frame
        }
        else if (Input.GetKeyDown(HotkeyCode) && !Il2CppScheduleOne.GameInput.IsTyping)
        {
            if (Overlay.IsOpen) Overlay.Close();
            else
            {
                Catalog.Rescan();
                OpenTarget = LookTarget.Current;
                Overlay.Open(OpenTarget != null ? "This object" : null);
            }
        }
        Overlay.Tick();
        LookTarget.Tick(Overlay.IsOpen);
        PhoneMenu.Tick();
        LobbySync.Tick();
        _hint.Show(LookTarget.Current != null && !Overlay.IsOpen, Settings.Hotkey);
    }

    private static readonly HintView _hint = new();
    // the object the menu was opened on; drives the This object tab
    public static Target? OpenTarget { get; private set; }

    private static void CaptureHotkey()
    {
        if (!Input.anyKeyDown) return;
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            // mouse buttons would grab the click on the Cancel button
            if (key == KeyCode.None || (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)) continue;
            if (Input.GetKeyDown(key))
            {
                var capture = HotkeyCapture;
                HotkeyCapture = null;
                capture?.Invoke(key);
                return;
            }
        }
    }

    // Call after any change to Settings: applies to the game and saves.
    public static void SettingsChanged()
    {
        ApplyAll();
        if (!IsClient)
        {
            _store.Save(Settings);
            LobbySync.MarkDirty();
        }
        Overlay.Refresh();
        PhoneMenu.Refresh();
    }

    public static void ReplaceSettings(ModSettings s)
    {
        Settings = s;
        SettingsChanged();
    }

    // Multiplayer client: use the host's settings in memory; our saved file is left alone.
    public static void ApplyHostSettings(ModSettings host)
    {
        host.Hotkey = Settings.Hotkey;
        IsClient = true;
        Settings = host;
        SettingsChanged();
    }

    public static void RestoreLocalSettings()
    {
        IsClient = false;
        Settings = _store.Load();
        ApplyAll();
        Overlay.Refresh();
        PhoneMenu.Refresh();
    }

    private static void ApplyAll()
    {
        Catalog.Apply(Settings);
        try { WorldValues.Apply(Settings); }
        catch (Exception ex) { ErrorOnce($"Could not apply cash/rack/mixer values: {ex.Message}"); }
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
