using UnityEngine;

namespace SCH1_StackLimitPlus.UI
{
    public abstract class Tab
    {
        public abstract string Title { get; }
        public virtual bool PhoneVisible => true;
        // false hides the tab from the sidebar (e.g. This object with nothing targeted)
        public virtual bool Available => true;

        public abstract void Build(RectTransform content, bool phone);
        public virtual void Refresh() { }

        protected static bool ReadOnly => Core.IsClient;

        // Applies a change to the live settings, then saves and refreshes every open menu.
        protected static void Commit(System.Action<Settings.ModSettings> change)
        {
            if (ReadOnly) return;
            change(Core.Settings);
            Core.SettingsChanged();
        }

        // Padded scrolling column filling the tab's content area.
        protected static RectTransform ScrollBody(RectTransform content, bool phone)
        {
            var scroll = UiFactory.ScrollList(content, out var body);
            UiFactory.Stretch(scroll.GetComponent<RectTransform>());
            var pad = (int)(phone ? 12 : Theme.Pad);
            body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding = new RectOffset(pad, pad, 4, pad);
            return body;
        }
    }

    // shortcut: stands in for tabs not built yet; removed once every tab exists
    public class PlaceholderTab : Tab
    {
        readonly string _title;
        public PlaceholderTab(string title) => _title = title;
        public override string Title => _title;
        public override void Build(RectTransform content, bool phone)
        {
            var t = UiFactory.Text(content, _title, Theme.Body, Theme.TextMuted);
            UiFactory.Stretch(t.rectTransform, Theme.Pad, Theme.Pad, Theme.Pad, Theme.Pad);
        }
    }
}
