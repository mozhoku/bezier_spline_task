using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Bezier : MonoBehaviour
{
    [SerializeField] private List<Vector3> localControlPoints = new();
    public float lerpValue = 0;
    public float sphereRadius = 0.1f;

    // Line color
    [ColorUsage(false, false)] public Color lineColor;

    // Subdivision
    [Range(2, 50)] public int curveSubdivisions = 50;

    // Node stamping
    public CurveSamplingMode curveSamplingMode;
    public float nodeStampDistance = 0.5f;
    public float nodeStampPercentage = 0.2f;
    public int nodeStampCount = 7;

    // debug
    [SerializeField] private bool _showDebug;

    // Sample points coordinates
    [SerializeField] private GameObject samplePointsHolder;
    [SerializeField] private List<Vector3> samplePointCoords = new();


    // Add 4 local control points
    public void AddControlPoints(Vector3 worldPoint, float length)
    {
        var dividedLength = length / 3f;
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint + Vector3.forward * (dividedLength * -1.5f)));
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint + Vector3.forward * (dividedLength * -0.5f)));
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint + Vector3.forward * (dividedLength * 0.5f)));
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint + Vector3.forward * (dividedLength * 1.5f)));
    }

    // Get World space coords
    public List<Vector3> GetWorldControlPoints()
    {
        List<Vector3> worldPoints = new();
        foreach (var localPoint in localControlPoints)
        {
            worldPoints.Add(transform.TransformPoint(localPoint));
        }

        return worldPoints;
    }

    public void SetLineColor(Color color)
    {
        lineColor = color;
    }

    // Set control points
    public void UpdateControlPoint(int index, Vector3 newWorldPosition)
    {
        if (index < 0 || index >= localControlPoints.Count)
        {
            Debug.LogError("Index out of range");
            return;
        }

        localControlPoints[index] = transform.InverseTransformPoint(newWorldPosition);
    }

    private void OnDrawGizmos()
    {
        // Debug
        var worldPoints = GetWorldControlPoints();
        var bezierLerpPointsL1 =
            VisualizeAndLerpPoints(worldPoints, lerpValue, Color.cyan, sphereRadius, _showDebug, true);
        var bezierLerpPointsL2 =
            VisualizeAndLerpPoints(bezierLerpPointsL1, lerpValue, Color.red, sphereRadius / 2, _showDebug);
        var bezierLerpPointsL3 =
            VisualizeAndLerpPoints(bezierLerpPointsL2, lerpValue, Color.yellow, sphereRadius / 2, _showDebug);
        VisualizeAndLerpPoints(bezierLerpPointsL3, lerpValue, Color.green, sphereRadius / 2, _showDebug);

        // Bezier line visualization
        List<Vector3> handleVisualization = new();
        for (int i = 0; i <= curveSubdivisions; i++)
        {
            float t1 = i / (float)curveSubdivisions;
            var point1 = GetDiscreteBezierPoint(worldPoints, t1);
            handleVisualization.Add(point1);
        }

        Handles.color = lineColor;
        Handles.DrawAAPolyLine(3f, handleVisualization.ToArray());

        // Point creation mode
        switch (curveSamplingMode)
        {
            case CurveSamplingMode.SampleWithPercentage:
            {
                SampleCurveWithPercentage(worldPoints, nodeStampPercentage, false, true);
                break;
            }
            case CurveSamplingMode.SampleWithDistance:
            {
                SampleCurveWithDistance(worldPoints, nodeStampDistance, false, true);
                break;
            }
            case CurveSamplingMode.SampleWithCount:
            {
                SampleCurveWithCount(worldPoints, nodeStampCount, false, true);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static Vector3 GetDiscreteBezierPoint(List<Vector3> controlPoints, float lerpValue)
    {
        var bezierLerpPointsL1 = VisualizeAndLerpPoints(controlPoints, lerpValue, Color.cyan);
        var bezierLerpPointsL2 = VisualizeAndLerpPoints(bezierLerpPointsL1, lerpValue, Color.red);
        var bezierLerpPointsL3 = VisualizeAndLerpPoints(bezierLerpPointsL2, lerpValue, Color.yellow);

        return bezierLerpPointsL3.First();
    }

    private static float EstimateCurveLength(List<Vector3> points, int subdivisions)
    {
        float length = 0f;
        Vector3 previousPoint = GetDiscreteBezierPoint(points, 0f);
        for (int i = 1; i <= subdivisions; i++)
        {
            float t = i / (float)subdivisions;
            Vector3 currentPoint = GetDiscreteBezierPoint(points, t);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        return length;
    }

    private static Vector3 GetPointAtDistance(List<Vector3> points, float targetDistance, int subdivisions)
    {
        float accumulatedDistance = 0f;
        Vector3 previousPoint = GetDiscreteBezierPoint(points, 0f);

        for (int i = 1; i <= subdivisions; i++)
        {
            float t = i / (float)subdivisions;
            Vector3 currentPoint = GetDiscreteBezierPoint(points, t);
            float segment = Vector3.Distance(previousPoint, currentPoint);

            if (accumulatedDistance + segment >= targetDistance)
            {
                float overshoot = targetDistance - accumulatedDistance;
                return Vector3.Lerp(previousPoint, currentPoint, overshoot / segment);
            }

            accumulatedDistance += segment;
            previousPoint = currentPoint;
        }

        return GetDiscreteBezierPoint(points, 1f); // fallback to end
    }

    private static List<Vector3> VisualizeAndLerpPoints(List<Vector3> pointList, float lerpValue, Color gizmoColor,
        float sphereRadius = 0.1f, bool debugMode = false, bool isHandle = false)
    {
        if (debugMode)
        {
            foreach (var point in pointList)
            {
                Gizmos.color = gizmoColor;

                Gizmos.DrawSphere(point, sphereRadius);
            }
        }

        List<Vector3> lerpPoints = new();
        
        // Draw lines
        for (var i = 0; i < pointList.Count - 1; i++)
        {
            var p1 = pointList[i];
            var p2 = pointList[i + 1];


            // populate next point list
            var lP = Vector3.Lerp(p1, p2, lerpValue);
            lerpPoints.Add(lP);

            // skip for first handles
            if (debugMode)
            {
                Gizmos.DrawLine(p1, p2);
            }
            else if (isHandle)
            {
                if (i == 1)
                {
                    continue;
                }
                Gizmos.color = gizmoColor;
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawSphere(p1, sphereRadius);
                Gizmos.DrawSphere(p2, sphereRadius);
            }
        }

        return lerpPoints;
    }

    public void SampleCurve(CurveSamplingMode samplingMode, float value, bool updateChildren, bool drawGizmos)
    {
        switch (samplingMode)
        {
            case CurveSamplingMode.SampleWithDistance:
                SampleCurveWithDistance(GetWorldControlPoints(), value, updateChildren, drawGizmos);
                break;
            case CurveSamplingMode.SampleWithPercentage:
                SampleCurveWithPercentage(GetWorldControlPoints(), value, updateChildren, drawGizmos);
                break;
            case CurveSamplingMode.SampleWithCount:
                SampleCurveWithCount(GetWorldControlPoints(), Mathf.FloorToInt(value), updateChildren, drawGizmos);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(samplingMode), samplingMode, null);
        }
    }

    private void SampleCurveWithPercentage(List<Vector3> points, float percentage, bool updateChildrenCoords,
        bool drawGizmos)
    {
        float step = Mathf.Clamp01(percentage);
        int sampleCount = Mathf.FloorToInt(1f / step);
        if (drawGizmos)
        {
            Gizmos.color = Color.white;
        }

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i * step;
            Vector3 point = GetDiscreteBezierPoint(points, t);
            if (drawGizmos)
            {
                Gizmos.DrawSphere(point, sphereRadius / 2);
            }

            if (updateChildrenCoords)
            {
                samplePointCoords.Add(point);
            }
        }

        // draw endpoint
        if (sampleCount * step < 1f)
        {
            Vector3 endPoint = GetDiscreteBezierPoint(points, 1f);
            if (drawGizmos)
            {
                Gizmos.DrawSphere(endPoint, sphereRadius / 2);
            }

            if (updateChildrenCoords)
            {
                samplePointCoords.Add(endPoint);
            }
        }
    }

    private void SampleCurveWithDistance(List<Vector3> points, float nodeDistance, bool updateChildrenCoords,
        bool drawGizmos)
    {
        float spacing = nodeDistance;
        float totalLength = EstimateCurveLength(points, curveSubdivisions);
        int sampleCount = Mathf.FloorToInt(totalLength / spacing);

        if (drawGizmos)
        {
            Gizmos.color = Color.white;
        }

        for (int i = 0; i <= sampleCount; i++)
        {
            float distance = i * spacing;
            Vector3 point = GetPointAtDistance(points, distance, curveSubdivisions);
            if (drawGizmos)
            {
                Gizmos.DrawSphere(point, sphereRadius / 2);
            }

            if (updateChildrenCoords)
            {
                samplePointCoords.Add(point);
            }
        }

        // draw endpoint
        Vector3 endPoint = GetDiscreteBezierPoint(points, 1f);
        if (drawGizmos)
        {
            Gizmos.DrawSphere(endPoint, sphereRadius / 2);
        }

        if (updateChildrenCoords)
        {
            samplePointCoords.Add(endPoint);
        }
    }

    private void SampleCurveWithCount(List<Vector3> points, int nodeCount, bool updateChildrenCoords, bool drawGizmos)
    {
        int count = Mathf.Max(3, nodeCount);
        float step = 1f / (count - 1);

        if (drawGizmos)
        {
            Gizmos.color = Color.white;
        }

        for (int i = 0; i < count; i++)
        {
            float t = i * step;
            Vector3 point = GetDiscreteBezierPoint(points, t);
            if (drawGizmos)
            {
                Gizmos.DrawSphere(point, sphereRadius / 2);
            }

            if (updateChildrenCoords)
            {
                samplePointCoords.Add(point);
            }
        }
    }

    public List<Vector3> GetSamplePoints()
    {
        return samplePointCoords;
    }

    public void ClearSamplePoints()
    {
        samplePointCoords.Clear();
    }

    public GameObject CreateSamplePointObjects(List<Vector3> coords)
    {
        GameObject samplePointParent = new GameObject("Sampled Points");
        samplePointsHolder = samplePointParent;
        foreach (var coord in coords)
        {
            GameObject samplePoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            samplePoint.transform.position = coord;
            samplePoint.transform.localScale = Vector3.one / 4;
            samplePoint.transform.parent = samplePointParent.transform;
        }

        return samplePointParent;
    }

    public void DestroySamplePointObjects()
    {
        if (samplePointsHolder != null)
        {
            DestroyImmediate(samplePointsHolder);
            samplePointsHolder = null;
        }
    }

    public void SetDebugState(bool debug)
    {
        _showDebug = debug;
    }

    public bool GetDebugState()
    {
        return _showDebug;
    }
}