using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Gilder : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;
    [SerializeField] private float _airDensity = 1.225f;
    [SerializeField] private float _wingArea = 1.5f;
    [SerializeField] private float _wingAspect = 8.0f;
    [SerializeField] private float _wingCDD = 0.02f;
    [SerializeField] private float _wingClaplha = 5.5f;

    private Rigidbody _rigidbody;
    private JetEngine _jetEngine;
    private Vector3 _vPoint;
    private float _speadMS;
    private float _alphaRad;
    private float _cl, _cd, _qDyn, _lMag, _dMag, _qlidek;
    private bool IsGround;
    private float _startPosition;

    private Rect _telemetryRect;
    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private bool _guiInitialized;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _jetEngine = GetComponent<JetEngine>();
        CalculateTelemetryRect();
    }

    private void CalculateTelemetryRect()
    {
        float boxWidth = 320f;
        float boxHeight = 460f;
        float margin = 10f;
        _telemetryRect = new Rect(Screen.width - boxWidth - margin, margin, boxWidth, boxHeight);
    }

    private void InitializeGUI()
    {
        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            normal = { textColor = Color.cyan }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };

        _guiInitialized = true;
    }

    private void FixedUpdate()
    {
        CalculateAerodynamics();
        ApplyAerodynamicForces();
    }

    private void CalculateAerodynamics()
    {
        _vPoint = _rigidbody.GetPointVelocity(_wingCP.position);
        _speadMS = _vPoint.magnitude;

        Vector3 flowDir = (-_vPoint).normalized;
        Vector3 xChord = _wingCP.forward;
        Vector3 zUP = _wingCP.up;
        Vector3 ySpan = _wingCP.right;

        float flowX = Vector3.Dot(flowDir, xChord);
        float flowZ = Vector3.Dot(flowDir, zUP);
        _alphaRad = Mathf.Atan2(flowZ, flowX);

        _cl = _wingClaplha * _alphaRad;
        _cd = _wingCDD + _cl * _cl / (Mathf.PI * _wingAspect * 0.85f);

        _qDyn = 0.5f * _airDensity * _speadMS * _speadMS;
        _lMag = _qDyn * _wingArea * _cl;
        _dMag = _qDyn * _wingArea * _cd;
        _qlidek = (_lMag > 0.1f) ? _lMag / _dMag : 0f;
    }

    private void ApplyAerodynamicForces()
    {
        Vector3 flowDir = (-_vPoint).normalized;
        Vector3 Ddir = -flowDir;
        Vector3 ySpan = _wingCP.right;

        Vector3 liftDir = Vector3.Cross(flowDir, ySpan);
        liftDir.Normalize();

        Vector3 L = _lMag * liftDir;
        Vector3 D = _dMag * Ddir;

        _rigidbody.AddForceAtPosition(L + D, _wingCP.position, ForceMode.Force);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            _startPosition = transform.position.y;
            IsGround = true;
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            IsGround = false;
        }
    }

    private void OnGUI()
    {
        if (!_guiInitialized)
        {
            InitializeGUI();
        }

        GUI.Box(_telemetryRect, "ТЕЛЕМЕТРИЯ САМОЛЕТА", _boxStyle);

        GUILayout.BeginArea(new Rect(_telemetryRect.x + 10, _telemetryRect.y + 25,
                                   _telemetryRect.width - 20, _telemetryRect.height - 35));

        GUILayout.Label("--- ОБЩЕЕ ---", _headerStyle);
        GUILayout.Label($"Скорость: {_speadMS:F1} м/с ({(int)(_speadMS * 3.6f)} км/ч)", _labelStyle);
        GUILayout.Label($"Высота: {transform.position.y:F1} м", _labelStyle);
        GUILayout.Label($"Верт. скорость: {_rigidbody.linearVelocity.y:F1} м/с", _labelStyle);
        GUILayout.Label($"Состояние: {(IsGround ? "НА ЗЕМЛЕ" : "В ВОЗДУХЕ")}", _labelStyle);

        GUILayout.Space(10);

        GUILayout.Label("--- АЭРОДИНАМИКА ---", _headerStyle);
        GUILayout.Label($"Угол атаки: {_alphaRad * Mathf.Rad2Deg:F1}°", _labelStyle);
        GUILayout.Label($"Коэф. подъемной силы (Cl): {_cl:F3}", _labelStyle);
        GUILayout.Label($"Коэф. сопротивления (Cd): {_cd:F4}", _labelStyle);
        GUILayout.Label($"Аэродинамическое качество: {_qlidek:F1}", _labelStyle);
        GUILayout.Label($"Подъемная сила: {(int)_lMag} Н", _labelStyle);
        GUILayout.Label($"Сопротивление: {(int)_dMag} Н", _labelStyle);
        GUILayout.Label($"Динамический напор: {(int)_qDyn} Па", _labelStyle);

        GUILayout.Space(10);

        if (_jetEngine != null)
        {
            GUILayout.Label("--- ДВИГАТЕЛЬ ---", _headerStyle);
            GUILayout.Label($"Тяга: {_jetEngine._throttle01:P0}", _labelStyle);
            GUILayout.Label($"Форсаж: {(_jetEngine._afterBurner ? "ВКЛ" : "ВЫКЛ")}", _labelStyle);
            GUILayout.Label($"Сила тяги: {(int)_jetEngine._lastAppliedThrust} Н", _labelStyle);
        }

        GUILayout.EndArea();
    }
}