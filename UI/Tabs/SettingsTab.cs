using Il2CppTMPro;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI.Tabs
{
    public class SettingsTab : Tab
    {
        TextMeshProUGUI _hotkey = null!;
        Button _change = null!;
        Button _reset = null!;
        float _resetArmedUntil;

        public override string Title => "Settings";

        public override void Build(RectTransform content, bool phone)
        {
            var body = ScrollBody(content, phone);
            UiFactory.SectionTitle(body, "Menu", 12);
            var hotkeyRow = UiFactory.SettingRow(body, "Menu key", "Opens this menu, or the settings of the object you look at", phone, out _);
            _hotkey = UiFactory.Text(hotkeyRow, "", Theme.Body, Theme.Text, align: TextAlignmentOptions.MidlineRight);
            UiFactory.Size(_hotkey, width: 120, height: 34);
            _change = UiFactory.Button(hotkeyRow, "Change", ToggleCapture, width: 120);
            if (!phone) // the tour runs in the F6 window only
            {
                var tourRow = UiFactory.SettingRow(body, "Tutorial", "A quick tour of what the mod does", phone, out _);
                UiFactory.Button(tourRow, "Show tutorial", () => Core.Overlay.StartTutorial(null), width: 160);
            }

            UiFactory.SectionTitle(body, "Reset");
            var resetRow = UiFactory.SettingRow(body, "Reset everything", "Every limit back to vanilla. Your menu key stays.", phone, out _);
            _reset = UiFactory.Button(resetRow, "Reset", OnReset, width: 160);

            var path = UiFactory.Text(body, "Saved in UserData/StackLimitPlus.json", Theme.Small, Theme.TextFaint);
            UiFactory.Size(path, height: 40);
        }

        void ToggleCapture()
        {
            if (Core.HotkeyCapture != null) { Core.HotkeyCapture = null; Refresh(); return; }
            Core.HotkeyCapture = key =>
            {
                if (key != KeyCode.Escape) Commit(s => s.Hotkey = key.ToString());
                Core.Overlay.Refresh();
            };
            Refresh();
        }

        void OnReset()
        {
            // second click within 3 seconds confirms
            if (Time.unscaledTime > _resetArmedUntil)
            {
                _resetArmedUntil = Time.unscaledTime + 3f;
                Refresh();
                return;
            }
            _resetArmedUntil = 0;
            if (ReadOnly) return;
            var fresh = ModSettings.CreateDefault();
            fresh.Hotkey = Core.Settings.Hotkey;
            fresh.HasTutorialCompleted = Core.Settings.HasTutorialCompleted;
            Core.ReplaceSettings(fresh);
        }

        public override void Refresh()
        {
            bool waiting = Core.HotkeyCapture != null;
            _hotkey.text = waiting ? "Press a key…" : Core.Settings.Hotkey;
            _hotkey.color = waiting ? Theme.Accent : Theme.Text;
            _change.GetComponentInChildren<TextMeshProUGUI>().text = waiting ? "Cancel" : "Change";

            bool armed = Time.unscaledTime <= _resetArmedUntil;
            var label = _reset.GetComponentInChildren<TextMeshProUGUI>();
            label.text = armed ? "Click again to reset" : "Reset";
            label.color = armed ? Theme.Danger : Theme.Text;
            _reset.interactable = !ReadOnly;
        }
    }
}
