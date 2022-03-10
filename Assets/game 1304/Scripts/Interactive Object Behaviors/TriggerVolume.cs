using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TriggerVolume : MonoBehaviour 
{
    [Header("Event Sending")]
    public List<EventPackage> EventsToSendOnEnter;
    public List<EventPackage> EventsToSendOnExit;

    [Header("Event Listening")]
    public string eventToEnableThis;
	public string eventToDisableThis;
	public bool startEnabled = true;
	//public bool PlayerOnly = true;
	private bool isEnabled;
    public bool onlyTriggerOnPlayer = false;
    public List<GameObject> onlyTriggerOnTheseObjects;
	public List<GameObject> ignoreTheseObjects;
	public int maxTriggerCount = 0;
	private int currentTriggerCount;    

    void Start () 
	{
		isEnabled = startEnabled;
		if(eventToEnableThis != "")
			EventRegistry.AddEvent(eventToEnableThis, enableThisOnEvent, gameObject);
		if(eventToDisableThis != "")
			EventRegistry.AddEvent(eventToDisableThis, disableThisOnEvent, gameObject);
		currentTriggerCount = 0;
	}

	void Update () 
	{
		
	}

    private void enableThis()
    {
        isEnabled = true;
    }
	public void enableThisOnEvent(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != this.gameObject))
            return;
        enableThis();
	}

    private void disableThis()
    {

    }
	public void disableThisOnEvent(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != this.gameObject))
            return;
        isEnabled = false;
	}

	private bool objectChecksOut(GameObject go)
	{
		if(ignoreTheseObjects.Count>0)
        {
			foreach(GameObject g in ignoreTheseObjects)
            {
				if (go == g)
					return false;
            }
        }
        if(onlyTriggerOnPlayer)
        {
            if (go == GameManager.player)
                return true;
            else
                return false;
        }
		if(onlyTriggerOnTheseObjects.Count > 0)
		{
			foreach(GameObject g in onlyTriggerOnTheseObjects)
			{
				if(go == g)
					return true;
			}
			return false;
		}
		else
			return true;
	}

	void OnTriggerEnter(Collider other) 
	{
		if(isEnabled)
		{
			if(objectChecksOut(other.gameObject)&&(!other.isTrigger))
			{
				if (maxTriggerCount > 0)
				{
					currentTriggerCount += 1;
					if (currentTriggerCount > maxTriggerCount)
						isEnabled = false;
				}
				if(EventsToSendOnEnter.Count > 0)
				{
                    foreach (EventPackage ep in EventsToSendOnEnter)
                    {
                        if ((ep.scope == eventScope.instigator) || (ep.scope == eventScope.visualScriptingInstigator))
                            EventRegistry.SendEvent(ep, other.gameObject);
                        else
                            EventRegistry.SendEvent(ep, this.gameObject);
                    }                    
                }
			}
		}
	}

	void OnTriggerExit(Collider other) 
	{
		if(isEnabled)
		{
			if(objectChecksOut(other.gameObject)&&(!other.isTrigger))
			{
				if(EventsToSendOnExit.Count > 0)
				{
                    foreach (EventPackage ep in EventsToSendOnExit)
                    {
                        if ((ep.scope == eventScope.instigator) || (ep.scope == eventScope.visualScriptingInstigator))
                            EventRegistry.SendEvent(ep, other.gameObject);
                        else
                            EventRegistry.SendEvent(ep, this.gameObject);
                    }                        
                }
			}
		}
	}

}
