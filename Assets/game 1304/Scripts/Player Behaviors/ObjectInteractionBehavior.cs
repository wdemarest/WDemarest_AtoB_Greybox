using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum interactionType {none, read,examine,pickup,use,carry,ladder};
public enum damageTypes {fire,ice,water,earth,metal,salt,magic,holy};


[RequireComponent(typeof(GAME1304PlayerController))]
public class ObjectInteractionBehavior : MonoBehaviour
{

    private Camera _playerCamera;
    private GAME1304PlayerController playerController;
    private GameObject aimedAtObject;
    private string interactString = "none";

    //behavior pointers for the types of things you can interact with
    private InteractiveObject interactInfo;
    private PickupItem pickupInfo;
    private ReadableObject readableInfo;
    private ExaminableObject examineInfo;
    private PhysicsCarryObject carryInfo;
    private Rigidbody carryRBInfo;
    private LadderBehavior ladderInfo;

    private interactionType currentInteractMode = interactionType.none;
    private Canvas _playerCanvas;

    //inventory stuff    
    private Inventory _inventory;
    [HideInInspector]
    public Inventory inventory { get { return _inventory; } }    
    //readable stuff
    private bool isReading = false;
    private bool isExamining = false;
    private bool isCarrying = false;
    private bool isInDialogue = false;
    private GameObject carriedObject = null;
    private DialogueContainer currentDialogue;
    private DialogueEntry currentDialogueEntry;

    [Header("Interact Parameters")]
    public float interactionDistance = 2.0f;
    public float ladderInteractionDistance = 1.5f;
    [Space(3)]
    [Header("UI Objects")]
    public Text interactTextObject;
    public Text interactRequirementTextObject;
    public Image standardReticule;
    public Image interactReticule;
    public Image interactPromptBackground;
    [Space(3)]
    [Header("Utility Screen Stuff")]
    public Canvas utilityMenuCanvas;

    [Space(3)]
    [Header("Read/examine UI Stuff")]
    public Canvas dialogueCanvas;
    public Canvas readingCanvas;
    public Text readingTitleUIText;
    public Text readingBodyUIText;
    public Canvas examineCanvas;
    public Text examineTitleUIText;
    public Image examineImage;
    public Image readingBackground;
    public GameObject exitPromptText;
    [Space(3)]
    [Header("Objective system UI Stuff")]        
    public Canvas HUDCanvas;
    public GameObject taskMarkerPrefab;
    public Text HUDNotificationText;
    private List<GameObject> objectiveMarkers;
    
    [Space(3)]
    [Header("Physics Carry Stuff")]
    public float carryOffset = 1.0f;
    public float throwForce = 10.0f;

    void Start()
    {
        playerController = GetComponent<GAME1304PlayerController>();
        interactInfo = null;
        _playerCamera = transform.root.GetComponentInChildren<Camera>();

        HUDManager.init();
        if (_playerCamera == null)
        {
            throw new Exception("Player camera not found");
        }
        HUDManager.playerCam = _playerCamera;
        _inventory = new Inventory();        
      
        if (readingCanvas != null)
        {
            readingCanvas.gameObject.SetActive(true);
            readingCanvas.enabled = false;
            HUDManager.readingCanvas = readingCanvas;
            HUDManager.readingTitleUIText = readingTitleUIText;
            HUDManager.readingBodyUIText = readingBodyUIText;
        }
        if(HUDCanvas != null)
        {
            HUDManager.HUDCanvas = HUDCanvas;
        }
        if (examineCanvas != null)
        {
            examineCanvas.gameObject.SetActive(true);
            examineCanvas.enabled = false;
        }
      
        if (utilityMenuCanvas  != null)
        {
            utilityMenuCanvas.gameObject.SetActive(false);
            //utilityMenuCanvas.enabled = false;
        }
        if (interactReticule!=null)
        {
            interactReticule.transform.parent.gameObject.SetActive(true);
        }  
                
        HUDManager.taskMarkerPrefab = taskMarkerPrefab;
        HUDManager.HUDNotificationText = HUDNotificationText;

        HUDManager.UtilityMenuCanvas = utilityMenuCanvas;
    }

    RaycastHit filterCastResults(RaycastHit[] hitInfos)
    {
        RaycastHit closeHit = new RaycastHit();
        float newDist;
        float closeDist = float.PositiveInfinity;
        foreach(RaycastHit rch in hitInfos)
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

    void Update()
    {
        RaycastHit[] hitInfos;
        RaycastHit hitInfo;
        Renderer _renderer;
        LayerMask mask = LayerMask.GetMask("Default");
        //Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hitInfo, interactionDistance, mask.value);
        hitInfos = Physics.RaycastAll(_playerCamera.transform.position, _playerCamera.transform.forward, interactionDistance, mask.value);
        hitInfo = filterCastResults(hitInfos);
        /*if (hitInfo.collider == null)
        {
           // Debug.Log("Interact failed: "+hitInfos.Length);
            return;
        }*/
        //Physics.Raycast(_playerCamera.transform.position,  _playerCamera.transform.forward, out hitInfo, interactionDistance);
        //Physics.ra
        //set defaults
        currentInteractMode = interactionType.none;
        aimedAtObject = null;
        interactInfo = null;
        interactString = "none";
        interactTextObject.enabled = false;
        interactRequirementTextObject.enabled = false;
        interactReticule.enabled = false;
        interactPromptBackground.enabled = false;
        standardReticule.enabled = true;

        if ((hitInfo.collider != null) && (!hitInfo.collider.isTrigger))
        {
            aimedAtObject = hitInfo.collider.gameObject;


            //is this a usable object?
            interactInfo = aimedAtObject.GetComponent<InteractiveObject>();
            if (interactInfo != null)
            {
                currentInteractMode = interactionType.use;
                
                
                    if (!(interactInfo.isUsed && interactInfo.UseOnce))
                    {
                        interactString = interactInfo.interactLabel;
                        interactTextObject.enabled = true;
                        interactReticule.enabled = true;
                        interactPromptBackground.enabled = true;
                        standardReticule.enabled = false;
                        interactTextObject.text = interactString;
                        interactRequirementTextObject.text = "";
                        interactPromptBackground.rectTransform.offsetMin = interactTextObject.rectTransform.offsetMin;
                        interactPromptBackground.rectTransform.offsetMax = interactTextObject.rectTransform.offsetMax;

                        //object requires an inventory item
                        if ((interactInfo.InventoryItemNeeded != ""))
                        {
                            interactRequirementTextObject.enabled = true;
                            if (!_inventory.hasItem(interactInfo.InventoryItemNeeded, interactInfo.quantityRequired))
                            {
                                interactRequirementTextObject.color = Color.red;
                                interactRequirementTextObject.text = "Requires " + (interactInfo.quantityRequired > 1 ? (interactInfo.quantityRequired.ToString() + "X ") : "") + interactInfo.InventoryItemNeeded;


                            }
                            else
                            {
                                if (interactInfo.ConsumeInventoryItem)
                                {
                                    interactRequirementTextObject.color = Color.green;
                                    interactRequirementTextObject.text = "Consumes " + (interactInfo.quantityRequired > 1 ? (interactInfo.quantityRequired.ToString() + "X ") : "") + interactInfo.InventoryItemNeeded;
                                }
                                else
                                {
                                    interactRequirementTextObject.color = Color.green;
                                    interactRequirementTextObject.text = "Uses " + (interactInfo.quantityRequired > 1 ? (interactInfo.quantityRequired.ToString() + "X ") : "") + interactInfo.InventoryItemNeeded;
                                }

                            }
                        }
                    }
                DoorknobBehavior dnb = aimedAtObject.GetComponent<DoorknobBehavior>(); // (DoorknobBehavior)(interactInfo);
                if (dnb != null)
                {
                    if (dnb.isLockedArtificially)
                    {
                        interactRequirementTextObject.enabled = true;
                        interactRequirementTextObject.text = dnb.artificialLockedLabel;
                    }
                }
            }

            //is this a pickup?
            pickupInfo = aimedAtObject.GetComponent<PickupItem>();
            if (pickupInfo != null)
            {
                if ((pickupInfo.enabled) && (!pickupInfo.pickUpOnCollision))
                {
                    currentInteractMode = interactionType.pickup;
                    interactString = pickupInfo.inventoryItemName;
                    interactTextObject.enabled = true;
                    interactReticule.enabled = true;
                    interactPromptBackground.enabled = true;
                    standardReticule.enabled = false;
                    interactTextObject.text = "Pick up " + interactString;
                    interactPromptBackground.rectTransform.offsetMin = interactTextObject.rectTransform.offsetMin;
                    interactPromptBackground.rectTransform.offsetMax = interactTextObject.rectTransform.offsetMax;
                }
            }

            //is this a readable?
            readableInfo = aimedAtObject.GetComponent<ReadableObject>();
            if (readableInfo != null)
            {
                if ((readableInfo.enabled) && (readableInfo.isEnabled))
                {
                    currentInteractMode = interactionType.read;
                    interactString = readableInfo.readingVerbLabel + readableInfo.titleText;
                    interactTextObject.enabled = true;
                    interactReticule.enabled = true;
                    interactPromptBackground.enabled = true;
                    standardReticule.enabled = false;
                    interactTextObject.text = interactString;
                    interactPromptBackground.rectTransform.offsetMin = interactTextObject.rectTransform.offsetMin;
                    interactPromptBackground.rectTransform.offsetMax = interactTextObject.rectTransform.offsetMax;
                }
            }

            //is this an examinable?
            examineInfo = aimedAtObject.GetComponent<ExaminableObject>();
            if (examineInfo != null)
            {
                if (examineInfo.enabled)
                {
                    currentInteractMode = interactionType.examine;
                    interactString = examineInfo.examineItemName;
                    interactTextObject.enabled = true;
                    interactReticule.enabled = true;
                    interactPromptBackground.enabled = true;
                    standardReticule.enabled = false;
                    interactTextObject.text = "Examine " + interactString;
                    interactPromptBackground.rectTransform.offsetMin = interactTextObject.rectTransform.offsetMin;
                    interactPromptBackground.rectTransform.offsetMax = interactTextObject.rectTransform.offsetMax;
                }
            }

            //is this a physics carry object?
            carryInfo = aimedAtObject.GetComponent<PhysicsCarryObject>();
            carryRBInfo = aimedAtObject.GetComponent<Rigidbody>();
            if (carryInfo != null && carryRBInfo != null)
            {
                if (carryInfo.enabled)
                {
                    currentInteractMode = interactionType.carry;
                    interactString = carryInfo.objectName;
                    interactTextObject.enabled = true;
                    interactReticule.enabled = true;
                    interactPromptBackground.enabled = true;
                    standardReticule.enabled = false;
                    interactTextObject.text = "Carry " + interactString;
                    interactPromptBackground.rectTransform.offsetMin = interactTextObject.rectTransform.offsetMin;
                    interactPromptBackground.rectTransform.offsetMax = interactTextObject.rectTransform.offsetMax;
                }
            }

           
        }

        //separate, shorter raycast to check for ladders
        Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hitInfo, ladderInteractionDistance, mask.value);        
        if ((hitInfo.collider != null) && (!hitInfo.collider.isTrigger))
        {
            //is this a ladder?
            ladderInfo = aimedAtObject.GetComponent<LadderBehavior>();
            if (ladderInfo != null)
            {
                //TODO: get rid of the redundancy here, factor out, etc.
                if (playerController.getCurrentLocomotionMode() == locomotionType.climbing)
                {
                    currentInteractMode = interactionType.ladder;
                    interactString = ladderInfo.interactLabel;
                    interactTextObject.enabled = true;
                    interactReticule.enabled = true;
                    interactPromptBackground.enabled = true;
                    standardReticule.enabled = false;
                    interactTextObject.text = "Drop";
                    interactPromptBackground.rectTransform.offsetMin = interactTextObject.rectTransform.offsetMin;
                    interactPromptBackground.rectTransform.offsetMax = interactTextObject.rectTransform.offsetMax;

                }
                else
                {
                    if (ladderInfo.enabled)
                    {
                        currentInteractMode = interactionType.ladder;
                        interactString = ladderInfo.interactLabel;
                        interactTextObject.enabled = true;
                        interactReticule.enabled = true;
                        interactPromptBackground.enabled = true;
                        standardReticule.enabled = false;
                        interactTextObject.text = "Climb";
                        interactPromptBackground.rectTransform.offsetMin = interactTextObject.rectTransform.offsetMin;
                        interactPromptBackground.rectTransform.offsetMax = interactTextObject.rectTransform.offsetMax;
                    }
                }
            }
        }
     
        if(!GameManager.isPaused)
        {
            if(Input.GetKeyDown(KeyCode.Tab)) // || Input.GetButtonDown("backButton"))
            {
                HUDManager.openUtilityMenu();
            }
            if (Input.GetButtonDown("Inventory"))
            {
                HUDManager.openUtilityMenu(UtilityMenuTabType.inventory);
            }
            if (Input.GetButtonDown("Objectives"))
            {
                HUDManager.openUtilityMenu(UtilityMenuTabType.objectives);
            }
            if (Input.GetButtonDown("Map"))
            {
                HUDManager.openUtilityMenu(UtilityMenuTabType.map);
            }
        }

        if (Input.GetButtonDown("Use"))
        {
            if ((isCarrying) && (!GameManager.isPaused))
            {
                //TODO: impart velocity based on players movement when they drop the object
                carriedObject.GetComponent<Rigidbody>().isKinematic = false;
                carriedObject.GetComponent<Collider>().isTrigger = false;// enabled = true;
                carriedObject.transform.parent = null;
                carryInfo = carriedObject.GetComponent<PhysicsCarryObject>();
                if (carryInfo != null)
                {
                    carryInfo.dropObject();
                    carryInfo = null;
                }
                isCarrying = false;
                Renderer r = carriedObject.GetComponent<Renderer>();
                Color materialColor = r.material.color;
                r.material.color = new Color(materialColor.r, materialColor.g, materialColor.b, 1.0f);
                carriedObject = null;
            }

            if (isReading || isExamining) 
            {
                if (isReading)
                {
                    GameManager.unPause();
                    readingCanvas.enabled = false;
                    isReading = false;
                }
                /*if (isInDialogue)
                {
                    if (currentDialogue.canExit)
                        exitDialogue(currentDialogue);
                }*/
                if (isExamining)
                {
                    GameManager.unPause();
                    examineCanvas.enabled = false;
                    isExamining = false;
                }

            }
            else if (!GameManager.isPaused)
            {
                switch (currentInteractMode)
                {
                    case interactionType.carry:
                        {
                            carryInfo.gameObject.transform.position = _playerCamera.transform.position + _playerCamera.transform.forward * carryOffset;
                            carryInfo.gameObject.transform.parent = _playerCamera.transform;
                            carryInfo.gameObject.GetComponent<Collider>().isTrigger = true;
                            carryInfo.pickupObject();
                            Renderer r = carryInfo.gameObject.GetComponent<Renderer>();
                            Color materialColor = r.material.color;
                            r.material.color = new Color(materialColor.r, materialColor.g, materialColor.b, 0.5f);

                            carryRBInfo.isKinematic = true;
                            carriedObject = carryInfo.gameObject;
                            /*_renderer = carryInfo.gameObject.GetComponent<Renderer>();
                            if(_renderer != null)
                            {
                                _renderer.material.color = new Color(1.0f, 1.0f, 1.0f, 0.25f);
                            }*/
                            isCarrying = true;
                        }
                        break;
                    case interactionType.use:

                        if ((interactInfo.InventoryItemNeeded != ""))
                        {

                            if (_inventory.hasItem(interactInfo.InventoryItemNeeded, interactInfo.quantityRequired))
                            {
                                interactInfo.onInteract();
                                if (interactInfo.ConsumeInventoryItem)
                                {
                                    _inventory.removeItem(interactInfo.InventoryItemNeeded, interactInfo.quantityRequired);
                                }
                            }
                        }
                        else
                        {
                            DoorknobBehavior dnb = aimedAtObject.GetComponent<DoorknobBehavior>(); // (DoorknobBehavior)(interactInfo);
                            if (dnb == null)
                            {
                                interactInfo.onInteract();
                                
                            }
                            else
                            {
                                if (!dnb.isLockedArtificially)
                                {
                                    interactInfo.onInteract();
                                }

                            }
                        }
                        break;
                    case interactionType.pickup:
                        if (pickupInfo != null)
                        {
                            pickupItem(pickupInfo);
                        }
                        break;
                    case interactionType.read:
                        {                            
                            read();
                        }
                        break;
                    case interactionType.examine:
                        {                            
                            isExamining = true;
                            GameManager.pause();
                            examineCanvas.enabled = true;                            
                            //readingBodyUIText.text = readableInfo.bodyText;
                            examineTitleUIText.text = examineInfo.examineItemName;
                            examineImage.GetComponent<Image>().sprite = examineInfo.examineImage;
                            if (examineInfo.EventsSentOnExamine.Count != 0)
                            {
                                foreach (string s in examineInfo.EventsSentOnExamine)
                                    EventRegistry.SendEvent(s);
                                foreach (EventPackage ep in examineInfo.EventsToSendOnExamine)
                                    EventRegistry.SendEvent(ep,examineInfo.gameObject);

                            }
                        }
                        break;
                    case interactionType.ladder:
                        if (playerController.getCurrentLocomotionMode() == locomotionType.climbing)
                            playerController.dropFromLadder();
                        else
                            playerController.mountLadder(ladderInfo);
                        break;

                }
            }

        }

        /*if (isInDialogue)
        {
            int dialogueIndex = 11;
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                dialogueIndex = 0;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                dialogueIndex = 1;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                dialogueIndex = 2;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                dialogueIndex = 3;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                dialogueIndex = 4;
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                dialogueIndex = 5;
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                dialogueIndex = 6;
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                dialogueIndex = 7;
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                dialogueIndex = 8;
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                dialogueIndex = 9;
            }

            if (dialogueIndex < currentDialogueEntry.getAdjustedReplyCount())
            {                
                int newIDX = -1;
                for (int x=0;x<=dialogueIndex;x++)
                {
                    newIDX += 1;
                    while ( !checkDialogueLineCondition(currentDialogueEntry.playerReplies[newIDX].condition))
                    {
                        newIDX++;
                    }                    
                }
                /*int x=0;
                while (x<dialogueIndex)
                {                    
                    if (TokenRegistry.testToken(currentDialogueEntry.playerReplies[x].condition))
                        x += 1;
                    newIDX++;
                }*/
                /*if (currentDialogueEntry.playerReplies[newIDX ].thisReplyEndsTheDialogue)
                {
                    exitDialogue(currentDialogue);
                }
                else
                {
                    if (currentDialogueEntry.playerReplies[newIDX].replyAddress != "")
                    {
                        currentDialogueEntry = currentDialogue.getEntryByAddress(currentDialogueEntry.playerReplies[newIDX].replyAddress);
                    }                    
                    //TODO: might be a problem here!!
                    showCurrentDialogue();
                }
            }
        }*/

        if (!GameManager.isPaused)
        {                                    
            if (Input.GetButtonDown("Fire1") && isCarrying)
            {
                //TODO: impart velocity based on players movement when they drop the object
                carriedObject.GetComponent<Rigidbody>().isKinematic = false;

                carriedObject.GetComponent<Collider>().isTrigger = false;
                carriedObject.transform.parent = null;
                carriedObject.GetComponent<Rigidbody>().velocity = gameObject.GetComponent<Rigidbody>().velocity;
                carriedObject.GetComponent<Rigidbody>().AddForce(_playerCamera.transform.forward * throwForce);
                carryInfo = carriedObject.GetComponent<PhysicsCarryObject>();
                if (carryInfo != null)
                {
                    carryInfo.throwObject();
                    carryInfo = null;
                }
                isCarrying = false;
                Renderer r = carriedObject.GetComponent<Renderer>();
                Color materialColor = r.material.color;
                r.material.color = new Color(materialColor.r, materialColor.g, materialColor.b, 1.0f);
                carriedObject = null;
            }
        }
        updateObjectiveMarkers(Time.deltaTime); 
    }

    private void updateObjectiveMarkers(float deltaTime)
    {
        HUDManager.updateTaskMarkerPositions(deltaTime); //NOTE: I don't feel great about forcing an update through a non-ticking class.
    }

    private bool checkDialogueLineCondition(tokenCondition condition)
    {
        if (condition.tokenName == "")
            return true;
        else
            return TokenRegistry.testToken(condition);
    }

    private void pickupItem(PickupItem pickupInfo)
    {
        if (pickupInfo.consumeOnPickup == false)
        {            
            _inventory.addItem(pickupInfo.inventoryItemName, pickupInfo.inventoryItemImage, pickupInfo.itemAmount);
        }               
        foreach (EventPackage ep in pickupInfo.EventsSentOnPickup)
            EventRegistry.SendEvent(ep,pickupInfo.gameObject);

        GameObject.Destroy(pickupInfo.gameObject);
    }

    public void forceRead(ReadableObject ro)
    {
        readableInfo = ro;
        ro.isEnabled = true;
        read();
    }

    private void read()
    {
        if (!readableInfo.isEnabled)
            return;
        isReading = true;
        GameManager.pause();
        readingCanvas.enabled = true;
        if (exitPromptText != null)
            exitPromptText.SetActive(true);
        readingTitleUIText.text = readableInfo.titleText;
        readingTitleUIText.color = readableInfo.titleFontColor;
        readingBodyUIText.text = readableInfo.bodyText;
        readingBodyUIText.fontSize = readableInfo.bodyFontSize;
        readingBodyUIText.color = readableInfo.bodyFontColor;
        readingBackground.color = readableInfo.backgroundColor;
        readingBodyUIText.supportRichText = true;
        if (readableInfo.isFullScreen)
            readingBackground.transform.localScale = new Vector3(10, 10, 10);


        if (readableInfo.EventsToSendOnRead.Count != 0)
        {            
            foreach (EventPackage ep in readableInfo.EventsToSendOnRead)
                EventRegistry.SendEvent(ep,readableInfo.gameObject);
        }
        if (readableInfo.onlyTriggerOnce)
            readableInfo.disableThis("nothing",null);
    }

    public void startDialogue(DialogueScriptableObject dialogue)
    {
        if (dialogue == null)
            return;


        HUDManager.currentDialogue = dialogue;
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);
            dialogueCanvas.enabled = true;
            if (dialogue.isBlocking)
            {
                isInDialogue = true;
                GameManager.pause();
            }
            dialogueCanvas.GetComponent<DialogueManager>().Init();
        }
    }
    
    

    void OnCollisionEnter(Collision collision)
    {

        PickupItem pi = collision.gameObject.GetComponent<PickupItem>();
        if (pi != null)
        {
            if (pi.pickUpOnCollision)
            {
                pickupItem(pi);
            }
        }
    }
}
