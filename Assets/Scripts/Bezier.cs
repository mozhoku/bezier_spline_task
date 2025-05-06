using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Bezier : MonoBehaviour
{
    [SerializeField] private List<Vector3> localControlPoints = new();
    [SerializeField] public float lerpVal = 0;

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
        var bezierLerpPointsL1 = VisualizeAndLerpPoints(worldPoints, lerpVal, Color.cyan, 0.1f, true);
        var bezierLerpPointsL2 = VisualizeAndLerpPoints(bezierLerpPointsL1, lerpVal, Color.red, 0.1f, true);
        var bezierLerpPointsL3 = VisualizeAndLerpPoints(bezierLerpPointsL2, lerpVal, Color.yellow, 0.1f, true);
        VisualizeAndLerpPoints(bezierLerpPointsL3, lerpVal, Color.green, 0.1f, true);

        for (int i = 0; i < 100; i++)
        {
            float lerp = i / 100f;
            var point = GetDiscreteBezierPoint(worldPoints, lerp);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(point, 0.05f);
        }
    }

    private Vector3 GetDiscreteBezierPoint(List<Vector3> controlPoints, float lerpValue)
    {
        var bezierLerpPointsL1 = VisualizeAndLerpPoints(controlPoints, lerpValue, Color.cyan);
        var bezierLerpPointsL2 = VisualizeAndLerpPoints(bezierLerpPointsL1, lerpValue, Color.red);
        var bezierLerpPointsL3 = VisualizeAndLerpPoints(bezierLerpPointsL2, lerpValue, Color.yellow);

        return bezierLerpPointsL3.First();
    }

    private List<Vector3> VisualizeAndLerpPoints(List<Vector3> pointList, float lerpValue, Color gizmoColor,
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