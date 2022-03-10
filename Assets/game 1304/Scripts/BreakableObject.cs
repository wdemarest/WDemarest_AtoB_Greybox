﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [Header("Mortality")]
    public GameObject brokenVersionPrefab;
    public Vector3 brokenSpawnOffset;
    public Vector3 minForceForBrokenMesh = Vector3.zero;
    public Vector3 maxForceForBrokenMesh = Vector3.zero;
    public int maxHealth = 100;
    private int _currentHealth;
    public bool ragdollOnDeath = true;

    public List<EventPackage> eventsToSendOnBreak;

    [Header("Deprecated")]
    public List<string> eventsToFireOnBreak;
    
    public List<signalEventEntry> damageEvents;
    public List<damageMultiplier> damageTypeMultipliers;

    
    void Start()
    {
        _currentHealth = maxHealth;    
    }    

    public void Damage(int damageAmount, signalTypes damageType)
    {
        //TODO: add health change threshold events here
        
        bool damagePassesEventCheck = false;
        if (_currentHealth <= 0)
            return;
        foreach (damageMultiplier dm in damageTypeMultipliers)
        {
            if (damageType == dm.damageType)
                damageAmount = (int)(damageAmount * dm.multiplier);
        }
        _currentHealth -= damageAmount;
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
                            EventRegistry.SendEvent(ep,this.gameObject);
                    }
                }
            }
        }
        if (_currentHealth <= 0)
        {
            breakThis();   
            
        }
    }

    private void breakThis()
    {
        GameObject brokenObject;

        if (brokenVersionPrefab != null)
        {
            brokenObject = Instantiate(brokenVersionPrefab);
            brokenObject.transform.position = gameObject.transform.position + brokenSpawnOffset;
            for (int i = 0; i < brokenObject.transform.childCount; i++)
            {
                //if (brokenObject.transform.GetChild(i).gameObject.TryGetComponent(out Rigidbody rb)) //TODO: implement when we hit 2019.2
                Rigidbody rb = brokenObject.transform.GetChild(i).GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(Vector3.Lerp(minForceForBrokenMesh, maxForceForBrokenMesh, Random.Range(0f, 1f)));
            }
        }

        GameObject.Destroy(gameObject);

        foreach (string s in eventsToFireOnBreak)        
            EventRegistry.SendEvent(s);
        foreach(EventPackage ep in eventsToSendOnBreak)
            EventRegistry.SendEvent(ep,gameObject);

    }
}
