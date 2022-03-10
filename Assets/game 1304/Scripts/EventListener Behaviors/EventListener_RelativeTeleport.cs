using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EventListener_RelativeTeleport : MonoBehaviour
{
    public Transform sourceReferencePoint;
    public Transform destinationReferencePoint;
    public GameObject target;
    public bool targetPlayer = false;

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
        if (targetPlayer)
        {
            target = GameManager.player;
            GameManager.player.GetComponent<GAME1304PlayerController>().teleport((target.transform.position - sourceReferencePoint.position) + destinationReferencePoint.transform.position);
        }
        else
            target.transform.position = (target.transform.position - sourceReferencePoint.position) + destinationReferencePoint.transform.position;
    }
}
