using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpawnAndDelete : MonoBehaviour
{
    public GameObject explosionPrefab;
    public void Explode()
    {
        Instantiate(explosionPrefab, this.transform.position, Quaternion.identity);
        this.transform.gameObject.SetActive(false);
    }
}
