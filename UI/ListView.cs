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
    //List<GameObject> itemPool = null; // �洢δ�õ���listViewItemButton
    List<RectTransform> itemRtPool = null;
    List<RectTransform> items = null;
    List<RectTransform> showingItem = null;
    List<System.Object> infoList = null; // �洢Item��Ϣ
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
    /// ���¶���ش�С�����ݿ���ʾ��Χ����Item��С����
    /// </summary>
    void UpdateMaxItemCount()
    {
        if (contentRt == null) return;

        maxItemCount = (int)( viewportRt.sizeDelta.y / listViewItemButtonRt.sizeDelta.y) + 2;
        //maxItemCount = 10;
    }


    /// <summary>
    /// ���Item�������¼�
    /// </summary>
    /// <param name="buttonRt"></param>
    public virtual void ClickItemAction(RectTransform buttonRt)
    {
        // ���µ������ItemIndex
        nowIndex = GetItemIndex(buttonRt);


        //Debug.Log( buttonRt.localPosition.y);
    }

    /// <summary>
    /// ��Ϣ������ÿ��Item����Ϣ
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
    /// ɾ��infoList�е�һ������
    ///  ���ö���������ʾ������Ҫ������ʾ�����´˶���������ʾ��Χ�ڵĶ�����ɾ������ʾ�����һ��Ϊ��Ч������л���
    ///  ����ֱ��ɾ����������contentRt�Ĵ�С��sizeDelta��
    /// </summary>
    /// <param name="index"></param>
    void DeleteinfoListItem(int index)
    {
        if (index < 0 || index >= infoList.Count)  return;

        if(index>=startIndex && index <= endIndex) // ��ʾ��Χ��
        {
            // ɾ��Item����
            infoList.RemoveAt(index);
            // ����������ʾ
            for(int i = index; i < endIndex && i < infoList.Count; ++i)
            {
                _UpdateItemShow(i);
            }
            // �������ֿ�ȱ��ɾ��showingItem���һ�--endIndex
            if(endIndex >= infoList.Count-1) // ����ֿ�ȱ
            {
                RectTransform rectTransform = showingItem[showingItem.Count - 1];
                IntoPool(rectTransform);
                showingItem.RemoveAt(showingItem.Count - 1);
                --endIndex;
            }
        }
        else // ����ʾ��Χ�ڣ�ֱ��ɾ����������contentRt��С����
        {
            infoList.RemoveAt(index);
        }
        UpdateRectSize();
    }

    /// <summary>
    /// β�����һ�ֱ�Ӹ��£�����ʾ����û����������������ʾ
    /// </summary>
    void AddinfoListItem(System.Object info)
    {
        if (infoList == null) infoList = new List<object>() { info };
        else infoList.Add(info);

        UpdateRectSize();

        if (endIndex - startIndex < maxItemCount) // ��ʾδ����
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
        // ����
        if(contentRt.anchoredPosition.y > ((startIndex + 1) ) * (topPadding + listViewItemButtonRt.sizeDelta.y) + topPadding)
        {
            if(startIndex < infoList.Count-1) // ����һ�����һ�����
            {
                RectTransform intoGameObject = showingItem[0];
                IntoPool(intoGameObject);
                showingItem.RemoveAt(0);
                ++startIndex;
            }

            if (endIndex < infoList.Count - 1) // ����һ�������һ��(λ��y=��һ��λ��y - hight - topPadding)
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
        // ���� 0 1 0 1
        if (contentRt.anchoredPosition.y <= (-showingItem[0].anchoredPosition.y - topPadding))
        {
            if (startIndex > 0) //����һ�������һ��, y = ��һ��y - topPadding - hight
            {
                RectTransform newRectTransform = GetGameObjectByPool();
                newRectTransform.anchoredPosition = new Vector2(newRectTransform.anchoredPosition.x,
                  showingItem[0].anchoredPosition.y + topPadding + listViewItemButtonRt.sizeDelta.y);
                newRectTransform.gameObject.SetActive(true);
                showingItem.Insert(0, newRectTransform);
                --startIndex;
                _UpdateItemShow(startIndex);
            }

            if (endIndex - startIndex > maxItemCount - 1) // ��һ�����ʾ��Χ�������
            {
                RectTransform outRectTransform = showingItem[showingItem.Count-1];
                IntoPool(outRectTransform);
                showingItem.RemoveAt(showingItem.Count - 1);
                --endIndex;
            }
        }
    }

    /// <summary>
    /// ��ʼ����
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
            // �����ڿ���ʾ�ڵ�Item
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
    /// ������ʾ
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
    /// �ɶ����Լ�����ʾ����
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
    /// Pool ��ʼ�������ݿ���ʾ��Χ �� Item��С���е���
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
