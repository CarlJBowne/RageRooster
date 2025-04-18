using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using DG.Tweening;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class TweenButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler , ISelectHandler
{
    
    [SerializeField] bool doRotate = true;
    Selectable selectable;
    void Awake()
    {
        DOTween.Init();
        selectable = GetComponent<Selectable>();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Hovering over button");
        BounceButton();

    }

    public void OnPointerExit(PointerEventData eventData)
    {       
        //Sequence s = DOTween.Sequence();
        //s.AppendInterval(.5f);
        //transform.localRotation = Quaternion.Euler(Vector3.one);
        //s.Append(transform.DORotate(Vector3.one, .5f, RotateMode.Fast));
    }

    void BounceButton()
    {        
        if(selectable.interactable)
        {
        Sequence sequence = DOTween.Sequence();
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        transform.localScale = Vector3.one;
        if(doRotate)
        {
            sequence.Append(transform.DORotate(new Vector3(1,1,-25), .4f).From().SetEase(Ease.OutBack));
        }

        sequence.Join(transform.DOScale(.6f, 1f).From().SetEase(Ease.OutBounce));
        }

    }

    public void OnSelect(BaseEventData eventData)
    {
        BounceButton();
    }
}
