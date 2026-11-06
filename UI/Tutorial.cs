using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Il2CppTMPro;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // Game-style walkthrough over the F6 window. The screen dims except a spotlight; info steps block every click,
    // do-it steps unlock only the spotlighted control and wait until the player changes something, and one quest
    // step closes the menu and sends the player into the world. Everything is a test run: settings are restored
    // when the tour ends, however it ends. The full tour runs until Settings.HasTutorialCompleted; ? replays a page.
    public class Tutorial
    {
        // Quest: menu closes, look at an object and press the key. PhoneQuest: menu closes, open the phone app.
        enum Kind { Info, Do, Quest, PhoneQuest }

        // Check: when a do-it step counts as done (default: any settings change), given the step's resolved targets
        record Step(string Page, bool InTour, Kind Kind, string? Tab, Func<MenuView, RectTransform?[]> Targets,
            string Title, string Body, string? Done = null, Func<bool>? When = null, Func<MenuView, bool>? SkipIf = null,
            Func<RectTransform?[], bool>? Check = null, bool Follow = false);

        const string Intro = "Intro"; // full-tour-only steps, never in a page replay
        const float HolePad = 6, Gap = 18, Edge = 16, BoxWidth = 400, ReopenDelay = 0.6f;
        static readonly Color Dim = new(0, 0, 0, 0.72f);

        readonly MenuView _menu;
        readonly Action _closeMenu;
        readonly RectTransform _layer;
        readonly Image _blocker;
        readonly RectTransform[] _dim = new RectTransform[4];
        readonly RectTransform[] _frame = new RectTransform[4];
        readonly RectTransform _box;
        readonly TextMeshProUGUI _counter, _title, _body;
        readonly Button _back, _next, _skipStep;
        readonly TextMeshProUGUI _nextLabel;
        readonly ObjectiveView _objective = new();

        List<Step> _steps = new();
        int _index;
        RectTransform?[] _targets = Array.Empty<RectTransform?>();
        int _settleFrames;
        int _versionAtStep;
        bool _stepDone;
        bool _phoneSeen;
        float _reopenAt;
        string? _snapshot;
        int _versionAtStart;

        public bool Running { get; private set; }
        public bool WaitingForQuest { get; private set; }

        public Tutorial(Transform canvas, MenuView menu, Action closeMenu)
        {
            _menu = menu;
            _closeMenu = closeMenu;
            _layer = UiFactory.Node("Tutorial", canvas);
            UiFactory.Stretch(_layer);

            // info steps: nothing under the tutorial can be clicked, the spotlight included
            _blocker = UiFactory.Panel(_layer, new Color(0, 0, 0, 0), name: "Blocker");
            UiFactory.Stretch(_blocker.rectTransform);
            for (int i = 0; i < 4; i++)
            {
                _dim[i] = Centered(UiFactory.Panel(_layer, Dim, name: "Dim").rectTransform); // also blocks clicks around the hole
                var f = UiFactory.Panel(_layer, Theme.Accent, name: "Frame");
                f.raycastTarget = false;
                _frame[i] = Centered(f.rectTransform);
            }

            var box = UiFactory.Panel(_layer, new Color(Theme.Window.r, Theme.Window.g, Theme.Window.b, 1f), rounded: true, name: "Box");
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = Theme.Line;
            outline.effectDistance = new Vector2(1, -1);
            _box = Centered(box.rectTransform);
            var col = box.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(20, 20, 16, 14);
            col.spacing = 6;
            col.childControlWidth = true; col.childControlHeight = true;
            col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _box.sizeDelta = new Vector2(BoxWidth, 0);

            _counter = UiFactory.Text(box.transform, "", Theme.Small, Theme.TextFaint, name: "Counter");
            _title = UiFactory.Text(box.transform, "", 18, Theme.Text, FontStyles.Bold, name: "Title");
            _body = UiFactory.Text(box.transform, "", Theme.Body, Theme.TextMuted, name: "Body");
            _body.enableWordWrapping = true;
            _body.overflowMode = TextOverflowModes.Overflow;

            var buttons = UiFactory.Row(box.transform, 8, 34, "Buttons");
            UiFactory.LinkButton(buttons.transform, "Skip tour", Finish);
            _skipStep = UiFactory.LinkButton(buttons.transform, "Skip this step", () => Go(_index + 1, 1)); // quest steps only
            UiFactory.Size(UiFactory.Node("Spacer", buttons.transform), height: 1, flexWidth: 1);
            _back = UiFactory.Button(buttons.transform, "Back", () => Go(_index - 1, -1), width: 80);
            _next = UiFactory.Button(buttons.transform, "Next", Next, primary: true, width: 96);
            _nextLabel = _next.GetComponentInChildren<TextMeshProUGUI>();

            _layer.gameObject.SetActive(false);
        }

        static RectTransform Centered(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        // page null = the full tour; otherwise that page's steps (the ? button)
        public void Start(string? page)
        {
            var all = Steps().Where(s => s.When == null || s.When()).ToList();
            _steps = page == null ? all.Where(s => s.InTour).ToList() : all.Where(s => s.Page == page).ToList();
            if (_steps.Count == 0) return;
            // clients can't change anything, so there is nothing to restore
            _snapshot = Core.IsClient ? null : JsonSerializer.Serialize(Core.Settings, SettingsStore.Options);
            _versionAtStart = Core.SettingsVersion;
            Running = true;
            _layer.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            Go(0, 1);
        }

        // Done, Skip, closing the menu, a scene change or quitting: all end the tour, undo the test run and count as completed.
        public void Finish()
        {
            if (!Running) return;
            Running = WaitingForQuest = false;
            _reopenAt = 0;
            try { _layer.gameObject.SetActive(false); } catch (Exception) { } // may be gone after a scene change
            _objective.Hide();
            Core.PhoneMenu.Locked = false;
            if (_snapshot != null && Core.SettingsVersion != _versionAtStart)
            {
                var restored = JsonSerializer.Deserialize<ModSettings>(_snapshot, SettingsStore.Options)!;
                restored.Hotkey = Core.Settings.Hotkey; // personal, not part of the test run
                restored.WindowWidth = Core.Settings.WindowWidth;
                restored.WindowHeight = Core.Settings.WindowHeight;
                restored.HasTutorialCompleted = true;
                Core.ReplaceSettings(restored);
            }
            _snapshot = null;
            Core.MarkTutorialCompleted();
        }

        // the player opened the menu again while the quest objective was showing (on an object, or anywhere to skip)
        public void ResumeAfterQuest()
        {
            WaitingForQuest = false;
            _reopenAt = 0;
            var quest = _steps[_index].Kind;
            bool done = quest == Kind.Quest ? _menu.IsAvailable("This object") : _phoneSeen;
            if (done && quest == Kind.Quest) _objective.Complete();
            else if (done) _objective.HideTop(); // phone: already ticked off when the app opened
            else _objective.Hide();
            Core.PhoneMenu.Locked = false;
            if (Core.PhoneMenu.IsOpen) Core.PhoneMenu.CloseWithPhone();
            _layer.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            if (done) { Go(_index + 1, 1); return; }
            // menu key pressed without doing it: back to the same step, to try again or skip it
            Go(_index, 1);
            _body.text = _steps[_index].Body + (quest == Kind.Quest
                ? "\n\n<i>You weren't looking at one. Press Go to try again, or skip this step.</i>"
                : "\n\n<i>The app wasn't opened. Press Go to try again, or skip this step.</i>");
        }

        void Next()
        {
            var step = _steps[_index];
            string key = Core.Settings.Hotkey;
            if (step.Kind is Kind.Quest or Kind.PhoneQuest)
            {
                WaitingForQuest = true;
                _phoneSeen = false;
                _layer.gameObject.SetActive(false);
                if (step.Kind == Kind.Quest)
                    _objective.Show("Look at a shelf, rack or station and press [" + key + "]", $"Press {key} away from objects to return to the tour",
                        $"Press {key} on a shelf, rack or station");
                else
                {
                    Core.PhoneMenu.Locked = true;
                    _objective.Show("Open your phone and tap the StackLimit app", $"Press {key} to return to the tour",
                        "Open StackLimit on your phone");
                }
                _closeMenu();
                return;
            }
            Go(_index + 1, 1);
        }

        // Every frame, menu open or not: follows the phone step while the menu is closed.
        public void TickQuest()
        {
            if (!Running || !WaitingForQuest || _steps[_index].Kind != Kind.PhoneQuest) return;
            if (_reopenAt > 0)
            {
                // give the phone time to put itself away before the menu takes the cursor
                if (Time.unscaledTime >= _reopenAt) { _reopenAt = 0; Core.Overlay.Open(null); }
                return;
            }
            if (!_phoneSeen && Core.PhoneMenu.IsOpen)
            {
                _phoneSeen = true;
                _objective.ShowDone("That's the same StackLimit menu, right on your phone. Changes there apply straight away, just like in this window.", () =>
                    {
                        _objective.HideTop();
                        Core.PhoneMenu.Locked = false;
                        Core.PhoneMenu.CloseWithPhone();
                        _reopenAt = Time.unscaledTime + ReopenDelay;
                    });
            }
        }

        void Go(int index, int direction)
        {
            // skip steps that don't apply right now (This object without a target); quests are skipped going back
            while (index >= 0 && index < _steps.Count &&
                   ((_steps[index].SkipIf?.Invoke(_menu) ?? false) || (direction < 0 && _steps[index].Kind == Kind.Quest)))
                index += direction;
            if (index >= _steps.Count) { Finish(); return; }
            _index = Mathf.Max(0, index);
            var step = _steps[_index];
            if (step.Tab != null && _menu.IsAvailable(step.Tab) && _menu.CurrentTitle != step.Tab) _menu.Show(step.Tab);

            bool waits = IsWaiting(step);
            _counter.text = _steps.Count > 1 ? $"{_index + 1} / {_steps.Count}" : "";
            _title.text = step.Title;
            _body.text = step.Body;
            _blocker.raycastTarget = !waits; // do-it steps: the spotlighted control is clickable
            UiFactory.SetShown(_back, _index > 0);
            UiFactory.SetShown(_next, !waits);
            _skipStep.gameObject.SetActive(step.Kind is Kind.Quest or Kind.PhoneQuest);
            _nextLabel.text = step.Kind is Kind.Quest or Kind.PhoneQuest ? "Go" : _index == 0 && _steps.Count > 1 ? "Start" : _index == _steps.Count - 1 ? "Done" : "Next";
            _versionAtStep = Core.SettingsVersion;
            _stepDone = false;
            _targets = Array.Empty<RectTransform?>();
            _settleFrames = 2; // a freshly shown tab lays out over the next frame; find and scroll to targets after
            Layout();
        }

        static bool IsWaiting(Step s) => s.Kind == Kind.Do && !Core.IsClient;

        // Called every frame by the overlay while the tour is showing.
        public void Tick()
        {
            if (!Running || WaitingForQuest) return;
            var step = _steps[_index];
            bool typing = Il2CppScheduleOne.GameInput.IsTyping;
            if (!typing && Input.GetKeyDown(KeyCode.Escape)) { Finish(); return; }
            bool waiting = IsWaiting(step) && !_stepDone;
            if (!typing && !waiting && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) { Next(); return; }

            // done: explain the result and wait for Next, so there's time to read and look
            if (waiting && _settleFrames == 0 && (step.Check?.Invoke(_targets) ?? Core.SettingsVersion != _versionAtStep))
            {
                _stepDone = true;
                _body.text = $"<color=#8FD88F>Done.</color> {step.Done}";
                _blocker.raycastTarget = true;
                UiFactory.SetShown(_next, true);
            }

            if (step.Follow && _settleFrames == 0) ScrollIntoView(); // e.g. search results change size while typing
            if (_settleFrames > 0 && --_settleFrames == 0)
            {
                _targets = step.Targets(_menu);
                ScrollIntoView();
                if (TargetRect() == null) UiFactory.SetShown(_next, true); // control not found: never leave a do-it step without a way on
            }
            Layout();
        }

        void Layout()
        {
            var screen = _layer.rect;
            if (TargetRect() is Rect h)
            {
                h = Rect.MinMaxRect(h.xMin - HolePad, h.yMin - HolePad, h.xMax + HolePad, h.yMax + HolePad);
                Place(_dim[0], Rect.MinMaxRect(screen.xMin, h.yMax, screen.xMax, screen.yMax));
                Place(_dim[1], Rect.MinMaxRect(screen.xMin, screen.yMin, screen.xMax, h.yMin));
                Place(_dim[2], Rect.MinMaxRect(screen.xMin, h.yMin, h.xMin, h.yMax));
                Place(_dim[3], Rect.MinMaxRect(h.xMax, h.yMin, screen.xMax, h.yMax));
                const float t = 2;
                Place(_frame[0], Rect.MinMaxRect(h.xMin - t, h.yMax, h.xMax + t, h.yMax + t));
                Place(_frame[1], Rect.MinMaxRect(h.xMin - t, h.yMin - t, h.xMax + t, h.yMin));
                Place(_frame[2], Rect.MinMaxRect(h.xMin - t, h.yMin, h.xMin, h.yMax));
                Place(_frame[3], Rect.MinMaxRect(h.xMax, h.yMin, h.xMax + t, h.yMax));
                foreach (var f in _frame) f.gameObject.SetActive(true);
                PlaceBox(screen, h);
            }
            else
            {
                Place(_dim[0], screen);
                for (int i = 1; i < 4; i++) Place(_dim[i], Rect.zero);
                foreach (var f in _frame) f.gameObject.SetActive(false);
                _box.anchoredPosition = Vector2.zero;
            }
        }

        static void Place(RectTransform rt, Rect r)
        {
            rt.anchoredPosition = r.center;
            rt.sizeDelta = new Vector2(Mathf.Max(0, r.width), Mathf.Max(0, r.height));
        }

        // beside the spotlight (right, left, below, above: first that fits), kept on screen
        void PlaceBox(Rect screen, Rect h)
        {
            var size = _box.rect.size;
            var candidates = new[]
            {
                new Vector2(h.xMax + Gap + size.x / 2, h.center.y),
                new Vector2(h.xMin - Gap - size.x / 2, h.center.y),
                new Vector2(h.center.x, h.yMin - Gap - size.y / 2),
                new Vector2(h.center.x, h.yMax + Gap + size.y / 2),
            };
            var inner = Rect.MinMaxRect(screen.xMin + Edge + size.x / 2, screen.yMin + Edge + size.y / 2,
                                        screen.xMax - Edge - size.x / 2, screen.yMax - Edge - size.y / 2);
            var pos = candidates.FirstOrDefault(inner.Contains, candidates[2]);
            _box.anchoredPosition = new Vector2(Mathf.Clamp(pos.x, inner.xMin, inner.xMax), Mathf.Clamp(pos.y, inner.yMin, inner.yMax));
        }

        // union of the step's targets in layer space, cut to the scroll view each sits in
        Rect? TargetRect()
        {
            Rect? union = null;
            foreach (var t in _targets)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                var r = InSpace(t, _layer);
                var scroll = t.GetComponentInParent<ScrollRect>();
                if (scroll != null)
                {
                    var view = InSpace(scroll.viewport != null ? scroll.viewport : scroll.GetComponent<RectTransform>(), _layer);
                    r = Rect.MinMaxRect(Mathf.Max(r.xMin, view.xMin), Mathf.Max(r.yMin, view.yMin), Mathf.Min(r.xMax, view.xMax), Mathf.Min(r.yMax, view.yMax));
                    if (r.width <= 0 || r.height <= 0) continue;
                }
                union = Union(union, r);
            }
            return union;
        }

        static Rect Union(Rect? a, Rect b) =>
            a is Rect u ? Rect.MinMaxRect(Mathf.Min(u.xMin, b.xMin), Mathf.Min(u.yMin, b.yMin), Mathf.Max(u.xMax, b.xMax), Mathf.Max(u.yMax, b.yMax)) : b;

        static Rect InSpace(RectTransform t, RectTransform space)
        {
            Vector2 a = space.InverseTransformPoint(t.TransformPoint(t.rect.min));
            Vector2 b = space.InverseTransformPoint(t.TransformPoint(t.rect.max));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        // scroll the page so the highlighted rows are in view (top first if they don't all fit)
        void ScrollIntoView()
        {
            var scroll = _targets.FirstOrDefault(t => t != null)?.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null) return;
            var view = scroll.viewport != null ? scroll.viewport : scroll.GetComponent<RectTransform>();
            Rect? union = null;
            foreach (var t in _targets) if (t != null) union = Union(union, InSpace(t, view));
            if (union is not Rect target) return;
            var v = view.rect;
            float delta = 0;
            if (target.yMax > v.yMax || target.height > v.height - 24) delta = v.yMax - 12 - target.yMax;
            else if (target.yMin < v.yMin) delta = v.yMin + 12 - target.yMin;
            if (delta == 0) return;
            scroll.StopMovement();
            var pos = scroll.content.anchoredPosition;
            float max = Mathf.Max(0, scroll.content.rect.height - v.height);
            scroll.content.anchoredPosition = new Vector2(pos.x, Mathf.Clamp(pos.y + delta, 0, max));
        }

        static Func<MenuView, RectTransform?[]> Row(string label) => m => new[] { m.FindInCurrentTab("Setting:" + label) };
        static RectTransform?[] Nothing(MenuView m) => Array.Empty<RectTransform?>();

        static IEnumerable<Step> Steps()
        {
            string key = Core.Settings.Hotkey;

            // the full tour
            yield return new Step(Intro, true, Kind.Info, "Items", Nothing, "Welcome to StackLimit Plus",
                "Pockets full again? Let's make some room.");
            yield return new Step("Items", true, Kind.Do, "Items", Row("Stack limit"), "Raise the limit",
                "Type <b>100</b> here and press Enter. Every stackable item will then fit 100 in one slot: your pockets, shelves and vans.",
                "Every stackable item now stacks to 100. Clear the field any time to go back to the game's own sizes.");
            yield return new Step("Items", true, Kind.Do, "Items", Row("Product"), "Product gets its own",
                "Give <b>Product</b> its own number. Your product will stack to that, while everything else keeps the limit you just set.",
                "Product now has its own limit. The most specific setting always wins: an item beats its category, a category beats the global limit.");
            yield return new Step("Items", true, Kind.Do, "Items", m => new[] { m.FindInCurrentTab("SearchRow"), m.FindInCurrentTab("ItemList") }, "Find one item",
                "Search for an item, like <b>Baggie</b> or <b>OG Kush</b>. Any single item can get a limit that beats everything above.",
                "Found it. Type a number in its row and only that item changes.",
                Check: t => (t.FirstOrDefault()?.GetComponentInChildren<TMP_InputField>()?.text.Length ?? 0) > 0, Follow: true);
            yield return new Step("Places", true, Kind.Do, "Places", Row("Preset"), "Where it applies",
                "Pick where your limits apply: just your pockets, everything except stations, or everywhere.",
                "Saved. Unless you pick Everywhere, stations keep the game's own amounts, so your workers spread batches over every slot and nothing gets halved.");
            yield return new Step(Intro, true, Kind.Info, null, m => new[] { m.TabButton("Cash"), m.TabButton("Stations") }, "More to tweak",
                "Cash per slot and the ATM limit live under Cash. Drying rack and mixer sizes live under Stations.");
            yield return new Step(Intro, true, Kind.Quest, null, Nothing, "One object, its own rules",
                $"Press Go, then look at a shelf, rack or station and press <b>[{key}]</b>.");
            // a worker's bed shows Cash per slot instead of Stack size; the hidden one is skipped by the spotlight
            yield return new Step("This object", true, Kind.Do, "This object",
                m => new[] { m.FindInCurrentTab("Setting:Stack size"), m.FindInCurrentTab("Setting:Cash per slot") }, "Just this one",
                "Change the limit for just this object. Everything else keeps following Places.",
                "Only this one changed. It keeps its own limit until you reset it here.",
                SkipIf: m => !m.IsAvailable("This object"));
            yield return new Step("Settings", true, Kind.Info, "Settings", Row("Menu key"), "Your key",
                $"<b>{key}</b> opens this menu. Change it here if it clashes with another mod.");
            yield return new Step(Intro, true, Kind.PhoneQuest, null, Nothing, "On your phone",
                "StackLimit is also an app on your phone. Press Go, then open your phone and tap the StackLimit app.");
            yield return new Step("Settings", true, Kind.Info, "Settings", Nothing, "Co-op",
                "The host's settings apply. Only they can change them.", When: () => Core.IsClient);
            yield return new Step(Intro, true, Kind.Info, null, m => new[] { m.HelpButton?.GetComponent<RectTransform>() }, "Need a reminder?",
                "Every page has its own short tutorial. Press <b>?</b> on a page to walk through just that page.");

            // page replays only (?)
            yield return new Step("Cash", false, Kind.Do, "Cash", Row("Cash per slot"), "Cash per slot",
                "Set how much cash one storage slot holds. Bigger numbers mean fewer slots full of cash.",
                "Every storage slot that takes cash now holds that much. Clear it to go back to vanilla.");
            yield return new Step("Stations", false, Kind.Do, "Stations", Row("Drying rack"), "Bigger batches",
                "Set how much a drying rack works on at once. Higher means bigger batches per rack.",
                "Every drying rack uses the new size. To change one rack alone, look at it and press " + key + ".");
        }
    }

    // A game-style objective line on the HUD while the tour sends the player into the world.
    public class ObjectiveView
    {
        GameObject? _canvas;
        TextMeshProUGUI? _label, _text, _sub;
        Button? _next;
        Action? _onNext;

        // left side: a copy of the game's own quest entry (title + objective line), completed with its own animation
        GameObject? _quest;
        Il2CppScheduleOne.UI.QuestEntryHUDUI? _entry;
        static bool _loggedQuest;

        public void Show(string text, string sub, string questLine)
        {
            Set("NEW OBJECTIVE", text, sub, null);
            ShowQuest(questLine);
        }

        // objective done: explain what happened and wait for Next (the cursor is free while the phone is up)
        public void ShowDone(string text, Action onNext)
        {
            Set("OBJECTIVE COMPLETE", text, "", onNext);
            CompleteQuest();
        }

        // done without an explanation (the menu reopens on top): tick the quest off and drop the top line
        public void Complete()
        {
            HideTop();
            CompleteQuest();
        }

        void ShowQuest(string line)
        {
            try
            {
                RemoveQuest();
                Il2CppScheduleOne.UI.Phone.JournalApp? journal = null;
                foreach (var j in Resources.FindObjectsOfTypeAll<Il2CppScheduleOne.UI.Phone.JournalApp>())
                    if (j.gameObject.scene.IsValid()) { journal = j; break; }
                var hud = UnityEngine.Object.FindObjectOfType<Il2CppScheduleOne.UI.HUD>();
                if (journal?.QuestHUDUIPrefab == null || journal.QuestEntryHUDUIPrefab == null || hud?.QuestEntryContainer == null) return;

                _quest = UnityEngine.Object.Instantiate(journal.QuestHUDUIPrefab.gameObject, hud.QuestEntryContainer);
                _quest.name = "StackLimitPlusQuest";
                var quest = _quest.GetComponent<Il2CppScheduleOne.UI.QuestHUDUI>();
                quest.MainLabel.text = "StackLimit Plus";
                var entry = UnityEngine.Object.Instantiate(journal.QuestEntryHUDUIPrefab.gameObject, quest.EntryContainer);
                _entry = entry.GetComponent<Il2CppScheduleOne.UI.QuestEntryHUDUI>();
                _entry.MainLabel.text = line;
                _quest.transform.SetAsFirstSibling();
                if (!_loggedQuest) { _loggedQuest = true; MelonLoader.MelonLogger.Msg("[tutorial] quest hud: " + Describe(_quest.transform, 0)); }
            }
            catch (Exception ex) { Core.ErrorOnce($"Tutorial objective (quest list) failed: {ex.Message}"); RemoveQuest(); }
        }

        void CompleteQuest()
        {
            if (_quest == null) return;
            try { _entry?.Complete(); } // the game's tick + fade
            catch (Exception)
            {
                if (_entry?.MainLabel != null) _entry.MainLabel.color = new Color(0.56f, 0.85f, 0.56f);
            }
            UnityEngine.Object.Destroy(_quest, 2.5f); // then it leaves the list, like a finished quest
            _quest = null;
            _entry = null;
        }

        void RemoveQuest()
        {
            try { if (_quest != null) UnityEngine.Object.Destroy(_quest); } catch (Exception) { }
            _quest = null;
            _entry = null;
        }

        static string Describe(Transform t, int depth)
        {
            var parts = t.name + "[" + string.Join(",", t.GetComponents<Component>().Select(c => c.GetIl2CppType().Name)) + "]";
            if (depth > 4 || t.childCount == 0) return parts;
            var children = new string[t.childCount];
            for (int i = 0; i < t.childCount; i++) children[i] = Describe(t.GetChild(i), depth + 1);
            return parts + "{" + string.Join(" ", children) + "}";
        }

        void Set(string label, string text, string sub, Action? onNext)
        {
            try
            {
                if (_canvas == null) Build();
                _label!.text = label;
                _text!.text = text;
                _sub!.text = sub;
                _sub.gameObject.SetActive(sub.Length > 0);
                _onNext = onNext;
                _next!.gameObject.SetActive(onNext != null);
                _canvas!.SetActive(true);
            }
            catch (Exception ex) { Core.ErrorOnce($"Tutorial objective failed: {ex.Message}"); }
        }

        // tour ended or step skipped: remove both the top line and the quest entry
        public void Hide()
        {
            HideTop();
            RemoveQuest();
        }

        public void HideTop()
        {
            try { if (_canvas != null) _canvas.SetActive(false); } catch (Exception) { } // destroyed on scene change
        }

        void Build()
        {
            _canvas = new GameObject("StackLimitPlusObjective") { layer = 5 };
            var canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 420;
            var scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            _canvas.AddComponent<GraphicRaycaster>();

            var col = UiFactory.Column(_canvas.transform, 6, "Objective");
            col.childAlignment = TextAnchor.UpperCenter;
            col.childForceExpandWidth = false;
            var rt = col.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -110);
            rt.sizeDelta = new Vector2(760, 0);
            col.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _label = UiFactory.Text(col.transform, "", Theme.Small, Theme.TextMuted, FontStyles.Bold, TextAlignmentOptions.Center);
            _label.characterSpacing = 6;
            _text = UiFactory.Text(col.transform, "", 20, Theme.Text, FontStyles.Normal, TextAlignmentOptions.Center);
            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Overflow;
            UiFactory.Size(_text, width: 760);
            _sub = UiFactory.Text(col.transform, "", Theme.Small, Theme.TextFaint, FontStyles.Italic, TextAlignmentOptions.Center);
            _next = UiFactory.Button(col.transform, "Next", () => _onNext?.Invoke(), primary: true, width: 120);
            foreach (var t in new[] { _label, _text, _sub })
            {
                var shadow = t.gameObject.AddComponent<Shadow>(); // readable over bright scenery
                shadow.effectColor = new Color(0, 0, 0, 0.8f);
                shadow.effectDistance = new Vector2(1, -1);
            }
            _canvas.SetActive(false);
        }
    }

    // One-time corner notice on load until the tour is done: tells new players the mod exists and how to open it.
    public class TutorialNotice
    {
        GameObject? _canvas;
        CanvasGroup? _group;
        float _left;

        public void Show(string hotkey)
        {
            try
            {
                if (_canvas == null) Build(hotkey);
                _left = 10f;
                _canvas!.SetActive(true);
            }
            catch (Exception ex) { Core.ErrorOnce($"Tutorial notice failed: {ex.Message}"); }
        }

        public void Tick(bool menuOpen)
        {
            if (_canvas == null || !_canvas.activeSelf) return;
            _left -= Time.unscaledDeltaTime;
            if (menuOpen || _left <= 0) { _canvas.SetActive(false); return; }
            _group!.alpha = Mathf.Clamp01(_left); // fades over the last second
        }

        void Build(string hotkey)
        {
            _canvas = new GameObject("StackLimitPlusNotice") { layer = 5 };
            var canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;
            var scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            _canvas.AddComponent<GraphicRaycaster>();
            _group = _canvas.AddComponent<CanvasGroup>();

            var box = UiFactory.Panel(_canvas.transform, Theme.Window, rounded: true, name: "Notice");
            var rt = box.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-24, -24);
            rt.sizeDelta = new Vector2(360, 0);
            var col = box.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(18, 18, 14, 14);
            col.spacing = 4;
            col.childControlWidth = true; col.childControlHeight = true;
            col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.Text(box.transform, "StackLimit Plus is installed", 16, Theme.Text, FontStyles.Bold);
            var body = UiFactory.Text(box.transform, $"Press <b>{hotkey}</b> for a quick tour. You'll also find it as an app on your phone.", Theme.Small, Theme.TextMuted);
            body.enableWordWrapping = true;
            body.overflowMode = TextOverflowModes.Overflow;

            var b = box.gameObject.AddComponent<Button>(); // click to dismiss
            b.targetGraphic = box;
            UiFactory.Listen(b, () => _canvas.SetActive(false));
        }
    }
}
