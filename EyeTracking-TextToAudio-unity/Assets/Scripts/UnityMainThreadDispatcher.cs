using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();
    private static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var obj = new GameObject("MainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
        else
        {
            Destroy(GameObject.Find("MainThreadDistpatcher"));
        }
        return _instance;
    }

    // Update is called once per frame
    private void Update()
    {
        while (_executionQueue.Count > 0)
        {
            var action = _executionQueue.Dequeue();
            action.Invoke();
        }
    }

    public void Enqueue(System.Action action)
    {
        if (action == null) throw new System.ArgumentNullException(nameof(action));
        Debug.Log("Enqueue action");
        _executionQueue.Enqueue(action);
    }
}
