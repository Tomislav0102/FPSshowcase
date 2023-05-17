using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEditor;

/// <summary>
/// Generates a list of new instances of a class "SomClass" based on time that instance finished async/await.
/// Note that main class (the one that listen for a event to be called) doesn't have any reference to System.Action, Lambda method does all the work.
/// </summary>
public class UsingEvent_DifferentWay : MonoBehaviour
{
    [SerializeField] List<SomeClass> someClasses = new List<SomeClass>();
    private void Start()
    {
        for (int i = 0; i < 50; i++)
        {
            var someClass = new SomeClass(GUID.Generate().ToString(), (int delay, string id, SomeClass klass) => 
            { 
                print($"delay is {delay}, id is {id}");
                someClasses.Add(klass);
            });
        }
    }

    [System.Serializable]
    public class SomeClass
    {
        string _idName;

        public SomeClass(string id, System.Action<int, string, SomeClass> act)
        {
            _idName = id;
            Counter(act);
        }

        async void Counter(System.Action<int, string, SomeClass> act)
        {
            int delay = Random.Range(1000, 3000);
            await Task.Delay(delay);
            act(delay, _idName, this);
        }
    }
}
