using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class CustomEditorCollection 
{

}

//[CustomEditor(typeof(EnemyBehaviour))]
//public class Custom_EnemyBehaviour : Editor
//{
//    float size = 1f;

//    protected virtual void OnSceneGUI()
//    {
//        if (Event.current.type == EventType.Repaint)
//        {
//            Transform transform = ((EnemyBehaviour)target).transform;
//            Handles.color = Handles.xAxisColor;
//            Handles.ConeHandleCap(
//                0,
//                transform.position + new Vector3(3f, 0f, 0f),
//                transform.rotation * Quaternion.LookRotation(Vector3.right),
//                size,
//                EventType.Repaint
//            );
//            Handles.color = Handles.yAxisColor;
//            Handles.ConeHandleCap(
//                0,
//                transform.position + new Vector3(0f, 3f, 0f),
//                transform.rotation * Quaternion.LookRotation(Vector3.up),
//                size,
//                EventType.Repaint
//            );
//            Handles.color = Handles.zAxisColor;
//            Handles.ConeHandleCap(
//                0,
//                transform.position + new Vector3(0f, 0f, 3f),
//                transform.rotation * Quaternion.LookRotation(Vector3.forward),
//                size,
//                EventType.Repaint
//            );
//        }
//    }
//}
