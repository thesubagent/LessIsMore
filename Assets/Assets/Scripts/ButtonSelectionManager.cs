using UnityEngine;

public class UIButtonSelectionManager : MonoBehaviour
{
    public static UIButtonSelectionManager Instance { get; private set; }

    private UIButtonHoverSelectEffect selectedButton;

    private void Awake()
    {
        Instance = this;
    }

    public void SelectButton(UIButtonHoverSelectEffect button)
    {
        if (selectedButton == button)
            return;

        if (selectedButton != null)
            selectedButton.Deselect();

        selectedButton = button;
        selectedButton.Select();
    }
}