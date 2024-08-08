using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class CardManager : MonoBehaviour
{
    [SerializeField] Card[] cards;

#if UNITY_EDITOR
    [Button("Populate Cards List")]
    private void PopulateCards()
    {
        this.cards = new Card[(int)SUIT.MAX * (int)NUMBER.MAX];

        for (int i = 0; i < (int)SUIT.MAX; i++)
        {
            string suitName = "";
            switch ((SUIT)i)
            {
                case SUIT.HEART:
                    suitName = "Heart";
                    break;
                case SUIT.SPADE:
                    suitName = "Spade";
                    break;
                case SUIT.CLOVER:
                    suitName = "Club";
                    break;
                case SUIT.DIAMOND:
                    suitName = "Diamond";
                    break;
            }

            for (int j = 0; j < (int)NUMBER.MAX; j++)
            {
                Card card = new Card();
                Sprite spr =  (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Playing Cards/Image/PlayingCards/" + suitName + ((j + 1) < 10 ? "0" + (j + 1) : (j + 1)) + ".png", typeof(Sprite));
                card.sprite = spr;
                card.suit = (SUIT)i;
                card.number = (NUMBER)j;
                
                Debug.Log(suitName + (j+1 < 10 ? "0" + (j + 1) : (j + 1)) + ".png");

                cards[j + (int)NUMBER.MAX * i] = card;
            }
        }
    }
#endif
    
    public Sprite cardBackSprite;
    public SpriteRenderer cardPrefab;
    public EventListener deckEventListener;
    
    public Transform deckPos;
    public Transform[] spaces;
    public Transform[] topSpaces;
    public Transform deckSpace;

    public Vector2 offset = new Vector2(0.2f, 0.2f);
    
    public Stack<int> deck = new Stack<int>();

    private Stack<int>[][] stacks = new Stack<int>[3][];
    private Transform[][] stackTransforms = new Transform[3][];

    void Start()
    {
        InitializeDeck();
        ShuffleDeck();
        
        NewGame();
        deckEventListener.onClick.AddListener(() =>
        {
            DeckDeal();
            
        });
    }

    private void SwapCard(int[] cards, int a, int b)
    {
        (cards[a], cards[b]) = (cards[b], cards[a]);
    }

    void InitializeDeck()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            deck.Push(i);
        }

        stacks[(int)STACKTYPE.TOP] = new Stack<int>[4];
        stacks[(int)STACKTYPE.BOTTOM] = new Stack<int>[7];
        stacks[(int)STACKTYPE.DECK] = new Stack<int>[1];

        stackTransforms[(int)STACKTYPE.TOP] = topSpaces;
        stackTransforms[(int)STACKTYPE.BOTTOM] = spaces;
        stackTransforms[(int)STACKTYPE.DECK] = new []{deckSpace};
        
        foreach(Stack<int>[] stack in stacks)
        {
            for (int i = 0; i < stack.Length; i++)
            {
                stack[i] = new Stack<int>();
            }
        }
    }

    void ShuffleDeck()
    {
        int totalCards = deck.Count;
        int[] tmp = deck.ToArray();
        for (int i = 0; i < totalCards; i++)
        {
            int rand = i + (Random.Range(0, totalCards - i));
            rand %= totalCards;
            SwapCard(tmp, i, rand);
        }
        
        deck.Clear();
        for (int i = 0; i < totalCards; i++)
        {
            deck.Push(tmp[i]);
        } 
    }

    void DeckDeal()
    {
        if (deck.Count == 0)
        {
            Sequence mySequence = DOTween.Sequence();
            
            deckPos.gameObject.layer = 0;
            foreach (int cardIndex in stacks[(int)STACKTYPE.DECK][0])
            {
                deck.Push(cardIndex);
                Card card = cards[cardIndex];
                card.Disable();
                card.gameObject.transform.parent = null;
                card.gameObject.transform.DOMove(deckPos.position, 1).OnComplete(() =>
                {
                    GameObject.Destroy(card.gameObject);
                });
            }

            mySequence.AppendInterval(1.5f);
            mySequence.OnComplete(() =>
            {
                deckPos.gameObject.layer = LayerMask.NameToLayer("Card");
            });

            mySequence.Play();
            
            stacks[(int)STACKTYPE.DECK][0].Clear();
            Debug.LogWarning("Deck was Empty when dealt");
            return;
        }
        
        Deal(STACKTYPE.DECK, 0, false);
    }

    [Button("Deal")]
    void Deal(STACKTYPE stackType, int stackID, bool hidden, Card card = null)
    {
        if (card == null)
        {
            if (deck.Count == 0)
            {
                Debug.LogWarning("Deck was Empty when dealt");
                return;
            }
            
            int cardIndex = deck.Pop();
            int stackTypeIndex = (int)stackType;
            Card cardToDeal = cards[cardIndex];
            
            cardToDeal.Init(GameObject.Instantiate<SpriteRenderer>(cardPrefab, deckPos.position, Quaternion.identity), cardBackSprite, cardIndex);
            cardToDeal.events.onDrop.AddListener(() =>
            {
                Drop(cardIndex);
            });
            
            cardToDeal.events.onBeginDrag.AddListener(() =>
            {
                cardToDeal.BringToFront();
            });
            
            cardToDeal.events.onClick.AddListener(() =>
            {
                MoveToFirstValidMove(cardToDeal);
            });
            
            if (hidden)
                cardToDeal.Hide();
            else
            {
                cardToDeal.Show();
            }
            
            MoveToStack(cardToDeal, stackTypeIndex, stackID);
        }
        else
        {
            Stack<int> cardStack = stacks[(int)card.stackType][card.stackID];

            List<int> cardsToDeal = new List<int>();
            
            int cardIndex = 0;
            do
            {
                cardIndex = cardStack.Pop();
                cardsToDeal.Add(cardIndex);
            } while (cardStack.Count > 0 && card.cardID != cardIndex);

            for (int i = cardsToDeal.Count - 1; i >= 0; i--)
            {
                int stackTypeIndex = (int)stackType;
                Card cardToDeal = cards[cardsToDeal[i]];
                
                MoveToStack(cardToDeal, stackTypeIndex, stackID);
            }

            if (cardStack.Count > 0)
            {
                cards[cardStack.Peek()].Show();
            }
        }
    }

    private void MoveToStack(Card card, int stackIndex, int stackID)
    {
        if (stacks[stackIndex][stackID].Count > 0)
        {
            Card lastCard = cards[stacks[stackIndex][stackID].Peek()];
            
            if ((STACKTYPE)stackIndex == STACKTYPE.DECK)
                lastCard.Disable();
            else
            {
                card.gameObject.transform.SetParent(lastCard.gameObject.transform,true);
            }
        }
        else
        {
            card.gameObject.transform.parent = null;
        }
        
        card.sortOrder = stacks[stackIndex][stackID].Count + 1;
        card.stackID = stackID;
        card.stackType = (STACKTYPE)stackIndex;

        Vector3 off = Vector3.zero;
        switch (card.stackType)
        {
            case STACKTYPE.TOP:
                off = Vector2.zero;
                break;
            case STACKTYPE.BOTTOM:
                off = Vector2.down * offset.y * stacks[stackIndex][stackID].Count;
                break;
            case STACKTYPE.DECK:
                
                off = Vector2.right * offset.x * Mathf.Min(stacks[stackIndex][stackID].Count, 2);
                break;
        }

        Sequence mySequence = DOTween.Sequence();
        card.Disable();
        card.gameObject.transform.DOMove(stackTransforms[stackIndex][stackID].position + off + (Vector3.back * 0.05f * stacks[stackIndex][stackID].Count), 1).OnComplete(
            () =>
            {
                card.oPos = card.position;
            });
        mySequence.AppendInterval(1);
        mySequence.OnComplete(() =>
        {
            card.Enable();
        });
        
        stacks[stackIndex][stackID].Push(card.cardID);
    }
    
    private int[] GetNearestStack(int cardID)
    {
        Card card = cards[cardID];

        float closest = Mathf.Infinity;

        int[] stack = {-1,-1};
        //Check Top Spaces
        for (int i = 0; i < topSpaces.Length; i++)
        {
            float dist = Vector2.Distance(topSpaces[i].position, card.gameObject.transform.position);
            Debug.Log("TOP: " + dist);
            if (dist < closest && dist < 1)
            {
                stack[0] = (int)STACKTYPE.TOP;
                int tmp = i;
                stack[1] = tmp;
                closest = dist;
                Debug.Log("STACK FOUND: " + stack);
            }
                
        }
        
        //Check Bottom Spaces
        //Check Top Spaces
        for (int i = 0; i < spaces.Length; i++)
        {
            Vector2 referencePos = stacks[(int)STACKTYPE.BOTTOM][i].Count > 0 && stacks[(int)STACKTYPE.BOTTOM][i].Peek() != cardID
                ? cards[stacks[(int)STACKTYPE.BOTTOM][i].Peek()].position
                : spaces[i].position;
            float dist = Vector2.Distance(referencePos, card.gameObject.transform.position);
            
            Debug.Log("BOTTOM: " + dist);
            if (dist < closest && dist < 1)
            {
                stack[0] = (int)STACKTYPE.BOTTOM;
                int tmp = i;
                stack[1] = tmp;
                closest = dist;
            }
        }
        
        return stack;
    }

    void MoveToFirstValidMove(Card card)
    {
        Debug.Log("Checking Available Moves");
        bool foundMove = false;

        for(int i = 0; i < stacks.Length; i++)
        {
            for(int j = 0; j < stacks[i].Length; j++)
            {
                if (i == (int)card.stackType && j == card.stackID){ continue; }
                if (stacks[i][j].Count == 0)
                {
                    if (card.number == NUMBER.KING && (STACKTYPE)i == STACKTYPE.BOTTOM)
                    {
                        Deal(STACKTYPE.BOTTOM, j, false, card);
                        foundMove = true;
                        break;
                    }
                    
                    if (card.number == NUMBER.ACE && (STACKTYPE)i == STACKTYPE.TOP)
                    {
                        Deal(STACKTYPE.TOP, j, false, card);
                        foundMove = true;
                        break;
                    }
                }
                else
                {
                    Card stackCard = cards[stacks[i][j].Peek()];
                    if (stackCard.CheckStackRules(card))
                    {
                        Deal(stackCard.stackType, stackCard.stackID, false, card);
                        foundMove = true;
                        break;
                    }
                }
            }

            if (foundMove)
                break;
        }

        if (!foundMove)
            card.gameObject.transform.DOShakePosition(0.25f, Vector3.right * 0.05f, 100);
    }

    void Drop(int cardID)
    {
        Card card = cards[cardID];
        //Distance check for the nearest stack. 
        int[] obj = GetNearestStack(cardID);
        
        Debug.Log(obj[0] + "|" + obj[1]);
        
        if (obj[0] == -1) { 
            card.ReturnToStack();
            return;
        }
        
        Stack<int> stack = stacks[obj[0]][obj[1]];
        
        if (stack.Count > 0)
        {
            Card stackCard = cards[stack.Peek()];
            if (stackCard.CheckStackRules(card))
            {
                Deal(stackCard.stackType, stackCard.stackID, false, card);
            }
            else
            {
                card.ReturnToStack();
            }
        }
        else if (card.number == NUMBER.KING && (STACKTYPE)obj[0] == STACKTYPE.BOTTOM)
        {
            Deal(STACKTYPE.BOTTOM, obj[1], false, card);
        }
        else if (card.number == NUMBER.ACE && (STACKTYPE)obj[0] == STACKTYPE.TOP)
        {
            Deal(STACKTYPE.TOP, obj[1], false, card);
        }
        else
        {
            card.ReturnToStack();
        }
        
        //Check Victory
        if (CheckVictory())
        {
            NewGame();
        }
    }

    public bool CheckVictory()
    {
        bool isVictory = true;

        foreach (Stack<int> stack in stacks[(int)STACKTYPE.TOP])
        {
            if (stack.Count < 13)
            {
                isVictory = false;
                break;
            }
        }
        
        return isVictory;
    }
    
    [Button("NewGame")]
    void NewGame()
    {
        Sequence mySequence = DOTween.Sequence();

        foreach (Stack<int>[] list in stacks)
        {
            foreach (Stack<int> stack in list)
            {
                foreach (int cardIndex in stack)
                {
                    deck.Push(cardIndex);
                    Card card = cards[cardIndex];
                    card.Disable();
                    card.gameObject.transform.DOMove(deckPos.position, 1).OnComplete(() =>
                    {
                        GameObject.Destroy(card.gameObject);
                    });
                }
                
                stack.Clear();
            }
        }

        mySequence.AppendInterval(1.5f);
        mySequence.OnComplete(() =>
        {
            Deal(STACKTYPE.BOTTOM, 0, false);
            
            for (int i = 1; i < 7; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    Deal(STACKTYPE.BOTTOM, i, j < i);
                }
            }
        });
        
    }
}
