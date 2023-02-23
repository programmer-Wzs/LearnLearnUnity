using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ListView : MonoBehaviour
{
    RectTransform contentRt = null;
    RectTransform viewportRt = null;
    GameObject listViewItemButton = null;
    RectTransform listViewItemButtonRt = null;
    ScrollRect scrollRect = null;

    public int nowIndex = -1;
    int maxItemCount = 0;
    int topPadding = 20;
    public int startIndex = 0;
    public int endIndex;
    //List<GameObject> itemPool = null; // 存储未用到的listViewItemButton
    List<RectTransform> itemRtPool = null;
    List<RectTransform> items = null;
    List<RectTransform> showingItem = null;
    List<System.Object> infoList = null; // 存储Item消息
    public Vector2 nowScrollPosition = Vector2.zero;

    private void Awake()
    {
        contentRt = transform.Find("Viewport").Find("Content").GetComponent<RectTransform>();
        viewportRt = transform.Find("Viewport").GetComponent<RectTransform>();
        listViewItemButton = contentRt.gameObject.transform.Find("ListViewItemButton").gameObject;
        listViewItemButtonRt = listViewItemButton.GetComponent<RectTransform>();
        listViewItemButton.gameObject.SetActive(false);
        scrollRect = GetComponent<ScrollRect>();
        scrollRect.onValueChanged.AddListener(OnChangeValue);

        nowScrollPosition.y = 1;
        showingItem = new List<RectTransform>();
        infoList = new List<System.Object>();
        itemRtPool = new List<RectTransform>();
        items = new List<RectTransform>();
    }


    private void Start()
    {
        for(int i = 0; i < 100; ++i)
        {
            infoList.Add(null);
        }
        UpdateRectSize();
        InitPool();
        InitShow();
    }

    void SetContentSize(float x, float y)
    {
        if (contentRt == null) return;
        contentRt.sizeDelta = new Vector2(x, y);
    }

    /// <summary>
    /// 更新对象池大小：根据可显示范围，与Item大小计算
    /// </summary>
    void UpdateMaxItemCount()
    {
        if (contentRt == null) return;

        maxItemCount = (int)( viewportRt.sizeDelta.y / listViewItemButtonRt.sizeDelta.y) + 2;
        //maxItemCount = 10;
    }


    /// <summary>
    /// 点击Item触发的事件
    /// </summary>
    /// <param name="buttonRt"></param>
    public virtual void ClickItemAction(RectTransform buttonRt)
    {
        // 更新点击的项ItemIndex
        nowIndex = GetItemIndex(buttonRt);


        //Debug.Log( buttonRt.localPosition.y);
    }

    /// <summary>
    /// 消息解析：每个Item的消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="info"></param>
    /// <returns></returns>
    public virtual T GetInfo<T>(Object info)
    {
        T ret = default(T);



        return ret; 
    } 

    /// <summary>
    /// 删除infoList中的一个对象：
    ///  若该对象正在显示：则需要调整显示：更新此对象及往后显示范围内的对象；若删除后，显示的最后一个为无效，则进行回收
    ///  否则，直接删除，并更新contentRt的大小（sizeDelta）
    /// </summary>
    /// <param name="index"></param>
    void DeleteinfoListItem(int index)
    {
        if (index < 0 || index >= infoList.Count)  return;

        if(index>=startIndex && index <= endIndex) // 显示范围内
        {
            // 删除Item数据
            infoList.RemoveAt(index);
            // 更新以下显示
            for(int i = index; i < endIndex && i < infoList.Count; ++i)
            {
                _UpdateItemShow(i);
            }
            // 如果会出现空缺，删除showingItem最后一项，--endIndex
            if(endIndex >= infoList.Count-1) // 会出现空缺
            {
                RectTransform rectTransform = showingItem[showingItem.Count - 1];
                IntoPool(rectTransform);
                showingItem.RemoveAt(showingItem.Count - 1);
                --endIndex;
            }
        }
        else // 非显示范围内：直接删除，并更新contentRt大小即可
        {
            infoList.RemoveAt(index);
        }
        UpdateRectSize();
    }

    /// <summary>
    /// 尾部添加一项：直接更新，若显示区域没有填满，则生成显示
    /// </summary>
    void AddinfoListItem(System.Object info)
    {
        if (infoList == null) infoList = new List<object>() { info };
        else infoList.Add(info);

        UpdateRectSize();

        if (endIndex - startIndex < maxItemCount) // 显示未填满
        {
            RectTransform rectTransform = GetGameObjectByPool();

            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x,
                    showingItem[showingItem.Count-1].anchoredPosition.y - listViewItemButtonRt.sizeDelta.y - topPadding
                );

            rectTransform.gameObject.SetActive(true);
            ++endIndex;
        }
        
    }

    void UpdateRectSize()
    {
        SetContentSize(0, infoList.Count * (topPadding + listViewItemButtonRt.sizeDelta.y));
    }

    int GetItemIndex(RectTransform rectTransform)
    {
        if (null == rectTransform) return -1;
        return -(int)(rectTransform.localPosition.y / (topPadding + listViewItemButtonRt.sizeDelta.y));
    }

    void OnChangeValue(Vector2 v2) // y 1 -> 0
    {
        nowScrollPosition = v2;
        if (startIndex == -1) return;
        // 上移
        if(contentRt.anchoredPosition.y > ((startIndex + 1) ) * (topPadding + listViewItemButtonRt.sizeDelta.y) + topPadding)
        {
            if(startIndex < infoList.Count-1) // 有下一项，将上一项回收
            {
                RectTransform intoGameObject = showingItem[0];
                IntoPool(intoGameObject);
                showingItem.RemoveAt(0);
                ++startIndex;
            }

            if (endIndex < infoList.Count - 1) // 有下一项，生成下一项(位置y=上一项位置y - hight - topPadding)
            {
                RectTransform newRectTransform = GetGameObjectByPool();
                newRectTransform.anchoredPosition = new Vector2(newRectTransform.anchoredPosition.x,
                    showingItem[showingItem.Count - 1].anchoredPosition.y
                                                  - listViewItemButtonRt.sizeDelta.y - topPadding);
                newRectTransform.gameObject.SetActive(true);
                showingItem.Add(newRectTransform);
                ++endIndex;
                _UpdateItemShow(endIndex);
            }
        }
        Debug.Log("UI: " + contentRt.anchoredPosition);
        // 下移 0 1 0 1
        if (contentRt.anchoredPosition.y <= (-showingItem[0].anchoredPosition.y - topPadding))
        {
            if (startIndex > 0) //有上一项，生成上一项, y = 下一项y - topPadding - hight
            {
                RectTransform newRectTransform = GetGameObjectByPool();
                newRectTransform.anchoredPosition = new Vector2(newRectTransform.anchoredPosition.x,
                  showingItem[0].anchoredPosition.y + topPadding + listViewItemButtonRt.sizeDelta.y);
                newRectTransform.gameObject.SetActive(true);
                showingItem.Insert(0, newRectTransform);
                --startIndex;
                _UpdateItemShow(startIndex);
            }

            if (endIndex - startIndex > maxItemCount - 1) // 下一项超过显示范围，则回收
            {
                RectTransform outRectTransform = showingItem[showingItem.Count-1];
                IntoPool(outRectTransform);
                showingItem.RemoveAt(showingItem.Count - 1);
                --endIndex;
            }
        }
    }

    /// <summary>
    /// 初始化：
    /// </summary>
    void InitShow()
    {
        //Debug.Log("infoList : "+infoList.Count);
        if(infoList.Count == 0)
        {
            startIndex = -1;
            endIndex = -1;
        }
        else
        {
            // 生成在可显示内的Item
            startIndex = 0;
            endIndex = -1;
            for(int i = 0; i < 10 && i < infoList.Count; ++i)
            {
                ++endIndex;
                RectTransform rectTransform = GetGameObjectByPool();
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -topPadding*(i+1) - i * listViewItemButtonRt.sizeDelta.y);
                showingItem.Add(rectTransform);
                _UpdateItemShow(i);
                rectTransform.gameObject.SetActive(true);
            }
        }
    }
    #region Item

    /// <summary>
    /// 更新显示
    /// </summary>
    /// <param name="showingListIndex"></param>
    void _UpdateItemShow(int infoListIndex)
    {
        int showingListIndex = infoListIndex - startIndex;
        if(showingListIndex>=0 && showingListIndex < showingItem.Count)
        {
            infoList[infoListIndex] = (System.Object)infoListIndex;
            UpdateItemShow(showingItem[showingListIndex].Find("Text").GetComponent<Text>(), infoList[infoListIndex]);
        }
    }

    /// <summary>
    /// 可定制自己的显示规则
    /// </summary>
    /// <param name="text"></param>
    /// <param name="info"></param>
    protected virtual void UpdateItemShow(Text text, System.Object info)
    {
        text.text = ((int)info).ToString();
    }

    #endregion

    #region about pool 
    /// <summary>
    /// Pool 初始化：根据可显示范围 和 Item大小进行调整
    /// </summary>
    void InitPool()
    {
        UpdateMaxItemCount();
        itemRtPool.Clear();
        for (int i = 0; i < maxItemCount + 6; ++i)
        {
            GameObject newGameObject = Instantiate<GameObject>(listViewItemButton);
            itemRtPool.Add(newGameObject.GetComponent<RectTransform>());
            items.Add(itemRtPool[itemRtPool.Count - 1]);
            int j = i;
            newGameObject.GetComponent<Button>().onClick.AddListener(() => {
                ClickItemAction(items[j]);
            });
            newGameObject.transform.SetParent(contentRt, false);
            newGameObject.SetActive(false);
        }
    }

    // using AA = KeyValuePair<GameObject, RectTransform>();
    RectTransform GetGameObjectByPool()
    {
        RectTransform ret = null;

        if(itemRtPool == null || itemRtPool.Count == 0)
        {
            ret = Instantiate<GameObject>(listViewItemButton).GetComponent<RectTransform>();
            ret.SetParent(contentRt, false);
            items.Add(ret);
            int index = items.Count - 1;
            ret.gameObject.GetComponent<Button>().onClick.AddListener(()=> { ClickItemAction(items[index]); });

        }
        else
        {
            ret = itemRtPool[0];
            itemRtPool.RemoveAt(0);
        }
        return ret;
    }

    void IntoPool(RectTransform intoRectGameObject)
    {
        if (itemRtPool == null)
        {
            //itemPool = new List<GameObject>() { intoGameObject };
            itemRtPool = new List<RectTransform>() { intoRectGameObject };
        }
        else 
        {
            //itemPool.Add(intoGameObject);
            itemRtPool.Add(intoRectGameObject);
        }
        intoRectGameObject.transform.SetParent(contentRt, false);
        intoRectGameObject.gameObject.SetActive(false);
    }


    #endregion
}
