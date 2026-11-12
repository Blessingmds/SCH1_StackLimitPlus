using System;
using System.Collections.Generic;
using System.IO;
using MDSAPI.Players;
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

    // off for the session when the installed MDSAPI is too old (MDSAPI says so on the main menu)
    private static bool _disabled;

    public override void OnInitializeMelon()
    {
        if (!MDSAPI.MdsApi.Requires("0.2.0", "StackLimit Plus")) { _disabled = true; return; }
        MDSAPI.Updates.UpdateCheck.Register("StackLimit Plus", Info.Version, "Blessingmds/SCH1_StackLimitPlus",
            "https://www.nexusmods.com/schedule1/mods/623");

        var dir = MelonEnvironment.UserDataDirectory;
        _store = new SettingsStore(Path.Combine(dir, "StackLimitPlus.json"), Path.Combine(dir, "StackLimitPlus.txt"));
        _store.Error += msg => MelonLogger.Error(msg);
        Settings = _store.Load();
        MelonLogger.Msg($"StackLimitPlus loaded. Global limit: {Settings.Global?.ToString() ?? "vanilla"}, hotkey: {Settings.Hotkey}");

        // every game hook goes through MDSAPI, which reports any this game version lacks
        Hooks.Install();
        WorldValues.Install();
        WorldValues.ApplyConstants(Settings);
    }

    // a tour cut short by quitting or changing scene still undoes its test-run changes
    public override void OnApplicationQuit()
    {
        if (!_disabled) Overlay.EndTutorial();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (_disabled) return;
        Overlay.EndTutorial();
        _sceneLoaded = sceneName == "Main";
        _playerReady = false;
        PlayerReady = false;
        _frameCounter = 0;
        PlaceResolver.ClearCache();
        ItemCatalog.ClearPointerCache();
        Theme.ResetCaches();
    }

    public override void OnUpdate()
    {
        if (_disabled) return;
        if (_sceneLoaded && !_playerReady && ++_frameCounter >= CHECK_INTERVAL)
        {
            _frameCounter = 0;
            if (IsPlayerLoaded())
            {
                _playerReady = true;
                PlayerReady = true;
                ApplyAll();
                MelonLogger.Msg($"Local player detected. Applied limits to {Catalog.Items.Count} items.");
                if (!Settings.HasTutorialCompleted) _notice.Show(Settings.Hotkey);
            }
        }
        if (!PlayerReady) return;

        // picks up newly placed racks/mixers without hooking their init during save loading;
        // the scene search costs a hitch, so only when a capacity is actually customised
        _worldTimer += Time.unscaledDeltaTime;
        if (_worldTimer >= 30f)
        {
            _worldTimer = 0;
            bool anyCapacity = Settings.DryingRackCapacity.HasValue || Settings.MixerMaxMix.Count > 0
                || System.Linq.Enumerable.Any(Settings.Objects.Values, o => o.Capacity.HasValue);
            if (anyCapacity)
            try { WorldValues.Apply(Settings); }
            catch (Exception ex) { ErrorOnce($"Could not apply cash/rack/mixer values: {ex.Message}"); }
        }

        if (HotkeyCapture != null)
        {
            CaptureHotkey();
            return; // the captured key (e.g. Esc) must not also close the menu this frame
        }
        else if (Input.GetKeyDown(HotkeyCode) && !Controls.IsTyping)
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
        LookTarget.TickCash();
        PhoneMenu.Tick();
        LobbySync.Tick();
        _notice.Tick(Overlay.IsOpen || PhoneMenu.IsOpen);
    }

    // after the game has placed its own interaction prompt this frame, so ours sits right under it
    public override void OnLateUpdate()
    {
        if (!PlayerReady) return;
        _hint.Show(LookTarget.Current != null && !Overlay.IsOpen && !PhoneMenu.IsOpen, Settings.Hotkey);
    }

    private static readonly HintView _hint = new();
    private static readonly TutorialNotice _notice = new();
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
    // bumped on every change; the tutorial's do-it steps watch it
    public static int SettingsVersion { get; private set; }

    public static void SettingsChanged()
    {
        SettingsVersion++;
        ApplyAll();
        if (!IsClient)
        {
            _store.Save(Settings);
            LobbySync.MarkDirty();
        }
        Overlay.Refresh();
        PhoneMenu.Refresh();
    }

    // saves without re-applying (window size and other personal state); never while using the host's settings
    public static void SaveLocal()
    {
        if (!IsClient) _store.Save(Settings);
    }

    public static void MarkTutorialCompleted()
    {
        if (Settings.HasTutorialCompleted) return;
        Settings.HasTutorialCompleted = true;
        if (!IsClient) { _store.Save(Settings); return; }
        // co-op client: Settings holds the host's values, so flip the flag in our own file only
        var local = _store.Load();
        local.HasTutorialCompleted = true;
        _store.Save(local);
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
        host.WindowWidth = Settings.WindowWidth;
        host.WindowHeight = Settings.WindowHeight;
        host.HasTutorialCompleted = Settings.HasTutorialCompleted;
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
            return LocalPlayer.Exists;
        }
        catch (Exception ex)
        {
            ErrorOnce($"Error checking if player is loaded: {ex.Message}");
            return false;
        }
    }
}
}
