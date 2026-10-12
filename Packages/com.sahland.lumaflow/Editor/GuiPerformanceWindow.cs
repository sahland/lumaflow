#nullable enable
using System;
using System.Globalization;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace LumaFlow.Editor {
    /// <summary>Low-overhead Editor frame sampling, with a LumaFlow-authored interface.</summary>
    public sealed class GuiPerformanceWindow : EditorWindow {
        [SerializeField] private string _scenarioText = "Current screen";
        [SerializeField] private string _lastReport = "Enter Play Mode. Automatic capture starts after a short warm-up.";
        [SerializeField] private int _sampleCount = 300;
        [SerializeField] private bool _captureInProgress;
        private readonly State<string> _scenario = new("Current screen");
        private readonly State<string> _report = new("");
        private readonly State<int> _samples = new(300);
        private readonly State<bool> _automatic = new(true);
        private readonly State<int> _controlsRevision = new(0);
        private MountHandle? _uiMount;
        private ProfilerRecorder _mainThread, _gc, _drawCalls;
        private bool _capturing, _pendingAutomatic;
        [SerializeField] private bool _capturedThisPlay;
        [SerializeField] private bool _hasReport;
        private double _started, _automaticReady;
        private string _captureScenario = "";
        private int _captureSamples;
        private static string AutomaticKey => "LumaFlow.Performance." + Hash128.Compute(Application.dataPath) + ".Automatic";
        internal bool IsCapturing => _capturing;
        internal bool HasRecorders => _mainThread.Valid || _gc.Valid || _drawCalls.Valid;

        [MenuItem("Tools/LumaFlow/GUI Performance")]
        public static void Open() => GetWindow<GuiPerformanceWindow>("GUI Performance");

        public void CreateGUI() {
            _uiMount?.Dispose();
            rootVisualElement.Clear();
            minSize = new Vector2(520, 420);
            if (_captureInProgress && !_capturing) {
                _captureInProgress = false;
                _lastReport = "Previous capture was interrupted by assembly reload. No incomplete result is shown.";
                _hasReport = false;
            }
            _scenario.Value = _scenarioText;
            _report.Value = _lastReport;
            _samples.Value = _sampleCount is 60 or 300 or 1000 ? _sampleCount : 300;
            _automatic.Value = EditorPrefs.GetBool(AutomaticKey, true);
            _uiMount = global::LumaFlow.LumaFlow.Mount(new Theme(EditorToolWidgets.Theme,
                new Scaffold(BuildWindow())), rootVisualElement);
            ScheduleAutomatic();
        }

        private Widget BuildWindow() => SizedBox.Expand(new Padding(new ScrollView(new Column(new Widget[] {
            new Row(new Widget[] {
                new Expanded(new Text("GUI Performance", EditorToolWidgets.Theme.Typography.Title)),
                new Button("UXML", UxmlPreviewWindow.Open, ButtonVariant.Secondary)
            }, gap: 12),
            EditorToolWidgets.Hint("Editor diagnostics, not Player benchmarks. Main Thread includes waits; GC and draw calls cover the whole application, including Editor UI."),
            new Checkbox(_automatic, label: "Capture automatically on entering Play Mode", onChanged: SetAutomatic),
            EditorToolWidgets.Field("Scenario", new TextField(_scenario, onChanged: value => _scenarioText = value)),
            EditorToolWidgets.Field("Capture size", new Dropdown<int>(_samples, new[] {60, 300, 1000}, value => value + " frames",
                onChanged: value => _sampleCount = value)),
            new ReactiveBuilder<int>(_controlsRevision, _ => new Row(new Widget[] {
                new Button("Capture now", BeginCapture, enabled: CanCapture),
                new Button("Cancel", CancelCapture, ButtonVariant.Secondary, enabled: _capturing || _pendingAutomatic),
                new Button("Copy report", () => EditorGUIUtility.systemCopyBuffer = _lastReport,
                    ButtonVariant.Secondary, enabled: !_capturing && _hasReport)
            }, gap: 8)),
            EditorToolWidgets.Card(new ReactiveBuilder<string>(_report, text => new Text(text))),
            EditorToolWidgets.Hint("No continuous repaint or managed sample list during capture. Counters are released on completion, cancel, pause, reload or window close. Missing counters are reported as unavailable, never zero.")
        }, gap: 16)), EdgeInsets.All(20)));

        private bool CanCapture => EditorApplication.isPlaying && !EditorApplication.isPaused && !_capturing;

        private void OnEnable() {
            _automatic.Value = EditorPrefs.GetBool(AutomaticKey, true);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.pauseStateChanged += OnPauseChanged;
            ScheduleAutomatic();
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.pauseStateChanged -= OnPauseChanged;
            if (_capturing) SetReport("Capture canceled: window closed or assemblies reloaded.");
            Stop();
            _uiMount?.Dispose(); _uiMount = null;
        }

        private void SetAutomatic(bool value) {
            EditorPrefs.SetBool(AutomaticKey, value);
            if (value) ScheduleAutomatic();
            else if (!_capturing) { _pendingAutomatic = false; EditorApplication.update -= Tick; RefreshControls(); }
        }

        private void OnPlayModeChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode) {
                if (_capturing) SetReport("Capture canceled: leaving the current mode.");
                Stop();
                _capturedThisPlay = false;
            }
            if (state == PlayModeStateChange.EnteredPlayMode) ScheduleAutomatic();
            RefreshControls();
        }

        private void OnPauseChanged(PauseState state) {
            if (state == PauseState.Paused) {
                if (_capturing) SetReport("Capture canceled: Play Mode paused.");
                Stop();
            } else ScheduleAutomatic();
            RefreshControls();
        }

        private void ScheduleAutomatic() {
            if (!_automatic.Value || _capturedThisPlay || !CanCapture || _pendingAutomatic) return;
            _pendingAutomatic = true;
            _automaticReady = EditorApplication.timeSinceStartup + 1.0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            RefreshControls();
        }

        internal void BeginCapture() {
            if (!CanCapture) return;
            Stop();
            _captureScenario = string.IsNullOrWhiteSpace(_scenario.Value) ? "Current screen" : _scenario.Value.Trim();
            _captureSamples = _samples.Value;
            _hasReport = false;
            // Update UI before starting the native recorders, not on each measured frame.
            SetReport("Capturing " + _captureSamples + " frame samples… Keep the target scenario active.\nThe report appears automatically; maximum wait is 30 seconds.");
            try {
                const ProfilerRecorderOptions options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame;
                _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", _captureSamples, options);
                _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", _captureSamples, options);
                _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", _captureSamples, options);
                if (!_mainThread.Valid) {
                    Stop(); SetReport("Main Thread counter unavailable. Use Unity Profiler."); return;
                }
                _capturedThisPlay = true;
                _started = EditorApplication.timeSinceStartup;
                _capturing = true;
                _captureInProgress = true;
                EditorApplication.update += Tick;
                RefreshControls();
            } catch (Exception exception) {
                Stop(); SetReport("Capture unavailable: " + exception.Message);
            }
        }

        internal void CancelCapture() {
            Stop();
            _hasReport = false;
            SetReport("Capture canceled. No incomplete data is presented as a completed result.");
        }

        private void Tick() {
            if (_pendingAutomatic) {
                if (!CanCapture) { Stop(); return; }
                if (EditorApplication.timeSinceStartup < _automaticReady) return;
                _pendingAutomatic = false;
                BeginCapture();
                return;
            }
            if (!_capturing) { EditorApplication.update -= Tick; return; }
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) { CancelCapture(); return; }
            if (_mainThread.Count < _captureSamples && EditorApplication.timeSinceStartup - _started < 30) return;
            string report;
            try {
                _mainThread.Stop();
                if (_gc.Valid) _gc.Stop();
                if (_drawCalls.Valid) _drawCalls.Stop();
                report = "EDITOR · " + _captureScenario + "\n\n" +
                    Summarize(_mainThread, "Main Thread", 1e-6, "ms") + "\n" +
                    Summarize(_gc, "GC/frame", 1.0 / 1024, "KiB") + "\n" +
                    Summarize(_drawCalls, "Draw calls", 1, "") + "\n\n" +
                    Application.unityVersion + " · " + SystemInfo.graphicsDeviceName +
                    "\nWhole-Editor counters; validate final budgets in a target-device Player build.";
                _hasReport = true;
            } catch (Exception exception) {
                report = "Could not read capture: " + exception.Message;
            } finally { Stop(); }
            SetReport(report);
        }

        private static string Summarize(ProfilerRecorder recorder, string label, double factor, string unit) {
            if (!recorder.Valid || recorder.Count == 0) return label + ": unavailable (not zero)";
            var values = new double[Math.Min(recorder.Count, recorder.Capacity)];
            double total = 0;
            for (var i = 0; i < values.Length; i++) { values[i] = recorder.GetSample(i).Value * factor; total += values[i]; }
            Array.Sort(values);
            return string.Format(CultureInfo.InvariantCulture,
                "{0}: avg {1:0.###}, p95 {2:0.###}, max {3:0.###} {4} (n={5})",
                label, total / values.Length, values[(int)Math.Ceiling(values.Length * .95) - 1], values[^1], unit, values.Length);
        }

        private void SetReport(string text) { _lastReport = text; _report.Value = text; RefreshControls(); }
        private void RefreshControls() => _controlsRevision.Value++;

        private void Stop() {
            EditorApplication.update -= Tick;
            _pendingAutomatic = false; _capturing = false;
            _captureInProgress = false;
            _mainThread.Dispose(); _gc.Dispose(); _drawCalls.Dispose();
            _mainThread = default; _gc = default; _drawCalls = default;
            RefreshControls();
        }
    }
}
