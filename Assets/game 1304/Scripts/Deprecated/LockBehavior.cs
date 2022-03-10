using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockBehavior : MonoBehaviour {

	public GameObject doorObject;
	public Color lockColor;
	public List<string> eventsOnUnlock;
	// Use this for initialization

	void Start () 
	{
		Renderer rend = GetComponent<Renderer>();
		if (rend != null)
		{
			rend.material.color = lockColor;
		}
	}

	public void useKey()
	{
		DestroyObject(doorObject);
		DestroyObject(gameObject);
		if(eventsOnUnlock.Count > 0)
		{
			foreach(string s in eventsOnUnlock)
			{
				if(s != "")
					EventRegistry.SendEvent(s);
			}
		}
	}
	// Update is called once per frame
	void Update () 
	{
		
	}
}
