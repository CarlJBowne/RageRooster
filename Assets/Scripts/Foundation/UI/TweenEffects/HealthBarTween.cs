using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HealthBarTween : MonoBehaviour
{

        float sequenceDelay = 0.25f;
        public static float totalDelay = 0;

        public static Tween originalTween;
        public Tween healthBob;
        public float startingY = -80;
        public Vector3 origin;
    // Start is called before the first frame update
    void Awake()
    {
        DOTween.Init();
        UpdateTween();

        origin = transform.localPosition;

    }

    public void UpdateTween()
    {
            totalDelay += sequenceDelay;



            

    }
    public void RestartBob()
    {
        totalDelay = 0;
        originalTween = null;
        UpdateTween();
    }

}
