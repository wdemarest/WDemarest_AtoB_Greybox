using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointBehavior : MonoBehaviour 
{
    [Header("Event Listening")]
    public List<string> eventsToTriggerThis;

    [Header("Event Sending")]
    public List<EventPackage> EventsToSendOnUse;
    public bool consumeOnUse = false;

    public Material dormantMaterial;
    public Material activeMaterial;
    
    public Transform respawnTransform;

    private static GameObject activeCheckpoint;    
    private static List<GameObject> checkpoints;
    
    // Use this for initialization
    void Awake () 
	{
		foreach(string s in eventsToTriggerThis)
        {
            EventRegistry.AddEvent(s, useCheckpointOnEvent, gameObject);
        }
        GetComponent<Renderer>().material = dormantMaterial;
        if (checkpoints == null)
            checkpoints = new List<GameObject>();
        checkpoints.Add(gameObject);
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}
    void useCheckpointOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        useCheckpoint();
    }

    public Transform getRespawnTransform()
    {
        if (respawnTransform != null)
            return respawnTransform;
        else
            return transform;
    }
    public void useCheckpoint()
    {
        activeCheckpoint = gameObject;
        foreach (GameObject go in checkpoints)
        {
            go.GetComponent<CheckpointBehavior>().UpdateCheckpointMaterial();
        }
        foreach(EventPackage ep in EventsToSendOnUse)
        {
            if(ep.scope == eventScope.instigator)
            {
                ep.scope = eventScope.privateEvent;
                EventRegistry.SendEvent(ep, GameManager.player); //TODO: remove hard coded reference and figure something out
            }
            else
                EventRegistry.SendEvent(ep,gameObject);
        }
        if (consumeOnUse)
        {
            checkpoints.Add(gameObject); //should this be remove?
            Destroy(gameObject);
        }
    }

    public void UpdateCheckpointMaterial()
    {

        if (gameObject == activeCheckpoint)
        {
            GetComponent<Renderer>().material = activeMaterial;
        }
        else
        {
            GetComponent<Renderer>().material = dormantMaterial;
        }

    }
    private void OnCollisionEnter(Collision collision)
    {
        GAME1304PlayerController pc = collision.gameObject.GetComponent<GAME1304PlayerController>();
        
        if (pc != null)
        {
            pc.setCheckpoint(this);
            HUDManager.notificationQueue.Enqueue("Checkpoint reached");

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        GAME1304PlayerController pc = other.gameObject.GetComponent<GAME1304PlayerController>();

        if (pc != null)
        {
            pc.setCheckpoint(this);
            HUDManager.notificationQueue.Enqueue("Checkpoint reached");
        }
    }
}
