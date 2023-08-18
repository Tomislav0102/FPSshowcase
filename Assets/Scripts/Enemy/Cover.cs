using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cover : MonoBehaviour
{
    [HorizontalGroup]
    [LabelWidth(80)]
    public bool leftOpening, rightOpening;
    [Space]
    public Transform myTransform;
    public IFaction resident;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        if (leftOpening) Gizmos.DrawWireSphere(myTransform.position - myTransform.right, 0.2f);
        if (rightOpening) Gizmos.DrawWireSphere(myTransform.position + myTransform.right, 0.2f);
    }
}
