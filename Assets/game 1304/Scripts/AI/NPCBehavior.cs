﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEditor;
using UnityEngine.Serialization;

public enum BehaviorType { attacking, patrolling, following, fleeing, idle, none, distracted };
public enum AttitudeType { neutral, aggressive, fearful, friendly };
public enum NPCAttackType { none, touch, melee, rangedProjectile };
public enum PatrolSwitchBehaviorType { pickClosest, pickFirst, pickRandom, pickSimilarIndex };

[Serializable]
public class patrolChangeEventEntry
{
    public string eventName;
    public PatrolSwitchBehaviorType patrolSwitchBehavior;
    public NavPointContainer navContainerObject;
}

[Serializable]
public class behaviorChangeEventEntry
{
    public string eventName;
    public BehaviorType behaviorToChangeTo;
}

[Serializable]
public class HealthChangeEventEntry
{
    public string eventName;
    public int amount;
    public operationType operation;
    public signalTypes damageType = signalTypes.scriptedDamage;
}


[Serializable]
public class attitudeChangeEventEntry
{
    public string eventName;
    public AttitudeType attitudeToChangeTo;
}

[Serializable]
public class visionRangeChangeEventEntry
{
    public string eventName;
    public float newVisionRange;
}

[Serializable]
public class followTargetChangeEventEntry
{
    public string eventName;
    public GameObject newFollowTarget;
}

[Serializable]
public class healthThresholdData
{
    public float healthPercent;
    public bool changeBehavior = false;
    public bool changeAttitude = false;
    public BehaviorType behaviorToChangeTo;
    public AttitudeType attitudeToChangeTo;
}

[Serializable]
public class signalEventEntry
{
    public int damageThreshold = 0;
    public bool filterByDamageType = false;
    public comparisonOperator comparisonForThreshold = comparisonOperator.greaterThanEqual;
    public signalTypes damageType = signalTypes.genericHazard;
    public List<EventPackage> eventsToSend;
    [Header("Deprecated")]
    [FormerlySerializedAs("eventsToSend")]
    public List<string> oldEventsToSend;
    //public bool onlySendEventsToSameObject = false;
}

[Serializable]
public class damageMultiplier
{
    public float multiplier = 1;
    public signalTypes damageType;
    public damageMultiplier(signalTypes type, float amount)
    {
        multiplier = amount;
        damageType = type;
    }
}


public class NPCBehavior : MonoBehaviour
{
    [Header("Mortality")]
    public GameObject objectToDrop;
    public int maxHealth = 100;
    private int _currentHealth;
    public bool ragdollOnDeath = true;
    [Tooltip("Events that will change this NPC's health")]
    public List<HealthChangeEventEntry> healthChangeEvents;
    public List<EventPackage> eventsToSendOnDeath;

    private bool _isAlive = false;
    public bool isAlive
    {
        get
        {
            return (_isAlive);
        }
    }
    public List<signalEventEntry> damageEvents;
    public List<damageMultiplier> damageTypeMultipliers;
    public bool isInvincible = false;
    [Space(5)]

    [Header("Movement")]
    public float patrolSpeed = 5.0f;
    public float investigateSpeed = 7.0f;
    public float attackingSpeed = 10.0f;
    public float fleeingSpeed = 10.0f;
    private bool isFlying = false;

    [Space(5)]
    [Header("Behavior")]
    //public
    public BehaviorType startingBehavior;
    public BehaviorType currentBehavior { get { return _currentBehavior; } }
    [Tooltip("Events that will change this NPC's behavior")]
    public List<behaviorChangeEventEntry> behaviorChangeEvents;
    public AttitudeType attitudeTowardsPlayer = AttitudeType.neutral;
    [Tooltip("Events that will change how this NPC reacts to the player")]
    public List<attitudeChangeEventEntry> attitudeChangeEvents;
    private BehaviorType _currentBehavior, _previousBehavior, _behaviorLastFrame;
    //    private PatrollingNavAgentBehavior patrolBehavior;
    public GameObject followFleeTarget;
    public float followDistance = 5.0f;
    public List<followTargetChangeEventEntry> followDifferentTargetEvents;
    [Tooltip("Nav container object that contains this character's patrol information")]
    public List<patrolChangeEventEntry> patrolRouteChangeEvents;
    [FormerlySerializedAs("navContainerObject") ]
    public NavPointContainer PatrolContainer;
    private List<navPoint> navPoints;
    private NavMeshAgent agent;
    private FlyingLocomotionBehavior flyBehavior;
    private Vector3 fleeDestination;
    private int currentPointIndex;
    private int patrolEventsToFireIndex;
    private int pointCount;
    private navBehavior patrolType;
    private int pingPongDir = 1;
    private float patrolWaitTimer = 0f;
    private float patrolWaitDuration = 0f;
    public List<healthThresholdData> healthLevelChanges;
    public Faction faction;

    [Space(5)]
    [Header("Attacking")]
    public NPCAttackType attackType = NPCAttackType.touch;
    public GameObject bulletPrefab;
    [Tooltip("The time between acquiring a target and attacking. Min-max range.")]
    public int attackWindupMax;
    [Tooltip("The time between acquiring a target and attacking. Min-max range.")]
    public int attackWindupMin;
    [Tooltip("The time between attacks. Min-max range.")]
    public int attackcoolDownMin;
    [Tooltip("The time between attacks. Min-max range.")]
    public int attackcoolDownMax;
    [Tooltip("Only used if attack mode is ranged. Set to 0 for unlimited range.")]
    public float rangedAttackMaxDistance = 10f;
    [Tooltip("Only used if attack mode is ranged.")]
    public float rangedAttackMinDistance = 0f;
    private float currentAttackCooldown = 0f;
    public AudioClip rangedAttackSound;
    public AudioClip meleedAttackSound;
    private bool isReacquiring = false;

    [Space(5)]
    [Header("Suspicion")]
    public float awarenessMeterUnawareToInvestigate = 5.0f;
    public float awarenessMeterInvestigateToBusted = 7.5f;
    public float idleSenseRate = 5.0f;
    public float suspiciousSenseRate = 10.0f;
    public float veryNearSenseMultiplier = 5f;
    public float nearSenseMultiplier = 2f;
    public float farSenseMultiplier = 0.75f;
    public float veryFarSenseMultiplier = 0.5f;
    public float senseBleedOffRate = 1.0f;
    private float currentSuspicion = 0;
    private float previousSuspicion;
    public float maximumSuspicion = 15f;
    [Tooltip("This means that the AI will always know where the player is and won't lose track.")]
    public bool hasPsychicAwarenessOfPlayer = false;
    public AudioClip soundToPlayOnBusted;
    private AudioSource enemyAudioSource;
    public GameObject suspicionIndicator;
    public GameObject alertedIndicator;
    public Image alertnessMeter;
    public Text alertnessRateText;
    public Text currentStateText;
    public Canvas suspicionDebugCanvas;

    [Space(5)]
    [Header("Senses")]
    public float patrolFOV = 100.0f;
    public float investigateFOV = 110.0f;
    public float pursuitFOV = 130.0f;
    //TODO: add peripheral cone
    private bool _playerInSight;
    public bool playerInSight { get { return _playerInSight; } }
    private bool playerInSenseRange = false;
    public Vector3 personalLastSighting;        
    private Vector3 previousSighting;
    private GameObject player;
    private SphereCollider visionSphere;
    public List<visionRangeChangeEventEntry> visionRadiusChangeEvents;

    [Space(5)]
    [Header("Fleeing")]
    public float fleeRadius = 5.0f;
    public List<GameObject> fleePoints;
    private GameObject nextFleeTarget, currentFleeTarget;

    [Space(5)]
    [Header("Distractions")]
    public float distractionCooldownMin = 5f;
    public float distractionCooldownMax = 10f;
    private NPCDistractor currentDistraction;
    private float currentDistractionCooldown = 0f;
    private float distractionWaitTimer = 0f;

    private NPCMouth mouth;
    //framerate savers
    int timer = 0;
    int interval = 4;

    public void updatePlayerRef()
    {
        //TODO: Phase this out. The player is not unique or important
        if(player==null)
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Awake()
    {
        //Initialize everything
        //GameManager.registerAI(this);
        NPCManager.registerAI(this);
        updatePlayerRef();
        _currentHealth = maxHealth;
        _currentBehavior = startingBehavior;

        if (_currentHealth > 0)
            _isAlive = true;
        
        player = GameObject.FindGameObjectWithTag("Player");
        enemyAudioSource = GetComponent<AudioSource>();
        foreach (SphereCollider sc in GetComponentsInChildren<SphereCollider>())
        {
            if (sc.isTrigger == true)
                visionSphere = sc;
        }

        //Hide the indicators for the NPCs suspicion
        HelperFunctions.hideObjectAndChildren(suspicionIndicator);
        HelperFunctions.hideObjectAndChildren(alertedIndicator);

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            flyBehavior = GetComponent<FlyingLocomotionBehavior>();
            if (flyBehavior != null)
                isFlying = true;
        }
        


        //TODO: Just use SetBehavior for this and let it do the heavy lifting
        switch (_currentBehavior)
        {
            case BehaviorType.attacking:
                setAgentSpeed(attackingSpeed);                
                break;
            case BehaviorType.patrolling:
                setPatrol(PatrolContainer);
                setAgentSpeed(patrolSpeed);                
                GoToNextPoint();
                break;
            case BehaviorType.following:
                setAgentSpeed(patrolSpeed);
                setAgentDestination(followFleeTarget.transform.position);                
                InvokeRepeating("followTarget", 0f, 0.25f);
                break;
            case BehaviorType.fleeing:
                setAgentSpeed(fleeingSpeed);                
                break;
        }
        _previousBehavior = BehaviorType.none;
        _behaviorLastFrame = BehaviorType.none;
        _playerInSight = false; // hasPsychicAwarenessOfPlayer;
        if (hasPsychicAwarenessOfPlayer)
            currentSuspicion = awarenessMeterInvestigateToBusted;

        mouth = GetComponentInChildren<NPCMouth>();

        if(attackType == NPCAttackType.touch)
        {
            if(!GetComponent<KillSurfaceBehavior>())
            {
                gameObject.AddComponent<KillSurfaceBehavior>();
            }
        }
    }
    
    public void SetDebugVisuals(bool newState)
    {
        if(suspicionDebugCanvas!=null)
            suspicionDebugCanvas.enabled = newState;
        else
        {
            int x = 10;
        }
            /*alertnessMeter.enabled = newState;
            alertnessRateText.enabled = newState;
            currentStateText.enabled = newState;*/
    }
    public void SetMouthState(NPCMouthState newState)
    {
        if (mouth != null)
            mouth.SetState(newState);
    }
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
          /*  foreach (SphereCollider sc in GetComponents<SphereCollider>())
            {
                if (sc.isTrigger == true)
                    visionSphere = sc;
            }
            Gizmos.matrix = Matrix4x4.Rotate(Quaternion.Euler(transform.forward));
            Gizmos.DrawFrustum(transform.position, 45, visionSphere.radius, 0, 1.5f);
            
         */
         
        }
#endif
    }


    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (PatrolContainer != null)
        {
            Handles.color = Color.blue;
            Handles.DrawDottedLine(transform.position, PatrolContainer.gameObject.transform.position, 2.0f);
            PatrolContainer.drawPatrolGizmos();
        }
#endif
    }

    void Start()
    {
        foreach (attitudeChangeEventEntry acee in attitudeChangeEvents)
        {
            if (acee.eventName != "")
                EventRegistry.AddEvent(acee.eventName, changeAttitudeOnEvent, gameObject);
        }
        foreach (behaviorChangeEventEntry bcee in behaviorChangeEvents)
        {
            if (bcee.eventName != "")
                EventRegistry.AddEvent(bcee.eventName, changeBehaviorOnEvent, gameObject);
        }
        foreach (HealthChangeEventEntry hcee in healthChangeEvents)
        {
            if (hcee.eventName != "")
                EventRegistry.AddEvent(hcee.eventName, HealthChangeOnEvent, gameObject);
        }
        foreach (followTargetChangeEventEntry bcee in followDifferentTargetEvents)
        {
            if (bcee.eventName != "")
                EventRegistry.AddEvent(bcee.eventName, changeFollowTargetEvent, gameObject);
        }
        foreach (visionRangeChangeEventEntry vcee in visionRadiusChangeEvents)
        {
            if (vcee.eventName != "")
                EventRegistry.AddEvent(vcee.eventName, changeVisionRangeEvent, gameObject);
        }
        foreach (patrolChangeEventEntry pcee in patrolRouteChangeEvents)
        {
            if (pcee.eventName != "")
                EventRegistry.AddEvent(pcee.eventName, patrolChangeEvent, gameObject);
        }

    }

    public void changeBehaviorOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (behaviorChangeEventEntry bcee in behaviorChangeEvents)
        {
            if (bcee.eventName == eventName)
            {
                setBehavior(bcee.behaviorToChangeTo);
            }
        }
    }

    
    public void HealthChangeOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (HealthChangeEventEntry hcee in healthChangeEvents)
        {
            if (hcee.eventName == eventName)
            {
                switch (hcee.operation)
                {
                    case operationType.add:
                        _currentHealth += hcee.amount;
                        
                        break;
                    case operationType.multiply:
                        _currentHealth *= hcee.amount;
                        break;
                    case operationType.set:
                        _currentHealth = hcee.amount;
                        Damage(0, signalTypes.scriptedDamage);
                        break;
                    case operationType.subtract:
                        Damage(hcee.amount, signalTypes.scriptedDamage);
                        break;
                    case operationType.divide:
                        _currentHealth /= hcee.amount;
                        break;
                }
            }
        }
    }
    public void changeBehavior()
    {

    }

    public void changeAttitudeOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (attitudeChangeEventEntry ace in attitudeChangeEvents)
        {
            if (ace.eventName == eventName)
            {
                setAttitude(ace.attitudeToChangeTo);
                //attitudeTowardsPlayer = ace.attitudeToChangeTo;
                //setAttitude(ace.attitudeToChangeTo);
            }
        }
    }

    public void changeFollowTargetEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (followTargetChangeEventEntry ftce in followDifferentTargetEvents)
        {
            if (ftce.eventName == eventName)
            {
                followFleeTarget = ftce.newFollowTarget;
            }
        }
    }

    public void changeVisionRangeEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (visionRangeChangeEventEntry vce in visionRadiusChangeEvents)
        {
            if ((vce.eventName == eventName) && (visionSphere != null))
            {
                visionSphere.radius = vce.newVisionRange;
            }
        }
    }

    public void patrolChangeEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (patrolChangeEventEntry pce in patrolRouteChangeEvents)
        {
            if (pce.eventName == eventName)
            {
                setPatrol(pce.navContainerObject, pce.patrolSwitchBehavior);
                if (currentBehavior == BehaviorType.patrolling)
                    GoToNextPoint();
            }
        }
    }

    private void setPatrol(NavPointContainer navContainer, PatrolSwitchBehaviorType psb = PatrolSwitchBehaviorType.pickFirst)
    { //Patrol stuff
        int psbIndex = 0;
        int oldNavIndex = currentPointIndex;
        NavPointContainer npc = null;
        if (navContainer != null)
            npc = navContainer.GetComponent<NavPointContainer>();
        else
        {
            _currentBehavior = BehaviorType.idle;
            return;
        }

        if (npc != null)
        {
            navPoints = npc.navPoints;
            if(navPoints.Count == 0)
            {
                _currentBehavior = BehaviorType.idle;
                return;
            }
            patrolType = npc.navigationBehaviorType;
        }
        else
        {
            _currentBehavior = BehaviorType.idle;
            return;
        }

        switch (psb)
        {
            case PatrolSwitchBehaviorType.pickFirst:
                psbIndex = 0;
                break;
            case PatrolSwitchBehaviorType.pickClosest:
                int counter = -1;
                int closestIndex = 0;
                float dist = 0f;
                float closestDistance = 1000000.0f;
                foreach (navPoint np in navPoints)
                {
                    counter++;
                    dist = Vector3.Distance(np.navPointObject.transform.position, gameObject.transform.position);
                    if (dist < closestDistance)
                    {
                        closestIndex = counter;
                        closestDistance = dist;
                    }
                }
                psbIndex = closestIndex;
                break;
            case PatrolSwitchBehaviorType.pickRandom:
                psbIndex = UnityEngine.Random.Range(0, navPoints.Count);
                break;
            case PatrolSwitchBehaviorType.pickSimilarIndex:
                psbIndex = oldNavIndex % navPoints.Count;
                break;
        }

        if (patrolType == navBehavior.wander)
            currentPointIndex = UnityEngine.Random.Range(0, navPoints.Count);
        else
            currentPointIndex = psbIndex;

    }

    public void setAttitude(AttitudeType newAttitude)
    {
        attitudeTowardsPlayer = newAttitude;
        if ((currentBehavior == BehaviorType.attacking) && ((attitudeTowardsPlayer == AttitudeType.friendly) || (attitudeTowardsPlayer == AttitudeType.neutral)))
            setBehavior(_previousBehavior);
        if (attitudeTowardsPlayer == AttitudeType.fearful)
            setBehavior(BehaviorType.fleeing);

        if ((attitudeTowardsPlayer == AttitudeType.friendly) || (attitudeTowardsPlayer == AttitudeType.neutral))
            wipeSuspicion();
    }

    public void setBehavior(BehaviorType newBehavior)
    {
        //TODO: add some state machine transition stuff here if needed
        _previousBehavior = currentBehavior;
        _currentBehavior = newBehavior;
        if (_previousBehavior == BehaviorType.following)
            CancelInvoke();
        if (currentBehavior == BehaviorType.attacking)
        {
            currentSuspicion = awarenessMeterInvestigateToBusted;
        }
        if (currentBehavior != BehaviorType.following)
        {
            if (_previousBehavior == BehaviorType.following)
                CancelInvoke();
        }
        if (currentBehavior == BehaviorType.following)
        {
            if (_previousBehavior != BehaviorType.following)
            {
                setAgentSpeed(patrolSpeed);
                setAgentDestination(followFleeTarget.transform.position);                
                InvokeRepeating("followTarget", 0f, 0.25f);
            }
        }
        if (currentBehavior != BehaviorType.fleeing)
        {
            if (_previousBehavior == BehaviorType.fleeing)
                CancelInvoke();
        }
        if (currentBehavior == BehaviorType.idle)
        {
            setAgentStopped(true);            
        }
        if ((currentBehavior == BehaviorType.patrolling) && (_previousBehavior != BehaviorType.patrolling))
        {
            setPatrol(PatrolContainer);
            setAgentSpeed(patrolSpeed);
            //setPatrol(navContainerObject); //TODO: Figure out why past Rich commented this out, as it breaks stuff. NPCs that start idle or spawn in won't adapt a patrol in this case.
            GoToNextPoint();
        }

    }

    public void Damage(int damageAmount, signalTypes damageType)
    {
        if (isInvincible)
            return;
        //TODO: add health change threshold events here
        //TODO: add suspicion/busted on damage source here
        GameObject dropObject;
        bool damagePassesEventCheck = false;
        if (_currentHealth <= 0)
            return;
        foreach (damageMultiplier dm in damageTypeMultipliers)
        {
            if (damageType == dm.damageType)
                damageAmount = (int)(damageAmount * dm.multiplier);
        }

        _currentHealth -= damageAmount;
        //TODO: consider moving to signal receiver behavior
        foreach (signalEventEntry dee in damageEvents)
        {
            damagePassesEventCheck = false;
            if (damageAmount >= dee.damageThreshold)
            {
                if (dee.filterByDamageType == false || ((dee.filterByDamageType) && (dee.damageType == damageType)))
                {
                    switch (dee.comparisonForThreshold)
                    {
                        case comparisonOperator.Equal:
                            damagePassesEventCheck = (dee.damageThreshold == damageAmount);
                            break;
                        case comparisonOperator.greaterThan:
                            damagePassesEventCheck = (damageAmount > dee.damageThreshold);
                            break;
                        case comparisonOperator.greaterThanEqual:
                            damagePassesEventCheck = (damageAmount >= dee.damageThreshold);
                            break;
                        case comparisonOperator.lessThan:
                            damagePassesEventCheck = (damageAmount < dee.damageThreshold);
                            break;
                        case comparisonOperator.lessThanEqual:
                            damagePassesEventCheck = (damageAmount <= dee.damageThreshold);
                            break;
                        case comparisonOperator.notEqual:
                            damagePassesEventCheck = (dee.damageThreshold != damageAmount);
                            break;
                    }
                    if (damagePassesEventCheck)
                    {
                        foreach (string s in dee.oldEventsToSend)
                            EventRegistry.SendEvent(s);
                        foreach (EventPackage ep in dee.eventsToSend)
                            EventRegistry.SendEvent(ep, this.gameObject);
                    }
                }
            }
        }
        if (_currentHealth <= 0)
        {
            if (isAlive)
            {
                _isAlive = false;
                HelperFunctions.hideObjectAndChildren(suspicionIndicator);
                HelperFunctions.hideObjectAndChildren(alertedIndicator);
                CancelInvoke();
                if (objectToDrop != null)
                {
                    dropObject = Instantiate(objectToDrop);
                    dropObject.transform.position = gameObject.transform.position;
                }
                if (ragdollOnDeath)
                {
                    setAgentEnabled(false);
                    Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        if (isFlying)
                            rb.useGravity = true;
                        Vector3 currentVel = rb.velocity;
                        rb.isKinematic = false;
                        rb.AddRelativeTorque(new Vector3(30, 0, 0));
                        rb.AddForceAtPosition(transform.forward * 20 + new Vector3(0, 1, 0), transform.position + new Vector3(0, 0.5f, 0));
                    }
                    PhysicsCarryObject pco = gameObject.GetComponent<PhysicsCarryObject>();
                    if (pco != null)
                    {
                        pco.enabled = true;
                    }
                    setBehavior(BehaviorType.none);

                    if(TryGetComponent<KillSurfaceBehavior>(out KillSurfaceBehavior ksb))
                    {
                        ksb.setEnabled(false);
                    }
                    
                }
                else
                    GameObject.Destroy(gameObject);
                /*foreach (string s in eventsToFireOnDeath)
                {
                    EventRegistry.SendEvent(s);
                }*/
                foreach (EventPackage ep in eventsToSendOnDeath)
                {
                    EventRegistry.SendEvent(ep, this.gameObject);
                }
            }
        }
    }

    public void wipeSuspicion()
    {
        currentSuspicion = 0;
    }

    void Update()
    {
        float suspicionDelta = 0;
        if (timer % interval != 0)
        {
            timer++;
            return;
        }
        else
            timer = 0;

        
        //paused or dead. Nothing to do here
        if ((GameManager.isPaused) || (_currentHealth <= 0))
            return;
        updatePlayerRef();
        if (playerInSenseRange)
            PlayerTriggerStuff();

        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
            if (currentAttackCooldown < 0)
                currentAttackCooldown = 0;
        }

        if (currentBehavior != BehaviorType.distracted)
        {
            currentDistractionCooldown -= Time.deltaTime;
            if (currentDistractionCooldown < 0)
                currentDistractionCooldown = 0;
        }

        if ((playerInSight) || (hasPsychicAwarenessOfPlayer))
        {
            //The player is in sight, but we aren't currently engaged with attacking them or running from them
            if ((currentBehavior != BehaviorType.attacking) && (currentBehavior != BehaviorType.fleeing))
            {
                float playerDistance = getDistanceToPlayer();
                float distanceMultiplier = 1f;
                if (playerDistance >= visionSphere.radius * 0.75f)
                {
                    distanceMultiplier = veryFarSenseMultiplier;
                }

                if ((playerDistance >= visionSphere.radius * 0.5f) && (playerDistance < visionSphere.radius * 0.75f))
                {
                    distanceMultiplier = farSenseMultiplier;
                }

                if ((playerDistance >= visionSphere.radius * 0.25f) && (playerDistance < visionSphere.radius * 0.5f))
                {
                    distanceMultiplier = nearSenseMultiplier;
                }

                if (playerDistance < visionSphere.radius * 0.25f)
                {
                    distanceMultiplier = veryNearSenseMultiplier;
                }


                previousSuspicion = currentSuspicion;
                suspicionDelta = Time.deltaTime * idleSenseRate * player.GetComponent<GAME1304PlayerController>().currentVisibility * distanceMultiplier;
                currentSuspicion += suspicionDelta;
                
                

                if ((currentSuspicion >= awarenessMeterUnawareToInvestigate)&& (previousSuspicion < awarenessMeterUnawareToInvestigate))
                {
                    
                        if ((attitudeTowardsPlayer == AttitudeType.aggressive) || (attitudeTowardsPlayer == AttitudeType.fearful))
                            HelperFunctions.showObjectAndChildren(suspicionIndicator);
                    
                }
                if (currentSuspicion >= maximumSuspicion)
                    currentSuspicion = maximumSuspicion;
                if ((currentSuspicion > awarenessMeterInvestigateToBusted)&& (previousSuspicion <= awarenessMeterInvestigateToBusted))
                {                    
                    
                        if ((attitudeTowardsPlayer == AttitudeType.aggressive) || (attitudeTowardsPlayer == AttitudeType.fearful))
                            HelperFunctions.showObjectAndChildren(alertedIndicator);
                        HelperFunctions.hideObjectAndChildren(suspicionIndicator);
                        if ((soundToPlayOnBusted != null) && (enemyAudioSource != null))
                        {
                            enemyAudioSource.clip = soundToPlayOnBusted;
                            enemyAudioSource.Play();
                        }
                        if (attitudeTowardsPlayer == AttitudeType.aggressive)
                            setBehavior(BehaviorType.attacking);
                        if (attitudeTowardsPlayer == AttitudeType.fearful)
                            setBehavior(BehaviorType.fleeing);
                    
                }
            }
        }
        else
        {
            //Player is out of sight, so cool down the suspicion meter
            previousSuspicion = currentSuspicion;
            suspicionDelta = -Time.deltaTime * senseBleedOffRate;
            currentSuspicion += suspicionDelta;
            //TODO: make this cleaner
            if(alertnessRateText!=null)
                alertnessRateText.text = (Time.deltaTime * senseBleedOffRate).ToString();

            if ((currentSuspicion < awarenessMeterInvestigateToBusted)&& (previousSuspicion >= awarenessMeterInvestigateToBusted))
            {                
                if((attitudeTowardsPlayer == AttitudeType.aggressive)|| (attitudeTowardsPlayer == AttitudeType.fearful))
                    HelperFunctions.showObjectAndChildren(suspicionIndicator);
                HelperFunctions.hideObjectAndChildren(alertedIndicator);
            }
            if ((currentSuspicion < awarenessMeterUnawareToInvestigate) && (previousSuspicion >= awarenessMeterUnawareToInvestigate))
            {
                HelperFunctions.hideObjectAndChildren(suspicionIndicator);
            }
            if (currentSuspicion < 0)
                currentSuspicion = 0;
            if ((currentBehavior == BehaviorType.attacking) || (currentBehavior == BehaviorType.fleeing))

            {
                //removed second check here because if you're attacking, then your previous suspicion would have to be high
                //also fixes psychic awareness on death bug
                if ((currentSuspicion <= awarenessMeterUnawareToInvestigate)) //&& (previousSuspicion > awarenessMeterUnawareToInvestigate)) 
                {
                    HelperFunctions.hideObjectAndChildren(suspicionIndicator);
                    HelperFunctions.hideObjectAndChildren(alertedIndicator);
                    currentSuspicion = 0; //TODO: rethink this
                    setBehavior(_previousBehavior);
                    CancelInvoke();
                }
            }
        }

        switch (currentBehavior)
        {
            case BehaviorType.idle: //do nothing, you're idle
                break;
            case BehaviorType.attacking:
                setAgentSpeed(attackingSpeed);
                //TODO: incorporate the actual attack types
                if (_behaviorLastFrame != BehaviorType.attacking)
                {
                    InvokeRepeating("chasePlayer", 0f, 0.25f);
                }
                if (isReacquiring && getAgentRemainingDistance() <= 1.0f)
                {
                    isReacquiring = false;
                    facePlayer();
                }
                break;
            case BehaviorType.fleeing:
                setAgentSpeed(fleeingSpeed);
                if (_behaviorLastFrame != BehaviorType.fleeing)
                {
                    setFleePoint();
                    InvokeRepeating("fleePlayer", 0f, 0.25f);
                }
                break;
            case BehaviorType.following:
                break;
            case BehaviorType.patrolling:
                /*if (_behaviorLastFrame != BehaviorType.patrolling)
                {
                    setAgentSpeed(patrolSpeed);                    
                    HelperFunctions.hideObjectAndChildren(suspicionIndicator);
                    HelperFunctions.hideObjectAndChildren(alertedIndicator);
                    GoToNextPoint();
                }*/
                if (!getAgentPathPending() && getAgentRemainingDistance() < 0.5f)
                {
                    patrolWaitTimer += Time.deltaTime;
                    if (patrolWaitTimer >= patrolWaitDuration)
                    {
                        //TODO: make this more efficient. take the assumptions out of the chain somewhere else
                        //TODO: this all fires when the character is leaving the node, add a list for arrival at node
                        if (navPoints != null)
                        {
                            if (navPoints.Count > 0)
                            {
                                if (navPoints[patrolEventsToFireIndex] != null)
                                {                                                                     
                                    foreach (EventPackage ep in navPoints[patrolEventsToFireIndex].eventsToSend)
                                        EventRegistry.SendEvent(ep, gameObject);                                 
                                }
                            }
                        }
                        GoToNextPoint();
                    }
                }
                break;
            case BehaviorType.distracted:
                {
                    //NPC has arrived at distraction
                    if (!getAgentPathPending() && getAgentRemainingDistance() <= currentDistraction.minDistance) 
                    {
                        if(currentDistraction.NPCLookTarget != null)
                        {
                            Vector3 tempVec = currentDistraction.NPCLookTarget.transform.position - transform.position;
                            tempVec.y = 0;
                            transform.rotation  = Quaternion.LookRotation(tempVec, Vector3.up);                            
                        }
                        //TODO: set NPC senses dulled
                        distractionWaitTimer += Time.deltaTime;
                        if (distractionWaitTimer >=  currentDistraction.currentDuration)
                        {
                            setBehavior(_previousBehavior);
                            currentDistraction.finishDistraction();
                            currentDistractionCooldown = UnityEngine.Random.Range(distractionCooldownMin, distractionCooldownMax);
                        }
                    }
                }
                break;
        }
        /**/

        _behaviorLastFrame = currentBehavior;

        //add condition for debug viz here
        if(alertnessRateText!=null)
            alertnessRateText.text = suspicionDelta.ToString();
        if (alertnessMeter != null)
        {
            alertnessMeter.fillAmount = Mathf.InverseLerp(0, maximumSuspicion, currentSuspicion);
            if (currentSuspicion < awarenessMeterUnawareToInvestigate)
                alertnessMeter.color = Color.green;
            else
            {
                if (currentSuspicion < awarenessMeterInvestigateToBusted)
                    alertnessMeter.color = Color.yellow;
                else
                    alertnessMeter.color = Color.red;
            }
        }
        if(currentStateText!=null)
            currentStateText.text = _currentBehavior.ToString();
    }

    void followTarget()
    {
        if (currentBehavior == BehaviorType.following)
        {
            setAgentDestination(followFleeTarget.transform.position);            
            //if(!agent.pathPending && agent.remainingDistance >= followDistance)
            if (getAgentRemainingDistance() >= followDistance)
            {
                setAgentSpeed(patrolSpeed);                
            }
            else
            {
                setAgentSpeed(0);
            }
        }
    }

    float getDistanceToPlayer()
    {
        if (player != null)
            return ((transform.position - player.transform.position).magnitude);
        else
            return (float.PositiveInfinity);
    }

    public bool canBeDistracted()
    {
        return ((currentDistractionCooldown == 0) && ((currentBehavior == BehaviorType.idle) || (currentBehavior == BehaviorType.patrolling)));

    }

    public void startDistraction(NPCDistractor npcd)
    {
        setAgentSpeed(patrolSpeed);
        currentDistraction = npcd;
        setBehavior(BehaviorType.distracted);
        distractionWaitTimer = 0f;
        setAgentDestination(currentDistraction.NPCPlacementTarget.position);
    }

    public void endDistraction()
    {

        currentDistractionCooldown = UnityEngine.Random.Range(distractionCooldownMin, distractionCooldownMax);
        setBehavior(_previousBehavior);
    }

    void facePlayer()
    {
        Vector3 direction = (player.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
    }
    void chasePlayer()
    {
        switch (attackType)
        {
            case NPCAttackType.touch:
                setAgentDestination(player.transform.position);                
                break;
            case NPCAttackType.rangedProjectile:                
                if ((getDistanceToPlayer() <= rangedAttackMaxDistance) && playerInSight)
                {
                    if (getDistanceToPlayer() < rangedAttackMinDistance)
                        reacquire();
                    else
                    {
                        //agent.isStopped = true;
                        facePlayer();
                        isReacquiring = false;
                        if (currentAttackCooldown <= 0)
                        {
                            fireProjectileAtPlayer();
                        }//TODO: what does it do if it's in range but outside of the cooldown?
                    }

                }
                else
                {
                    //if(agent.remainingDistance > 0.5f)
                    reacquire();
                }
                break;
            case NPCAttackType.melee:
                break;
        }


    }

    bool isValidFiringPosition(GameObject target, Vector3 firingPosition)
    {
        RaycastHit hitInfo;
        Physics.Raycast(firingPosition, target.transform.position - firingPosition, out hitInfo);
        if (hitInfo.collider.gameObject == target)
            return true;
        else
            return false;
    }

    float sumCorners(NavMeshPath path)
    {
        if (path.corners.Length == 0)
            return 0;
        Vector3 lastPoint = path.corners[0];
        float totalDist = 0;

        foreach(Vector3 c in path.corners)
        {
            totalDist += Vector3.Distance(lastPoint, c);
            lastPoint = c;
        }
        return totalDist;
    }
    void reacquire()
    {
        if (isReacquiring)
            return;
        Vector3 destinationPosition;
        Vector3 destinationOffset;
        Vector2 horizontalOffset;
        Vector3 normalizedOffset;
        Vector3 newOffset;
        float offsetDegrees;
        Vector3 testPoint;
        Vector3 bestPoint = Vector3.zero;
        float bestDist = float.PositiveInfinity;
        float testDist;
        NavMeshPath tempPath = new NavMeshPath();
        float[] angleOffsets = { 0,45,-45,90,-90};

        normalizedOffset = (transform.position - player.transform.position).normalized;
        offsetDegrees = Vector2.Angle(new Vector2(transform.position.x,transform.position.z),new Vector2(player.transform.position.x, player.transform.position.z));

        foreach (float f in angleOffsets)
        {

            newOffset = new Vector3((float)Math.Cos(Mathf.Deg2Rad * (offsetDegrees + f)), (float)Math.Sin(Mathf.Deg2Rad * (offsetDegrees + f)));
            testPoint = player.transform.position + (newOffset * rangedAttackMaxDistance);
            if (isValidFiringPosition(player, testPoint))
            {
                agent.CalculatePath(testPoint, tempPath);
                testDist = sumCorners(tempPath);
                if (testDist < bestDist)
                {
                    bestDist = testDist;
                    bestPoint = testPoint;
                }
            }
        }        
        /*
        horizontalOffset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(rangedAttackMinDistance,rangedAttackMaxDistance);
        destinationOffset = new Vector3(horizontalOffset.x, 0, horizontalOffset.y);

        destinationPosition = player.transform.position + destinationOffset;*/

        NavMeshHit hit;
        NavMesh.SamplePosition(bestPoint, out hit, 1.0f, 1);
        destinationPosition = hit.position;

        setAgentDestination(destinationPosition);
        isReacquiring = true;
    }

    void fireProjectileAtPlayer()
    {
        //TODO: turn to face player
        //TODO: wait for windup
        GameObject bulletObj = GameObject.Instantiate(bulletPrefab, transform.position + transform.forward * 1.5f, Quaternion.identity);
        bulletObj.transform.parent = null;
        if (bulletObj.GetComponent<BulletBehavior>() != null)
        {
            bulletObj.GetComponent<BulletBehavior>().init(transform.forward);
            currentAttackCooldown = attackcoolDownMax;
            if ((rangedAttackSound != null) && (enemyAudioSource != null))
            {
                enemyAudioSource.clip = rangedAttackSound;
                enemyAudioSource.Play();
            }
        }
        //TODO: make this a random value between the min and max
    }

    void setFleePoint()
    {
        //TODO: make this not terrible
        Vector3 fleePos;
        Vector2 fleeOffset;
        if (fleePoints.Count == 0)
        {
            float dist = (transform.position - player.transform.position).magnitude;
            fleeOffset = (UnityEngine.Random.insideUnitCircle).normalized * dist * 2;
            fleePos = transform.position + new Vector3(fleeOffset.x, 0, fleeOffset.y);

            NavMeshHit hit;
            NavMesh.SamplePosition(fleePos, out hit, 10.0f, 1);
            setAgentDestination(hit.position);            
        }
        else
        {
            setAgentDestination(getFurthestFleePointFromPlayer());            
        }
    }

    Vector3 getFurthestFleePointFromPlayer()
    {
        Vector3 playerLoc = player.transform.position;
        Vector3 farthestPoint = transform.position;
        float dist = 0;
        foreach (GameObject go in fleePoints)
        {
            if ((go.transform.position - playerLoc).magnitude > dist)
            {
                farthestPoint = go.transform.position;
                dist = (go.transform.position - playerLoc).magnitude;
            }
        }
        return farthestPoint;
    }

    void fleePlayer()
    {
        if (getAgentRemainingDistance() <= 1.0f)
            setFleePoint();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            playerInSenseRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            playerInSenseRange = false;
        }
    }

    RaycastHit filterCastResults(RaycastHit[] hitInfos)
    {
        RaycastHit closeHit = new RaycastHit();
        float newDist;
        float closeDist = float.PositiveInfinity;
        foreach (RaycastHit rch in hitInfos)
        {
            if (!rch.collider.isTrigger)
            {
                newDist = rch.distance;
                if (newDist < closeDist)
                {
                    closeHit = rch;
                    closeDist = newDist;
                }
            }
        }
        return closeHit;
    }

   

        void PlayerTriggerStuff()
    {
        if (GameManager.isPaused || _currentHealth <= 0)
            return;
        float testFOV;
        
        if (currentBehavior == BehaviorType.attacking)
            testFOV = pursuitFOV;
        else
            testFOV = patrolFOV;
        //default "player in sight" before re-checking to see if the player is actually in sight
        _playerInSight = false;
        Vector3 direction = player.transform.position - transform.position;
        float angle = Vector3.Angle(direction, transform.forward);

        if (angle < (testFOV * 0.5f))
        {
            RaycastHit hit;
            //TODO: offset the raycast by the character's half height instead of just 1
            LayerMask mask = LayerMask.GetMask("Player");
            if (Physics.Raycast(transform.position + transform.up, direction.normalized, out hit, visionSphere.radius)) //,mask.value))
            {
                if (hit.collider.gameObject == player)
                {
                    _playerInSight = true;

                }
            }
            if (Physics.Raycast(transform.position, direction.normalized, out hit, visionSphere.radius)) //,mask.value))
            {
                if (hit.collider.gameObject == player)
                {
                    _playerInSight = true;

                }
            }
        }
        
    }

    void GoToNextPoint()
    {
        //if we've been given bad data, exit
        //should probably pop an assert here as well
        setAgentStopped(false);        
        patrolWaitTimer = 0;
        if ((navPoints == null)||(navPoints.Count==0))
        {
            setBehavior(BehaviorType.idle);
            return;
        }
        if (navPoints.Count <= 0)
        {
            setBehavior(BehaviorType.idle);
            return;
        }
        setAgentDestination(navPoints[currentPointIndex].navPointObject.transform.position);
        patrolWaitDuration = UnityEngine.Random.Range(navPoints[currentPointIndex].pauseDurationMin, navPoints[currentPointIndex].pauseDurationMax);
        patrolEventsToFireIndex = currentPointIndex;
        switch (patrolType)
        {
            case navBehavior.patrolLoop:
                currentPointIndex += 1;
                if (currentPointIndex >= navPoints.Count)
                    currentPointIndex = 0;
                break;
            case navBehavior.oneWay:
                currentPointIndex += 1;
                if (currentPointIndex >= navPoints.Count)
                    setBehavior(BehaviorType.idle);                
                break;
            case navBehavior.patrolPingPong:
                currentPointIndex += pingPongDir;
                if (currentPointIndex >= navPoints.Count)
                {
                    pingPongDir = -1;
                    currentPointIndex = navPoints.Count - 2;
                }
                else if (currentPointIndex < 0)
                {
                    pingPongDir = 1;
                    currentPointIndex = 1;
                }
                break;
            case navBehavior.wander:
                currentPointIndex = UnityEngine.Random.Range(0, navPoints.Count);
                break;
        }
    }

    public void setAgentStopped(bool isStopped)
    {
        if (!isFlying)
        {
            if(agent.isOnNavMesh)
                agent.isStopped = isStopped;
        }
        else
        {
            if (flyBehavior != null)
                flyBehavior.setIsStopped(isStopped);
        }
    }

    public void setAgentSpeed(float speed)
    {
        if (!isFlying)
        {
            if (agent.isOnNavMesh)
                agent.speed = speed;
        }
        else
        {
            if (flyBehavior != null)
                flyBehavior.setSpeed(speed);
        }
    }

    public void setAgentDestination(Vector3 destination)
    {
        if(!isFlying)
        {
            if (agent == null)
                Debug.Log(gameObject.name + " has no agent!");
            else
            {
                if (agent.isOnNavMesh)
                {
                    if (!agent.SetDestination(destination))
                        Debug.Log(gameObject.name + " has problem navigating!");
                }
            }
        }
        else
        {
            if (flyBehavior != null)
                flyBehavior.setDestination(destination);
        }
    }

    public float getAgentRemainingDistance()
    {
        if (!isFlying)
        {
            if (agent != null)
            {
                if (agent.isOnNavMesh)
                    return agent.remainingDistance;
            }
            else
                return -1;
        }
        else
        {
            if (flyBehavior != null)
                return flyBehavior.getRemainingDistance();            
        }
        return -1;
    }

    public bool getAgentPathPending()
    {
        if (!isFlying)
        {
            if (agent != null)
            {
                return agent.pathPending;
            }
            else
                return false;
        }
        else
        {
            if (flyBehavior != null)
                return flyBehavior.getPathPending();
        }
        
        return true;
    }

    public void setAgentEnabled(bool enabled)
    {
        if (!isFlying)
        {
            if (agent != null)
            {
                agent.enabled = enabled;
            }
        }
        else
        {
            if (flyBehavior != null)
                flyBehavior.enabled = enabled;
        }        
    }
}