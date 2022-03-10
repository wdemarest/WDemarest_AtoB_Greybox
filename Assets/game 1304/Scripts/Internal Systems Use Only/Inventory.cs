using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class inventoryEntry
{
    public Sprite entrySprite;
    public string entryName;
    private string baseEntryName;
    public int count = 1;


    /*public void updateName()
    {
        //TODO: check for null text component
        if (count > 1)
            UIName.GetComponent<Text>().text = count.ToString() + " X " + entryName;
        else
            UIName.GetComponent<Text>().text = entryName;
    }*/
}

[System.Serializable]
public class Inventory
{
    private List<inventoryEntry> _inventoryEntries;
    public Inventory()
    {
        _inventoryEntries = new List<inventoryEntry>();
    }

    public int GetItemCount(string itemName)
    {
        foreach (inventoryEntry iE in _inventoryEntries)
        {
            if (iE.entryName == itemName)
            {
                return iE.count;
            }
        }
        return 0;
    }

    public bool hasItem(string itemName, int count = 1)
    {
        //TODO: remove redundant code here with the other overloaded versions of this method
        if (GetItemCount(itemName) >= count)
            return true;
        else
            return false;
    }
    public void addItem(inventoryEntry ie)
    {
        _inventoryEntries.Add(ie);
    }

    public void removeItem(string itemName, int count = 1)
    {
        foreach (inventoryEntry iE in _inventoryEntries)
        {
            if (iE.entryName == itemName)
            {
                iE.count -= count;
                if (iE.count == 0)
                {
                   // GameObject.Destroy(iE.UIName);
                   // GameObject.Destroy(iE.UISprite);
                    _inventoryEntries.Remove(iE);
                }
                else
                {
                   // iE.updateName();
                }
            }
        }        
    }

    public void addItem(string itemName, Sprite itemSprite, int count = 1)
    {        
        inventoryEntry tempIE;
        if (hasItem(itemName, out tempIE))
        {
            tempIE.count += 1;
            //tempIE.updateName();
        }
        else
        {
            tempIE = new inventoryEntry();
            tempIE.entryName = itemName;
            tempIE.entrySprite = itemSprite;
            //tempIE.UIName = UIName; // GameObject.Instantiate(InventoryTextUIPrefab, inventoryCanvas.transform);
            //tempIE.UIName.GetComponent<Text>().text = tempIE.entryName;
            //tempIE.UISprite = UISprite; // GameObject.Instantiate(InventoryImageUIPrefab, inventoryCanvas.transform);
            //tempIE.UISprite.GetComponent<Image>().sprite = tempIE.entrySprite;            
            for(int x=0;x<count;x++)
                _inventoryEntries.Add(tempIE);
            
        }        
    }

    public bool hasItem(string itemName)
    {
        inventoryEntry tempIE;
        return hasItem(itemName, out tempIE);
    }

    public bool hasItem(string itemName, out inventoryEntry entry)
    {
        foreach (inventoryEntry iE in _inventoryEntries)
        {
            if (iE.entryName == itemName)
            {
                entry = iE;
                return true;
            }
        }
        entry = null;
        return false;
    }

    public List<inventoryEntry> getEntries()
    {
        return _inventoryEntries;
    }
  
}
