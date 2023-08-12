using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwingPivotManager : MonoBehaviour
{
    public static SwingPivotManager Instance;

    [SerializeField] Transform[] swingPoints;

    private void Awake()
    {
        Instance = this;    
    }

    public Transform GetSwingPivotPoint(Transform player) 
    {
        Vector3 position = player.position;
        position += player.forward * 5;
        position.y-=(transform.position.y - position.y);

        int index = 0;
        float minDis = float.MaxValue;

        for (int i = 0; i < swingPoints.Length; i++) 
        {
            float dis= Vector3.Distance(position, swingPoints[i].position);
            if (dis < minDis) 
            {
                index = i;
                minDis = dis;
            }
        }

        return swingPoints[index];
    }
}
