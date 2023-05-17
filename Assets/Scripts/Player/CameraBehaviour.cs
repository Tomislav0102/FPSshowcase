using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    [HideInInspector] public float speed;
    Transform _myTransform;
    float _posY;
    readonly Vector2 _range = new Vector2(0f, 0.2f);
    bool _up = true;

    private void Awake()
    {
        _myTransform = transform; 
    }




    private void LateUpdate()
    {
        _posY += (_up ? 1 : -1) * speed * Time.deltaTime;
        if (_posY < _range.x) _up = true;
        if (_posY > _range.y) _up = false;

        _myTransform.localPosition = _posY * Vector3.up;
    }
}
