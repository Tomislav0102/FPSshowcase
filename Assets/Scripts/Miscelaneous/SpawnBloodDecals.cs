using Knife.RealBlood;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBloodDecals : MonoBehaviour
{
    GameManager _gm;
    /// <summary>
    /// Collision events buffer
    /// </summary>
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    /// <summary>
    /// Minimum random scale of decal instance
    /// </summary>
    [SerializeField] float minScale = 1f;
    /// <summary>
    /// Maximum random scale of decal instance
    /// </summary>
    [SerializeField] float maxScale = 1.5f;
    /// <summary>
    /// Duration of curve (timer will started from OnEnable event)
    /// </summary>
    float duration = 1f;
    /// <summary>
    /// Scale curve over time
    /// </summary>
    [SerializeField] AnimationCurve scaleOverTime = AnimationCurve.Constant(0, 1, 1);

    private ParticleSystem system;


    private float enabledTime = 0;
    private float scale;

    private void Awake()
    {
        _gm = GameManager.Instance;
        system = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        enabledTime = Time.time;

    }

    private void Update()
    {
        float fraction = (Time.time - enabledTime) / duration;
        scale = scaleOverTime.Evaluate(fraction);
    }

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = system.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 pos = collisionEvents[i].intersection;
            Vector3 normal = collisionEvents[i].normal;

            Quaternion rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * Quaternion.LookRotation(-normal);

            GameObject decal = _gm.poolManager.GetBloodDecals();
            decal.transform.SetPositionAndRotation(pos + normal * 0.0002f, rotation);
            decal.transform.localScale *= Random.Range(minScale, maxScale) * scale;
        }
    }

}
