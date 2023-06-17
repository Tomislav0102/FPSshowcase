using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class IsCoverAvailableNode : Node
{
    Cover[] _avaliableCovers;
    Transform _target;
    EnemyAI _enemyAI;

    public IsCoverAvailableNode(Cover[] covers, Transform target, EnemyAI enemyAI)
    {
        _avaliableCovers = covers;
        _target = target;
        _enemyAI = enemyAI;
    }

    public override NodeState Evaluate()
    {
        Transform bestSpot = FindBestCoverSpot();
        _enemyAI.bestSpot = bestSpot;
        nodeState = bestSpot != null ? NodeState.Success : NodeState.Failure;
        return nodeState;
    }

    Transform FindBestCoverSpot()
    {
        if(_enemyAI.bestSpot != null)
        {
            if (CheckIfSpotIsValid(_enemyAI.bestSpot))
            {
                return _enemyAI.bestSpot;
            }
        }

        float minAngle = 90f;
        Transform bestSpot = null;
        for (int i = 0; i < _avaliableCovers.Length; i++)
        {
            Transform bestSpotInCover = FindBestCoverSpot(_avaliableCovers[i], ref minAngle);
            if (bestSpotInCover != null)
            {
                bestSpot = bestSpotInCover;
            }
        }
        return bestSpot;
    }
    Transform FindBestCoverSpot(Cover cov, ref float minAngle)
    {
        Transform[] avaliableSpots = cov.coverSpots;
        Transform bestSpot = null;
        for (int i = 0; i < avaliableSpots.Length; i++)
        {
            Vector3 dir = (_target.position - avaliableSpots[i].position).normalized;
            if (CheckIfSpotIsValid(avaliableSpots[i]))
            {
                float angle = Vector3.Angle(avaliableSpots[i].forward, dir);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestSpot = avaliableSpots[i];
                }
            }
        }
        return bestSpot;
    }

    bool CheckIfSpotIsValid(Transform spot)
    {
        RaycastHit hit;
        Vector3 dir = (_target.position - spot.position).normalized;
        if(Physics.Raycast(spot.position, dir, out hit))
        {
            if(hit.transform != _target)
            {
                return true;
            }
        }
        return false;
    }
}
