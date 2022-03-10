using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTriggerVolumeBehavior : MonoBehaviour 
{
    public List<GameObject> lights;    

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<GAME1304PlayerController>() != null)
        {
            foreach (GameObject l in lights)
            {
                if (l != null)
                {
                    Light lb = l.GetComponent<Light>();
                    if (lb != null)
                    {
                        lb.enabled = !lb.enabled;
                    }
                }
            }
        }        
    }
}
