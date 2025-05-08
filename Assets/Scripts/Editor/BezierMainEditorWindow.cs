using System;
using UnityEditor;
using UnityEngine;

public class BezierMainEditorWindow : EditorWindow
{
    private readonly Vector3 _userViewCenter = new(0.5f, 0.5f, 0);

    // Reticle Variables
    private static bool _showReticle = true;
    private static bool _showLaneGuides = true;
    private Vector3? _reticlePoint;

    private bool _showDebug;
    private float _tVal = 0.5f;
    private float _laneWidth = 3f;
    private float _laneLength = 5f;

    private Lane _selectedLane;
    private Bezier _selectedBezier;

    // Line Color
    [ColorUsage(false, false)] private Color _rightLineColor = Color.green;
    [ColorUsage(false, false)] private Color _leftLineColor = Color.blue;

    // Node Stamping
    [Range(0.01f, 100f)] private float _nodeSampleDistance = 0.5f;
    [Range(0.01f, 1f)] private float _nodeSamplePercentage = 0.2f;
    private int _nodeSampleCount = 7;

    private float _samplingValue;
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
        if (_selectedBezier)
        {
            _selectedBezier.SetDebugState(_showDebug);
        }

        if (_showDebug)
        {
            _tVal = EditorGUILayout.Slider("t Value", _tVal, 0f, 1f);
        }

        // guide toggles
        _showLaneGuides = GUILayout.Toggle(_showLaneGuides, "Show placement guides");
        _showReticle = _showLaneGuides;
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

        _curveSamplingMode =
            (CurveSamplingMode)EditorGUILayout.EnumPopup("Curve Sampling Mode", _curveSamplingMode);
        switch (_curveSamplingMode)
        {
            case CurveSamplingMode.SampleWithPercentage:
            {
                _nodeSamplePercentage =
                    EditorGUILayout.Slider("Node Stamp Percentage", _nodeSamplePercentage, 0.01f, 1f);
                _samplingValue = _nodeSamplePercentage;
                break;
            }
            case CurveSamplingMode.SampleWithDistance:
            {
                _nodeSampleDistance =
                    EditorGUILayout.Slider("Node Sample Distance", _nodeSampleDistance, 0.01f, 100f);
                _samplingValue = _nodeSampleDistance;
                break;
            }
            case CurveSamplingMode.SampleWithCount:
            {
                _nodeSampleCount = EditorGUILayout.IntField("Node Sample Count", _nodeSampleCount);

                // prevent negative or zero values
                if (_samplingValue < 1)
                {
                    _samplingValue = 1;
                }

                // conversion to float!
                _samplingValue = _nodeSampleCount;

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (GUILayout.Button("Sample Bezier Curve"))
        {
            if (_selectedBezier != null)
            {
                _selectedBezier.ClearSamplePoints();
                _selectedBezier.SampleCurve(_curveSamplingMode, _samplingValue, true, false);
                // destroy old points if exists
                _selectedBezier.DestroySamplePointObjects();
                // create sample points
                var samplePointsHolder =
                    _selectedBezier.CreateSamplePointObjects(_selectedBezier.GetSamplePoints());
                samplePointsHolder.transform.SetParent(_selectedBezier.transform);
            }
            else
            {
                Debug.LogWarning("Please select an object that has a 'Bezier' component attached to it.");
            }
        }

        if (GUILayout.Button("Sample Lanes"))
        {
            if (_selectedLane != null)
            {
                _selectedLane.SampleBezierLines(_curveSamplingMode, _samplingValue, true, false);
            }
            else
            {
                Debug.LogWarning("Please select an object that has a 'Lane' component attached to it.");
            }
        }

        SceneView.lastActiveSceneView.Repaint();
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
                Handles.DrawLine(_reticlePoint.Value + new Vector3(_laneWidth / 2, 0, _laneLength / 2),
                    _reticlePoint.Value + new Vector3(_laneWidth / 2, 0, -_laneLength / 2), 3);
                Handles.DrawLine(_reticlePoint.Value + new Vector3(-_laneWidth / 2, 0, _laneLength / 2),
                    _reticlePoint.Value + new Vector3(-_laneWidth / 2, 0, -_laneLength / 2), 3);
            }
        }


        // Draw control point handles
        if (_selectedBezier != null)
        {
            Handles.color = Color.cyan;
            var worldPoints = _selectedBezier.GetWorldControlPoints();

            for (var index = 0; index < worldPoints.Count; index++)
            {
                EditorGUI.BeginChangeCheck();

                var point = worldPoints[index];
                Vector3 newPos = Handles.PositionHandle(point, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move Control Point");
                    // update control point with the new pos
                    _selectedBezier.UpdateControlPoint(index, newPos);
                }
            }

            // Update lerp value
            _selectedBezier.lerpValue = _tVal;

            // Update stamp options & values
            switch (_curveSamplingMode)
            {
                case CurveSamplingMode.SampleWithPercentage:
                    _selectedBezier.nodeStampPercentage = _nodeSamplePercentage;
                    _selectedBezier.curveSamplingMode = CurveSamplingMode.SampleWithPercentage;
                    break;
                case CurveSamplingMode.SampleWithDistance:
                    _selectedBezier.nodeStampDistance = _nodeSampleDistance;
                    _selectedBezier.curveSamplingMode = CurveSamplingMode.SampleWithDistance;
                    break;
                case CurveSamplingMode.SampleWithCount:
                    _selectedBezier.nodeStampCount = _nodeSampleCount;
                    _selectedBezier.curveSamplingMode = CurveSamplingMode.SampleWithCount;
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
        // Get selected bezier
        if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent(out Bezier bezier))
        {
            if (bezier != null)
            {
                _selectedBezier = bezier;
                _selectedLane = _selectedBezier.transform.parent.gameObject.GetComponent<Lane>();
                _showDebug = _selectedBezier.GetDebugState();
            }
        }
        else
        {
            _selectedBezier = null;
            _selectedLane = null;
        }

        Repaint();
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

        return userView;
    }

    private GameObject SpawnLane()
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
            var rightLineObj = CreateLineObject(posRight, _laneLength, "Right Line", _rightLineColor);
            rightLineObj.transform.SetParent(laneObject.transform);
            var leftLineObj =
                CreateLineObject(posLeft, _laneLength, "Left Line", _leftLineColor);
            leftLineObj.transform.SetParent(laneObject.transform);

            // add lane component
            var lane = laneObject.AddComponent<Lane>();
            lane.SetRightBezier(rightLineObj.GetComponent<Bezier>());
            lane.SetLeftBezier(leftLineObj.GetComponent<Bezier>());

            return laneObject;
        }

        Debug.Log("Aim to a mesh to spawn a handle.");

        return null;
    }

    private static GameObject CreateLineObject(Vector3 nodePosition, float length, string objName, Color color)
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