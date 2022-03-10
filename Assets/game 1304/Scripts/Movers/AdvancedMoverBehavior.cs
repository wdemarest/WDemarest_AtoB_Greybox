using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[System.Serializable]
public class moverTargetInfo
{
    public GameObject targetObject;
    public List<string> eventsToFireOnArrival;
    public List<string> eventsToFireOnDeparture;
    public float timeToWaitHere = 0f;
    public float timeToNextPoint = 1.0f;
}

[System.Serializable]
public class EventIndexPackage
{
    public string eventName;
    public int indexDestination;
}

public enum advancedMoverRouteBehavior { oneWay, loop, pingPong, wander };
public enum advancedMoverState { MovingForward, MovingBackward, Waiting };

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class AdvancedMoverBehavior : MonoBehaviour
{

    [Header("Basic Settings")]
    public bool startOn = true;
    private bool _isActive;

    [Tooltip("Set the time that the mover has already waited at A. Should not exceed A's wait time")]
    public float startTimeOffset;
    public List<moverTargetInfo> moverTargets;

    [Header("Sounds")]
    //public AudioClip StartSound;
    public AudioClip StopSound;
    public AudioClip MovingSound;
    //public AudioClip IdlingSound;
    private AudioSource _audioSource;

    private advancedMoverState currentState;    

    private moverTargetInfo currentMTI;
    private moverTargetInfo nextMTI;
    private int currentTargetIndex;
    private int nextTargetIndex;

    private float distanceToDestination;

    private Rigidbody rb;
    private float waitTime;
    private float _currentWaitTime;
    private float lerpValue;
    private bool eventControlled = false;

    private Vector3 positionA, positionB;
    private Vector3 velAtoB;

    [Header("Event Listening")]
    [Tooltip("If TRUE, this mover will only move when events affect it. Otherwise events will still affect it, but it will resume normal movement after each event.")]
    public bool onlyMoveOnEvents = false;
    public string pauseEvent;
    public string resumeEvent;
    public string toggleActiveEvent;
    public string goToNextEvent;
    public string goToPreviousEvent;
    public List<EventIndexPackage> goToIndexEvents;

    [Header("Destination Ordering")]
    public navBehavior MovementType = navBehavior.patrolLoop;
    private int pingPongDir = 1;

    [Header("Debug")]
    public bool hideDebugDraw = false;
    public bool onlyShowDebugWhenSelected = false;

    private moverTargetInfo getTarget(int index)
    {
        if (moverTargets.Count == 0)
            return null;
        if (index < moverTargets.Count)
            return (moverTargets[index]);
        else
            return (moverTargets[(index - 1) % moverTargets.Count]);
    }

    void Start()
    {

        _audioSource = GetComponent<AudioSource>();
        rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.GetComponentInChildren<Rigidbody>();
        }
        rb.isKinematic = true;
        currentTargetIndex = 0;
        nextTargetIndex = 0;

        GetNextTarget();        

        rb.velocity = velAtoB;
        currentState = advancedMoverState.MovingForward;

        _isActive = startOn;
        lerpValue = 0;        

        //set up events        
        if (pauseEvent != "")
        {
            EventRegistry.AddEvent(pauseEvent, pauseOnEvent, gameObject);
        }
        if (resumeEvent != "")
        {
            EventRegistry.AddEvent(resumeEvent, resumeOnEvent, gameObject);
        }
        if (toggleActiveEvent != "")
        {
            EventRegistry.AddEvent(toggleActiveEvent, toggleActiveOnEvent, gameObject);
        }
        if (goToNextEvent != "")
        {
            EventRegistry.AddEvent(goToNextEvent, goToNextOnEvent, gameObject);
        }
        if (goToPreviousEvent != "")
        {
            EventRegistry.AddEvent(goToPreviousEvent, goToPrevOnEvent, gameObject);
        }
        foreach(EventIndexPackage eip in goToIndexEvents)
        {
            EventRegistry.AddEvent(eip.eventName, goToIndexOnEvent, gameObject);
        }

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
        eventControlled = false;
    }

    void toggleActiveOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        _isActive = !_isActive;
        
    }

    public void processInteractionInput(moverInteractionModes mode)
    {
        switch (mode)                    
        {
            case moverInteractionModes.goToA:
                
                break;
            case moverInteractionModes.goToB:
                
                break;
            case moverInteractionModes.goToNextStop:
                goToNextOnEvent("",null);
                break;
            case moverInteractionModes.toggleOnOff:
                toggleActiveOnEvent("", null);
                break;
            case moverInteractionModes.turnOff:
                pauseOnEvent("", null);
                break;
            case moverInteractionModes.turnOn:
                resumeOnEvent("", null);
                break;
        }
    }

    void goToNextOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        _isActive = true;
        eventControlled = true;
    }

    void goToIndexOnEvent(string eventName, GameObject obj)
    {
        foreach(EventIndexPackage eip in goToIndexEvents)
        {
            if (eip.eventName == eventName)
            {
                if ((obj != null) && (obj != this.gameObject))
                    return;
                _isActive = true;
                eventControlled = true;
                GoToIndex(eip.indexDestination);
            }
        }
    }

    void GoToIndex(int destination)
    {
        currentTargetIndex = nextTargetIndex;
        currentMTI = getTarget(currentTargetIndex);
        nextTargetIndex = destination;
        nextMTI = getTarget(nextTargetIndex);
        waitTime = 0;

        positionA = currentMTI.targetObject.transform.position;
        positionB = nextMTI.targetObject.transform.position;
        velAtoB = Vector3.Normalize(positionB - positionA);// * moveSpeed;

        rb.velocity = velAtoB;
        currentState = advancedMoverState.MovingForward;
        
        lerpValue = 0;

    }

    void goToPrevOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;

        _isActive = true;
        eventControlled = true;
    }


    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        if (!_isActive)
            return;
        //Time.time
        //TODO: use a LERP instead of whatever the hell this stuff is
        switch (currentState)
        {
            case advancedMoverState.MovingForward:
                {
                    if (currentMTI.timeToNextPoint > 0)
                    {
                        lerpValue += Time.deltaTime / currentMTI.timeToNextPoint;
                        distanceToDestination = Vector3.Distance(transform.position, positionB);
                    }
                    else
                    {
                        transform.position = positionB;                        
                        distanceToDestination = 0f;
                    }

                    if (distanceToDestination <= Time.deltaTime / currentMTI.timeToNextPoint)
                    {
                        if ((_audioSource != null) && (StopSound != null))
                        {
                            _audioSource.loop = false;
                            _audioSource.clip = StopSound;
                            _audioSource.Play();
                        }


                        currentState = advancedMoverState.Waiting;
                        GetNextTarget();
                        

                        
                        lerpValue = 0;
                        _currentWaitTime = 0;
                        if (eventControlled)
                        {
                            _isActive = false;                            
                        }
                    }
                    else
                    {
                        rb.MovePosition(Vector3.Lerp(positionA, positionB, lerpValue));                        
                    }
                }
                break;
            case advancedMoverState.Waiting:
                {
                    rb.velocity = Vector3.zero;
                    if ((!onlyMoveOnEvents) && (_isActive))
                    {

                        _currentWaitTime += Time.deltaTime;
                        if (_currentWaitTime >= waitTime)
                        {
                            currentState = advancedMoverState.MovingForward;
                            if ((_audioSource != null) && (MovingSound != null))
                            {
                                _audioSource.loop = true;
                                _audioSource.clip = MovingSound;
                                _audioSource.Play();

                            }                            
                        }
                    }
                }
                break;
            

        }

    }

    private void GetNextTarget()
    {
        switch(MovementType)
        {
            case navBehavior.oneWay:
                {
                    currentTargetIndex = nextTargetIndex;
                    currentMTI = getTarget(currentTargetIndex);
                    nextTargetIndex = currentTargetIndex+1;
                    if (nextTargetIndex >= moverTargets.Count)
                    {
                        _isActive = false;
                    }
                    else
                    {
                        nextMTI = getTarget(nextTargetIndex);
                        waitTime = currentMTI.timeToWaitHere;

                        positionA = currentMTI.targetObject.transform.position;
                        positionB = nextMTI.targetObject.transform.position;
                        velAtoB = Vector3.Normalize(positionB - positionA);// * moveSpeed;
                    }
                }
                break;
            case navBehavior.patrolLoop:
                {
                    currentTargetIndex=nextTargetIndex;
                    currentMTI = getTarget(currentTargetIndex);
                    nextTargetIndex = currentTargetIndex+1;
                    nextMTI = getTarget(nextTargetIndex);
                    waitTime = currentMTI.timeToWaitHere;

                    positionA = currentMTI.targetObject.transform.position;
                    positionB = nextMTI.targetObject.transform.position;
                    velAtoB = Vector3.Normalize(positionB - positionA);// * moveSpeed;
                }
                break;
            case navBehavior.wander:
                {
                    
                    currentTargetIndex=nextTargetIndex;
                    currentMTI = getTarget(currentTargetIndex);
                    nextTargetIndex = UnityEngine.Random.Range(0, moverTargets.Count);
                    nextMTI = getTarget(nextTargetIndex);
                    waitTime = currentMTI.timeToWaitHere;

                    positionA = currentMTI.targetObject.transform.position;
                    positionB = nextMTI.targetObject.transform.position;
                    velAtoB = Vector3.Normalize(positionB - positionA);// * moveSpeed;
                }
                break;
            case navBehavior.patrolPingPong:
                {
                    currentTargetIndex = nextTargetIndex;
                    currentMTI = getTarget(currentTargetIndex);
                    nextTargetIndex = currentTargetIndex+pingPongDir;
                    if(nextTargetIndex < 0)
                    {
                        nextTargetIndex = 1;
                        pingPongDir *= -1;
                    }
                    else
                    {
                        if (nextTargetIndex >= moverTargets.Count)
                        {
                            nextTargetIndex = moverTargets.Count-2;
                            pingPongDir *= -1;
                        }
                    }
                    nextMTI = getTarget(nextTargetIndex);
                    waitTime = currentMTI.timeToWaitHere;

                    positionA = currentMTI.targetObject.transform.position;
                    positionB = nextMTI.targetObject.transform.position;
                    velAtoB = Vector3.Normalize(positionB - positionA);// * moveSpeed;
                }
                break;
        }
        
    }

    private void OnDrawGizmos()
    {
        if (!hideDebugDraw && !onlyShowDebugWhenSelected)
            drawTargetGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (!hideDebugDraw)
            drawTargetGizmos();
    }

    private void drawTargetGizmos()
    {
#if UNITY_EDITOR
        List<Vector3> boundingBoxVerts;
        Collider col;
        
        

        col = GetComponent<Collider>();

        
        boundingBoxVerts = new List<Vector3>();
        boundingBoxVerts.Add(new Vector3(col.bounds.min.x, col.bounds.max.y, col.bounds.min.z) - transform.position);
        boundingBoxVerts.Add(new Vector3(col.bounds.max.x, col.bounds.max.y, col.bounds.min.z) - transform.position);
        boundingBoxVerts.Add(new Vector3(col.bounds.min.x, col.bounds.max.y, col.bounds.max.z) - transform.position);
        boundingBoxVerts.Add(new Vector3(col.bounds.max.x, col.bounds.max.y, col.bounds.max.z)- transform.position);
        boundingBoxVerts.Add(new Vector3(col.bounds.min.x, col.bounds.min.y, col.bounds.min.z)- transform.position);
        boundingBoxVerts.Add(new Vector3(col.bounds.max.x, col.bounds.min.y, col.bounds.min.z)- transform.position);
        boundingBoxVerts.Add(new Vector3(col.bounds.min.x, col.bounds.min.y, col.bounds.max.z)- transform.position);
        boundingBoxVerts.Add(new Vector3(col.bounds.max.x, col.bounds.min.y, col.bounds.max.z) - transform.position);                

        Vector3 lookDir;
        if (moverTargets.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int lcv = 0; lcv < moverTargets.Count; lcv++)
            {
                //Gizmos.DrawLine(transform.position, np.navPointObject.transform.position);
                Handles.color = Color.blue;
                Handles.DrawDottedLine(transform.position, moverTargets[lcv].targetObject.transform.position, 2.0f);
                Handles.color = Color.white;
                Handles.Label(moverTargets[lcv].targetObject.transform.position, lcv.ToString());
                //add movement mode (loop, ping pong, etc) stuff here
                switch (MovementType)
                {
                    case navBehavior.patrolLoop:
                    {
                        if (lcv < moverTargets.Count - 1)
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawLine(moverTargets[lcv].targetObject.transform.position, moverTargets[lcv + 1].targetObject.transform.position);
                            lookDir = moverTargets[lcv + 1].targetObject.transform.position - moverTargets[lcv].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);

                        }
                        else
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawLine(moverTargets[lcv].targetObject.transform.position, moverTargets[0].targetObject.transform.position);
                            lookDir = moverTargets[0].targetObject.transform.position - moverTargets[lcv].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);

                        }
                    }
                    break;
                    case navBehavior.wander:
                    {
                            Handles.color = Color.yellow;
                            Handles.DrawLine(transform.position, moverTargets[lcv].targetObject.transform.position);
                            lookDir = moverTargets[lcv].targetObject.transform.position - transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);
                            //Handles.DrawDottedLine(transform.position, moverTargets[lcv].targetObject.transform.position, 2.0f);
                    }
                    break;
                    case navBehavior.patrolPingPong:
                        if (lcv < moverTargets.Count - 1)
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawLine(moverTargets[lcv].targetObject.transform.position, moverTargets[lcv + 1].targetObject.transform.position);
                            lookDir = moverTargets[lcv + 1].targetObject.transform.position - moverTargets[lcv].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);
                            lookDir = moverTargets[lcv].targetObject.transform.position - moverTargets[lcv + 1].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv + 1].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);
                        }
                        else
                        {
                          /*  Handles.color = Color.yellow;
                            Handles.DrawLine(moverTargets[lcv].targetObject.transform.position, moverTargets[0].targetObject.transform.position);
                            lookDir = moverTargets[0].targetObject.transform.position - moverTargets[lcv].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);
                            lookDir = moverTargets[lcv].targetObject.transform.position - moverTargets[0].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[0].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);*/
                        }
                        break;
                    case navBehavior.oneWay:
                        if (lcv < moverTargets.Count - 1)
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawLine(moverTargets[lcv].targetObject.transform.position, moverTargets[lcv + 1].targetObject.transform.position);
                            lookDir = moverTargets[lcv + 1].targetObject.transform.position - moverTargets[lcv].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);
                            /*lookDir = moverTargets[lcv].targetObject.transform.position - moverTargets[lcv + 1].targetObject.transform.position;
                            Handles.ArrowHandleCap(0, moverTargets[lcv + 1].targetObject.transform.position, Quaternion.LookRotation(lookDir), 1.4f, EventType.Repaint);*/
                        }                        
                        break;

                }
                Handles.color = Color.blue;
                //top
                Handles.DrawLine(boundingBoxVerts[0] + moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[1] + moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[1]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[3] + moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[3]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[2] + moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[2] + moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[0] + moverTargets[lcv].targetObject.transform.position);

                    //bottom
                    Handles.DrawLine(boundingBoxVerts[4]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[5]+ moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[5]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[7]+ moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[7]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[6]+ moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[6] + moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[4] + moverTargets[lcv].targetObject.transform.position);

                    //sides
                    Handles.DrawLine(boundingBoxVerts[0]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[4]+ moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[2]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[6]+ moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[3]+ moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[7]+ moverTargets[lcv].targetObject.transform.position);
                    Handles.DrawLine(boundingBoxVerts[1] + moverTargets[lcv].targetObject.transform.position, boundingBoxVerts[5] + moverTargets[lcv].targetObject.transform.position);                
                       
            }
        }
#endif
    }
}
