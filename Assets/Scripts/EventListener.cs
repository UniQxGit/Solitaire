using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class EventListener : MonoBehaviour
{
    public UnityEvent onDrop = new UnityEvent();
    public UnityEvent onBeginDrag = new UnityEvent();
    public UnityEvent onClick = new UnityEvent();
    public void OnDrop()
    {
        onDrop.Invoke();
    }

    public void OnBeginDrag()
    {
        onBeginDrag.Invoke();
    }

    public void OnClick()
    {
        onClick.Invoke();
    }
}
