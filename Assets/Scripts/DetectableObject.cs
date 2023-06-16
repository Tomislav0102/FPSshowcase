using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectableObject : MonoBehaviour
{
    GameManager _gm;
    GameObject _myGameObject;

    public Transform myTransform;
    public Collider myCollider;
    public IFaction owner;


    void Awake()
    {
        _gm = GameManager.Instance;
        _myGameObject = gameObject;
        myCollider.enabled = false;
    }

    public void PositionMe(Vector3 pos, float size, IFaction ownerInterface)
    {
        myTransform.position = pos;
        myTransform.localScale = size * Vector3.one;
        owner = ownerInterface;
        myCollider.enabled = true;
        Invoke(nameof(EndMe), 0.7f);
    }
    void EndMe()
    {
        myCollider.enabled = false;
    }
}
