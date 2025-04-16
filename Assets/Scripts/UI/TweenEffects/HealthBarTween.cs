using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HealthBarTween : MonoBehaviour
{
        [SerializeField] float sequenceDelay;
        static float totalDelay = 0;
    // Start is called before the first frame update
    void Start()
    {

            transform.DOLocalMoveY(transform.localPosition.y-50, 2f)
                .SetEase(Ease.OutSine)
                .SetDelay(totalDelay)
                .SetLoops(-1, LoopType.Yoyo);

            totalDelay += sequenceDelay;
    }

}
