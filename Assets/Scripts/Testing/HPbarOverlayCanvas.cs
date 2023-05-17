using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPbarOverlayCanvas : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Transform target;
    Vector3 _targetPosition;
    [SerializeField] float height;
    [SerializeField] Transform uiObject;

    float _timer;

    private void Start()
    {
        _targetPosition = target.position;
        _targetPosition.y += height;
        
    }
    private void LateUpdate()
    {
        _timer = Mathf.PingPong(Time.time, 3f);
        _targetPosition.y = _timer;
        uiObject.position = cam.WorldToScreenPoint(_targetPosition);
    }
    //private void LateUpdate()
    //{
    //    _targetPosition = target.position;
    //    _targetPosition.y += height;
    //    uiObject.position = cam.WorldToScreenPoint(_targetPosition);
    //}
}
