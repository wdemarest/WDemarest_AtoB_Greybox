using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryTabManager : UtilityTabBehavior
{
    [Space(3)]
    [Header("Inventory UI Stuff")]
    public GameObject InventoryTextUIPrefab;
    public GameObject InventoryImageUIPrefab;    
    private List<GameObject> inventoryChildren;
    public Canvas inventoryGridCanvas;
    public GameObject cursor;
    public Text itemNameText;
    public Text itemDescriptionText;
    /*public  Text objectiveDescriptionText;
    public Text objectiveTitleText;
    public Text objectiveTasksText;
    public static string objectiveTitle;
    public static string objectiveDescription;

    public static List<objectiveTaskEntry> taskEntries;*/
    public static InventoryTabManager instance;
    private Inventory inventory;
    private int cursorIndex = 0;

    private static bool isInitialized = false;
    void Awake()
    {
        ObjectInteractionBehavior tempOIB;
        instance = this;
        if (GameManager.player != null)
        {
            if (GameManager.player.TryGetComponent<ObjectInteractionBehavior>(out tempOIB))
            {
                inventory = tempOIB.inventory;
            }
        }        
    }

    public override void display()
    {
        if (inventory == null)
        {       
            ObjectInteractionBehavior tempOIB;        
            if (GameManager.player.TryGetComponent<ObjectInteractionBehavior>(out tempOIB))
            {
                inventory = tempOIB.inventory;
            }            
        }
        if (inventory == null)
            return;

        if (inventoryChildren != null)
            foreach (GameObject go in inventoryChildren)
                Destroy(go);
        inventoryChildren = new List<GameObject>();
        /*int index = 0;
        int xpos, ypos;*/
        foreach (inventoryEntry iE in inventory.getEntries())
        {
            /*xpos = index % inventoryGridWidth;
            ypos = index / inventoryGridWidth;*/
            
            GameObject UISprite = Instantiate(InventoryImageUIPrefab, inventoryGridCanvas.transform);
            GameObject UIName = Instantiate(InventoryTextUIPrefab, UISprite.transform);            
            inventoryChildren.Add(UISprite);
            inventoryChildren.Add(UIName);
            if (iE.count > 1)
                UIName.GetComponent<Text>().text = " X "+iE.count.ToString();
            else
                UIName.GetComponent<Text>().text = "";

            UISprite.GetComponent<Image>().sprite = iE.entrySprite;

            //TODO: make the position not based on hard numbers
            /*UIName.transform.position = new Vector3(inventoryGridCanvas.pixelRect.xMin + (xpos * 100), inventoryGridCanvas.pixelRect.yMax - (ypos * 100), UIName.transform.position.z);
            if(UISprite.GetComponent<Image>() != null)
                UISprite.GetComponent<Image>().transform.position = new Vector3(inventoryGridCanvas.pixelRect.xMin+ (xpos*100), inventoryGridCanvas.pixelRect.yMax - (ypos*100) - 50, UISprite.transform.position.z);*/
          //  index++;
        }
        if (inventoryGridCanvas.transform.childCount > 0)
        {
            cursor.transform.position = inventoryGridCanvas.transform.GetChild(cursorIndex).transform.position; // inventoryChildren
            itemNameText.text = inventory.getEntries()[cursorIndex].entryName;
        }
    }

    public static void init()
    {
        if (isInitialized)
            return;
        isInitialized = true;        
        
    }

    // Update is called once per frame
    void Update()
    {
        int tempInt;

        if(Input.GetKeyDown(KeyCode.W))
        {
            tempInt = cursorIndex - 10;
            if (tempInt >= 0)
            {
                cursorIndex = tempInt;
                cursor.transform.position = inventoryGridCanvas.transform.GetChild(cursorIndex).transform.position;
                itemNameText.text = inventory.getEntries()[cursorIndex].entryName;
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            tempInt = cursorIndex + 10;
            if (tempInt < inventoryGridCanvas.transform.childCount)
            {
                cursorIndex = tempInt;
                cursor.transform.position = inventoryGridCanvas.transform.GetChild(cursorIndex).transform.position;
                itemNameText.text = inventory.getEntries()[cursorIndex].entryName;
            }
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            tempInt = cursorIndex - 1;
            if (tempInt >= 0)
            {
                if((tempInt / 10) == (cursorIndex/10))
                {
                    cursorIndex = tempInt;
                    cursor.transform.position = inventoryGridCanvas.transform.GetChild(cursorIndex).transform.position;
                    itemNameText.text = inventory.getEntries()[cursorIndex].entryName;
                }
                
            }
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            tempInt = cursorIndex + 1;
            if (tempInt < inventoryGridCanvas.transform.childCount)
            {
                if ((tempInt / 10) == (cursorIndex / 10))
                {
                    cursorIndex = tempInt;
                    cursor.transform.position = inventoryGridCanvas.transform.GetChild(cursorIndex).transform.position;
                    itemNameText.text = inventory.getEntries()[cursorIndex].entryName;
                }
                
            }

        }
    }

   
}
