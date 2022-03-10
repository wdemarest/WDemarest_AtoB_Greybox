using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSwitchBehavior : InteractiveObject
{
    public List<GameObject> lights;    

    public override void interact()
    {
        base.interact();
        if (UseOnce && _used)
            return;
        if (!isEnabled)
            return;
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