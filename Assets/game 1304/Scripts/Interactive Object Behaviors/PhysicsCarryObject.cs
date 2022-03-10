using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsCarryObject : MonoBehaviour 
{
	public string objectName;
	public bool snapToRotation;
	public Vector3 snapRotation;
	public int baseDamage = 0;
    public signalTypes damageType = signalTypes.physics;
	public float velocityDamageThreshold = 1.0f;
    //public damageTypes damageType;
    public List<EventPackage> eventsToSendOnPickup;
    public List<EventPackage> eventsToSendOnDrop;
    public List<EventPackage> eventsToSendOnThrow;    

	void Start () 
	{
		
	}
	

	void Update () 
	{
		
	}

	public void pickupObject()
	{		
        foreach (EventPackage ep in eventsToSendOnPickup)
            EventRegistry.SendEvent(ep, this.gameObject);
    }

	public void dropObject()
	{		
        foreach (EventPackage ep in eventsToSendOnDrop)
            EventRegistry.SendEvent(ep, this.gameObject);
    }

	public void throwObject()
	{		
        foreach (EventPackage ep in eventsToSendOnThrow)
            EventRegistry.SendEvent(ep, this.gameObject);
    }

	void OnCollisionEnter(Collision other)
	{
		NPCBehavior otherEnemy = other.gameObject.GetComponent<NPCBehavior>();
		Rigidbody thisRB = GetComponent<Rigidbody>();
		if(otherEnemy != null)
		{
			if(thisRB.velocity.magnitude >= velocityDamageThreshold)
			{
				otherEnemy.Damage(baseDamage, damageType);
			}
		}
	}
}
