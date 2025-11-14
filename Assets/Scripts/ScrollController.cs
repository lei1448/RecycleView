using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class ScrollController : MonoBehaviour
{
    public CarouselView carouselView;

    public TMP_InputField indexInputField;
    public Button goToButton;

    public TMP_InputField updateIdInputField;
    public TMP_InputField updateNameInputField;
    public Button submitBtn;

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

        if(updateIdInputField != null && updateNameInputField != null && submitBtn != null)
        {
            submitBtn.onClick.AddListener(HandleUpdateRequest);
        }
    }

    private void OnInputSubmit(string text)
    {
        HandleScrollRequest();
    }

    public void HandleUpdateRequest()
    {
        string text = updateIdInputField.text;
        if (int.TryParse(text, out int targetIndex))
        {
            Debug.Log($"修改索引: {targetIndex}");
            carouselView.UpdateData(targetIndex,new ListItemData(){ id = targetIndex, name = updateNameInputField.text});
        }
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