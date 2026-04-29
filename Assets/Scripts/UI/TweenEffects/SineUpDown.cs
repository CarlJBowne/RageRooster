using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SineUpDown : MonoBehaviour
{

    [SerializeField] float heightDifference;
    void Awake()
    {
        DOTween.Init();
    }
    // Start is called before the first frame update
    void Start()
    {

            transform.DOLocalMoveY(transform.localPosition.y-heightDifference, 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

    }

}
