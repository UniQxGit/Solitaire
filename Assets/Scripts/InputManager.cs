using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    [SerializeField] private InputAction mouseDown;
    [SerializeField] private InputAction mouseDrag;
    [SerializeField] private InputAction mouseClick;
    [SerializeField] private float mouseDragSpeed = 0.1f;
    [SerializeField] private float dragTriggerPixelValue = 0.05f;
    
    private Camera cam;
    private Vector2 velocity;

    private GameObject selectedObject;
    private Vector2 dragginStartPos;
    private bool hasBeenDragged;
    
    private void Awake()
    {
        cam = Camera.main;
        dragTriggerPixelValue = Screen.width * dragTriggerPixelValue;
    }
    
    private void OnEnable()
    {
        mouseDown.Enable();
        mouseDown.performed += MousePressed;
        mouseDown.canceled += MouseReleased;
        
        mouseDrag.Enable();
        mouseDrag.performed += MouseDrag;
        
        mouseClick.Enable();
        mouseClick.performed += MouseClicked;
    }

    private void OnDisable()
    {
        mouseDown.performed -= MousePressed;
        mouseDown.canceled -= MouseReleased;
        mouseDown.Disable();
        
        mouseDrag.performed -= MouseDrag;
        mouseDrag.Disable();
        
        mouseClick.Disable();
        mouseClick.performed -= MouseClicked;
    }

    private void MouseClicked(InputAction.CallbackContext context)
    {
        Debug.Log("Mouse CLicked");
        Vector2 inputPos = mouseDrag.ReadValue<Vector2>();
        Vector2 mousePos = cam.ScreenToWorldPoint(inputPos);
        
        
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 100, 1 << LayerMask.NameToLayer("Card"));
        if (hit.collider != null)
        {
            EventListener listener = hit.collider.gameObject.GetComponent<EventListener>();
            if (listener != null)
                listener.OnClick();
        }
    }

    private void MousePressed(InputAction.CallbackContext context)
    {
        if (selectedObject != null) { return; }
        Vector2 inputPos = mouseDrag.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(inputPos);
        Vector2 mousePos = cam.ScreenToWorldPoint(inputPos);
        
        
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 100, 1 << LayerMask.NameToLayer("Card"));
        
        Debug.Log("Mouse Pressed " + hit.point + "|" + mousePos);
        if (hit.collider != null)
        {
            selectedObject = hit.collider.gameObject;
            dragginStartPos = inputPos;
        }
    }

    private void MouseReleased(InputAction.CallbackContext context)
    {
        Debug.Log("Mouse Released ");
        if (selectedObject == null){return;}
    
        EventListener listener = selectedObject.GetComponent<EventListener>();
        if (listener != null)
        {
            if (hasBeenDragged)
                listener.OnDrop();
            else
                listener.OnClick();
        }
        
        selectedObject = null;
        hasBeenDragged = false;
    }

    private void MouseDrag(InputAction.CallbackContext context)
    {
        if (selectedObject == null){return;}
        if (!selectedObject.gameObject.CompareTag("Draggable")){return;}
        float initialDistance = Vector2.Distance(selectedObject.transform.position, cam.transform.position);
        
        Vector2 inputPos = mouseDrag.ReadValue<Vector2>();

        if (!hasBeenDragged && Vector2.Distance(dragginStartPos, inputPos) > dragTriggerPixelValue)
        {
            hasBeenDragged = true;
            EventListener listener = selectedObject.GetComponent<EventListener>();
            if (listener != null)
                listener.OnBeginDrag();
        }

        if (hasBeenDragged)
        {
            Ray ray = cam.ScreenPointToRay(inputPos);
        
            float cacheZ = selectedObject.transform.position.z;
            selectedObject.transform.position = Vector2.SmoothDamp(selectedObject.transform.position,
                ray.GetPoint(initialDistance), ref velocity, mouseDragSpeed);
        
            selectedObject.transform.position = new Vector3(selectedObject.transform.position.x,
                selectedObject.transform.position.y, cacheZ);
        }
    }
}
