using System;
using UnityEditor;
using UnityEngine;

public class BezierMainEditorWindow : EditorWindow
{
    private readonly Vector3 _userViewCenter = new(0.5f, 0.5f, 0);

    // Editor Mode
    private BezierToolMode _bezierToolMode;

    // Reticle Variables
    private static bool _showReticle = true;
    private Vector3? _reticlePoint;

    private static bool _showLaneGuides = true;

    private float _laneWidth = 3f;
    private float _laneLength = 5f;
    private float _tVal = 0.5f;
    private bool _showDebug;
    private Bezier _activeBezier;

    // Line Color
    [ColorUsage(false, false)] private Color _rightLineColor = Color.green;
    [ColorUsage(false, false)] private Color _leftLineColor = Color.blue;

    // Node Stamping
    [Range(0.01f, 100f)] private float _nodeStampDistance = 1f;
    [Range(0.01f, 1f)] private float _nodeStampPercentage = 0.25f;
    private int _nodeStampCount = 3;
    private CurveSamplingMode _curveSamplingMode;

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

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Edit Mode", GUILayout.Height(30), GUILayout.MinWidth(150), GUILayout.ExpandWidth(true)))
        {
            _bezierToolMode = BezierToolMode.EditMode;
        }

        EditorGUILayout.Space(10, false);

        if (GUILayout.Button("Spawn Mode", GUILayout.Height(30), GUILayout.MinWidth(150), GUILayout.ExpandWidth(true)))
        {
            _bezierToolMode = BezierToolMode.SpawnMode;
        }

        EditorGUILayout.EndHorizontal();

        switch (_bezierToolMode)
        {
            case BezierToolMode.SpawnMode:
            {
                SceneView.lastActiveSceneView.Repaint();
                EditorGUILayout.LabelField("Bezier Tool - Spawn Mode", EditorStyles.boldLabel);

                // guide toggles 
                _showReticle = true;
                _showLaneGuides = GUILayout.Toggle(_showLaneGuides, "Show lane width guide");
                _laneWidth = EditorGUILayout.Slider("Lane Width (m)", _laneWidth, 0.1f, 10f);
                _laneLength = EditorGUILayout.Slider("Lane Length (m)", _laneLength, 0.1f, 100f);

                // Line Color
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Left Line Color", GUILayout.Width(100));
                _leftLineColor = EditorGUILayout.ColorField(_leftLineColor);
                EditorGUILayout.Space(10, false);
                EditorGUILayout.LabelField("Right Line Color", GUILayout.Width(100));
                _rightLineColor = EditorGUILayout.ColorField(_rightLineColor);
                EditorGUILayout.EndHorizontal();

                // Spawn Lane
                if (GUILayout.Button("Spawn Lane Objects"))
                {
                    SpawnLane();
                }

                // Node Creation Mode
                _curveSamplingMode =
                    (CurveSamplingMode)EditorGUILayout.EnumPopup("Curve Sampling Mode", _curveSamplingMode);
                switch (_curveSamplingMode)
                {
                    case CurveSamplingMode.SampleWithPercentage:
                        _nodeStampPercentage =
                            EditorGUILayout.Slider("Node Stamp Percentage", _nodeStampPercentage, 0.01f, 1f);
                        break;
                    case CurveSamplingMode.SampleWithDistance:
                        _nodeStampDistance =
                            EditorGUILayout.Slider("Node Stamp Distance", _nodeStampDistance, 0.01f, 100f);
                        break;
                    case CurveSamplingMode.SampleWithCount:
                    {
                        _nodeStampCount = EditorGUILayout.IntField("Node Stamp Count", _nodeStampCount);

                        // prevent negative or zero values
                        if (_nodeStampCount < 1)
                        {
                            _nodeStampCount = 1;
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (GUILayout.Button("Generate Bezier Curve"))
                {
                    if (_activeBezier != null)
                    {
                        _activeBezier.ClearSamplePoints();
                        switch (_curveSamplingMode)
                        {
                            case CurveSamplingMode.SampleWithPercentage:
                                _activeBezier.SampleCurveWithPercentage(_activeBezier.GetWorldControlPoints(),
                                    _nodeStampPercentage, true, false);
                                break;
                            case CurveSamplingMode.SampleWithDistance:
                                _activeBezier.SampleCurveWithDistance(_activeBezier.GetWorldControlPoints(),
                                    _nodeStampDistance, true, false);
                                break;
                            case CurveSamplingMode.SampleWithCount:
                                _activeBezier.SampleCurveWithCount(_activeBezier.GetWorldControlPoints(),
                                    _nodeStampCount, true, false);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        var samplePointsHolder =
                            _activeBezier.CreateSamplePointObjects(_activeBezier.GetSamplePoints());
                        samplePointsHolder.transform.SetParent(_activeBezier.transform);
                    }
                    else
                    {
                        Debug.LogWarning("No active Bezier object selected.");
                    }
                }

                break;
            }
            case BezierToolMode.EditMode:
            {
                SceneView.lastActiveSceneView.Repaint();
                // disable guides for edit mode
                _showReticle = false;
                _showLaneGuides = false;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
                Handles.DrawLine(_reticlePoint.Value + new Vector3(_laneWidth / 2, 0, _laneLength),
                    _reticlePoint.Value + new Vector3(_laneWidth / 2, 0, 0), 3);
                Handles.DrawLine(_reticlePoint.Value + new Vector3(-_laneWidth / 2, 0, _laneLength),
                    _reticlePoint.Value + new Vector3(-_laneWidth / 2, 0, 0), 3);
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
            _activeBezier.lerpValue = _tVal;

            // Update stamp options & values
            switch (_curveSamplingMode)
            {
                case CurveSamplingMode.SampleWithPercentage:
                    _activeBezier.nodeStampPercentage = _nodeStampPercentage;
                    _activeBezier.curveSamplingMode = CurveSamplingMode.SampleWithPercentage;
                    break;
                case CurveSamplingMode.SampleWithDistance:
                    _activeBezier.nodeStampDistance = _nodeStampDistance;
                    _activeBezier.curveSamplingMode = CurveSamplingMode.SampleWithDistance;
                    break;
                case CurveSamplingMode.SampleWithCount:
                    _activeBezier.nodeStampCount = _nodeStampCount;
                    _activeBezier.curveSamplingMode = CurveSamplingMode.SampleWithCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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

    private void SpawnLane()
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
            AddLineBezier(posRight, _laneLength, "rightLine", _rightLineColor).transform
                .SetParent(laneObject.transform);
            AddLineBezier(posLeft, _laneLength, "leftLine", _leftLineColor).transform.SetParent(laneObject.transform);
        }
        else
        {
            Debug.Log("Aim to a mesh to spawn a handle.");
        }
    }

    private static GameObject AddLineBezier(Vector3 nodePosition, float length, string objName, Color color)
    {
        GameObject bezierObject = new GameObject(objName)
        {
            transform =
            {
                position = nodePosition
            }
        };
        var bezier = bezierObject.AddComponent<Bezier>();
        bezier.AddControlPoints(nodePosition, length);
        bezier.SetLineColor(color);
        return bezierObject;
    }
}