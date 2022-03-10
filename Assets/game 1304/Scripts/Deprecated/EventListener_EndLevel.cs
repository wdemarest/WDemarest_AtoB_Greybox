using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EventListener_EndLevel : MonoBehaviour 
{

	public string eventToListenFor;

	public string nextLevel = "Intro";

	void Start () 
	{
		if(eventToListenFor != "")
		{
			EventRegistry.AddEvent(eventToListenFor, endLevel, gameObject);
		}
	}
	

	void endLevel (string eventName, GameObject obj) 
	{
        if ((obj != null) && (obj != this.gameObject))
            return;
        if (nextLevel != "")
		{
			//SceneManager.LoadScene(nextLevel);
			SceneManager.LoadScene (nextLevel, LoadSceneMode.Single);
		}
		else
			Application.Quit();
	}
}
