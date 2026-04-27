using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class POIUIManager : MonoBehaviour
{
    public static POIUIManager Instance { get; private set; }

    [Header("Hint")]
    public GameObject hintPanel;
    public float slideDistance = 400f;
    public float slideDuration = 0.4f;

    [Header("Fact Panel")]
    public GameObject factPanel;
    public Image factImage; 

    private RectTransform hintRect;
    private Vector2 hintTargetPos;
    private Coroutine slideCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        hintRect = hintPanel.GetComponent<RectTransform>();
        hintTargetPos = hintRect.anchoredPosition;
        hintPanel.SetActive(false);
        factPanel.SetActive(false);
    }

    public void ShowHint()
    {
        factPanel.SetActive(false);
        hintPanel.SetActive(true);
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideIn());
    }

    public void ShowFact(Sprite sprite = null)
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        hintPanel.SetActive(false);

        if (factImage != null)
        {
            factImage.sprite = sprite;
            factImage.gameObject.SetActive(sprite != null);
        }

        factPanel.SetActive(true);
    }

    public void HideAll()
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideOut(() =>
        {
            hintPanel.SetActive(false);
            factPanel.SetActive(false);
        }));
    }

    IEnumerator SlideIn()
    {
        Vector2 startPos = hintTargetPos + new Vector2(slideDistance, 0);
        hintRect.anchoredPosition = startPos;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            hintRect.anchoredPosition = Vector2.Lerp(startPos, hintTargetPos, t);
            yield return null;
        }

        hintRect.anchoredPosition = hintTargetPos;
    }

    IEnumerator SlideOut(System.Action onDone)
    {
        if (!hintPanel.activeSelf) { onDone?.Invoke(); yield break; }

        Vector2 endPos = hintTargetPos + new Vector2(slideDistance, 0);
        Vector2 startPos = hintRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            hintRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        onDone?.Invoke();
    }
}