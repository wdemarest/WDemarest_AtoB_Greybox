using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MovingPlatformTriggerVolumeBehavior : MonoBehaviour
{
    public List<GameObject> movingPlatforms;
    [Header("On Enter Behavior")]
    [FormerlySerializedAs("interactionMode")]
    public moverInteractionModes interactionModeOnEnter;
    [Header("On Exit Behavior")]
    public moverInteractionModes interactionModeOnExit;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<GAME1304PlayerController>() != null)
        {
            foreach (GameObject mp in movingPlatforms)
            {
                if (mp != null)
                {
                    MoverBehavior mb = mp.GetComponent<MoverBehavior>();
                    if (mb != null)
                    {
                        mb.processInteractionInput(interactionModeOnEnter);
                    }
                }
            }
        }
        //base.OnTriggerEnter();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<GAME1304PlayerController>() != null)
        {
            foreach (GameObject mp in movingPlatforms)
            {
                if (mp != null)
                {
                    MoverBehavior mb = mp.GetComponent<MoverBehavior>();
                    if (mb != null)
                    {
                        mb.processInteractionInput(interactionModeOnExit);
                    }
                }
            }
        }
        //base.OnTriggerEnter();
    }
}
