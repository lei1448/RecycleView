using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class CarouselView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform[] slots = new RectTransform[7]; 
    public ListItemView itemPrefab;
    public float pixelPerUnit = 100f;
    [Range(0.85f, 0.99f)]
    public float damping = 0.95f;
    public float snapSpeed = 10f;
    public float minFlingVelocity = 1f;
    public float maxFlingVelocity = 20f;
    
    public float velocityThreshold = 5.0f; 
    
    private List<ListItemData> m_allData;
    private ListItemView[] m_itemViews = new ListItemView[7]; 
    private int m_currentCenterDataIndex = 0;
    private bool m_dataIsDirty = false;
    private List<float> m_velocityHistory = new List<float>();
    
    private CarouselScrollPhysics m_scrollPhysics;
    private bool m_isAnimatingScroll = false;

    void Start()
    {
        m_scrollPhysics = new CarouselScrollPhysics(damping, snapSpeed, minFlingVelocity);
        
        m_scrollPhysics.OnPositionUpdated += OnPhysicsPositionUpdated;
        
        for (int i = 0; i < 7; i++)
        {
            ListItemView view = Instantiate(itemPrefab, transform);
            view.gameObject.name = $"ItemView_Instance_{i}";
            
            RectTransform rt = view.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            view.OnItemClicked += HandleItemClick;
            m_itemViews[i] = view;
        }

        List<ListItemData> testData = new List<ListItemData>();
        for (int i = 0; i < 1000; i++)
        {
            testData.Add(new ListItemData { id = i, name = $"Item {i}" });
        }
        SetData(testData);
    }

    public void SetData(List<ListItemData> data)
    {
        m_allData = data;

        bool canScroll = m_allData.Count > 5;
        this.enabled = canScroll; 
        
        m_scrollPhysics.SetBounds(0, m_allData.Count - 1);

        int startIndex = 0;
        if (!canScroll && m_allData.Count > 0)
        {
            startIndex = (m_allData.Count - 1) / 2;
        }

        ScrollToIndex(startIndex, false);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        m_scrollPhysics.OnBeginDrag();
        m_velocityHistory.Clear();
        DOTween.Kill(m_scrollPhysics);
        m_isAnimatingScroll = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = -eventData.delta.x / pixelPerUnit;
        
        m_scrollPhysics.OnDrag(delta);
        
        float frameVelocity = delta / Time.deltaTime;
        m_velocityHistory.Add(frameVelocity);
        if (m_velocityHistory.Count > 10) 
        {
            m_velocityHistory.RemoveAt(0);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float velocity = 0;
        if (m_velocityHistory.Count > 0)
        {
            velocity = m_velocityHistory.Average();
        }
        m_velocityHistory.Clear();
        velocity = Mathf.Clamp(velocity, -maxFlingVelocity, maxFlingVelocity);
        
        m_scrollPhysics.OnEndDrag(velocity);
    }
    
    void Update()
    {
        if (!this.enabled || m_allData == null) return;
        
        m_scrollPhysics.Update(Time.deltaTime);
    }

    private void OnPhysicsPositionUpdated(float currentPosition)
    {
        UpdateItemVisuals(currentPosition);
    }

    private void UpdateItemVisuals(float currentPosition)
    {
        int newCenterDataIndex = Mathf.RoundToInt(currentPosition);
        float currentVelocity = m_scrollPhysics.Velocity;
        
        bool isHighSpeed = m_isAnimatingScroll || (Mathf.Abs(currentVelocity) > velocityThreshold);

        if (isHighSpeed)
        {
            if (newCenterDataIndex != m_currentCenterDataIndex)
            {
                m_dataIsDirty = true;
            }
        }
        else
        {
            if (m_currentCenterDataIndex != newCenterDataIndex || m_dataIsDirty)
            {
                m_currentCenterDataIndex = newCenterDataIndex;
                UpdateAllViewsData();
                m_dataIsDirty = false;
            }
        }
        
        float fraction = currentPosition - newCenterDataIndex;

        for (int i = 0; i < 7; i++) 
        {
            ListItemView view = m_itemViews[i];
            
            RectTransform slotA, slotB;
            float lerpFactor;
            
            if (fraction < 0) 
            {
                slotA = slots[i];
                slotB = (i == 6) ? slots[6] : slots[i + 1]; 
                lerpFactor = -fraction; 
            }
            else 
            {
                slotA = slots[i];
                slotB = (i == 0) ? slots[0] : slots[i - 1]; 
                lerpFactor = fraction; 
            }
            
            RectTransform viewRT = view.GetComponent<RectTransform>();
            viewRT.anchoredPosition = Vector2.Lerp(slotA.anchoredPosition, slotB.anchoredPosition, lerpFactor);
            viewRT.sizeDelta = Vector2.Lerp(slotA.sizeDelta, slotB.sizeDelta, lerpFactor);
            view.transform.localScale = Vector3.Lerp(slotA.localScale, slotB.localScale, lerpFactor);
        }
    }

    private void UpdateAllViewsData()
    {
        for (int i = 0; i < 7; i++)
        {
            int dataIndex = m_currentCenterDataIndex + (i - 3); 
            ListItemView view = m_itemViews[i];

            if (dataIndex < 0 || dataIndex >= m_allData.Count)
            {
                view.gameObject.SetActive(false);
            }
            else
            {
                view.gameObject.SetActive(true);
                view.UpdateView(m_allData[dataIndex]);
            }
        }
    }
    
    private void HandleItemClick(ListItemData data)
    {
        Debug.Log($"Clicked: ID={data.id}, Name={data.name}");
        
        int dataIndex = m_allData.FindIndex(d => d.id == data.id);
        if (dataIndex != -1 && dataIndex != m_currentCenterDataIndex)
        {
            ScrollToIndex(dataIndex, true);
        }
    }

    public void ScrollToIndex(int index, bool animated)
    {
        if (m_allData == null || m_allData.Count == 0) return;
        index = Mathf.Clamp(index, 0, m_allData.Count - 1);
        
        DOTween.Kill(m_scrollPhysics);
        m_isAnimatingScroll = false;

        if (!animated || !this.enabled)
        {
            m_scrollPhysics.CurrentPosition = index; 
            m_currentCenterDataIndex = index;  
            m_dataIsDirty = false;
            UpdateAllViewsData();
            UpdateItemVisuals(index);

      }
        else
        {
            float currentPos = m_scrollPhysics.CurrentPosition;
            float distance = Mathf.Abs(index - currentPos);
            float duration = 1.0f; // 固定的动画时间
            
            // [!!!] V3.6 核心修复: 计算动画速度
            float animationSpeed = distance / duration;

            // 只有当“动画速度”也超过阈值时，才启用高速模式
            if (animationSpeed > velocityThreshold)
            {
                 m_isAnimatingScroll = true;
            }

            DOTween.To(() => m_scrollPhysics.CurrentPosition, 
                       x => m_scrollPhysics.CurrentPosition = x, 
                       index, 
                       duration)
                .SetTarget(m_scrollPhysics)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    m_isAnimatingScroll = false;
                    UpdateItemVisuals(m_scrollPhysics.CurrentPosition);
                });
        }
    }
    
    public void UpdateData(int index, ListItemData newData)
    {
        if (m_allData == null || index < 0 || index >= m_allData.Count) return;

        m_allData[index] = newData;

        int minVisible = m_currentCenterDataIndex - 3;
        int maxVisible = m_currentCenterDataIndex + 3;

        if (index >= minVisible && index <= maxVisible)
        {
            for(int i=0; i < 7; i++) 
            {
                int viewDataIndex = m_currentCenterDataIndex + (i - 3);
                if (viewDataIndex == index)
                {
                    m_itemViews[i].UpdateView(newData);
                    break;
                }
            }
        }
    }
}