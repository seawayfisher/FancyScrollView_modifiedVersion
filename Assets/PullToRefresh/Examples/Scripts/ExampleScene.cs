using System.Collections;
using PullToRefresh;
using UnityEngine;

public class ExampleScene : MonoBehaviour
{
    [SerializeField] private UIRefreshControl m_UIRefreshControl;
    
    // Register the callback you want to call to OnRefresh when refresh starts.
    public void OnRefreshCallback()
    {
        Debug.Log("OnRefresh called.");
    }
}
