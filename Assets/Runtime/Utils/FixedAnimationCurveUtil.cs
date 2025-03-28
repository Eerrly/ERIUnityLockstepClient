using System.Collections.Generic;

public class FixedAnimationCurveUtil
{
    private static FixedNumber Hermite(FixedNumber start, FixedNumber end, FixedNumber time, FixedNumber inTangent, FixedNumber outTangent)
    {
        var t = time;
        var t2 = time * time;
        var t3 = t2 * time;
        var t2t3 = 2 * t3;
        var t3t2 = 3 * t2;
        var t2t3mt3t2 = t2t3 - t3t2;
        var t3st2 = t3 - t2;
        var h1 = t2t3mt3t2 + 1;
        var h2 = -t2t3mt3t2;
        var h3 = t3st2 - t2 + t;
        var h4 = t3st2;
        return h1 * start + h2 * end + h3 * inTangent + h4 * outTangent;
    }

    private static FixedNumber Evaluate(List<KeyFrame> points, FixedNumber time)
    {
        if (points == null || points.Count <= 0)
            return FixedNumber.Zero;
        for (int i = 1; i < points.Count; i++)
        {
            var point = points[i];
            var endKeyTime = FixedNumber.MakeFixNum(point.time, FixedMath.DataConrvertScale);
            if (time < endKeyTime)
            {
                var lastPoint = points[i - 1];
                var startKeyTime = FixedNumber.MakeFixNum(lastPoint.time, FixedMath.DataConrvertScale);
                var scale = (endKeyTime - startKeyTime);
                return Hermite(
                    FixedNumber.MakeFixNum(lastPoint.val, FixedMath.DataConrvertScale),
                    FixedNumber.MakeFixNum(point.val, FixedMath.DataConrvertScale),
                    (time - startKeyTime) / (endKeyTime - startKeyTime),
                    FixedNumber.MakeFixNum(lastPoint.outTan, FixedMath.DataConrvertScale) * scale,
                    FixedNumber.MakeFixNum(point.inTan, FixedMath.DataConrvertScale) * scale);
            }
        }
        return FixedNumber.MakeFixNum(points[^1].val, FixedMath.DataConrvertScale);
    }
    
    public static FixedVector3 HermiteEvaluateVector3(Vector3Curve pos, FixedNumber time)
    {
        return new FixedVector3(
            Evaluate(pos.x.points, time),
            Evaluate(pos.y.points, time),
            Evaluate(pos.z.points, time));
    }
    
    public static FixedQuaternion HermiteEvaluateQuaternion(QuaternionCurve pos, FixedNumber time)
    {
        return new FixedQuaternion(
            Evaluate(pos.x.points, time),
            Evaluate(pos.y.points, time),
            Evaluate(pos.z.points, time),
            Evaluate(pos.w.points, time));
    }
    
}