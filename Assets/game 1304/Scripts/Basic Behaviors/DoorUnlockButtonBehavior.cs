using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

public enum doorInteractionMode { openDoor,closeDoor,unlockDoor,lockDoor,forceOpenDoor,forceCloseDoor};

public class DoorUnlockButtonBehavior : InteractiveObject
{
    public List<GameObject> doors;
    public doorInteractionMode interactionMode = doorInteractionMode.unlockDoor;

    [Header("Deprecated")]
    public List<DoorknobBehavior> doorKnobs;
    public override void Start()
    {
        base.Start();
        switch (interactionMode)
        {
            case doorInteractionMode.closeDoor:
                interactLabel = "Close Door";
                break;
            case doorInteractionMode.forceCloseDoor:
                interactLabel = "Close Door";
                break;
            case doorInteractionMode.forceOpenDoor:
                interactLabel = "Open Door";
                break;
            case doorInteractionMode.lockDoor:
                interactLabel = "Lock Door";
                break;
            case doorInteractionMode.openDoor:
                interactLabel = "Open Door";
                break;
            case doorInteractionMode.unlockDoor:
                interactLabel = "Unlock Door";
                break;
        }
         
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (doorKnobs.Count > 0)
        {
            switch(interactionMode)
            {
                case doorInteractionMode.closeDoor:
                    Handles.color = Color.blue;
                    break;
                case doorInteractionMode.forceCloseDoor:
                    Handles.color = Color.magenta;
                    break;
                case doorInteractionMode.forceOpenDoor:
                    Handles.color = Color.white;
                    break;
                case doorInteractionMode.lockDoor:
                    Handles.color = Color.red;
                    break;
                case doorInteractionMode.openDoor:
                    Handles.color = Color.yellow;
                    break;
                case doorInteractionMode.unlockDoor:
                    Handles.color = Color.green;
                    break;
            }
            
            foreach(DoorknobBehavior dkb in doorKnobs)
            {
                if(dkb != null)                
                    Handles.DrawDottedLine(transform.position, dkb.gameObject.transform.position, 2.0f);
            }            
            
        }
#endif
    }

    public override void interact()
    {
        DoorknobBehavior dkb;
        base.interact();
        if (UseOnce && _used)
            return;
        if (!isEnabled)
            return;
        //TODO: remove redundant loop in future versions, deprecate old one
        foreach (DoorknobBehavior dkb2 in doorKnobs)
        {
            if (dkb2 != null)
            {
                affectDoor(dkb2, interactionMode);
            }
        }
        foreach(GameObject door in doors)
        {
            dkb = door.GetComponent<DoorknobBehavior>();
            if(dkb==null)
                dkb = door.GetComponentInChildren<DoorknobBehavior>();
            if (dkb != null)
            {
                affectDoor(dkb,interactionMode);                                
            }
        }
    }
    private void affectDoor(DoorknobBehavior dkb,doorInteractionMode interactionMode)
    {
        if (dkb == null)
            return;        
        switch (interactionMode)
        {
            case doorInteractionMode.closeDoor:
                dkb.closeThis("", null);
                break;
            case doorInteractionMode.forceCloseDoor:
                dkb.canOpenWithScriptIfLocked = true;
                dkb.closeThis("", null);
                break;
            case doorInteractionMode.forceOpenDoor:
                dkb.canOpenWithScriptIfLocked = true;
                dkb.openThis("", null);
                break;
            case doorInteractionMode.lockDoor:
                dkb.lockThis("", null);
                break;
            case doorInteractionMode.openDoor:
                dkb.openThis("", null);
                break;
            case doorInteractionMode.unlockDoor:
                dkb.unlockThis("", null);
                break;
        }
    }
}
