using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoScrollCredits : MonoBehaviour
{
    public float scrollSpeed = 20f;
    private RectTransform contentRect;

    private void Start()
    {
        contentRect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        contentRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
    }
}