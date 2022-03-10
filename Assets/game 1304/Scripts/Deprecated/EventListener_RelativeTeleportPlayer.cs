using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EventListener_RelativeTeleportPlayer : MonoBehaviour
{
    public Transform sourceReferencePoint;
    public Transform destinationReferencePoint;

    [Header("Event Listening")]
    public List<string> eventsToListenFor;
    
        

    void Start()
    {
        if (eventsToListenFor.Count > 0)
        {
            foreach (string s in eventsToListenFor)
            {
                if (s != "")
                    EventRegistry.AddEvent(s, teleport, gameObject);
            }
        }
    }


    void teleport(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != gameObject))
            return;
        GameManager.player.transform.position = (GameManager.player.transform.position - sourceReferencePoint.position)  + destinationReferencePoint.transform.position;
    }
}
