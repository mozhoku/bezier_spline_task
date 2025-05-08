using UnityEngine;

public class Lane : MonoBehaviour
{
    [SerializeField] private Bezier rightBezier;
    [SerializeField] private Bezier leftBezier;

    public void SetRightBezier(Bezier bezier)
    {
        rightBezier = bezier;
    }

    public void SetLeftBezier(Bezier bezier)
    {
        leftBezier = bezier;
    }

    public void SampleBezierLines(CurveSamplingMode mode, float value, bool updateChildren, bool drawGizmos)
    {
        if (rightBezier == null || leftBezier == null)
        {
            Debug.LogError("Right or Left Bezier is not set.");
            return;
        }

        // Clear existing sample points
        rightBezier.ClearSamplePoints();
        leftBezier.ClearSamplePoints();

        rightBezier.DestroySamplePointObjects();
        leftBezier.DestroySamplePointObjects();

        // sample curves
        rightBezier.SampleCurve(mode, value, updateChildren, drawGizmos);
        leftBezier.SampleCurve(mode, value, updateChildren, drawGizmos);

        // Sample the bezier lines
        var rightLinePointsHolder = rightBezier.CreateSamplePointObjects(rightBezier.GetSamplePoints());
        rightLinePointsHolder.transform.SetParent(rightBezier.transform);
        var leftLinePointsHolder = leftBezier.CreateSamplePointObjects(leftBezier.GetSamplePoints());
        leftLinePointsHolder.transform.SetParent(leftBezier.transform);
    }
}