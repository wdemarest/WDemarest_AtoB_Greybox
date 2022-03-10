using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum comparisonOperator { lessThan,lessThanEqual,Equal,greaterThanEqual,greaterThan,notEqual};

[System.Serializable]
public class TokenValuePair
{
    public string tokenName;
    public int value;
}

[System.Serializable]
public class EventRelay_TokenTest : MonoBehaviour
{
    public tokenCondition condition;
    public List<string> eventsToListenFor;
    public List<EventPackage> eventsTosendIfTestSucceeds;
    public List<EventPackage> eventsToSendIfTestFails;

    [Header("Deprecated")]
    public List<string> eventsToExecuteIfTestSucceeds;
    public List<string> eventsToExecuteIfTestFails;

    void Start()
    {
        if (eventsToListenFor.Count > 0)
        {
            foreach (string s in eventsToListenFor)
            {
                if (s != "")
                    EventRegistry.AddEvent(s, testToken, gameObject);
            }
        }
    }

    void testToken(string eventName, GameObject obj)
    {
        if ((obj != null) && (obj != gameObject))
            return;
        switch (condition.comparisonOp)
        {
            case comparisonOperator.Equal:
                executeEvents(TokenRegistry.getToken(condition.tokenName) == condition.value);                    
                break;
            case comparisonOperator.greaterThan:
                executeEvents(TokenRegistry.getToken(condition.tokenName) > condition.value);                    
                break;
            case comparisonOperator.greaterThanEqual:
                executeEvents(TokenRegistry.getToken(condition.tokenName) >= condition.value);                    
                break;
            case comparisonOperator.lessThan:
                executeEvents(TokenRegistry.getToken(condition.tokenName) < condition.value);                  
                break;
            case comparisonOperator.lessThanEqual:
                executeEvents(TokenRegistry.getToken(condition.tokenName) <= condition.value);                    
                break;
            case comparisonOperator.notEqual:
                executeEvents(TokenRegistry.getToken(condition.tokenName) != condition.value);                   
                break;
        }
        
    }

    void executeEvents(bool success)
    {
        if (success)
        {
            foreach (string s in eventsToExecuteIfTestSucceeds)            
                EventRegistry.SendEvent(s);            
            foreach (EventPackage ep in eventsTosendIfTestSucceeds)
                EventRegistry.SendEvent(ep, this.gameObject);
        }
        else
        {
            foreach (string s in eventsToExecuteIfTestFails)
            {
                if (s != "")
                    EventRegistry.SendEvent(s);
            }
            foreach (EventPackage ep in eventsToSendIfTestFails)
                EventRegistry.SendEvent(ep, this.gameObject);
        }

    }


}
