using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SineUpDown : MonoBehaviour
{


    void Awake()
    {
        DOTween.Init();
    }
    // Start is called before the first frame update
    void Start()
    {
        Sequence s = DOTween.Sequence();
        s.Append(transform.DOLocalMoveY(-.5f, 1).SetEase(Ease.OutSine));
        s.SetLoops(-1, LoopType.Yoyo);

    }

}
