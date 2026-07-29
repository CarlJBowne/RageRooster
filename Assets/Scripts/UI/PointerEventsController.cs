using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PointerEventsController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public UnityEvent PointerEnter;
    public UnityEvent PointerExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnter.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExit.Invoke();
    }


}
