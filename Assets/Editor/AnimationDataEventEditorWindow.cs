using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class AnimationDataEventEditorWindow : EditorWindow
{
    private const int DataConvertScale = 10000;
    private const string DefaultOutputFolder = "Assets/Resources/Data/AnimationData";
    private const string StylePath = "Assets/Editor/AnimationDataToolStyles.uss";
    private const int RuntimeEventFrameRate = 30;

    private AnimationClip _clip;
    private GameObject _previewTarget;
    private string _outputFolder = DefaultOutputFolder;
    private int _currentFrame;
    private int _frameRate = RuntimeEventFrameRate;
    private EAnimationEvent _eventToAdd = EAnimationEvent.Fire;
    private bool _relativePositionToFirstFrame = true;
    private bool _includeRotationCurve = true;
    private bool _dirty;
    private bool _isPlaying;
    private double _lastPlaybackTime;
    private float _playbackFrameAccumulator;
    private bool _loadedExistingAsset;
    private string _lastStatus = "Ready";
    private readonly List<Event> _events = new();

    private PreviewRenderUtility _previewUtility;
    private GameObject _previewInstance;
    private Quaternion _previewRotation = Quaternion.Euler(18f, -28f, 0f);
    private float _previewDistance = 4f;

    private ObjectField _clipField;
    private ObjectField _previewTargetField;
    private TextField _outputFolderField;
    private SliderInt _frameSlider;
    private IntegerField _frameRateField;
    private Label _frameReadout;
    private Label _previewHint;
    private Button _playButton;
    private IMGUIContainer _previewContainer;
    private VisualElement _timeline;
    private VisualElement _eventList;
    private Label _validationLabel;
    private Label _statusLabel;
    private Button _generateButton;

    [MenuItem("Tools/Animation Data Event Editor")]
    public static void Open()
    {
        GetWindow<AnimationDataEventEditorWindow>("Animation Data");
    }

    public void CreateGUI()
    {
        minSize = new Vector2(820, 560);
        rootVisualElement.Clear();

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
        if (styleSheet != null)
            rootVisualElement.styleSheets.Add(styleSheet);

        rootVisualElement.AddToClassList("adt-root");
        rootVisualElement.Add(BuildHeader());

        var scroll = new ScrollView { name = "animation-data-scroll" };
        scroll.AddToClassList("adt-scroll");
        scroll.Add(BuildBody());
        rootVisualElement.Add(scroll);

        RefreshUi();
    }

    private void OnDisable()
    {
        StopPlayback();
        CleanupPreview();
    }

    private VisualElement BuildHeader()
    {
        var header = new VisualElement();
        header.AddToClassList("adt-header");
        header.Add(new Label("AnimationData Event Editor").WithClass("adt-title"));
        header.Add(new Label("Drag an AnimationClip, preview it frame by frame, mark events, and export AnimationData.asset.").WithClass("adt-subtitle"));
        return header;
    }

    private VisualElement BuildBody()
    {
        var body = new VisualElement();
        body.AddToClassList("adt-body");

        var split = new VisualElement();
        split.AddToClassList("adt-split");

        var left = new VisualElement();
        left.AddToClassList("adt-column");
        left.AddToClassList("adt-column-left");
        left.Add(BuildSourcePanel());
        left.Add(BuildPreviewPanel());

        var right = new VisualElement();
        right.AddToClassList("adt-column");
        right.Add(BuildEventPanel());
        right.Add(BuildExportPanel());

        split.Add(left);
        split.Add(right);
        body.Add(split);
        return body;
    }

    private VisualElement BuildSourcePanel()
    {
        var panel = Panel("Source");

        _clipField = new ObjectField("Animation Clip") { objectType = typeof(AnimationClip), allowSceneObjects = false };
        _clipField.SetValueWithoutNotify(_clip);
        _clipField.AddToClassList("adt-field");
        _clipField.RegisterValueChangedCallback(evt =>
        {
            _clip = evt.newValue as AnimationClip;
            ResetForClip();
            RebuildPreviewInstance();
        });

        _previewTargetField = new ObjectField("Preview Model") { objectType = typeof(GameObject), allowSceneObjects = true };
        _previewTargetField.SetValueWithoutNotify(_previewTarget);
        _previewTargetField.AddToClassList("adt-field");
        _previewTargetField.RegisterValueChangedCallback(evt =>
        {
            _previewTarget = evt.newValue as GameObject;
            RebuildPreviewInstance();
            RefreshUi();
        });

        var outputRow = new VisualElement();
        outputRow.AddToClassList("adt-row");
        _outputFolderField = new TextField("Output Folder") { value = _outputFolder };
        _outputFolderField.AddToClassList("adt-field");
        _outputFolderField.RegisterValueChangedCallback(evt => _outputFolder = evt.newValue);
        var selectButton = Button("Select", SelectOutputFolder);
        outputRow.Add(_outputFolderField);
        outputRow.Add(selectButton);

        panel.Add(_clipField);
        panel.Add(_previewTargetField);
        panel.Add(outputRow);
        return panel;
    }

    private VisualElement BuildPreviewPanel()
    {
        var panel = Panel("Frame Preview");

        _previewContainer = new IMGUIContainer(DrawPreview);
        _previewContainer.AddToClassList("adt-preview");
        _previewHint = new Label("Drop a model or an FBX clip with a source model to preview.");
        _previewHint.AddToClassList("adt-preview-hint");

        var controls = new VisualElement();
        controls.AddToClassList("adt-row");
        controls.Add(Button("<", () => SetFrame(_currentFrame - 1)));
        _playButton = Button("Play", TogglePlayback, true);
        controls.Add(_playButton);

        _frameSlider = new SliderInt(0, 0);
        _frameSlider.AddToClassList("adt-frame-slider");
        _frameSlider.RegisterValueChangedCallback(evt => SetFrame(evt.newValue, false));
        controls.Add(_frameSlider);
        controls.Add(Button(">", () => SetFrame(_currentFrame + 1)));

        _frameReadout = new Label();
        _frameReadout.AddToClassList("adt-frame-readout");

        _frameRateField = new IntegerField("Runtime Event FPS") { value = _frameRate };
        _frameRateField.tooltip = "AnimationManager converts event frames as 30fps runtime frames, so exported events stay on this scale regardless of clip sample rate.";
        _frameRateField.AddToClassList("adt-field");
        _frameRateField.SetEnabled(false);

        panel.Add(_previewContainer);
        panel.Add(_previewHint);
        panel.Add(controls);
        panel.Add(_frameReadout);
        panel.Add(_frameRateField);
        return panel;
    }

    private VisualElement BuildEventPanel()
    {
        var panel = Panel("Frame Events");
        var addRow = new VisualElement();
        addRow.AddToClassList("adt-row");

        var eventField = new EnumField("Event", _eventToAdd);
        eventField.AddToClassList("adt-event-add-field");
        eventField.RegisterValueChangedCallback(evt => _eventToAdd = (EAnimationEvent)evt.newValue);

        addRow.Add(eventField);
        addRow.Add(Button("Add At Current Frame", () =>
        {
            if (_eventToAdd == EAnimationEvent.None || _eventToAdd == EAnimationEvent.Count)
                return;

            AddOrReplaceEvent(_eventToAdd, _currentFrame);
            _dirty = true;
            RefreshUi();
        }, true));

        _timeline = new VisualElement();
        _timeline.AddToClassList("adt-timeline");
        _eventList = new VisualElement();

        panel.Add(addRow);
        panel.Add(_timeline);
        panel.Add(_eventList);
        return panel;
    }

    private VisualElement BuildExportPanel()
    {
        var panel = Panel("Export And Validation");

        var relativeToggle = new Toggle("RootT Relative To First Frame") { value = _relativePositionToFirstFrame };
        relativeToggle.RegisterValueChangedCallback(evt => _relativePositionToFirstFrame = evt.newValue);

        var rotationToggle = new Toggle("Include RootQ Curve") { value = _includeRotationCurve };
        rotationToggle.RegisterValueChangedCallback(evt => _includeRotationCurve = evt.newValue);

        _validationLabel = new Label();
        _validationLabel.AddToClassList("adt-status");

        var buttons = new VisualElement();
        buttons.AddToClassList("adt-row");
        buttons.Add(Button("Load Events", LoadExistingAssetEvents));
        buttons.Add(Button("Clear Events", ClearEvents));
        _generateButton = Button("Generate Asset", ExportAnimationData, true);
        buttons.Add(_generateButton);

        _statusLabel = new Label();
        _statusLabel.AddToClassList("adt-status");
        _statusLabel.AddToClassList("adt-status-info");

        panel.Add(relativeToggle);
        panel.Add(rotationToggle);
        panel.Add(_validationLabel);
        panel.Add(buttons);
        panel.Add(_statusLabel);
        return panel;
    }

    private VisualElement Panel(string title)
    {
        var panel = new VisualElement();
        panel.AddToClassList("adt-panel");
        panel.Add(new Label(title).WithClass("adt-section-title"));
        return panel;
    }

    private Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button(action) { text = text };
        button.AddToClassList("adt-button");
        if (primary)
            button.AddToClassList("adt-button-primary");
        return button;
    }

    private void RefreshUi()
    {
        var totalFrames = GetTotalFrames();
        if (_frameSlider != null)
        {
            _frameSlider.highValue = totalFrames;
            _frameSlider.SetValueWithoutNotify(Mathf.Clamp(_currentFrame, 0, totalFrames));
        }

        if (_frameReadout != null)
            _frameReadout.text = _clip == null
                ? "No AnimationClip loaded."
                : $"Frame {_currentFrame} / {totalFrames}    Time {GetCurrentTime():0.000}s / {_clip.length:0.000}s";

        if (_previewHint != null)
            _previewHint.text = _previewInstance == null
                ? "No preview model. Drag a model/prefab into Preview Model, or use a clip sub-asset from an FBX."
                : "Left drag: rotate preview. Mouse wheel: zoom.";
        if (_playButton != null)
            _playButton.text = _isPlaying ? "Pause" : "Play";
        if (_generateButton != null)
            _generateButton.text = HasExistingOutputAsset() ? "Update Asset" : "Create Asset";

        DrawTimeline();
        DrawEvents();
        DrawValidation();
        DrawStatus();
        RepaintPreview();
    }

    private void DrawTimeline()
    {
        if (_timeline == null)
            return;

        _timeline.Clear();
        var totalFrames = Mathf.Max(1, GetTotalFrames());
        foreach (var animationEvent in _events)
            _timeline.Add(Marker(animationEvent.frame / (float)totalFrames, (EAnimationEvent)animationEvent.type, false));

        var current = Marker(_currentFrame / (float)totalFrames, EAnimationEvent.None, true);
        current.AddToClassList("adt-marker-current");
        _timeline.Add(current);
    }

    private VisualElement Marker(float normalized, EAnimationEvent eventType, bool current)
    {
        var marker = new VisualElement();
        marker.AddToClassList("adt-marker");
        normalized = Mathf.Clamp01(normalized);
        if (normalized <= 0.001f)
        {
            marker.style.left = 0;
        }
        else if (normalized >= 0.999f)
        {
            marker.style.right = 0;
        }
        else
        {
            marker.style.left = Length.Percent(normalized * 100f);
            marker.style.marginLeft = current ? -1 : -2;
        }

        switch (eventType)
        {
            case EAnimationEvent.AnimStart:
                marker.AddToClassList("adt-marker-start");
                break;
            case EAnimationEvent.Fire:
                marker.AddToClassList("adt-marker-fire");
                break;
            case EAnimationEvent.AnimEnd:
                marker.AddToClassList("adt-marker-end");
                break;
        }
        return marker;
    }

    private void DrawEvents()
    {
        if (_eventList == null)
            return;

        _eventList.Clear();
        _events.Sort((left, right) =>
        {
            var frameCompare = left.frame.CompareTo(right.frame);
            return frameCompare != 0 ? frameCompare : left.type.CompareTo(right.type);
        });

        if (_events.Count == 0)
        {
            _eventList.Add(new Label("No events yet. Mark AnimStart, Fire, and AnimEnd for non-loop combat clips.").WithClass("adt-subtitle"));
            return;
        }

        for (var i = 0; i < _events.Count; i++)
        {
            var index = i;
            var animationEvent = _events[index];
            var row = new VisualElement();
            row.AddToClassList("adt-event-row");

            var typeField = new EnumField((EAnimationEvent)animationEvent.type);
            typeField.AddToClassList("adt-event-type");
            typeField.RegisterValueChangedCallback(evt =>
            {
                var item = _events[index];
                item.type = (int)(EAnimationEvent)evt.newValue;
                _events[index] = item;
                _dirty = true;
                RefreshUi();
            });

            var frameField = new IntegerField { value = animationEvent.frame };
            frameField.AddToClassList("adt-event-frame");
            frameField.RegisterValueChangedCallback(evt =>
            {
                var item = _events[index];
                item.frame = Mathf.Clamp(evt.newValue, 0, GetTotalFrames());
                _events[index] = item;
                _dirty = true;
                RefreshUi();
            });

            row.Add(typeField);
            row.Add(frameField);
            row.Add(Button("Go", () => SetFrame(_events[index].frame)));
            var deleteButton = Button("X", () =>
            {
                _events.RemoveAt(index);
                _dirty = true;
                RefreshUi();
            });
            deleteButton.AddToClassList("adt-button-danger");
            row.Add(deleteButton);
            _eventList.Add(row);
        }
    }

    private void DrawValidation()
    {
        if (_validationLabel == null)
            return;

        if (_clip == null)
        {
            SetStatusClass(_validationLabel, "adt-status-info");
            _validationLabel.text = "Load an AnimationClip to validate event frames and output path.";
            return;
        }

        var totalFrames = GetTotalFrames();
        var hasStart = false;
        var hasFire = false;
        var hasEnd = false;
        var invalidCount = 0;
        var duplicateCount = 0;
        var seen = new HashSet<string>();

        foreach (var animationEvent in _events)
        {
            var eventType = (EAnimationEvent)animationEvent.type;
            hasStart |= eventType == EAnimationEvent.AnimStart;
            hasFire |= eventType == EAnimationEvent.Fire;
            hasEnd |= eventType == EAnimationEvent.AnimEnd;
            if (eventType == EAnimationEvent.None || eventType == EAnimationEvent.Count || animationEvent.frame < 0 || animationEvent.frame > totalFrames)
                invalidCount++;
            if (!seen.Add($"{animationEvent.type}:{animationEvent.frame}"))
                duplicateCount++;
        }

        var hasError = invalidCount > 0 || duplicateCount > 0;
        var hasWarning = !hasStart || !hasFire || !hasEnd || _previewInstance == null;
        SetStatusClass(_validationLabel, hasError ? "adt-status-error" : hasWarning ? "adt-status-warning" : "adt-status-info");
        _validationLabel.text = $"Clip {_clip.length:0.000}s / {_frameRate} FPS / {totalFrames} frames\n" +
                                $"AnimStart {(hasStart ? "OK" : "Missing")}    Fire {(hasFire ? "OK" : "Missing")}    AnimEnd {(hasEnd ? "OK" : "Missing")}\n" +
                                $"Invalid {invalidCount}    Duplicate {duplicateCount}" +
                                (_previewInstance == null ? "\nPreview model missing." : "") +
                                (HasExistingOutputAsset() ? "\nExisting AnimationData found; Update Asset will modify it." : "\nNo existing AnimationData; Create Asset will make a new file.");
    }

    private void DrawStatus()
    {
        if (_statusLabel == null)
            return;

        _statusLabel.text = _clip == null
            ? "Target: -\nStatus: waiting for AnimationClip"
            : $"Target: {GetOutputAssetPath()}\nMode: {(HasExistingOutputAsset() ? "Modify existing asset" : "Create new asset")}\nStatus: {(_dirty ? "Modified, not saved" : _lastStatus)}";
    }

    private void SetStatusClass(Label label, string className)
    {
        label.RemoveFromClassList("adt-status-info");
        label.RemoveFromClassList("adt-status-warning");
        label.RemoveFromClassList("adt-status-error");
        label.AddToClassList(className);
    }

    private void ResetForClip()
    {
        _events.Clear();
        _currentFrame = 0;
        _frameRate = RuntimeEventFrameRate;
        if (_frameRateField != null)
            _frameRateField.SetValueWithoutNotify(_frameRate);
        AutoLoadExistingAsset();
        RefreshUi();
    }

    private int GetTotalFrames()
    {
        return _clip == null ? 0 : Mathf.Max(0, Mathf.RoundToInt(_clip.length * _frameRate));
    }

    private float GetCurrentTime()
    {
        return _clip == null ? 0 : Mathf.Clamp(_currentFrame / (float)_frameRate, 0, _clip.length);
    }

    private void SetFrame(int frame, bool updateSlider = true)
    {
        _currentFrame = Mathf.Clamp(frame, 0, GetTotalFrames());
        if (updateSlider && _frameSlider != null)
            _frameSlider.SetValueWithoutNotify(_currentFrame);
        RefreshUi();
    }

    private void TogglePlayback()
    {
        if (_isPlaying)
        {
            StopPlayback();
            RefreshUi();
            return;
        }

        if (_clip == null)
            return;

        _isPlaying = true;
        _lastPlaybackTime = EditorApplication.timeSinceStartup;
        _playbackFrameAccumulator = 0f;
        EditorApplication.update += UpdatePlayback;
        RefreshUi();
    }

    private void StopPlayback()
    {
        if (!_isPlaying)
            return;

        _isPlaying = false;
        EditorApplication.update -= UpdatePlayback;
    }

    private void UpdatePlayback()
    {
        if (!_isPlaying || _clip == null)
        {
            StopPlayback();
            return;
        }

        var now = EditorApplication.timeSinceStartup;
        var delta = Mathf.Max(0f, (float)(now - _lastPlaybackTime));
        _lastPlaybackTime = now;

        _playbackFrameAccumulator += delta * _frameRate;
        var frameStep = Mathf.FloorToInt(_playbackFrameAccumulator);
        if (frameStep <= 0)
            return;
        _playbackFrameAccumulator -= frameStep;

        var totalFrames = Mathf.Max(1, GetTotalFrames());
        var nextFrame = _currentFrame + frameStep;
        if (nextFrame > totalFrames)
            nextFrame = 0;

        _currentFrame = nextFrame;
        if (_frameSlider != null)
            _frameSlider.SetValueWithoutNotify(_currentFrame);
        RefreshUi();
    }

    private void AddOrReplaceEvent(EAnimationEvent eventType, int frame)
    {
        for (var i = 0; i < _events.Count; i++)
        {
            if (_events[i].type != (int)eventType)
                continue;

            _events[i] = new Event { type = (int)eventType, frame = frame };
            return;
        }

        _events.Add(new Event { type = (int)eventType, frame = frame });
    }

    private void SelectOutputFolder()
    {
        var absolutePath = EditorUtility.OpenFolderPanel("Select AnimationData Output Folder", Application.dataPath, "");
        if (string.IsNullOrEmpty(absolutePath))
            return;

        var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");
        absolutePath = absolutePath.Replace("\\", "/");
        if (!absolutePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Invalid Folder", "Select a folder inside this Unity project.", "OK");
            return;
        }

        _outputFolder = absolutePath.Substring(projectPath.Length + 1);
        _outputFolderField?.SetValueWithoutNotify(_outputFolder);
        RefreshUi();
    }

    private void LoadExistingAssetEvents()
    {
        if (_clip == null)
            return;

        var existing = AssetDatabase.LoadAssetAtPath<AnimationData>(GetOutputAssetPath());
        if (existing == null)
        {
            EditorUtility.DisplayDialog("Not Found", $"Not found: {GetOutputAssetPath()}", "OK");
            return;
        }

        LoadFromExistingAsset(existing);
        RefreshUi();
    }

    private void ClearEvents()
    {
        if (_events.Count == 0)
            return;

        if (!EditorUtility.DisplayDialog("Clear Events", "Clear the current event list? Saved assets are unchanged until you generate again.", "Clear", "Cancel"))
            return;

        _events.Clear();
        _dirty = true;
        RefreshUi();
    }

    private void ExportAnimationData()
    {
        if (_clip == null)
            return;

        if (!AssetDatabase.IsValidFolder(_outputFolder))
            Directory.CreateDirectory(Path.GetFullPath(_outputFolder));

        var assetPath = GetOutputAssetPath();
        var animationData = AssetDatabase.LoadAssetAtPath<AnimationData>(assetPath);
        if (animationData == null)
        {
            animationData = CreateInstance<AnimationData>();
            AssetDatabase.CreateAsset(animationData, assetPath);
        }

        animationData.length = _clip.length;
        animationData.eventList = new List<Event>(_events);
        BuildCurves(_clip, animationData);

        EditorUtility.SetDirty(animationData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = animationData;
        EditorGUIUtility.PingObject(animationData);
        _dirty = false;
        _loadedExistingAsset = true;
        _lastStatus = $"Saved {assetPath}";
        RefreshUi();
    }

    private void AutoLoadExistingAsset()
    {
        _loadedExistingAsset = false;
        _dirty = false;
        if (_clip == null)
        {
            _lastStatus = "Ready";
            return;
        }

        var existing = AssetDatabase.LoadAssetAtPath<AnimationData>(GetOutputAssetPath());
        if (existing == null)
        {
            _lastStatus = "Clip loaded; no matching AnimationData asset";
            return;
        }

        LoadFromExistingAsset(existing);
    }

    private void LoadFromExistingAsset(AnimationData existing)
    {
        _events.Clear();
        if (existing.eventList != null)
            _events.AddRange(existing.eventList);
        _loadedExistingAsset = true;
        _dirty = false;
        _lastStatus = "Existing AnimationData loaded for editing";
    }

    private bool HasExistingOutputAsset()
    {
        return _clip != null && AssetDatabase.LoadAssetAtPath<AnimationData>(GetOutputAssetPath()) != null;
    }

    private string GetOutputAssetPath()
    {
        var folder = string.IsNullOrEmpty(_outputFolder) ? DefaultOutputFolder : _outputFolder.TrimEnd('/', '\\');
        return $"{folder}/{_clip.name}.asset";
    }

    private void BuildCurves(AnimationClip clip, AnimationData data)
    {
        data.positionCurve = new Vector3Curve();
        data.rotationCurve = new QuaternionCurve();

        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
                continue;

            switch (binding.propertyName)
            {
                case "RootT.x":
                    data.positionCurve.x = GenerateCurve(curve, _relativePositionToFirstFrame);
                    break;
                case "RootT.y":
                    data.positionCurve.y = GenerateCurve(curve, _relativePositionToFirstFrame);
                    break;
                case "RootT.z":
                    data.positionCurve.z = GenerateCurve(curve, _relativePositionToFirstFrame);
                    break;
                case "RootQ.x":
                    if (_includeRotationCurve) data.rotationCurve.x = GenerateCurve(curve, false);
                    break;
                case "RootQ.y":
                    if (_includeRotationCurve) data.rotationCurve.y = GenerateCurve(curve, false);
                    break;
                case "RootQ.z":
                    if (_includeRotationCurve) data.rotationCurve.z = GenerateCurve(curve, false);
                    break;
                case "RootQ.w":
                    if (_includeRotationCurve) data.rotationCurve.w = GenerateCurve(curve, false);
                    break;
            }
        }
    }

    private static Curve GenerateCurve(AnimationCurve animationCurve, bool relativeToFirstKey)
    {
        var curve = new Curve { points = new List<KeyFrame>() };
        if (animationCurve == null || animationCurve.length == 0)
            return curve;

        var baseValue = relativeToFirstKey ? animationCurve[0].value : 0f;
        for (var i = 0; i < animationCurve.length; i++)
        {
            var key = animationCurve[i];
            curve.points.Add(new KeyFrame
            {
                val = Mathf.RoundToInt((key.value - baseValue) * DataConvertScale),
                time = Mathf.RoundToInt(key.time * DataConvertScale),
                inTan = Mathf.RoundToInt(key.inTangent * DataConvertScale),
                outTan = Mathf.RoundToInt(key.outTangent * DataConvertScale)
            });
        }

        return curve;
    }

    private void RebuildPreviewInstance()
    {
        DestroyPreviewInstance();
        var source = ResolvePreviewSource();
        if (source == null)
        {
            RefreshUi();
            return;
        }

        EnsurePreviewUtility();
        _previewInstance = Instantiate(source);
        _previewInstance.hideFlags = HideFlags.HideAndDontSave;
        _previewInstance.transform.position = Vector3.zero;
        _previewInstance.transform.rotation = Quaternion.identity;
        _previewUtility.AddSingleGO(_previewInstance);
        RepaintPreview();
    }

    private GameObject ResolvePreviewSource()
    {
        if (_previewTarget != null)
            return _previewTarget;
        if (_clip == null)
            return null;

        var path = AssetDatabase.GetAssetPath(_clip);
        if (string.IsNullOrEmpty(path))
            return null;

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private void EnsurePreviewUtility()
    {
        if (_previewUtility != null)
            return;

        _previewUtility = new PreviewRenderUtility();
        _previewUtility.camera.clearFlags = CameraClearFlags.Color;
        _previewUtility.camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
        _previewUtility.camera.fieldOfView = 35f;
        _previewUtility.lights[0].intensity = 1.2f;
        _previewUtility.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
        _previewUtility.lights[1].intensity = 0.8f;
    }

    private void DestroyPreviewInstance()
    {
        if (_previewInstance == null)
            return;

        DestroyImmediate(_previewInstance);
        _previewInstance = null;
    }

    private void CleanupPreview()
    {
        DestroyPreviewInstance();
        if (_previewUtility == null)
            return;

        _previewUtility.Cleanup();
        _previewUtility = null;
    }

    private void RepaintPreview()
    {
        _previewContainer?.MarkDirtyRepaint();
    }

    private void DrawPreview()
    {
        var rect = GUILayoutUtility.GetRect(10, 260, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
        HandlePreviewInput(rect);

        if (_clip == null || _previewInstance == null)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.11f, 0.13f, 1f));
            var message = _clip == null ? "Drop an AnimationClip" : "Drop a Preview Model";
            EditorGUI.LabelField(rect, message, EditorStyles.centeredGreyMiniLabel);
            return;
        }

        EnsurePreviewUtility();
        _clip.SampleAnimation(_previewInstance, GetCurrentTime());
        var bounds = CalculateBounds(_previewInstance);
        var center = bounds.center;
        var radius = Mathf.Max(0.5f, bounds.extents.magnitude);
        var distance = Mathf.Max(_previewDistance, radius * 2.4f);

        _previewUtility.BeginPreview(rect, GUIStyle.none);
        _previewUtility.camera.transform.position = center + _previewRotation * (Vector3.back * distance);
        _previewUtility.camera.transform.rotation = _previewRotation;
        _previewUtility.camera.nearClipPlane = 0.01f;
        _previewUtility.camera.farClipPlane = Mathf.Max(100f, distance * 5f);
        DrawGrid(center, radius);
        _previewUtility.camera.Render();
        var texture = _previewUtility.EndPreview();
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);

        var footer = new Rect(rect.x + 6, rect.yMax - 22, rect.width - 12, 18);
        EditorGUI.LabelField(footer, $"Frame {_currentFrame}    {GetCurrentTime():0.000}s", EditorStyles.whiteMiniLabel);
    }

    private void HandlePreviewInput(Rect rect)
    {
        var evt = UnityEngine.Event.current;
        if (!rect.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.MouseDrag && evt.button == 0)
        {
            _previewRotation = Quaternion.Euler(evt.delta.y * 0.35f, evt.delta.x * 0.35f, 0f) * _previewRotation;
            evt.Use();
            RepaintPreview();
        }
        else if (evt.type == EventType.ScrollWheel)
        {
            _previewDistance = Mathf.Clamp(_previewDistance + evt.delta.y * 0.12f, 0.8f, 20f);
            evt.Use();
            RepaintPreview();
        }
    }

    private void DrawGrid(Vector3 center, float radius)
    {
        Handles.SetCamera(_previewUtility.camera);
        var floorY = center.y - radius * 0.9f;
        var size = Mathf.Max(2f, radius * 2.5f);
        var step = size / 8f;
        Handles.color = new Color(1f, 1f, 1f, 0.12f);
        for (var i = -8; i <= 8; i++)
        {
            var offset = i * step;
            Handles.DrawLine(new Vector3(center.x - size, floorY, center.z + offset), new Vector3(center.x + size, floorY, center.z + offset));
            Handles.DrawLine(new Vector3(center.x + offset, floorY, center.z - size), new Vector3(center.x + offset, floorY, center.z + size));
        }
    }

    private Bounds CalculateBounds(GameObject gameObject)
    {
        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(gameObject.transform.position, Vector3.one);

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}

internal static class AnimationDataUiToolkitExtensions
{
    public static T WithClass<T>(this T element, string className) where T : VisualElement
    {
        element.AddToClassList(className);
        return element;
    }
}
