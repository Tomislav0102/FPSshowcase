//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    Transform _spawPoint;
    LineRenderer _lineRenderer;
    [SerializeField] GameObject cannonBall;
    [SerializeField] float speed = 5f;
    [SerializeField]
    [Range(10, 100)] int linePoints = 25;
    [SerializeField]
    [Range(0.01f, 0.25f)] float timeBetweenPoints = 0.1f;

    private void Awake()
    {
        _spawPoint = transform.GetChild(0);
        _lineRenderer = GetComponent<LineRenderer>();
    }
    private void Start()
    {
        _lineRenderer.enabled = false;
    }

    private void Update()
    {
        DrawProjection();
        if (Input.GetMouseButtonDown(1))
        {
            Rigidbody rb = Instantiate(cannonBall, _spawPoint.position, _spawPoint.rotation).GetComponent<Rigidbody>();
            rb.velocity = _spawPoint.forward * speed;
        }
    }

    private void DrawProjection()
    {
        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = Mathf.CeilToInt(linePoints / timeBetweenPoints) + 1;
        Vector3 startPos = _spawPoint.position;
        Vector3 startVel = speed * _spawPoint.forward / cannonBall.GetComponent<Rigidbody>().mass;

        int i = 0;
        _lineRenderer.SetPosition(i, startPos);
        for (float time = 0; time < linePoints; time+=timeBetweenPoints)
        {
            i++;
            Vector3 point = startPos + time * startVel;
            point.y = startPos.y + startVel.y * time + (Physics.gravity.y / 2f * time * time);

            _lineRenderer.SetPosition(i, point);
        }
    }
}
