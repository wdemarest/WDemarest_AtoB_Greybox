using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LightColorEntry
{
	public string eventName;
	public Color colorChange;
}

[RequireComponent(typeof(Light))]
public class EventListener_LightChange : MonoBehaviour 
{
	[Tooltip("Events to listen for that will turn the light on.")]
	public string TurnOnEventName;
	[Tooltip("Events to listen for that will turn the light off.")]
	public string TurnOffEventName;
	[Tooltip("Events to listen for that will toggle the state of the light, off->on, on->off.")]
	public string ToggleEventName;
	[Tooltip("Events to listen for that will change the color of the light.")]
	public  List<LightColorEntry> ColorEvents;
	[Tooltip("Determines whether or not the light is off at the start of the level")]
	public bool startOn = true;
	// Use this for initialization
	private Light _light;

	void Start () 
	{
		_light = GetComponent<Light>();


		foreach(LightColorEntry lce in ColorEvents)
		{
			EventRegistry.AddEvent(lce.eventName, LightColor, gameObject);
		}

		if (TurnOnEventName != "")
		{
			EventRegistry.AddEvent (TurnOnEventName, LightOn, gameObject);
		}

		if (TurnOffEventName != "")
		{
			EventRegistry.AddEvent (TurnOffEventName, LightOff, gameObject);
		}

		if (ToggleEventName != "")
		{
			EventRegistry.AddEvent (ToggleEventName, LightToggle, gameObject);
		}
		if(_light != null)
			_light.enabled = startOn;
			
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	void LightOff(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != gameObject))
            return;
        _light.enabled = false;
	}

	void LightOn(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != gameObject))
            return;
        _light.enabled = true;
	}

	void LightToggle(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != gameObject))
            return;
        _light.enabled = !_light.enabled;
	}

	void LightColor(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != gameObject))
            return;
        foreach (LightColorEntry lce in ColorEvents)
		{
			if(lce.eventName == eventName)
				_light.color = lce.colorChange;		
		}
		
	}
}