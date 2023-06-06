using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEditor.Animations;
using UnityEngine;

public class RagdollToAnimator : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] string[] clipNames;
    string _currentClip;
    [SerializeField] Transform[] ragdollTransforms;
    Rigidbody[] _ragdollRigids;
    Transform _hipsBone;
    bool FacingUp()
    {
        return _hipsBone.forward.y > 0f;
    }
    Vector3 _startPos;
    Quaternion _startRot;

    Transform[] _bones;
    BoneTransforms[] _standingFaceUpBones;
    BoneTransforms[] _standingFaceDownBones;
    BoneTransforms[] _standingCurrent;
    BoneTransforms[] _ragdollBones;

    public bool _readyToStandUp;
    public float timeToStandUp = 2f;
    float _timer;
    float _elapsedPercentage;


    private void Awake()
    {
        _hipsBone = anim.GetBoneTransform(HumanBodyBones.Hips);
        _ragdollRigids = new Rigidbody[ragdollTransforms.Length];
        for (int i = 0; i < ragdollTransforms.Length; i++)
        {
            _ragdollRigids[i] = ragdollTransforms[i].GetComponent<Rigidbody>();
        }
        _startPos = transform.position;
        _startRot = transform.rotation;

        _bones = _hipsBone.GetComponentsInChildren<Transform>();
        _standingFaceUpBones = new BoneTransforms[_bones.Length];
        _standingFaceDownBones = new BoneTransforms[_bones.Length];
        _ragdollBones = new BoneTransforms[_bones.Length];

    }

    private void Start()
    {
        PopulateAnimationBones(true);
        PopulateAnimationBones(false);


    }
    private void Update()
    {
        if (_readyToStandUp)
        {
            _timer += Time.deltaTime;
            _elapsedPercentage = _timer / timeToStandUp;
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = Vector3.Lerp(_ragdollBones[i].pos, _standingCurrent[i].pos, _elapsedPercentage);
                _bones[i].localRotation = Quaternion.Lerp(_ragdollBones[i].rot, _standingCurrent[i].rot, _elapsedPercentage);
            }

            if (_elapsedPercentage >= 1f)
            {
                anim.Play(_currentClip, 0, 0);
                _timer = 0f;
                _readyToStandUp = false;
                anim.enabled = true;
            }


        }
    }

    void PopulateBones(BoneTransforms[] bon)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            bon[i].pos = _bones[i].localPosition;
            bon[i].rot = _bones[i].localRotation;
        }
    }
    void PopulateAnimationBones(bool isFacingUp)
    {
        Vector3 posBeforeSampling = transform.position;
        Quaternion rotBeforeSampling = transform.rotation;

        _currentClip = isFacingUp ? clipNames[0] : clipNames[1];

        foreach (AnimationClip item in anim.runtimeAnimatorController.animationClips)
        {
            if (item.name == _currentClip)
            {
                item.SampleAnimation(gameObject, 0f);
                PopulateBones(isFacingUp ? _standingFaceUpBones: _standingFaceDownBones);
                break;
            }
        }

        transform.position = posBeforeSampling;
        transform.rotation = rotBeforeSampling;
    }

    [Button]
    void ActivateRagdoll() => ActivateRagdoll(true);
    [Button]
    void DeactivateRagdoll() => ActivateRagdoll(false);

    void ActivateRagdoll(bool activ)
    {
        for (int i = 0; i < _ragdollRigids.Length; i++)
        {
            _ragdollRigids[i].isKinematic = !activ;
        }
        if (activ)
        {
            if (!_readyToStandUp)
            {
                _ragdollRigids[8].AddForce(-40f * Vector3.forward, ForceMode.VelocityChange);
                anim.enabled = false;
            }
        }
        else
        {
            _standingCurrent = FacingUp() ? _standingFaceUpBones : _standingFaceDownBones;
            AlignRotationToHips();
            AlignPositionToHips();
            PopulateBones(_ragdollBones);
            _readyToStandUp = true;
            _currentClip = FacingUp() ? clipNames[0] : clipNames[1];
        }
    }

    private void AlignRotationToHips()
    {
        Vector3 originalHipsPosition = _hipsBone.position;
        Quaternion originalHipsRotation = _hipsBone.rotation;

        Vector3 desiredDirection = _hipsBone.up * -1;
        if (!FacingUp()) desiredDirection *= -1f;
        desiredDirection.y = 0;
        desiredDirection.Normalize();

        Quaternion fromToRotation = Quaternion.FromToRotation(transform.forward, desiredDirection);
        transform.rotation *= fromToRotation;

        _hipsBone.position = originalHipsPosition;
        _hipsBone.rotation = originalHipsRotation;
    }

    private void AlignPositionToHips()
    {
        Vector3 originalHipsPosition = _hipsBone.position;
        transform.position = _hipsBone.position;

        Vector3 positionOffset = _standingCurrent[0].pos;
        positionOffset.y = 0;
        positionOffset = transform.rotation * positionOffset;
        transform.position -= positionOffset;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo))
        {
            transform.position = new Vector3(transform.position.x, hitInfo.point.y, transform.position.z);
        }

        _hipsBone.position = originalHipsPosition;
    }

    struct BoneTransforms
    {
        public Vector3 pos;
        public Quaternion rot;
    }
}
