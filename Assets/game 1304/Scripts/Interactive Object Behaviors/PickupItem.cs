using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickupItem : MonoBehaviour 
{

	public string inventoryItemName;
    public int itemAmount = 1;
	public Sprite inventoryItemImage;

    [SerializeField]
    public List<EventPackage> EventsSentOnPickup;

	public bool consumeOnPickup = false;
    public bool pickUpOnCollision = false;

}
