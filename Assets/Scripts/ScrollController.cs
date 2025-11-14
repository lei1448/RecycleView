using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class ScrollController : MonoBehaviour
{
    public CarouselView carouselView;

    public TMP_InputField indexInputField;
    public Button goToButton;

    void Start()
    {
        if (goToButton != null)
        {
            goToButton.onClick.AddListener(HandleScrollRequest);
        }

        if (indexInputField != null)
        {
            indexInputField.onSubmit.AddListener(OnInputSubmit);
        }
    }

    private void OnInputSubmit(string text)
    {
        HandleScrollRequest();
    }

    public void HandleScrollRequest()
    {
        if (carouselView == null || indexInputField == null)
        {
            return;
        }

        string text = indexInputField.text;

        if (int.TryParse(text, out int targetIndex))
        {
            Debug.Log($"滚动到索引: {targetIndex}");
            carouselView.ScrollToIndex(targetIndex, true); // 带动画滚动
        }
    }

    private void OnDestroy()
    {
        if (goToButton != null)
        {
            goToButton.onClick.RemoveListener(HandleScrollRequest);
        }
        if (indexInputField != null)
        {
            indexInputField.onSubmit.RemoveListener(OnInputSubmit);
        }
    }
}