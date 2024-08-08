using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


public enum SUIT
{
    DIAMOND,
    HEART,
    SPADE,
    CLOVER,
    MAX
}

public enum NUMBER
{
    ACE,
    TWO,
    THREE,
    FOUR,
    FIVE,
    SIX,
    SEVEN,
    EIGHT,
    NINE,
    TEN,
    JACK,
    QUEEN,
    KING,
    MAX
}

public enum STACKTYPE
{
    TOP,
    BOTTOM,
    DECK,
    MAX
}

[System.Serializable]
public class Card
{
    public SUIT suit;
    public NUMBER number;
    public Sprite sprite;

    private GameObject m_go;
    private SpriteRenderer m_ren;
    private EventListener m_listener;
    private Sprite m_hiddenSpr;
    private int m_stackID;
    private STACKTYPE m_stackType;
    private int m_cardID;
    private int m_sortOrder;

    private Vector3 m_oPos;
    private bool isHidden = false;

    public void Init(SpriteRenderer renderer, Sprite hidden, int cardID)
    {
        m_ren = renderer;
        m_go = renderer.gameObject;
        m_hiddenSpr = hidden;
        m_ren.sprite = sprite;
        m_cardID = cardID;
        isHidden = false;
        m_listener = m_go.AddComponent<EventListener>();
    }

    public GameObject gameObject
    {
        get { return m_go; }
    }
    
    public SpriteRenderer renderer
    {
        get { return m_ren; }
    }

    public EventListener events
    {
        get { return m_listener; }
    }

    public Vector3 position
    {
        get { return m_go.transform.position; }
        set { m_go.transform.position = value; }
    }

    public Vector3 oPos
    {
        get { return m_oPos; }
        set { m_oPos = value; }
    }

    public int stackID
    {
        get { return m_stackID; }
        set { m_stackID = value; }
    }
    
    public int cardID
    {
        get { return m_cardID; }
    }
    
    public STACKTYPE stackType
    {
        get { return m_stackType; }
        set { m_stackType = value; }
    }

    public int sortOrder
    {
        get { return m_sortOrder; }
        set
        {
            m_ren.sortingOrder = value;
            m_sortOrder = value;
        }
    }

    public void Disable()
    {
        Debug.Log("Disabled");
        m_go.layer = 0;
    }

    public void Enable()
    {
        Debug.Log("Enabled");
        m_go.layer = isHidden ? 0 : LayerMask.NameToLayer("Card");
    }

    public void Hide()
    {
        isHidden = true;
        m_ren.sprite = m_hiddenSpr;
        Disable();
    }

    public void Show()
    {
        isHidden = false;
        m_ren.sprite = sprite;
        Enable();
    }

    public bool CheckStackRules(Card card)
    {
        switch (stackType)
        {
            case STACKTYPE.TOP:
                return card.suit == suit && card.number == number + 1;
                break;
            case STACKTYPE.BOTTOM:
                Debug.Log(suit + "|" + number + " vs " + card.suit + "|" + card.number + " -1: " + (number - 1));
                if (suit == SUIT.HEART || suit == SUIT.DIAMOND)
                    return (card.suit == SUIT.SPADE || card.suit == SUIT.CLOVER) && card.number == number - 1;
                else
                    return (card.suit == SUIT.HEART || card.suit == SUIT.DIAMOND) && card.number == number - 1;
                break;
        }
        return false;
    }

    public void BringToFront()
    {
        int count = 1;
        m_ren.sortingOrder = 1000;
        
        foreach (Transform child in m_go.transform)
        {
            child.GetComponent<SpriteRenderer>().sortingOrder = m_ren.sortingOrder + count;
            count++;
        }
        
    }

    public void ReturnToStack()
    {
        Debug.Log("Return To Stack");
        m_go.layer = 0;
        m_go.transform.DOMove(m_oPos, 0.25f).OnComplete(() =>
        {
            m_ren.sortingOrder = m_sortOrder;
            int count = 1;
            foreach (Transform child in m_go.transform)
            {
                child.GetComponent<SpriteRenderer>().sortingOrder = m_ren.sortingOrder + count;
            }
            m_go.layer = LayerMask.NameToLayer("Card");
        });
    }
}
