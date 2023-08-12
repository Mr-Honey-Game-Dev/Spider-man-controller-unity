using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public Action swingStarted;
    public Action swingEnded;
    public bool isSwinging {  get; private set; }
    public Vector2 moveDirection;
    public float rotHorizontal;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        TakeInput();    
    }

    void TakeInput() 
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isSwinging = true;
            swingStarted?.Invoke(); 
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isSwinging = false;
            swingEnded?.Invoke(); 
        }

        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        moveDirection = new Vector2(horizontal, vertical);

        rotHorizontal = Input.GetAxis("Mouse X");

    }
}
