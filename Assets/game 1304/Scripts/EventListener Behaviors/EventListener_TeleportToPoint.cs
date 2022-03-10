using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EventListener_TeleportToPoint : MonoBehaviour
{
    public List<string> eventsToListenFor;
    public GameObject gameObjectToTeleport;
    public GameObject gameObjectDestination;
    public float randomRadius = 0f;
    public bool constrainRandomToHorizontal = true;

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
        Vector3 destinationPosition;
        Vector3 destinationOffset;
        Vector2 horizontalOffset;

        if (constrainRandomToHorizontal)
        {
            horizontalOffset = Random.insideUnitCircle * randomRadius;
            destinationOffset = new Vector3(horizontalOffset.x, 0, horizontalOffset.y);
        }         
        else
        {
            destinationOffset = Random.insideUnitSphere * randomRadius;
        }

        destinationPosition = gameObjectDestination.transform.position + destinationOffset;

        if (gameObjectToTeleport.GetComponent<NavMeshAgent>() != null)
        {
            NavMeshHit hit;
            NavMesh.SamplePosition(destinationPosition, out hit, 5.0f,1);
            destinationPosition = hit.position;
        }
        gameObjectToTeleport.transform.position = destinationPosition;
	}
}
