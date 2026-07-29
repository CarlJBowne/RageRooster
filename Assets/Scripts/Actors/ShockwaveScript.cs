using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.ObjectUtilities;

public class ShockwaveScript : MonoBehaviour
{
    public Transform target;
    public float expandRate;
    public float maxTime;

    private new Transform transform;
    private float time = 0;
    private Vector3 scale = new(1, 0, 1);

    private void Awake()
    {
        if (target == null) target = Player.Transform;
        transform = base.transform;
    }

    private void FixedUpdate()
    {
        transform.eulerAngles = (target.position - transform.position).XZ().DirToRot();
        transform.localScale += expandRate * Time.fixedDeltaTime * scale;
        time += Time.fixedDeltaTime;
        if(time > maxTime)
        {
            time = 0;
            Spawnable.DestroyOrDisable(gameObject);
        }
    }
}
