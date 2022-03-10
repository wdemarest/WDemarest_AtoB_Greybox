using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorTriggerVolumeBehavior : MonoBehaviour
{
    
    public List<ConveyorBehavior> treadmills;
    [Header("On Enter Behavior")]
    public conveyorInteractionModes interactionModeEnter;
    [Header("On Exit Behavior")]
    public conveyorInteractionModes interactionModeExit;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<GAME1304PlayerController>() != null)
        {
            foreach (ConveyorBehavior tb in treadmills)
            {
                if (tb != null)
                {

                    tb.processsInteraction(interactionModeEnter);
                }
            }
        }        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<GAME1304PlayerController>() != null)
        {
            foreach (ConveyorBehavior tb in treadmills)
            {
                if (tb != null)
                {
                    tb.processsInteraction(interactionModeExit);
                }
            }
        }
    }
}
