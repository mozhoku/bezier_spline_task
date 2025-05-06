using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Bezier : MonoBehaviour
{
    [SerializeField] private List<Vector3> controlPoints = new();
    
    public void AddControlPoint(Vector3 point)
    {
        controlPoints.Add(point);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        foreach (var controlPoint in controlPoints)
        {
            Gizmos.DrawWireSphere(controlPoint, 0.1f);
        }
    }
}