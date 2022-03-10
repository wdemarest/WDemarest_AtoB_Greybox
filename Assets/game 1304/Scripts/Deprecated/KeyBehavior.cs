using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyBehavior : MonoBehaviour 
{

	public Color keyColor;
	public List<string> eventsOnPickup;

	// Use this for initialization
	void Start () 
	{
		Renderer rend = GetComponent<Renderer>();
		if (rend != null)
		{
			rend.material.color = keyColor;
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}
}
