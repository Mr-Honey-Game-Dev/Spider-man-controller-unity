using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public SpiderManController controller;

    public void ShootWebThread() 
    {
        controller.ReleaseThread();
    }
    public void StartSwing()
    {
        controller.StartSwing();
    }
}
