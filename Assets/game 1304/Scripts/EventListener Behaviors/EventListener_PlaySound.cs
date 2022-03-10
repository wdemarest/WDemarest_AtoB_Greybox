using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventListener_PlaySound : MonoBehaviour 
{
	private AudioSource _audio;
	public string startSoundEvent;
	public string stopSoundEvent;
    public float randomPitchVariance = 0;
	// Use this for initialization
	void Start () 
	{
		EventRegistry.Init();
		if (startSoundEvent != "")
		{
			EventRegistry.AddEvent(startSoundEvent, startSound, gameObject);
		}
		if (stopSoundEvent != "")
		{
			EventRegistry.AddEvent(stopSoundEvent, stopSound, gameObject);
		}
		_audio = GetComponent<AudioSource>();

	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	private void startSound(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != gameObject))
            return;
        if (_audio != null)
        {
            if (randomPitchVariance > 0)
                _audio.pitch = 1 + ((Random.Range(0, (int)(randomPitchVariance * 100))/100f)*2) - (randomPitchVariance);
            if (_audio.clip != null)
                _audio.PlayOneShot(_audio.clip);
        }
	}

	private void stopSound(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != gameObject))
            return;
        if (_audio != null)
			_audio.Stop();
	}
}
