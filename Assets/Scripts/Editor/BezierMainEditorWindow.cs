using System;
using UnityEditor;
using UnityEngine;

public class BezierMainEditorWindow : EditorWindow
{
    private readonly Vector3 _userViewCenter = new(0.5f, 0.5f, 0);

    // Reticle Variables
    private static bool _showReticle = true;
    private Vector3? _reticlePoint;

    private static bool _showLaneGuides = true;


    private float _laneWidth = 3f;
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

        _showLaneGuides = GUILayout.Toggle(_showLaneGuides, "Show Lane Guides");
        _laneWidth = EditorGUILayout.Slider("Lane Width", _laneWidth, 0.1f, 10f);
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

        if (_showLaneGuides)
        {
            if (_reticlePoint != null)
            {
                // z is length
                // x is width
                Handles.color = Color.yellow;
                Handles.DrawLine(_reticlePoint.Value + new Vector3(_laneWidth / 2, 0, 3),
                    _reticlePoint.Value + new Vector3(_laneWidth / 2, 0, 0));
                Handles.DrawLine(_reticlePoint.Value + new Vector3(-_laneWidth / 2, 0, 3),
                    _reticlePoint.Value + new Vector3(-_laneWidth / 2, 0, 0));
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
        if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent(out Bezier bezier))
        {
            if (bezier != null)
            {
                _activeBezier = bezier;
            }
        }
        else
        {
            _activeBezier = null;
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
            // create lane object
            GameObject laneObject = new GameObject("Lane Object")
            {
                transform =
                {
                    position = hit.point
                }
            };

            var posRight = hit.point + new Vector3(_laneWidth / 2, 0, 0);
            var posLeft = hit.point + new Vector3(-_laneWidth / 2, 0, 0);

            // add lines and set parent
            AddLineBezier(posRight, "rightLine").transform.SetParent(laneObject.transform);
            AddLineBezier(posLeft, "leftLine").transform.SetParent(laneObject.transform);
        }
        else
        {
            Debug.Log("Aim to a mesh to spawn a handle.");
        }
    }

    private static GameObject AddLineBezier(Vector3 position, string objName)
    {
        GameObject bezierObject = new GameObject(objName)
        {
            transform =
            {
                position = position
            }
        };
        bezierObject.AddComponent<Bezier>().AddControlPoint(position);
        return bezierObject;
    }
}