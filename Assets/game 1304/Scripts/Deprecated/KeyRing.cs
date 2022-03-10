using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyRing : MonoBehaviour 
{
	List<Color> keys;
	// Use this for initialization
	void Start () 
	{
		keys = new List<Color>();
	}

	public void addKey(Color keyColor)
	{
		keys.Add(keyColor);
	}

	public bool consumeKey(Color keyColor)
	{
		if (keys.Contains(keyColor))
		{
			keys.Remove(keyColor);
			return true;	
		}
		else
		{
			return false;
		}
	}


	void OnCollisionEnter (Collision col)
	{
		KeyBehavior kb = col.gameObject.gameObject.GetComponent<KeyBehavior>();
		if (kb != null)
		{
			keys.Add(kb.keyColor);
			if(kb.eventsOnPickup.Count > 0)
			{
				foreach(string s in kb.eventsOnPickup)
				{
					if(s != "")
					{
						EventRegistry.SendEvent(s);
					}
				}
			}
			DestroyObject(col.gameObject);
		}

		LockBehavior lb = col.gameObject.GetComponent<LockBehavior>();
		if (lb != null)
		{
			if(keys.Contains(lb.lockColor))
			{
				consumeKey(lb.lockColor);
				lb.useKey();
			}
		}
	}

	// Update is called once per frame
	void Update () 
	{
		
	}
}
