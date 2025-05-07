using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class Bezier : MonoBehaviour
{
    [SerializeField] private List<Vector3> localControlPoints = new();
    public float lerpValue = 0;
    public float sphereRadius = 0.75f;

    // Subdivision
    [Range(2, 50)] public int curveSubdivisions = 50;

    // Node stamping
    public CurveSamplingMode curveSamplingMode;
    public float nodeStampDistance = 1f;
    public float nodeStampPercentage = 0.25f;
    public int nodeStampCount = 3;

    // Add 4 local control points
    public void AddControlPoint(Vector3 worldPoint)
    {
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint));
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint + Vector3.forward));
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint + Vector3.forward * 2));
        localControlPoints.Add(transform.InverseTransformPoint(worldPoint + Vector3.forward * 3));
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
        var bezierLerpPointsL1 = VisualizeAndLerpPoints(worldPoints, lerpValue, Color.cyan, sphereRadius, true);
        var bezierLerpPointsL2 = VisualizeAndLerpPoints(bezierLerpPointsL1, lerpValue, Color.red, sphereRadius, true);
        var bezierLerpPointsL3 =
            VisualizeAndLerpPoints(bezierLerpPointsL2, lerpValue, Color.yellow, sphereRadius, true);
        VisualizeAndLerpPoints(bezierLerpPointsL3, lerpValue, Color.green, sphereRadius, true);

        // Bezier line visualization
        for (int i = 1; i <= curveSubdivisions; i++)
        {
            float t1 = (i - 1) / (float)curveSubdivisions;
            float t2 = i / (float)curveSubdivisions;
            var point1 = GetDiscreteBezierPoint(worldPoints, t1);
            var point2 = GetDiscreteBezierPoint(worldPoints, t2);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(point1, point2);
        }

        // Point creation mode
        switch (curveSamplingMode)
        {
            case CurveSamplingMode.SampleWithPercentage:
            {
                float step = Mathf.Clamp01(nodeStampPercentage);
                int sampleCount = Mathf.FloorToInt(1f / step);

                Gizmos.color = Color.white;

                for (int i = 0; i <= sampleCount; i++)
                {
                    float t = i * step;
                    Vector3 point = GetDiscreteBezierPoint(worldPoints, t);
                    Gizmos.DrawSphere(point, sphereRadius);
                }

                // draw endpoint
                if (sampleCount * step < 1f)
                {
                    Vector3 endPoint = GetDiscreteBezierPoint(worldPoints, 1f);
                    Gizmos.DrawSphere(endPoint, sphereRadius);
                }

                break;
            }
            case CurveSamplingMode.SampleWithDistance:
            {
                float spacing = nodeStampDistance;
                float totalLength = EstimateCurveLength(worldPoints, curveSubdivisions);
                int sampleCount = Mathf.FloorToInt(totalLength / spacing);

                Gizmos.color = Color.blue;

                for (int i = 0; i <= sampleCount; i++)
                {
                    float distance = i * spacing;
                    Vector3 point = GetPointAtDistance(worldPoints, distance, curveSubdivisions);
                    Gizmos.DrawSphere(point, sphereRadius);
                }

                // draw endpoint
                Vector3 endPoint = GetDiscreteBezierPoint(worldPoints, 1f);
                Gizmos.DrawSphere(endPoint, sphereRadius);
                break;
            }
            case CurveSamplingMode.SampleWithCount:
            {
                int count = Mathf.Max(3, nodeStampCount);
                float step = 1f / (count - 1);

                Gizmos.color = Color.magenta;

                for (int i = 0; i < count; i++)
                {
                    float t = i * step;
                    Vector3 point = GetDiscreteBezierPoint(worldPoints, t);
                    Gizmos.DrawSphere(point, sphereRadius);
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void GenerateBezierCurve()
    {
        // Generate the curve based on the point creation mode
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
        float sphereRadius = 0.1f, bool drawGizmos = false)
    {
        if (drawGizmos)
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
            if (drawGizmos)
            {
                Gizmos.DrawLine(p1, p2);
            }

            // populate next point list
            var lP = Vector3.Lerp(p1, p2, lerpValue);
            lerpPoints.Add(lP);
        }

        return lerpPoints;
    }
}