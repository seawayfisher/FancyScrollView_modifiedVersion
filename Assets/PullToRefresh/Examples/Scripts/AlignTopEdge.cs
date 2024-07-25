using System;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class AlignTopEdge : MonoBehaviour
{
    private void Start()
    {
        AlignTopEdgeWithParent();
    }

    // private void Update()
    // {
    //     AlignTopEdgeWithParent();
    // }

    private void AlignTopEdgeWithParent()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        RectTransform parentRectTransform = rectTransform.parent.GetComponent<RectTransform>();

        // rectTransform.anchoredPosition = GetAnchoredPositionWhenAlignTopEdge(rectTransform, parentRectTransform);
        rectTransform.anchoredPosition = GetAnchoredPositionWhenAlignBottomEdge(rectTransform, parentRectTransform);
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