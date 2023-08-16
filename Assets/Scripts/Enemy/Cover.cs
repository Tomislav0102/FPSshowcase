using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cover : MonoBehaviour
{
    public List<GenDirection> openSides = new List<GenDirection>();
    public Transform myTransform;
    public IFaction resident;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        for (int i = 0; i < openSides.Count; i++)
        {
            if (openSides[i] == GenDirection.Left) Gizmos.DrawWireSphere(myTransform.position - myTransform.right, 0.2f);
            if (openSides[i] == GenDirection.Right) Gizmos.DrawWireSphere(myTransform.position + myTransform.right, 0.2f);
        }
    }
}
