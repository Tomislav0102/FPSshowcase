using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapFire : MonoBehaviour
{
    [SerializeField] BreathCollider breathCollider;
    [SerializeField] Collider[] colliders;
    [SerializeField] SoItem weapon;

    private void Start()
    {
        breathCollider.Init(colliders, weapon);
    }
}
