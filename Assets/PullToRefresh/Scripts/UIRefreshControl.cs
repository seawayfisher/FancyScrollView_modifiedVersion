using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Serialization;


/*
    MIT License

    Copyright (c) 2018 kiepng

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace PullToRefresh
{
    public enum RefreshPullDirection
    {
        FromTopToBottom,    // 从上往下拉
        FromBottomToTop     // 从下往上拉
    }

    public class UIRefreshControl : UIBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Serializable] public class RefreshControlEvent : UnityEvent {}
        
        // todo ,等正式版本
        [Header("下拉方式")]
        [SerializeField] private RefreshPullDirection m_pullDirection = RefreshPullDirection.FromBottomToTop;
        [Header("控制的scrollRect")]
        [SerializeField] private ScrollRect m_ScrollRect;
        [Header("超出多少像素后松手才触发刷新")]
        [SerializeField] private float m_PullDistanceRequiredRefresh = 150f;
        [Header("超时恢复列表正常表现,小于等于0表示,永远不恢复原状")]
        [SerializeField] private float m_RefreshDuration = 1.5f;
        [Header("刷新动画")]
        [SerializeField] private Animator m_LoadingAnimator;
        [SerializeField] public RefreshControlEvent m_OnRefresh = new RefreshControlEvent();
        [FormerlySerializedAs("_activityIndicatorStartLoadingName")]
        [Header("刷新动画")]
        [SerializeField] public string m_activityIndicatorStartLoadingName = "Loading";
        
        private float m_Progress;
        private bool m_IsPulled;
        private bool m_IsRefreshing;

        private bool m_Dragging;
        /// <summary>
        /// Progress until refreshing begins. (0-1)
        /// </summary>
        public float Progress
        {
            get { return m_Progress; }
        }
        private void SetTimerForCloseAnim()
        {
            if (this.m_RefreshDuration <= 0)
            {
                return;
            }

            StartCoroutine(CloseRefreshAnimIfTimeOut());
        }
        
        /// <summary>
        /// 开一个协程,在超时后关闭动画
        /// </summary>
        /// <returns></returns>
        private IEnumerator CloseRefreshAnimIfTimeOut()
        {
            // Instead of data acquisition.
            yield return new WaitForSeconds(m_RefreshDuration);

            // Call EndRefreshing() when refresh is over.
            this.EndRefreshing();
        }

        /// <summary>
        /// Refreshing?
        /// </summary>
        public bool IsRefreshing
        {
            get { return m_IsRefreshing; }
        }

        /// <summary>
        /// Callback executed when refresh started.
        /// </summary>
        public RefreshControlEvent OnRefresh
        {
            get { return m_OnRefresh; }
            set { m_OnRefresh = value; }
        }

         /// <summary>
        /// Call When Refresh is End.
        /// </summary>
        public void EndRefreshing()
        {
            m_IsPulled = false;
            m_IsRefreshing = false;
            m_LoadingAnimator.SetBool(m_activityIndicatorStartLoadingName, false);
        }


        protected override void OnDisable()
        {
            m_Dragging = false;
            base.OnDisable();
        }
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;
            m_Dragging = true;
        }
        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        public bool GetIsDragging()
        {
            return m_Dragging;
        }

        private void Start()
        {
            m_ScrollRect.onValueChanged.AddListener(OnScroll);
        }

        float getInitialPosition()
        {
            var result = m_ScrollRect.content.sizeDelta.y + m_ScrollRect.viewport.rect.y;
            return result;
        }

        Vector2 GetPositionStopAtBottom()
        {
            // return new Vector2(m_ScrollRect.content.anchoredPosition.x, m_InitialPosition - m_PullDistanceRequiredRefresh); 
            var y = m_ScrollRect.content.sizeDelta.y + m_ScrollRect.viewport.rect.y + m_PullDistanceRequiredRefresh;
            var m_PositionStop = new Vector2(m_ScrollRect.content.anchoredPosition.x, y);
            Debug.Log($"停靠位置 m_ScrollRect.content.sizeDelta.y={m_ScrollRect.content.sizeDelta.y}, m_ScrollRect.viewport.rect.y=${m_ScrollRect.viewport.rect.y}, m_PullDistanceRequiredRefresh={m_PullDistanceRequiredRefresh}");
            return m_PositionStop;
        }

        private void LateUpdate()
        {
            if (!m_IsPulled)
            {
                return;
            }

            if (!m_IsRefreshing)
            {
                return;
            }

            m_ScrollRect.content.anchoredPosition = GetPositionStopAtBottom();  // 防止回弹
        }

        private void OnScroll(Vector2 normalizedPosition)
        {
            var initialPosition = this.getInitialPosition();
            var contentAnchoredPosition = this.GetContentAnchoredPosition();
            // var distance = initialPosition - contentAnchoredPosition;
            var distance =  contentAnchoredPosition - initialPosition;
            Debug.Log($"OnScroll, initialPosition={initialPosition}, contentAnchoredPosition={contentAnchoredPosition}, distance={distance}");
            if (distance < 0f)
            {
                return;
            }

            OnPull(distance);
        }

        private void OnPull(float distance)
        {
            if (m_IsRefreshing && Math.Abs(distance) < 1f)
            {
                m_IsRefreshing = false;
            }

            if (m_IsPulled && GetIsDragging())
            {
                return;
            }

            m_Progress = distance / m_PullDistanceRequiredRefresh;

            if (m_Progress < 1f)
            {
                return;
            }

            // Start animation when you reach the required distance while dragging.
            if (GetIsDragging())
            {
                m_IsPulled = true;
                m_LoadingAnimator.SetBool(m_activityIndicatorStartLoadingName, true);
            }

            // ドラッグした状態で必要距離に達したあとに、指を離したらリフレッシュ開始
            if (m_IsPulled && !GetIsDragging())
            {
                m_IsRefreshing = true;
                m_OnRefresh.Invoke();
                SetTimerForCloseAnim();
            }

            m_Progress = 0f;
        }

        private float GetContentAnchoredPosition()
        {
            // 写日志，打印content的size
            Debug.Log($"滚动视图内容的锚定位置y坐标: {m_ScrollRect.content.anchoredPosition.y}，" +
                      $"content_size={m_ScrollRect.content.sizeDelta}, viewPort_size={m_ScrollRect.viewport.rect.size}" );
            return m_ScrollRect.content.anchoredPosition.y;
        }
        
    }
}
