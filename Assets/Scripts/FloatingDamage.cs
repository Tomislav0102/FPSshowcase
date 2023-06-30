using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingDamage : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textFloatDam;
    Transform _myTransform;
    Camera _cam;
    Transform _camtr;

    [SerializeField] TMP_ColorGradient[] colorGradients;
    readonly Color _startCol = Color.white;
    readonly Color _endCol = Color.clear;
    readonly Vector3 _normalSize = Vector3.one;
    readonly Vector3 _bigSize = new Vector3(3f, 2f, 1f);

    Vector3 _startPos, _dir;
    float _duration;
    float _timer;

    float _height;

    private void LateUpdate()
    {
        _timer += Time.deltaTime;
        if (_timer > _duration)
        {
            _timer = 0f;
            textFloatDam.text = "";
        }
        float t = _timer / _duration;

        if (IsBehindPlayer()) return;

       // _height = Mathf.Lerp(0f, 200f, t);
        _height = Mathf.Lerp(0f, 2f, t);
        _myTransform.position = _cam.WorldToScreenPoint(_startPos + _height * Vector3.up);
        _myTransform.localScale = Vector3.Lerp(_bigSize, _normalSize, _timer * 2f);
        textFloatDam.color = Color.Lerp(_startCol, _endCol, t);
    }

    private bool IsBehindPlayer() 
    {
        _dir = new Vector3(_dir.x, 0f, _dir.z).normalized;
        Vector3 camForward = new Vector3(_camtr.forward.x, 0f, _camtr.forward.z).normalized;
        if (Vector3.Dot(_dir, camForward) < 0f)
        {
            _timer = Mathf.Infinity;
            return true;
        }
        return false;
    }

    public void FloatingDisplay(Vector3 pos, string textToDisplay, int duration, ElementType elementType)
    {
        if(_cam == null) _cam = GameManager.Instance.mainCam;
        if (_camtr == null) _camtr = _cam.transform;
        if (_myTransform == null) _myTransform = transform;

        _myTransform.localScale = _bigSize;
        _startPos = pos /*+ Vector3.up*/ + Random.Range(-0.2f, 0.2f) * Vector3.right;
        _dir = _startPos - _camtr.position;
        textFloatDam.colorGradientPreset = colorGradients[(int)elementType];
        textFloatDam.text = textToDisplay;
        _duration = duration;
        _timer = 0f;
    }
}
