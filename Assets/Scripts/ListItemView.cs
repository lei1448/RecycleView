using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; 

public class ListItemView : MonoBehaviour
{
    public TMP_Text nameText; 
    public TMP_Text idText; 
    
    // public Image iconImage;

    public ListItemData currentData { get; private set; }
    public event Action<ListItemData> OnItemClicked;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(HandleClick);
    }

    public void UpdateView(ListItemData data)
    {
        currentData = data;

        if (nameText != null)
        {
            nameText.text = data.name;
        }
        
        if (idText != null)
        {
            idText.text = "ID: " + data.id.ToString();
        }

        // if (iconImage != null && data.icon != null)
        // {
        //     iconImage.sprite = data.icon;
        // }
    }

    private void HandleClick()
    {
        if (currentData != null)
        {
            OnItemClicked?.Invoke(currentData);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }
}