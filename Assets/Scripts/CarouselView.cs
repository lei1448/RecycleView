using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class CarouselView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform[] slots = new RectTransform[7]; 
    public ListItemView itemPrefab;
    public float pixelPerUnit = 100f;
    [Range(0.8f, 0.99f)]
    public float damping = 0.95f; //阻力
    public float snapSpeed = 10f; //吸附速度
    public float minFlingVelocity = 1f;
    public float maxFlingVelocity = 20f;
    private List<ListItemData> allData;
    private ListItemView[] itemViews = new ListItemView[7];
    

    private float currentScrollPosition = 0f;
    private float velocity = 0f; //惯性
    private bool isDragging = false;
    private int currentCenterDataIndex = 0; 

    private List<float> velocityHistory = new();

    void Start()
    {
        if (slots.Length != 7 || itemViews.Length != 7)
        {
            Debug.LogError("检查是否放置了七个slot");
            this.enabled = false;
            return;
        }
        for (int i = 0; i < 7; i++) 
        {
            ListItemView view = Instantiate(itemPrefab, transform);
            view.gameObject.name = $"ItemView_Instance_{i}";
            
            RectTransform rt = view.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            view.OnItemClicked += HandleItemClick;
            itemViews[i] = view;
        }

        //测试
        List<ListItemData> testData = new List<ListItemData>();
        for (int i = 0; i < 1000; i++)
        {
            testData.Add(new ListItemData { id = i, name = $"Item {i}" });
        }
        SetData(testData);
    }

    public void SetData(List<ListItemData> data)
    {
        allData = data;

        bool canScroll = allData.Count > 5;
        this.enabled = canScroll;

        int startIndex = 0;
        if (!canScroll && allData.Count > 0)
        {
            startIndex = (allData.Count - 1) / 2;//数据量不够一屏时让中间元素居中
        }

        //设置列表
        ScrollToIndex(startIndex, false);
    }

    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        velocity = 0;
        velocityHistory.Clear();
        DOTween.Kill(this, "ScrollTween"); 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float delta = eventData.delta.x / pixelPerUnit;
        currentScrollPosition -= delta;
        
        float frameVelocity = -delta / Time.deltaTime;
        velocityHistory.Add(frameVelocity);
        if (velocityHistory.Count > 10) 
        {
            velocityHistory.RemoveAt(0);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        if (velocityHistory.Count > 0)
        {
            velocity = velocityHistory.Average();
        }
        velocityHistory.Clear();
        
        velocity = Mathf.Clamp(velocity, -maxFlingVelocity, maxFlingVelocity);
        
        if (Mathf.Abs(velocity) < minFlingVelocity)
        {
            velocity = 0;
        }
    }
    

    void Update()
    {
        if (!this.enabled) return; 
        

        if(!isDragging)
        {
            float minPos = 0;
            float maxPos = (allData != null) ? allData.Count - 1 : 0;
            
            if (currentScrollPosition < minPos || currentScrollPosition > maxPos)//超出边界
            {
                velocity = 0;
                float targetPos = (currentScrollPosition < minPos) ? minPos : maxPos;
                
                currentScrollPosition = Mathf.Lerp(currentScrollPosition, targetPos, Time.deltaTime * snapSpeed);
                
                if (Mathf.Abs(currentScrollPosition - targetPos) < 0.01f)
                {
                    currentScrollPosition = targetPos;
                }
            }
            else if (Mathf.Abs(velocity) > 0.01f)//未出界且有惯性
            {
                currentScrollPosition += velocity * Time.deltaTime;
                velocity *= damping; //模拟阻力
            }
            else//检查停止后最终位置并吸附
            {
                velocity = 0;
                int targetIndex = Mathf.RoundToInt(currentScrollPosition);
                
                if (Mathf.Abs(currentScrollPosition - targetIndex) > 0.01f)
                {
                    currentScrollPosition = Mathf.Lerp(currentScrollPosition, targetIndex, Time.deltaTime * snapSpeed);
                }
                else
                {
                    currentScrollPosition = targetIndex;
                }
            }
        }

        if (allData != null)
        {
            UpdateItemVisuals();
        }
    }

    private void UpdateItemVisuals()
    {
        int newCenterDataIndex = Mathf.RoundToInt(currentScrollPosition);
        if (newCenterDataIndex != currentCenterDataIndex)//每帧检测位置更新数据
        {
            currentCenterDataIndex = newCenterDataIndex;
            UpdateAllViewsData();
        }

        float fraction = currentScrollPosition - newCenterDataIndex;
        
        for (int i = 0; i < 7; i++) 
        {
            ListItemView view = itemViews[i];
            
            
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
            int dataIndex = currentCenterDataIndex + (i - 3); 
            
            ListItemView view = itemViews[i];

            if (dataIndex < 0 || dataIndex >= allData.Count)
            {
                view.gameObject.SetActive(false);
            }
            else
            {
                view.gameObject.SetActive(true);
                view.UpdateView(allData[dataIndex]);
            }
        }
    }
    
    private void HandleItemClick(ListItemData data)
    {
        Debug.Log($"Clicked: ID={data.id}, Name={data.name}");
        
        int dataIndex = allData.FindIndex(d => d.id == data.id);
        if (dataIndex != -1 && dataIndex != currentCenterDataIndex)
        {
            ScrollToIndex(dataIndex, true);
        }
    }

    public void ScrollToIndex(int index, bool animated)
    {
        if (index < 0 || index >= allData.Count) return;
        
        DOTween.Kill(this, "ScrollTween"); 
        isDragging = false;
        velocity = 0;

        if (animated)
        {
            DOTween.To(() => currentScrollPosition, 
                       x => currentScrollPosition = x, 
                       index, 
                       1.0f)
                .SetTarget(this)
                .SetId("ScrollTween")
                .SetEase(Ease.OutCubic);
        }
        else
        {
            currentScrollPosition = index;
            currentCenterDataIndex = Mathf.RoundToInt(currentScrollPosition);
            UpdateAllViewsData(); // 立即刷新数据
            UpdateItemVisuals();  // 立即刷新位置
        }
    }
    
    public void UpdateData(int index, ListItemData newData)
    {
        if (index < 0 || index >= allData.Count) return;

        allData[index] = newData;

        int minVisible = currentCenterDataIndex - 3; 
        int maxVisible = currentCenterDataIndex + 3; 

        if (index >= minVisible && index <= maxVisible)
        {
            for(int i=0; i < 7; i++)
            {
                int viewDataIndex = currentCenterDataIndex + (i - 3);
                if (viewDataIndex == index)
                {
                    itemViews[i].UpdateView(newData);
                    break;
                }
            }
        }
    }
}