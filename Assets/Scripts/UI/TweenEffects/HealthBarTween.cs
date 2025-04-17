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
            Tween tween =
            transform.DOLocalMoveY(transform.localPosition.y-50, 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            tween.fullPosition = totalDelay;

            totalDelay += sequenceDelay;
    }

    void UpdateSequence()
    {

    }

}
