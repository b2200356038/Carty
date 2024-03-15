using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Serialization;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

public class PlayerScript: NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private int moveSpeed;
    [SerializeField] private int rotationSpeed;
    [SerializeField]
    private Vector2 joystickSize = new Vector2(100, 100);
    [SerializeField]
    private FloatingJoystick Joystick;
    private Finger movementFinger;
    private Vector3 playerInput;
    private Vector2 movementAmount;
    private CharacterController characterController;
    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        ETouch.Touch.onFingerDown += HandleFingerDown;
        ETouch.Touch.onFingerUp += HandleLoseFinger;
        ETouch.Touch.onFingerMove += HandleFingerMove;
    }

    private void OnDisable()
    {
        ETouch.Touch.onFingerDown -= HandleFingerDown;
        ETouch.Touch.onFingerUp -= HandleLoseFinger;
        ETouch.Touch.onFingerMove -= HandleFingerMove;
        EnhancedTouchSupport.Disable();
    }

    private void HandleFingerMove(Finger MovedFinger)
    {
        if (MovedFinger == movementFinger)
        {
            Vector2 knobPosition;
            float maxMovement = joystickSize.x / 2f;
            ETouch.Touch currentTouch = MovedFinger.currentTouch;

            if (Vector2.Distance(
                    currentTouch.screenPosition,
                    Joystick.RectTransform.anchoredPosition
                ) > maxMovement)
            {
                knobPosition = (
                    currentTouch.screenPosition - Joystick.RectTransform.anchoredPosition
                    ).normalized * maxMovement;
            }
            else
            {
                knobPosition = currentTouch.screenPosition - Joystick.RectTransform.anchoredPosition;
            }
            Joystick.Knob.anchoredPosition = knobPosition;
            movementAmount = knobPosition / maxMovement;
        }
    }

    private void HandleLoseFinger(Finger LostFinger)
    {
        if (LostFinger == movementFinger)
        {
            movementFinger = null;
            Joystick.Knob.anchoredPosition = Vector2.zero;
            Joystick.gameObject.SetActive(false);
            movementAmount = Vector2.zero;
        }
    }

    private void HandleFingerDown(Finger TouchedFinger)
    {
        if (movementFinger == null && TouchedFinger.screenPosition.x <= Screen.width / 2f)
        {
            movementFinger = TouchedFinger;
            movementAmount = Vector2.zero;
            Joystick.gameObject.SetActive(true);
            Joystick.RectTransform.sizeDelta = joystickSize;
            Joystick.RectTransform.anchoredPosition = ClampStartPosition(TouchedFinger.screenPosition);
        }
    }

    private Vector2 ClampStartPosition(Vector2 StartPosition)
    {
        if (StartPosition.x < joystickSize.x / 2)
        {
            StartPosition.x = joystickSize.x / 2;
        }

        if (StartPosition.y < joystickSize.y / 2)
        {
            StartPosition.y = joystickSize.y / 2;
        }
        else if (StartPosition.y > Screen.height - joystickSize.y / 2)
        {
            StartPosition.y = Screen.height - joystickSize.y / 2;
        }
        return StartPosition;
    }

    void Start()
    {
        Joystick = GameObject.Find("MoveJoystick").GetComponent<FloatingJoystick>();
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(IsOwner)HandleMovement();
    }

    void HandleMovement()
    {
        float hAxis = Input.GetAxis("Horizontal");
        float vAxis = Input.GetAxis("Vertical");
        if (movementAmount != Vector2.zero)
        {
            playerInput.x=movementAmount.x*moveSpeed*Time.deltaTime;
            playerInput.z=movementAmount.y*moveSpeed*Time.deltaTime;
            Quaternion targetRotation= Quaternion.LookRotation(playerInput);
            characterController.Move(playerInput);
            GetComponent<Rigidbody>().MoveRotation(targetRotation);
        }
        
    }
}
