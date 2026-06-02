using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(AnimationData))]
public class AnimationDataInspector : Editor
{
    private const float ToFloatFactor = 1 / 10000.0f;
    private const string StylePath = "Assets/Editor/AnimationDataToolStyles.uss";

    public override VisualElement CreateInspectorGUI()
    {
        var animationData = (AnimationData)target;
        var root = new VisualElement();
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
        if (styleSheet != null)
            root.styleSheets.Add(styleSheet);

        root.AddToClassList("adt-root");
        root.Add(BuildHeader(animationData));
        root.Add(BuildEventSummary(animationData));
        root.Add(BuildCurveSummary("RootT Position", animationData.positionCurve.x, animationData.positionCurve.y, animationData.positionCurve.z));
        root.Add(BuildCurveSummary("RootQ Rotation", animationData.rotationCurve.x, animationData.rotationCurve.y, animationData.rotationCurve.z, animationData.rotationCurve.w));
        return root;
    }

    private VisualElement BuildHeader(AnimationData animationData)
    {
        var panel = Panel("AnimationData");
        panel.Add(new Label($"Length: {animationData.length:0.000}s").WithClass("adt-frame-readout"));
        panel.Add(new Label($"Events: {SafeCount(animationData.eventList)}").WithClass("adt-subtitle"));
        return panel;
    }

    private VisualElement BuildEventSummary(AnimationData animationData)
    {
        var panel = Panel("Events");
        if (animationData.eventList == null || animationData.eventList.Count == 0)
        {
            var status = new Label("No events. Non-loop combat clips usually need AnimStart, Fire, and AnimEnd.");
            status.AddToClassList("adt-status");
            status.AddToClassList("adt-status-warning");
            panel.Add(status);
            return panel;
        }

        var sortedEvents = new List<Event>(animationData.eventList);
        sortedEvents.Sort((left, right) =>
        {
            var frameCompare = left.frame.CompareTo(right.frame);
            return frameCompare != 0 ? frameCompare : left.type.CompareTo(right.type);
        });

        foreach (var animationEvent in sortedEvents)
        {
            var row = new VisualElement();
            row.AddToClassList("adt-event-row");
            row.Add(new Label(((EAnimationEvent)animationEvent.type).ToString()).WithClass("adt-event-type"));
            row.Add(new Label($"Frame {animationEvent.frame}").WithClass("adt-event-frame"));
            row.Add(new Label($"Time {(animationEvent.frame / 30f):0.000}s @30fps").WithClass("adt-subtitle"));
            panel.Add(row);
        }

        return panel;
    }

    private VisualElement BuildCurveSummary(string title, params Curve[] curves)
    {
        var panel = Panel(title);
        var names = title.Contains("RootT") ? new[] { "X", "Y", "Z" } : new[] { "X", "Y", "Z", "W" };
        for (var i = 0; i < curves.Length; i++)
            panel.Add(BuildCurveRow(names[i], curves[i]));
        return panel;
    }

    private VisualElement BuildCurveRow(string axisName, Curve curve)
    {
        var row = new VisualElement();
        row.AddToClassList("adt-curve-box");

        var header = new VisualElement();
        header.AddToClassList("adt-row");
        header.Add(new Label(axisName).WithClass("adt-curve-axis"));
        header.Add(new Label(DescribeCurve(curve)).WithClass("adt-curve-summary"));

        var curveField = new CurveField { value = ToAnimationCurve(curve) };
        curveField.AddToClassList("adt-curve-field");
        curveField.SetEnabled(false);

        row.Add(header);
        row.Add(curveField);
        return row;
    }

    private string DescribeCurve(Curve curve)
    {
        if (curve.points == null || curve.points.Count == 0)
            return "Missing";

        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var point in curve.points)
        {
            var value = point.val * ToFloatFactor;
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        var first = curve.points[0].val * ToFloatFactor;
        var last = curve.points[^1].val * ToFloatFactor;
        return $"Keys {curve.points.Count}    First {first:0.0000}    Last {last:0.0000}    Range [{min:0.0000}, {max:0.0000}]";
    }

    private AnimationCurve ToAnimationCurve(Curve curve)
    {
        var animationCurve = new AnimationCurve();
        if (curve.points == null)
            return animationCurve;

        for (var i = 0; i < curve.points.Count; i++)
        {
            var point = curve.points[i];
            animationCurve.AddKey(new Keyframe
            {
                time = point.time * ToFloatFactor,
                value = point.val * ToFloatFactor,
                inTangent = point.inTan * ToFloatFactor,
                outTangent = point.outTan * ToFloatFactor
            });
        }

        return animationCurve;
    }

    private VisualElement Panel(string title)
    {
        var panel = new VisualElement();
        panel.AddToClassList("adt-panel");
        panel.Add(new Label(title).WithClass("adt-section-title"));
        return panel;
    }

    private int SafeCount<T>(List<T> list)
    {
        return list == null ? 0 : list.Count;
    }
}
