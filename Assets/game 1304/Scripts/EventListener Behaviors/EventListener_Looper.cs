using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventListener_Looper : MonoBehaviour 
{
	public int loopCount;
	public string eventToListenFor;
    [Header ("Event Sending")]
    public List<EventPackage> eventsToSend;    
	private int loopIndex=0;
	private bool isLooping = false;    
    // Use this for initialization
    void Start () 
	{
		EventRegistry.AddEvent(eventToListenFor, loopEvents, gameObject);
	}

	void loopEvents(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != this.gameObject))
            return;
        isLooping = true;
		loopIndex = 0;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(isLooping)
		{							
           foreach (EventPackage ep in eventsToSend)                
                EventRegistry.SendEvent(ep, this.gameObject);
            if (loopCount != -1)
            {
                loopIndex += 1;
                if (loopIndex > loopCount)
                    isLooping = false;
            }
		}
	}
}
