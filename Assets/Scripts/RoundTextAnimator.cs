using System.Collections;
using TMPro;
using UnityEngine;

// If using TextMeshPro

public class RoundTextAnimator : MonoBehaviour
{
    public float slideInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float displayDuration = 1.0f;

    public TextMeshProUGUI text;
    private CanvasGroup canvasGroup;
    private Vector3 centerPosition;

    private RectTransform rectTransform;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = new Vector3(-Screen.width, 0, 0); // Start off-screen left
        centerPosition = Vector3.zero; // Center position relative to parent
        // Start the animation sequence
    }

    public void ShowRound(string roundText)
    {
        text.SetText(roundText);
        foreach (Transform child in transform) StartCoroutine(AnimateText(child));
    }

    private IEnumerator AnimateText(Transform child)
    {
        // Ensure each child has a CanvasGroup component for opacity control
        var canvasGroup = child.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = child.gameObject.AddComponent<CanvasGroup>();

        var rectTransform = child.GetComponent<RectTransform>();

        // Set initial positions and opacity
        rectTransform.anchoredPosition = startPosition;
        canvasGroup.alpha = 0;

        // Slide in
        var elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, centerPosition, elapsed / slideInDuration);
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / slideInDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = centerPosition;
        canvasGroup.alpha = 1;

        // Wait for the display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0;
    }
}