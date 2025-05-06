using System;
using UnityEditor;
using UnityEngine;

public class BezierMainEditorWindow : EditorWindow
{
    private readonly Vector3 _userViewCenter = new(0.5f, 0.5f, 0);

    // Reticle Variables
    private static bool _showReticle = true;
    private Vector3? _reticlePoint;

    private float _tVal;
    private bool _showDebug;
    private Bezier _activeBezier;

    [MenuItem("Window/Bezier Curve Tool")]
    public static void ShowWindow()
    {
        GetWindow<BezierMainEditorWindow>("Bezier Curve Tool");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public void OnGUI()
    {
        _showDebug = EditorGUILayout.Toggle("Show Debug", _showDebug);
        _tVal = EditorGUILayout.Slider("t Value", _tVal, 0f, 1f);

        if (GUILayout.Button("Generate Bezier Curve"))
        {
            // generate the bezier curve
        }

        if (GUILayout.Button("Spawn Handle"))
        {
            SpawnBezierObject();
        }

        _showReticle = GUILayout.Toggle(_showReticle, "Show Reticle");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_showReticle)
        {
            var ray = GetUserLookAt();
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                _reticlePoint = hit.point;

                // Draw reticle
                Handles.color = Color.green;
                Handles.DrawWireDisc(hit.point, hit.normal, 0.1f, 3);
                // Surface normal
                Handles.color = Color.red;
                Handles.DrawLine(hit.point, hit.point + hit.normal * 0.3f, 3);
            }
            else
            {
                _reticlePoint = null;
            }
        }


        // Draw control point handles
        if (_activeBezier != null)
        {
            Handles.color = Color.cyan;
            var worldPoints = _activeBezier.GetWorldControlPoints();

            for (var index = 0; index < worldPoints.Count; index++)
            {
                EditorGUI.BeginChangeCheck();

                var point = worldPoints[index];
                Vector3 newPos = Handles.PositionHandle(point, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move Control Point");
                    // update control point with the new pos
                    _activeBezier.UpdateControlPoint(index, newPos);
                }
            }

            // Update lerp value
            _activeBezier.lerpVal = _tVal;

            sceneView.Repaint();
        }
    }

    // Get active bezier
    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Bezier>() != null)
        {
            // Get bezier
            var bezier = Selection.activeGameObject.GetComponent<Bezier>();
            if (bezier != null)
            {
                _activeBezier = bezier;
            }
        }
    }

    private Ray GetUserLookAt()
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
        {
            Debug.LogWarning("No active SceneView or camera found.");
            return new Ray(Vector3.zero, Vector3.forward); // Fallback ray
        }

        Ray userView = sceneView.camera.ViewportPointToRay(_userViewCenter);

        if (_showDebug)
        {
            Debug.DrawRay(userView.origin, userView.direction * 100, Color.red, 3f);
        }

        return userView;
    }

    private void SpawnBezierObject()
    {
        if (Physics.Raycast(GetUserLookAt(), out RaycastHit hit))
        {
            GameObject bezierObject = new GameObject("Bezier Object")
            {
                transform =
                {
                    position = hit.point
                }
            };
            bezierObject.AddComponent<Bezier>().AddControlPoint(hit.point);
        }
        else
        {
            Debug.Log("Aim to a mesh to spawn a handle.");
        }
    }
}