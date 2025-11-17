using System;
using UnityEngine;

public class CarouselScrollPhysics
{
    private float m_damping;
    private float m_snapSpeed;
    private float m_minFlingVelocity;
    
    private float m_currentPosition;
    private float m_velocity;
    private bool m_isDragging;
    private float m_minPos;
    private float m_maxPos;

    public event Action<float> OnPositionUpdated;

    public float CurrentPosition
    {
        get => m_currentPosition;
        set
        {
            if (Mathf.Abs(m_currentPosition - value) > 0.0001f)
            {
                m_currentPosition = value;
                OnPositionUpdated?.Invoke(m_currentPosition);
            }
        }
    }
    
    public float Velocity => m_velocity;

    
    public CarouselScrollPhysics(float damping, float snapSpeed, float minFlingVelocity)
    {
        m_damping = damping;
        m_snapSpeed = snapSpeed;
        m_minFlingVelocity = minFlingVelocity;
    }

    public void SetBounds(float minPos, float maxPos)
    {
        m_minPos = minPos;
        m_maxPos = maxPos;
    }

    public void OnBeginDrag()
    {
        m_isDragging = true;
        m_velocity = 0;
    }

    public void OnDrag(float delta)
    {
        if (!m_isDragging) return;
        CurrentPosition += delta;
    }

    public void OnEndDrag(float flingVelocity)
    {
        m_isDragging = false;
        m_velocity = flingVelocity;
        
        if (Mathf.Abs(m_velocity) < m_minFlingVelocity)
        {
            m_velocity = 0;
        }
    }

    public void Update(float deltaTime)
    {
        if (m_isDragging)
        {
            return;
        }
        
        if (m_currentPosition < m_minPos || m_currentPosition > m_maxPos)//惯性移动超出边界时
        {
            m_velocity = 0;
            float targetPos = (m_currentPosition < m_minPos) ? m_minPos : m_maxPos;
            
            CurrentPosition = Mathf.Lerp(CurrentPosition, targetPos, deltaTime * m_snapSpeed);
            
            if (Mathf.Abs(CurrentPosition - targetPos) < 0.01f)
            {
                CurrentPosition = targetPos;
            }
        }
        else if (Mathf.Abs(m_velocity) > 0.01f)//模拟惯性
        {
            CurrentPosition += m_velocity * deltaTime;
            m_velocity *= m_damping; 
        }
        else//吸附到最近的整数索引，防止出现一半卡片
        {
            m_velocity = 0;
            int targetIndex = Mathf.RoundToInt(m_currentPosition);
            
            if (Mathf.Abs(m_currentPosition - targetIndex) > 0.01f)
            {
                CurrentPosition = Mathf.Lerp(CurrentPosition, targetIndex, deltaTime * m_snapSpeed);
            }
            else
            {
                CurrentPosition = targetIndex;
            }
        }
    }
}