using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Threading;
using System.Threading.Tasks;
using Sirenix.OdinInspector;

public class TestScript : MonoBehaviour
{
    Camera _cam;
  //  public Transform otherTransform;
    public float angl, signAngle;
  //  public CapsuleCollider capsuleCollider;
   // bool _ducked;
    //public NavMeshAgent agent;
    //public Transform movePoint;
    //RaycastHit _hit;
    //RaycastHit[] _multipleHits = new RaycastHit[1];
    //public Transform sphere;
    //Vector3[] _pos= new Vector3[0];
    //public Vector3 _nextPos;
    //float _ver, _hor;
    //public LayerMask layerMask;
    //public bool isPlayer;
     public Animator anim;
    public Transform targetMatchL, targetMatchR;
    MatchTargetWeightMask weightMask = new MatchTargetWeightMask(Vector3.one, 1f);

    //public int maska;

    //[System.Flags]
    public enum Days
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }
    public Days days;
    //Days days = Days.Monday | Days.Wednesday;
    //public LayerMask layerMask = ((1 << 5) + (1 << 16));
    //public int layerIndex;
    //[Button]
    //void MaskaEditor()
    //{
    //    maska = ((1 << 2) | (1 << 4));

    //    if ((layerMask & (1 << layerIndex)) != 0) print($"Layer {LayerMask.LayerToName(layerIndex)} is in mask");
    //    else print($"Layer {LayerMask.LayerToName(layerIndex)}  is not in the mask");
    //}
    private void Awake()
    {
        _cam = Camera.main;
    }
    private void Start()
    {
        print($"Test script is on '{gameObject.name}' gameobject, that is on '{gameObject.scene.name}' scene.");
    }

    private void Update()
    {
        //angl = Vector3.Angle(Vector3.one, otherTransform.localScale);
        //signAngle = Vector3.SignedAngle(Vector3.one, otherTransform.localScale, Vector3.up);

        //anim.MatchTarget(targetMatchL.position, targetMatchL.rotation, AvatarTarget.LeftFoot, weightMask, 0f, 1f);
        //anim.MatchTarget(targetMatchR.position, targetMatchR.rotation, AvatarTarget.RightFoot, weightMask, 0f, 1f);
        // anim.MatchTarget(targetMatchR.position, targetMatchR.rotation, AvatarTarget.Root, weightMask, 0f, 1f);


    }
}

