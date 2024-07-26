using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;


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
        Top,    // 从上往下拉
        Bottom     // 从下往上拉
    }
    [RequireComponent(typeof(ScrollRect))]
    public class UIRefreshControl : UIBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Header("下拉方式")]// TODO  不支持左右刷新
        [SerializeField] private RefreshPullDirection m_pullDirection = RefreshPullDirection.Bottom;
        [Header("控制的scrollRect")]
        [SerializeField] private ScrollRect m_ScrollRect;
        [Header("超出多少像素后松手才触发刷新")]
        [SerializeField] private float m_PullDistanceRequiredRefresh = 150f;
        [Header("超时恢复列表正常表现,小于等于0表示,永远不恢复原状")]
        [SerializeField] private float m_RefreshDuration = 1.5f;
        [Header("刷新动画")]
        [SerializeField] private List<Animator> m_LoadingAnimatorList;
        [SerializeField] public UnityEvent m_OnRefresh = new UnityEvent();
        [FormerlySerializedAs("_activityIndicatorStartLoadingName")]
        [Header("刷新动画")]
        [SerializeField] public string m_activityIndicatorStartLoadingName = "Loading";
        
        private float m_Progress;
        private bool m_IsPulled;
        private bool m_IsRefreshing;

        private bool m_Dragging;
        protected override void Awake()
        {
            base.Awake();
            // (1)支持编辑器拖入组件,(2)留空的话,自动获取本身的scrollRect组件
            m_ScrollRect = m_ScrollRect == null ? this.gameObject.GetComponent<ScrollRect>() : m_ScrollRect; 
            if (m_ScrollRect != null)
            {
                m_ScrollRect.onValueChanged.AddListener(OnScroll);
            }
        }
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
        public UnityEvent OnRefresh
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
            foreach (var animator in this.m_LoadingAnimatorList)
            {
                if (animator != null)
                {
                    animator.SetBool(m_activityIndicatorStartLoadingName, false);
                }
            }
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
        
        /// <summary>
        /// 获取content的y的基准位置
        /// 如果是顶部，那么y应该为0
        /// 如果是底部，那么y应该content的高度减去viewport的高度，但是viewPort的高度是负数，所以应该是加上
        /// </summary>
        /// <returns></returns>
        Vector2 getInitialPosition()
        {
            Vector2 result = Vector2.zero;
            switch (m_pullDirection)
            {
                case RefreshPullDirection.Bottom:
                    result = GetAnchoredPositionWhenAlignBottomEdge(
                        m_ScrollRect.content.GetComponent<RectTransform>(),
                        m_ScrollRect.viewport.GetComponent<RectTransform>());
                    break;
                case RefreshPullDirection.Top:
                    result = GetAnchoredPositionWhenAlignTopEdge(
                        m_ScrollRect.content.GetComponent<RectTransform>(),
                        m_ScrollRect.viewport.GetComponent<RectTransform>()
                    );
                    break;
            }
            
            return result;
        }

        Vector2 GetPositionStop()
        {
            Vector2 result = getInitialPosition();
            switch (m_pullDirection)
            {
                case RefreshPullDirection.Bottom:
                    result.y += m_PullDistanceRequiredRefresh;
                    break;
                case RefreshPullDirection.Top:
                    result.y -= m_PullDistanceRequiredRefresh;
                    break;                
            }

            return result; 
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

            m_ScrollRect.content.anchoredPosition = GetPositionStop();  // 防止回弹
        }
        
        /// <summary>
        /// distance是带正负号的.
        /// </summary>
        /// <returns></returns>
        private float GetDistance()
        {
            var initialPosition = this.getInitialPosition();
            var y = initialPosition.y;
            var contentAnchoredPosition = this.GetContentAnchoredPosition();
            float distance = 0f;
            
            // TODO 后续考虑整合.这个地方写得不太合理,distance应该
            switch (m_pullDirection)
            {
                case RefreshPullDirection.Bottom:
                    distance =  contentAnchoredPosition - initialPosition.y;
                    break;
                case RefreshPullDirection.Top:
                    distance = initialPosition.y - contentAnchoredPosition;
                    break;
            }
            return distance;
        }

        private void OnScroll(Vector2 normalizedPosition)
        {
            var initialPosition = this.getInitialPosition();
            var contentAnchoredPosition = this.GetContentAnchoredPosition();
            // var distance = initialPosition - contentAnchoredPosition;
            // 不满意distance的写法
            var distance = GetDistance();
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
                // m_LoadingAnimator.SetBool(m_activityIndicatorStartLoadingName, true);
                foreach (var animator in m_LoadingAnimatorList)
                {
                    if (animator != null)
                    {
                        animator.SetBool(m_activityIndicatorStartLoadingName, true);
                    }
                }
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
        
        
        /// <summary>
        /// 当子节点的上边缘与父节点的上边缘对齐时，child的anchoredPosition的最终位置
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="parentRectTransform"></param>
        /// <returns></returns>
        Vector2 GetAnchoredPositionWhenAlignTopEdge(RectTransform rectTransform, RectTransform parentRectTransform)
        {

            float distanceToTopEdge = GetDistanceToTopEdge(rectTransform, parentRectTransform);
            // 日志打印这五个变量

            // 更新子节点的位置，使其与父节点的上边缘对齐
            Vector2 newPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + distanceToTopEdge);
            return newPosition;
        }
        /// <summary>
        /// 计算子节点的上边缘与父节点的上边缘之间的距离.带符号，支持方向性
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="parentRectTransform"></param>
        /// <returns></returns>
        float GetDistanceToTopEdge(RectTransform rectTransform, RectTransform parentRectTransform)
        {
            // 计算子节点的上边缘与父节点的上边缘之间的距离
            var childHeight = rectTransform.rect.height;
            var offMinY = rectTransform.offsetMin.y;
            var parentHeight = parentRectTransform.rect.height;
            var anchorBottomY = rectTransform.anchorMin.y * parentHeight;
            // 计算child的上边缘，距离parent上边缘的距离。注意方向性。
            
            float distanceToTopEdge = parentHeight - (anchorBottomY + offMinY + childHeight);
            Debug.Log($"childHeight: {childHeight}, offMinY: {offMinY}, parentHeight: {parentHeight}, anchorBottomY: {anchorBottomY}, distanceToTopEdge: {distanceToTopEdge}");
            return distanceToTopEdge;
        }

        /// <summary>
        /// 当子节点的下边缘与父节点的下边缘对齐时，child的anchoredPosition的最终位置
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="parentRectTransform"></param>
        /// <returns></returns>
        Vector2 GetAnchoredPositionWhenAlignBottomEdge(RectTransform rectTransform, RectTransform parentRectTransform)
        {
            float distanceToTopEdge = GetDistanceToTopEdge(rectTransform, parentRectTransform);
            float childBottomEdgeToParentTopEdge = rectTransform.rect.height + distanceToTopEdge;
            float distanceToBottomEdge = childBottomEdgeToParentTopEdge - parentRectTransform.rect.height;
            return new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + distanceToBottomEdge);
        }
    }
}
