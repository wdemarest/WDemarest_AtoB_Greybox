using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EventListener_AbsoluteTeleportPlayer : MonoBehaviour
{    
    public Transform destinationPoint;

    [Header("Event Listening")]
    public List<string> eventsToListenFor;
    
        

    void Start()
    {
        if (eventsToListenFor.Count > 0)
        {
            foreach (string s in eventsToListenFor)
            {
                if (s != "")
                    EventRegistry.AddEvent(s, teleportOnEvent, gameObject);
            }
        }
    }


    void teleportOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;

        GameManager.player.GetComponent<GAME1304PlayerController>().teleport(destinationPoint.position);
        //TODO: get rotation working in here
    }
}
