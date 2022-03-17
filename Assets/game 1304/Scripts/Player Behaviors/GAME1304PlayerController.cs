﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum operationType { set, add, subtract, multiply, divide };
public enum locomotionType { walking, jumping, falling, climbing,mantling, swimming};
public enum playerStatType { health, walkSpeed, runSpeed, jumpCount, jumpForce, visibility }; //, incomingDamageMultiplier, throwForce};
public enum DebugMode { none, flying, flyingNoclip};

[Serializable]
public class playerStatChange
{
    public string eventToListenFor;
    public int amount;
    public operationType operation;
    public playerStatType playerStatToChange;
}

[Serializable]
public class damageMultiplierUpdate
{
    public string eventToListenFor;
    public damageMultiplier multiplier;
}

[Serializable]
public class tokenDisplayData
{
    public string tokenToUse;
    public string label;
    public bool hideIfValueIsZero = false;
    //Vector2 screenOffset;
}
public class GAME1304PlayerController : MonoBehaviour
{
    private float playerHeight = 2f;
    private float _playerRadius = 0.5f;
    public float playerRadius
    {

        get
        {
            return _playerRadius;
        }
    }

    [Header("Camera")]
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;
    public Camera PlayerCamera { get { return _playerCamera; } }
    public float leanAngle = 15.0f;
    public float leanRate = 50.0f;
    private float _currentLean = 0.0f;
    public float runFOVPercent = 1.25f;
    private float FOVRateChange = 50f;
    private float oldCamFOV;
    //TODO: add camera pitch bounds

    [Space(5)]

    [Header("Movement")]
    public float walkSpeed = 8.0f;
    public float runSpeed = 12.0f;
    public float acceleration = 1.0f;
    public float deceleration = 5.0f;
    public float deadZone = 0.125f;
    public bool canCrouch = true;
    public float crouchSpeedModifier = 0.75f;
    private bool _isCrouching = false;
    public float maxSlopeAngle = 50f;
    public float crouchHeightPercent = 0.75f;
    public float crawlHeightPercent = 0.25f;
    private float mouseX, mouseY;

    [Space(5)]

    [Header("Jumping")]
    public float jumpForce = 10.0f;
    public float airControlRatio = 1.0f;
    public int jumpCount = 1;
    public float killHeight = 10.0f;
    public float forceKillFallDistance = 30.0f;
    private int jumpsRemaining;
    private bool isFalling = false;
    private float fallApex = 0;
    private float lastYValue;
    private bool isJumping = false;
    private bool isMantling = false;
    private Vector3 mantleDestination;
    private float mantleTimer;
    private float mantleTimerThreshold = 0.125f;
    public float mantleHeight = 3f;
    LineRenderer mantleDebugLineRenderer;
    //private float mantleTimer;
    public float mantleSpeed = 30f;
    private bool drawMantleDebug = false;
    [Space(5)]

    [Header("Climbing")]
    public float climbSpeed = 1.0f;
    public float fastClimbSpeed = 2.0f;
    private locomotionType currentLocomotionMode = locomotionType.walking;
    private bool isMounted = false;
    private LadderBehavior currentLadder;
    [Space(5)]

    [Header("Mortality")]
    public int health = 100;
    public bool capHealthAtMax = true;
    public GameObject spawnPoint;
    public bool useSpawnPoint = true;
    public Text healthUITextEntity;
    public bool showHealthOnHUD = true;
    public bool pauseOnRespawn = true;
    public float respawnPauseDuration = 0.5f;
    private bool isRespawnPaused = false;
    private float respawnPauseCounter = 0f;
    private bool playFromHere = false;
    private Transform pfhTransform;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    public List<damageMultiplier> damageTypeMultipliers;

    [Space(5)]

    [Header("AI Interaction")]
    public float VisibilityModifierWhenCrouched = 0.5f;

    [Space(5)]

    [Header("HUD Stuff")]
    public List<tokenDisplayData> tokensToDisplay;
    public Text tokenDisplayText;

    public Text subtitleText;
    public Text timerText;
    public Text ammoText;



    [Space(5)]

    [Header("Events")]
    public List<playerStatChange> playerStatChangeEvents;
    public List<damageMultiplierUpdate> damageMultiplierChangeEvents;
    public List<string> eventsToCallOnFirstSpawn;
    public List<string> eventsToCallOnSpawn;
    public List<string> eventsToCallOnDeath;
    public string eventListenerToFreezePlayerInput;
    public string eventListenerToRestorePlayerInput;
    private bool isInputLocked = false;

    [Space(5)]

    [Header("Equipment")]
    private bool useGun = true;
    public bool useTorch = false;
    public GameObject gunPrefab;
    public GameObject torchPrefab;
    public GameObject gunHand;
    public GameObject torchHand;
    //public String rightHandInput;
    //public String leftHandInput;
    private List<GameObject> weapons;
    private Dictionary<string, int> ammunition;
    private GameObject currentWeapon;
    private int currentWeaponIndex = -1;
    //private GameObject gunObj;
    private List<GameObject> implements;
    private GameObject torchObj;
    private WeaponBehavior currentWeaponBehavior;
    private GunBehavior gunBehavior;
    //private TorchBehavior torchBehavior;
    private bool isTorchVisible = true;
    [Header("Event Sending")]
    public List<string> eventsOnTorchRaise;
    public List<string> eventsOnTorchLower;
    [Header("Event Listening")]
    public string eventToEnableGun;
    public string eventToDisableGun;
    public string eventToEnableTorch;
    public string eventToDisableTorch;

    //private camera stuff
    private Camera _playerCamera;
    private GameObject cameraPivot;
    private float rotY = 0.0f; // rotation around the up/y axis
    private float latentRotY;
    private float rotX = 0.0f; // rotation around the right/x axis
    private float lastMouseX, lastMouseY;
    private DebugMode debugmode = DebugMode.none;
    //private walking stuff
    private Vector3 playerFacing;
    private Vector2 movementInputVector;
    private Vector3 movementHeading;
    private Vector3 currentNormal;
    private float groundAngle;

    //private physics stuff
    private Rigidbody playerRB;
    private Collider playerCollider;
    private bool isGrounded = true;
    private bool isRunning = false;

    private ObjectOfInterest tempoi;

    private float biggestX = 0;

    [HideInInspector]
    public int currentHealth
    {
        set
        {
            _currentHealth = value;
            if (capHealthAtMax && _currentHealth > health)
                _currentHealth = health;
            updateHealthUI();
        }
        get
        {
            return _currentHealth;
        }
    }
    private int _currentHealth;
    private CapsuleCollider col;
    private Transform platformParent;

    [HideInInspector]
    public float currentVisibility
    {
        get
        {
            if (slr != null)
            {
                return _currentVisibility * (_isCrouching ? VisibilityModifierWhenCrouched : 1f) * slr.visibilityValue;
            }
            else
                return _currentVisibility * (_isCrouching ? VisibilityModifierWhenCrouched : 1f);
        }
    }
    private float _currentVisibility = 1.0f;
    private StealthLightReceiver slr;
    public void setPlayFromHere(Transform pfhT)
    {
        playFromHere = true;
        pfhTransform = pfhT;
    }

    public locomotionType getCurrentLocomotionMode()
    {
        return currentLocomotionMode;
    }
    void Awake()
    {
        TryGetComponent<StealthLightReceiver>(out slr);
        mantleDebugLineRenderer = GetComponent<LineRenderer>();
        playerHeight = GetComponent<CapsuleCollider>().height;
        _playerRadius = GetComponent<CapsuleCollider>().radius;

        GameManager.registerPlayer(this.gameObject);
        EventRegistry.Init();
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.y;

        rotX = rot.x;

        _playerCamera = transform.root.GetComponentInChildren<Camera>();
        if (_playerCamera == null)
        {
            throw new Exception("Player camera not found");
        }
        cameraPivot = _playerCamera.transform.parent.gameObject;

        playerRB = transform.root.GetComponentInChildren<Rigidbody>();
        playerCollider = transform.root.GetComponentInChildren<Collider>();
        col = GetComponent<CapsuleCollider>();

        if ((spawnPoint == null) || (useSpawnPoint == false))
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }
        else
        {
            spawnPosition = spawnPoint.transform.position;
            spawnRotation = spawnPoint.transform.rotation;
        }

        foreach (playerStatChange psc in playerStatChangeEvents)
        {
            if (psc.eventToListenFor != "")
                EventRegistry.AddEvent(psc.eventToListenFor, statChangeOnEvent, gameObject);
        }

        foreach (damageMultiplierUpdate dmu in damageMultiplierChangeEvents)
        {
            if (dmu.eventToListenFor != "")
                EventRegistry.AddEvent(dmu.eventToListenFor, damageMultiplierChange, gameObject);
        }

        weapons = new List<GameObject>();
        ammunition = new Dictionary<string, int>();
        if (eventToEnableGun != "")
            EventRegistry.AddEvent(eventToEnableGun, enableDisableGun, gameObject);
        if (eventToDisableGun != "")
            EventRegistry.AddEvent(eventToDisableGun, enableDisableGun, gameObject);

        if (eventToEnableTorch != "")
            EventRegistry.AddEvent(eventToEnableTorch, enableDisableTorch, gameObject);
        if (eventToDisableTorch != "")
            EventRegistry.AddEvent(eventToDisableTorch, enableDisableTorch, gameObject);

        healthUITextEntity.enabled = showHealthOnHUD;
        updateHealthUI();
        Invoke("initialSpawn", 0.005f);
        oldCamFOV = _playerCamera.fieldOfView;
        /*
		if(useGun)
		{
			//GameObject.Instantiate(gunPrefab, gunHand.transform);
			gunObj = GameObject.Instantiate(gunPrefab,gunHand.transform);
			//gunObj = null;
			gunBehavior = gunObj.GetComponent<GunBehavior>();
		}*/

        if (useTorch)
        {
            torchObj = GameObject.Instantiate(torchPrefab, torchHand.transform);
            //torchBehavior = torchObj.GetComponent<TorchBehavior>();
        }

        tempoi = GetComponent<ObjectOfInterest>();

        //Cursor.lockState = CursorLockMode.Locked;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    public void mountLadder(LadderBehavior targetLadder)
    {
        currentLocomotionMode = locomotionType.climbing;
        transform.position = new Vector3(targetLadder.getOffset().x, transform.position.y, targetLadder.getOffset().z);
        transform.rotation = targetLadder.transform.rotation;
        playerRB.velocity = Vector3.zero;
        playerRB.useGravity = false;
        currentLadder = targetLadder;
        setCrouchState(false);
        //TODO: eventually replace with a LERP

    }

    public void dismountTopOfLadder()
    {
        transform.position = currentLadder.topDismount.position;
        transform.rotation = currentLadder.topDismount.rotation;
        playerRB.velocity = Vector3.zero;
        dropFromLadder();
    }

    public void dropFromLadder()
    {
        currentLocomotionMode = locomotionType.walking;
        playerRB.useGravity = true;
        checkGrounded();
    }

    public void giveImplement()
    {

    }

    public void giveWeapon(GameObject weaponPrefab)
    {
        weapons.Add(weaponPrefab);
        //TODO: add some prioritization for if the new weapon gets equipped over the current one
        selectWeapon(weapons.Count - 1);
    }

    public void giveAmmo(string ammoType, int amount)
    {
        if (ammunition.ContainsKey(ammoType))
        {
            ammunition[ammoType] += amount;
        }
        else
            ammunition.Add(ammoType, amount);
        UpdateAmmoText();
    }

    public void selectWeapon(int weaponIndex)
    {
        if (!useGun)
            weaponIndex = -1;

        if (weaponIndex >= weapons.Count)
        {
            weaponIndex = -1;
            currentWeaponIndex = weaponIndex;
        }
        else
        {
            if (weaponIndex < -1)
                weaponIndex = weapons.Count - 1;
            currentWeaponIndex = weaponIndex;
        }

        if (currentWeapon != null)
        {
            GameObject.Destroy(currentWeapon);
            currentWeaponBehavior = null;
        }

        if (weapons.Count > weaponIndex && weaponIndex >= 0)
        {

            currentWeaponIndex = weaponIndex;
            currentWeapon = GameObject.Instantiate(weapons[currentWeaponIndex], gunHand.transform);
            currentWeaponBehavior = currentWeapon.GetComponent<WeaponBehavior>();



            //gunObj = GameObject.Instantiate(gunPrefab, gunHand.transform);            
            //gunBehavior = gunObj.GetComponent<GunBehavior>();
        }

        UpdateAmmoText();
    }

    public void UpdateAmmoText()
    {
        ammoText.text = "";
        if (currentWeaponBehavior != null)
        {
            if (currentWeaponBehavior.GetType() == typeof(GunBehavior))
            {
                string tempAmmoType = ((GunBehavior)currentWeaponBehavior).ammoType;
                if (!ammunition.ContainsKey(tempAmmoType))
                    ammunition.Add(tempAmmoType, 0);
                ammoText.text = tempAmmoType + ": " + ammunition[tempAmmoType];

            }
        }
    }

    public void enableDisableGun(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        if (eventName == eventToEnableGun)
        {
            useGun = true;
            selectWeapon(0);
            /*gunObj = GameObject.Instantiate(gunPrefab,gunHand.transform);
			gunBehavior = gunObj.GetComponent<GunBehavior>();*/
        }
        if (eventName == eventToDisableGun)
        {
            useGun = false;
            selectWeapon(-1);
            /*GameObject.Destroy (gunObj);
			gunBehavior = null;*/
        }
    }

    public void damageMultiplierChange(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        bool updated = false;
        foreach (damageMultiplierUpdate dmu in damageMultiplierChangeEvents)
        {
            if (dmu.eventToListenFor == eventName)
            {
                updated = false;
                foreach (damageMultiplier dm in damageTypeMultipliers)
                {
                    if (dmu.multiplier.damageType == dm.damageType)
                    {
                        dm.multiplier = dmu.multiplier.multiplier;
                    }
                }
                if (!updated)
                {
                    damageTypeMultipliers.Add(new damageMultiplier(dmu.multiplier.damageType, dmu.multiplier.multiplier));
                }
            }
        }
    }
    public void enableDisableTorch(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        if (eventName == eventToEnableTorch)
        {
            useTorch = true;
            torchObj = GameObject.Instantiate(torchPrefab, torchHand.transform);
        }
        if (eventName == eventToDisableTorch)
        {
            useTorch = false;
            GameObject.Destroy(torchObj);
        }
    }

    public void takeDamage(int damageAmount, signalTypes damageType)
    {
        foreach (damageMultiplier dm in damageTypeMultipliers)
        {
            if (damageType == dm.damageType)
                damageAmount = (int)(damageAmount * dm.multiplier);
        }
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            foreach (string s in eventsToCallOnDeath)
                EventRegistry.SendEvent(s);
            respawn();
        }
    }

    public void updateHealthUI()
    {
        if (healthUITextEntity != null)
        {
            healthUITextEntity.text = "Health: " + currentHealth.ToString();
        }
    }

    void initialSpawn()
    {
        if (playFromHere)
        {
            setSpawnTransform(pfhTransform.position, pfhTransform.rotation);
        }
        spawn();
        playerRB.useGravity = false;
        Invoke("UseGravity", 0.5f);
        if (eventsToCallOnFirstSpawn.Count != 0)
        {
            foreach (string s in eventsToCallOnFirstSpawn)
                EventRegistry.SendEvent(s);
        }
    }

    void UseGravity()
    {
        playerRB.useGravity = true;
    }

    void respawn()
    {
        NPCManager.wipeAllAISuspicion(); //TODO: think about this wrt factions 
        spawn();

        if (pauseOnRespawn)
        {
            isRespawnPaused = true;
            respawnPauseCounter = 0f;
            GameManager.pause();
            Invoke("Gamemanager.upPause()", respawnPauseDuration);
        }
    }

    public void setTransform(Transform t)
    {
        transform.position = t.position;
        transform.rotation = t.rotation;
    }

    public void spawn() //TODO: fix. quickie exposure for another script. I feel as gross writing it as you do reading it
    {
        currentHealth = health;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        playerRB.velocity = Vector3.zero;
        rotX = 0; //transform.rotation.eulerAngles.x;
        rotY = transform.rotation.eulerAngles.y;
        jumpsRemaining = jumpCount;
        isFalling = false;
        isMantling = false;
        fallApex = transform.position.y;
        if (eventsToCallOnSpawn.Count != 0)
        {
            foreach (string s in eventsToCallOnSpawn)
                EventRegistry.SendEvent(s);
        }
    }

    public void teleport(Vector3 newPosition)
    {
        transform.position = newPosition;
        isFalling = false;
        isMantling = false;
        fallApex = newPosition.y;
        lastYValue = newPosition.y;

    }
    public void RotateCamHorizontally(float angle)
    {
        //latentRotY = angle;

        //version A
        rotY += angle;

        //version B
        //Vector3 rotVec = new Vector3(0f, angle, 0f);
        //playerRB.MoveRotation(Quaternion.Euler(rotVec));

    }

    /*void OnGUI()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }*/



    void Update()
    {
        
        //TODO: replace with the real deal later        
        if (tempoi != null)
        {
            
                tempoi.visibility = currentVisibility;
        }

        if ((tokensToDisplay.Count > 0) && (tokenDisplayText != null))
        {
            string tempText = "";
            foreach (tokenDisplayData td in tokensToDisplay)
            {
                if (!((td.hideIfValueIsZero) && (TokenRegistry.getToken(td.tokenToUse) == 0)))
                    tempText += td.label + " " + (TokenRegistry.getToken(td.tokenToUse)).ToString() + "\n";
            }
            tokenDisplayText.text = tempText;
        }
        

        if (GameManager.isPaused)
        {
            //account for pause on respawn
            if (isRespawnPaused)
            {
                respawnPauseCounter += Time.unscaledDeltaTime;
                if (respawnPauseCounter >= respawnPauseDuration)
                    GameManager.unPause();
            }
            else
            {
                return;
            }
        }

        //mouse and keyboard input

        float moveRatio = 1.0f;
        float horizontalMagnitude;
        Vector3 xRotVec, yRotVec, leanRotVec;

        if (!isInputLocked)
            movementInputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        mouseX = Input.GetAxis("Mouse X");
        mouseY = -Input.GetAxis("Mouse Y");

        /*float mouseX = Input.GetAxis("RightHorizontal");
        float mouseY = -Input.GetAxis("RightVertical");*/

        rotY += mouseX * mouseSensitivity; // * Time.deltaTime;
        //Debug.Log("mouse X: " + mouseX);
        //rotY += 0.5f * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity; // * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        xRotVec = new Vector3(rotX, 0f, 0f);
        yRotVec = new Vector3(0f, rotY, 0f);
        //Debug.Log("X rot: "+ mouseX * mouseSensitivity * Time.deltaTime);
        /*if (Mathf.Abs(rotX) > biggestX)
        {
            Debug.Log("Y rot: " + rotX + " dt: " + Time.deltaTime);
            biggestX = Mathf.Abs(rotX);
        }*/
        Quaternion localRotationX = Quaternion.Euler(xRotVec);
        Quaternion localRotationY = Quaternion.Euler(yRotVec);

        _playerCamera.transform.localRotation = localRotationX;
        
        transform.rotation = localRotationY;
        //playerRB.MoveRotation(localRotationY);
        if (Input.GetKeyDown(KeyCode.F6))
        {
            NPCManager.ToggleDebugVisuals();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            switch (debugmode)
            {
                case DebugMode.none:
                    setCrouchState(false);
                    playerCollider.enabled = true;
                    playerRB.useGravity = true;
                    debugmode = DebugMode.flying;
                    break;
                case DebugMode.flying:
                    playerCollider.enabled = false;
                    playerRB.useGravity = false;
                    debugmode = DebugMode.flyingNoclip;
                    break;
                case DebugMode.flyingNoclip:
                    playerCollider.enabled = true;
                    playerRB.useGravity = false;
                    debugmode = DebugMode.none;
                    break;
            }
        }

        isRunning = Input.GetButton("Run");

        if (debugmode != DebugMode.none)
        {
            playerRB.velocity = ((PlayerCamera.transform.forward * Input.GetAxis("Vertical")) +
                                (PlayerCamera.transform.right * Input.GetAxis("Horizontal"))).normalized * (isRunning ? runSpeed : walkSpeed);
            return;
        }

        if (!isInputLocked)
        {
            //DEBUG time scale stuff
            if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                Time.timeScale = 1f;
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                Time.timeScale += 0.25f;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                Time.timeScale -= 0.25f;
            }

            //TODO: replace this with general purpose right/left hand use
            if (Input.GetButtonDown("Fire1"))
            {
                if (currentWeaponBehavior != null)
                {
                    currentWeaponBehavior.Fire();
                }
                /*if (gunBehavior != null)
                    gunBehavior.fire();*/
            }

            //TODO: replace this with general purpose right/left hand use
            if (Input.GetButtonDown("Torch"))
            {
                if ((useTorch) && (torchObj != null))
                {
                    if (isTorchVisible)
                    {
                        HelperFunctions.hideObjectAndChildren(torchObj);
                        isTorchVisible = false;
                        foreach (string s in eventsOnTorchLower)
                            EventRegistry.SendEvent(s);
                    }
                    else
                    {
                        HelperFunctions.showObjectAndChildren(torchObj);
                        isTorchVisible = true;
                        foreach (string s in eventsOnTorchRaise)
                            EventRegistry.SendEvent(s);
                    }
                }

            }

            //Weapon select
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if ((wheel < 0) || (Input.GetButtonDown("Previous Weapon")))
            {
                selectWeapon(currentWeaponIndex - 1);
            }
            if ((wheel > 0) || (Input.GetButtonDown("Next Weapon")))
            {
                selectWeapon(currentWeaponIndex + 1);
            }

            if (Input.GetButtonDown("Jump") && currentLocomotionMode == locomotionType.climbing)
            {
                dropFromLadder();
            }

            if (Input.GetButtonDown("Jump") && !_isCrouching)
            {
                //print("Jump button pressed " + Time.fixedTime);
                if (jumpsRemaining > 0)
                {
                    playerRB.velocity = new Vector3(playerRB.velocity.x, 0, playerRB.velocity.z);
                    playerRB.AddForce(new Vector3(0, jumpForce, 0));

                    jumpsRemaining -= 1;
                    isGrounded = false;
                    isJumping = true;
                }
            }

            if (Input.GetButton("Jump"))
            {
                if (isFacingWall() && !isMantling)
                {
                    mantleTimer += Time.deltaTime;
                    if (mantleTimer >= mantleTimerThreshold)
                    {
                        print("trying to mantle!");
                        attemptMantle();
                    }
                }
            }
            else
            {
                mantleTimer = 0;
            }
            if (Input.GetButtonDown("Crouch") && canCrouch)
            {
                //TODO: remove the hard coded numbers here in favor of exposed properties
                if (_isCrouching)
                {
                    RaycastHit hitInfo1;
                    LayerMask mask = LayerMask.GetMask("Default");
                    Physics.SphereCast(transform.position, _playerRadius, transform.up, out hitInfo1, 1.76f, mask.value, QueryTriggerInteraction.Ignore);
                    if (hitInfo1.collider == null)
                    {
                        setCrouchState(false);

                    }
                }
                else
                {
                    setCrouchState(true);

                }
            }

            if (Input.GetButton("Lean Left"))
            {
                _currentLean -= leanRate * Time.deltaTime;
                if (_currentLean <= -leanAngle)
                {
                    _currentLean = -leanAngle;
                }
            }

            if (Input.GetButton("Lean Right"))
            {
                _currentLean += leanRate * Time.deltaTime;
                if (_currentLean >= leanAngle)
                {
                    _currentLean = leanAngle;
                }
            }


        }


        if ((!isFalling) && (!isGrounded))
        {
            if ((transform.position.y < lastYValue) && (!isGrounded))
            {
                //TODO: roll this all into the locomotion state enum
                if (currentLocomotionMode != locomotionType.climbing)
                {
                    isFalling = true;
                    fallApex = lastYValue;
                }
            }
        }
        else if ((isFalling) && (currentLocomotionMode != locomotionType.climbing)) //TODO: unify currentlocomotion mode and isFalling
        {
            //TODO: move this into the collision code
            if (fallApex - transform.position.y > forceKillFallDistance)
            {
                takeDamage(health, signalTypes.scriptedDamage);
            }
        }
        lastYValue = transform.position.y;




        if (_currentLean != 0)
        {


            if ((!Input.GetButton("Lean Right")) && (!Input.GetButton("Lean Left")))
            {
                if (_currentLean < 0)
                {
                    _currentLean += leanRate * Time.deltaTime;
                    /*if ((_currentLean < 0) && (_currentLean > -leanRate * Time.deltaTime))
                        _currentLean = 0;*/
                }
                if (_currentLean > 0)
                    _currentLean -= leanRate * Time.deltaTime;
            }
            cameraPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -_currentLean));
        }


        //MAX slope calculation stuff
        movementHeading = Vector3.Normalize(transform.forward * movementInputVector.y + transform.right * movementInputVector.x);
        groundAngle = Vector3.Angle(currentNormal, movementHeading) - 90;

        float uphillCoeff = 1f;

        if (groundAngle >= 0 && groundAngle < 90)
        {
            uphillCoeff = Mathf.Cos(Mathf.Deg2Rad * groundAngle);
            //Debug.Log("uphill coefficient: "+uphillCoeff);
            if (uphillCoeff < 0)
            {
                int temp = 0;
            }
        }



        if (currentLocomotionMode == locomotionType.climbing)
        {
            isFalling = false;
            fallApex = lastYValue;
            if (movementInputVector.magnitude >= deadZone)
            {
                playerRB.velocity = new Vector3(0, movementInputVector.y * (isRunning ? fastClimbSpeed : climbSpeed), 0);

                if (((transform.position.y - playerHeight / 2) >= currentLadder.getTop()) && playerRB.velocity.y > 0)
                    dismountTopOfLadder();

                if (((transform.position.y - playerHeight / 2) <= currentLadder.getBottom()) && playerRB.velocity.y < 0)
                    dropFromLadder();
            }
            else
                playerRB.velocity = Vector3.zero;
        }
        else
        {
            //transform.root.GetComponentInChildren<Rigidbody>().AddForce ( cameraPivot.transform.forward * walkSpeed * movementVector.y );
            if (isGrounded)
            {
                if (movementInputVector.magnitude >= deadZone)
                {
                    if (groundAngle <= maxSlopeAngle)
                    {
                        Vector3 tempVec = new Vector3(0, playerRB.velocity.y, 0) + movementHeading * (isRunning ? runSpeed : walkSpeed) * (_isCrouching ? crouchSpeedModifier : 1f) * uphillCoeff;
                        playerRB.velocity = tempVec;
                    }
                    else
                    {
                        playerRB.velocity = Vector3.down * walkSpeed;
                    }
                }
                else
                    playerRB.velocity = new Vector3(0, playerRB.velocity.y, 0);
            }
            else
            {
                //air control ratio is now able to make you move faster than walking while in the air
                playerRB.AddForce(Vector3.Normalize(transform.forward * movementInputVector.y + transform.right * movementInputVector.x) * (isRunning ? runSpeed : walkSpeed) * airControlRatio * (_isCrouching ? crouchSpeedModifier : 1f));
                horizontalMagnitude = new Vector3(playerRB.velocity.x, 0, playerRB.velocity.z).magnitude;

                if (horizontalMagnitude > (isRunning ? runSpeed : walkSpeed))
                {
                    playerRB.velocity = Vector3.Normalize(new Vector3(playerRB.velocity.x, 0, playerRB.velocity.z)) * (isRunning ? runSpeed : walkSpeed) + new Vector3(0, playerRB.velocity.y, 0);

                }

                if (!isJumping)
                {
                    playerRB.velocity = new Vector3(playerRB.velocity.x, Math.Min(playerRB.velocity.y, 0), playerRB.velocity.z);
                }
            }

        }
        //transform.root.GetComponentInChildren<Rigidbody> ().velocity = ;


        //Lerp into running FOV if player is running
        if (isRunning)
        {
            _playerCamera.fieldOfView += Time.deltaTime * FOVRateChange;
            if (_playerCamera.fieldOfView >= oldCamFOV * runFOVPercent)
                _playerCamera.fieldOfView = oldCamFOV * runFOVPercent;
        }
        else
        {
            _playerCamera.fieldOfView -= Time.deltaTime * FOVRateChange;
            if (_playerCamera.fieldOfView <= oldCamFOV)
                _playerCamera.fieldOfView = oldCamFOV;

        }

        //Debug.Log("Ground angle:" + groundAngle);
        if (isMantling)
        {
            //TODO: add lerping
            playerRB.MovePosition(Vector3.Lerp(transform.position, mantleDestination, mantleSpeed * Time.deltaTime));
            playerRB.velocity = Vector3.zero;
            if (Vector3.Distance(playerRB.position, mantleDestination) < 0.25f)
            {
                playerRB.MovePosition(mantleDestination);
                isMantling = false;
                isGrounded = true;
                isJumping = false;
                mantleTimer = 0;
            }
        }
    }

    private void FixedUpdate()
    {

    }

    private void setCrouchState(bool newState)
    {
        if (_isCrouching == newState)
            return;
        //TODO:remove the hard coding from this
        _isCrouching = newState;

        if(newState == false)
        {
            col.height = playerHeight; // 2f;
            col.transform.position += new Vector3(0, playerHeight/2f - (playerHeight*crouchHeightPercent)/2f, 0);
            //cameraPivot.transform.localPosition = new Vector3(0, -0.5f, 0);
            //_playerCamera.transform.localPosition = new Vector3(0, 1.5f, 0);
            _playerCamera.transform.localPosition = new Vector3(0, playerHeight * 0.75f, 0);
        }
        if (newState == true)
        {
            col.height = playerHeight * crouchHeightPercent; // 0.25f;
            col.transform.position += new Vector3(0, -0.875f, 0);
            //cameraPivot.transform.localPosition = new Vector3(0, -0.75f, 0);
            _playerCamera.transform.localPosition = new Vector3(0, playerHeight * crouchHeightPercent * 0.75f, 0);
            //_playerCamera.transform.localPosition = new Vector3(0, 0.875f, 0);
        }
    }

    public bool ConsumeAmmo(string ammoType)
    {
        if (ammoType == "")
            return true;
        if (ammunition.ContainsKey(ammoType))
        {
            if (ammunition[ammoType] > 0)
            {
                ammunition[ammoType] -= 1;
                UpdateAmmoText();
                return true;
            }
            else
                return false;
        }
        else
            return false;

        //TODO: update ammo UI
    }
    
    private void attemptMantle()
    {
        RaycastHit hitInfo1, hitInfo2, hitInfo3;
        LayerMask mask = LayerMask.GetMask("Default");
        float mantleSweepIncrement = 0.025f;

        //hard coding radius and player half height
        float yCheck = mantleHeight - playerHeight / 2f; // transform.position.y + playerHeight/2f;

        isMantling = false;

        //cast up to check clearance
        /*Physics.SphereCast(transform.position, playerRadius, transform.up, out hitInfo1,
            yCheck, mask.value, QueryTriggerInteraction.Ignore);*/

        if ((mantleDebugLineRenderer != null) && drawMantleDebug)
        {
            mantleDebugLineRenderer.enabled = true;
            mantleDebugLineRenderer.SetPosition(0, transform.position);
            mantleDebugLineRenderer.SetWidth(0.025f, 0.025f);
        }

        
        print("Mantle up check cleared");
        //cast forward from that point
        if (mantleDebugLineRenderer != null)
        {
            if (drawMantleDebug)
                mantleDebugLineRenderer.SetPosition(1, transform.position + new Vector3(0, yCheck, 0));
        }

        
        float mantleSweepCounter = 0;
        Physics.SphereCast(transform.position + new Vector3(0, mantleSweepCounter, 0), _playerRadius * 0.9f, transform.forward, out hitInfo2, _playerRadius * 2, mask.value, QueryTriggerInteraction.Ignore);
        //TODO: Fix this ugly pre-test before the loop. Find a more elegant solution
        while ((hitInfo2.collider != null)&&(mantleSweepCounter<=yCheck))
        {
            //Physics.SphereCast(transform.position + new Vector3(0, yCheck, 0), playerRadius * 0.9f, transform.forward, out hitInfo2, playerRadius * 2, mask.value, QueryTriggerInteraction.Ignore);
            Physics.SphereCast(transform.position + new Vector3(0, mantleSweepCounter, 0), _playerRadius * 0.9f, transform.forward, out hitInfo2, _playerRadius * 2, mask.value, QueryTriggerInteraction.Ignore);
            mantleSweepCounter += mantleSweepIncrement;
        }

        if(hitInfo2.collider == null)
        {
            print("Mantle forward check cleared");
            if(drawMantleDebug)
                mantleDebugLineRenderer.SetPosition(2, transform.position + new Vector3(0, yCheck, 0)+transform.forward);
                
            //cast down to see where the player lands
            Physics.SphereCast(transform.position + new Vector3(0, yCheck, 0)+(transform.forward*_playerRadius*2), _playerRadius, Vector3.down, out hitInfo3,mantleHeight, mask.value, QueryTriggerInteraction.Ignore);
            if(hitInfo3.collider!=null)
            {
                isMantling = true;
                print("mantle succeeded");
                mantleDestination = hitInfo3.point + new Vector3(0, playerHeight/2f, 0);
                if (drawMantleDebug)
                    mantleDebugLineRenderer.SetPosition(3, mantleDestination);
                
                //crouch/standing check
                Physics.SphereCast(mantleDestination, _playerRadius * 0.9f, transform.up, out hitInfo1, playerHeight-(_playerRadius*2), mask.value, QueryTriggerInteraction.Ignore);
                if(hitInfo1.collider == null)
                {
                    setCrouchState(false);
                }
                else
                {
                    setCrouchState(true);
                }


            }
            else
            {
                print("mantle down check obstructed");
                mantleTimer = 0;
                if (drawMantleDebug)
                    mantleDebugLineRenderer.SetPosition(3, transform.position + transform.forward);
            }
        }
        else
        {
            print("mantle forward check obstructed");
            mantleTimer = 0;
            if (drawMantleDebug)
            {
                mantleDebugLineRenderer.SetPosition(2, transform.position + new Vector3(0, yCheck, 0));
                mantleDebugLineRenderer.SetPosition(3, transform.position + new Vector3(0, yCheck, 0));
            }
                
        }
    }
   
    

    private bool isFacingWall()
    {
        //TODO: set angle threshold on wall facing

        RaycastHit hitInfo1, hitInfo2;
        LayerMask mask = LayerMask.GetMask("Default");

        //hard coding radius and player half height
        //cast at eye level
        Physics.SphereCast(transform.position + new Vector3(0, 0.25f, 0), 0.35f, transform.forward, out hitInfo1,
            0.5f, mask.value, QueryTriggerInteraction.Ignore);

        //cast at waist level
        Physics.SphereCast(transform.position, 0.35f, Vector3.down, out hitInfo2,
            0.5f, mask.value, QueryTriggerInteraction.Ignore);

        return (hitInfo1.collider != null || hitInfo2.collider != null);

    }
    public RaycastHit platformCheck()
    {
        //return platformCheck(0.675f);
        return platformCheck(playerHeight/2f+0.05f);
        //return platformCheck(1.05f);
    }
    public RaycastHit platformCheck(float castDistance)
    {
        RaycastHit hitInfo;
        LayerMask mask = LayerMask.GetMask("Default");

        //hard coding radius and player half height

        Physics.SphereCast(transform.position, playerRadius, Vector3.down, out hitInfo,
            castDistance, mask.value, QueryTriggerInteraction.Ignore);

        return hitInfo;

    }

    public void setSpawnTransform(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
    }


    public void setCheckpoint(CheckpointBehavior cpb)
    {
        
        setSpawnTransform(cpb.getRespawnTransform().position, cpb.getRespawnTransform().rotation);
        //setSpawnTransform(cpb.gameObject.transform.position, cpb.gameObject.transform.rotation);
        cpb.useCheckpoint();
        
    }

    void OnCollisionEnter(Collision collision)
    {

        /*CheckpointBehavior cpb = collision.gameObject.GetComponent<CheckpointBehavior>();
        if (cpb != null)
        {
            setCheckpoint(cpb);
            
        }*/
        //if ((isFalling) && (collision.relativeVelocity.y > 0))
        if (collision.relativeVelocity.y > 0)
        {

            int fallDistance = (int)(fallApex - transform.position.y);
            //Debug.Log("fell: "+ fallDistance);
            //Debug.Log("YPos: "+ transform.position.y);
            if (fallDistance >= (killHeight - 1))
            {
                //TODO: replace this with a more robust damage model based on height
                if(collision.gameObject.GetComponent<PaddedSurfaceBehavior>()==null)
                    takeDamage(health, signalTypes.scriptedDamage);
            }
            isFalling = false;
            checkGrounded();
        }
        if ((currentLocomotionMode == locomotionType.climbing) && (collision.relativeVelocity.y > 0))
        {
            dropFromLadder();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        RaycastHit platformHit;
        if (!isJumping)
            platformHit = checkGrounded();
    }

    void OnCollisionExit(Collision collision)
    {
        if (!isJumping && (collision.relativeVelocity.y < 0))
        {
            isGrounded = false;
            jumpsRemaining -= 1;
            //Invoke("checkGrounded",0.05f);
        }
        else
        { 
            if (collision.relativeVelocity.y >= 0)
            {
                jumpsRemaining -= 1;
                checkGrounded();
            }
        }
    }

    RaycastHit checkGrounded()
    {
        //Debug.Log("GROUNDED CHECK: ");
        RaycastHit hitinfo = platformCheck();
        if (hitinfo.collider != null)
        {
            currentNormal = hitinfo.normal;
            groundAngle = Vector3.Angle(transform.forward, currentNormal);
        }

        if ((hitinfo.collider == null) || (hitinfo.collider.isTrigger))
        {            
            isGrounded = false;
            return hitinfo;
        }
        //Debug.Log("Is grounded");
        isGrounded = true;
        jumpsRemaining = jumpCount;
        isJumping = false;        
        fallApex = playerRB.transform.position.y;
        return hitinfo;        
    }

    public void changePlayerStat(playerStatChange psc)
    {
        switch (psc.playerStatToChange)
        {
            case playerStatType.health:
                switch (psc.operation)
                {
                    case operationType.add:
                        currentHealth += psc.amount;
                        break;
                    case operationType.multiply:
                        currentHealth *= psc.amount;
                        break;
                    case operationType.set:
                        currentHealth = psc.amount;
                        takeDamage(0, signalTypes.scriptedDamage);
                        break;
                    case operationType.subtract:
                        takeDamage(psc.amount, signalTypes.scriptedDamage);
                        break;
                    case operationType.divide:
                        currentHealth /= psc.amount;
                        break;
                }
                break;
            case playerStatType.visibility:
                switch (psc.operation)
                {
                    case operationType.add:
                        _currentVisibility += psc.amount;
                        break;
                    case operationType.multiply:
                        _currentVisibility *= psc.amount;
                        break;
                    case operationType.set:
                        _currentVisibility = psc.amount;
                        break;
                    case operationType.subtract:
                        _currentVisibility -= psc.amount;
                        break;
                    case operationType.divide:
                        _currentVisibility /= psc.amount;
                        break;
                }
                break;
            case playerStatType.jumpCount:
                switch (psc.operation)
                {
                    case operationType.add:
                        jumpCount += psc.amount;
                        jumpsRemaining += psc.amount;
                        if (jumpCount < 0)
                            jumpCount = 0;
                        if (jumpsRemaining < 0)
                            jumpsRemaining = 0;
                        break;
                    case operationType.multiply:
                        jumpCount *= psc.amount;
                        jumpsRemaining *= psc.amount;
                        if (jumpCount < 0)
                            jumpCount = 0;
                        if (jumpsRemaining < 0)
                            jumpsRemaining = 0;
                        break;
                    case operationType.set:
                        jumpCount = psc.amount;
                        jumpsRemaining = psc.amount;
                        if (jumpCount < 0)
                            jumpCount = 0;
                        if (jumpsRemaining < 0)
                            jumpsRemaining = 0;
                        break;
                    case operationType.subtract:
                        jumpCount -= psc.amount;
                        jumpsRemaining -= psc.amount;
                        if (jumpCount < 0)
                            jumpCount = 0;
                        if (jumpsRemaining < 0)
                            jumpsRemaining = 0;
                        break;
                    case operationType.divide:
                        jumpCount /= psc.amount;
                        jumpsRemaining /= psc.amount;
                        if (jumpCount < 0)
                            jumpCount = 0;
                        if (jumpsRemaining < 0)
                            jumpsRemaining = 0;
                        break;
                }
                break;
            case playerStatType.jumpForce:
                switch (psc.operation)
                {
                    case operationType.add:
                        jumpForce += psc.amount;

                        break;
                    case operationType.multiply:
                        jumpForce *= psc.amount;

                        break;
                    case operationType.set:
                        jumpForce = psc.amount;

                        break;
                    case operationType.subtract:
                        jumpForce -= psc.amount;

                        break;
                    case operationType.divide:
                        jumpForce /= psc.amount;

                        break;
                }
                break;
            case playerStatType.runSpeed:
                switch (psc.operation)
                {
                    case operationType.add:
                        runSpeed += psc.amount;

                        break;
                    case operationType.multiply:
                        runSpeed *= psc.amount;

                        break;
                    case operationType.set:
                        runSpeed = psc.amount;

                        break;
                    case operationType.subtract:
                        runSpeed -= psc.amount;
                        break;
                    case operationType.divide:
                        runSpeed /= psc.amount;
                        break;
                }
                break;
            case playerStatType.walkSpeed:
                switch (psc.operation)
                {
                    case operationType.add:
                        walkSpeed += psc.amount;

                        break;
                    case operationType.multiply:
                        walkSpeed *= psc.amount;

                        break;
                    case operationType.set:
                        walkSpeed = psc.amount;

                        break;
                    case operationType.subtract:
                        walkSpeed -= psc.amount;
                        break;
                    case operationType.divide:
                        walkSpeed /= psc.amount;
                        break;
                }
                break;
        }
    }
    public void statChangeOnEvent(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != this.gameObject))
            return;
        foreach (playerStatChange psc in playerStatChangeEvents)
        {
            if (psc.eventToListenFor == eventName)
            {
                changePlayerStat(psc);
               
            }
        }
    }

}

