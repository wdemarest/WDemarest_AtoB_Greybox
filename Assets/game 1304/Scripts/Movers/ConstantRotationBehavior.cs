using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ConstantRotationBehavior : MonoBehaviour 
{
	public bool startOn = true;
	private bool _isActive;    	
	
    [Tooltip("Velocity to move in units per second")]
    public Vector3 rotationSpeed;
	
	private moverState currentState;
	private moverState nextState;

	private Rigidbody rb;



	[Header("Events")]
	public bool isEventDriven = false;	
	public string pauseEvent;
	public string resumeEvent;	


	void Start () 
	{
        
		rb = gameObject.GetComponent<Rigidbody>();
        if(rb==null)
        {
            rb = gameObject.GetComponentInChildren<Rigidbody>();
        }
        if(rb!=null)
            rb.isKinematic = true;
        currentState = moverState.Waiting;
		_isActive = startOn;
	

		//set up events
		EventRegistry.Init();
		if(pauseEvent != "")
		{
			EventRegistry.AddEvent(pauseEvent, pauseOnEvent, gameObject);
		}
		if(resumeEvent != "")
		{
			EventRegistry.AddEvent(resumeEvent, resumeOnEvent, gameObject);
		}		
	}


	// Update is called once per frame
	void Update () 
	{
		
	}

	void pauseOnEvent(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != this.gameObject))
            return;
        _isActive = false;
	}

	void resumeOnEvent(string eventName, GameObject obj)
	{
        if ((obj != null) && (obj != this.gameObject))
            return;
        _isActive = true;
	}

    void FixedUpdate()
	{
		//Time.time
		if(!_isActive)
			return;
        rb.MoveRotation(transform.rotation * Quaternion.Euler(rotationSpeed.x * Time.fixedDeltaTime, rotationSpeed.y * Time.fixedDeltaTime, rotationSpeed.z * Time.fixedDeltaTime));
        //transform.rotation = transform.rotation * Quaternion.Euler(rotationSpeed.x, rotationSpeed.y, rotationSpeed.z);
        //rb.MoveRotation(transform.rotation * Quaternion.Euler( rotationSpeed.x, rotationSpeed.y , rotationSpeed.z ));
	}
}
