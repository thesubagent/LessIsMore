using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverSelectEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private Vector3 hoverRotation = new Vector3(0f, 0f, 5f);

    [Header("Selected")]
    [SerializeField] private bool scaleOnSelect = true;
    [SerializeField] private float selectedScale = 1.2f;

    [SerializeField] private bool moveUpOnSelect = true;
    [SerializeField] private float selectedYOffset = 20f;

    [Header("Selected Behavior")]
    [SerializeField] private bool isSelectableButton = true;

    [Header("Animation")]
    [SerializeField] private float animationSpeed = 10f;

    [SerializeField] private bool scaleOnHover = true;

    private RectTransform rectTransform;

    private Vector3 originalScale;
    private Vector2 originalPosition;
    private Quaternion originalRotation;

    private Vector3 targetScale;
    private Vector2 targetPosition;
    private Quaternion targetRotation;

    private bool isHovered;
    private bool isSelected;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;

        ResetTargets();
    }

    private void Update()
    {
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.deltaTime * animationSpeed);

        rectTransform.localRotation = Quaternion.Lerp(
            rectTransform.localRotation,
            targetRotation,
            Time.deltaTime * animationSpeed);

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (!isSelected)
        {
            targetScale = originalScale * hoverScale;
            targetRotation = originalRotation * Quaternion.Euler(hoverRotation);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!isSelected)
            ResetTargets();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIButtonSelectionManager.Instance.SelectButton(this);
    }

    public void Select()
    {
        isSelected = true;

        if (!isSelectableButton)
            return;

        targetScale = originalScale * selectedScale;
        targetPosition = originalPosition + Vector2.up * selectedYOffset;
        targetRotation = originalRotation;
    }

    public void Deselect()
    {
        isSelected = false;

        if (isHovered)
        {
            targetScale = originalScale * hoverScale;
            targetRotation = originalRotation * Quaternion.Euler(hoverRotation);
            targetPosition = originalPosition;
        }
        else
        {
            ResetTargets();
        }
    }

    private void ResetTargets()
    {
        targetScale = originalScale;
        targetPosition = originalPosition;
        targetRotation = originalRotation;
    }
}