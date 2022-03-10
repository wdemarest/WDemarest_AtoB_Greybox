///This listener takes events broadcast from the event manager and broadcasts other events in response.
///This is ideal if you want things A and B to cause C to happen, but A also causes D to happen when B doesn't cause it.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventDistiller : MonoBehaviour 
{
    [Header("Event Listening")]
    public List<string> eventsToListenFor;
    [Header("Event Sending")]
    public List<EventPackage> eventsToSend;
    [Header("Deprecated")]
    public List<string> eventsToExecute;
	// Use this for initialization
	void Start () 
	{
		if(eventsToListenFor.Count >0)
		{
			foreach (string s in eventsToListenFor)
			{
				if(s != "")
					EventRegistry.AddEvent(s, sendEvent, gameObject);
			}
		}
	}

	void sendEvent(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (string s in eventsToExecute)		
			EventRegistry.SendEvent(s);		
        foreach (EventPackage ep in eventsToSend)
            EventRegistry.SendEvent(ep, this.gameObject);
    }


}
