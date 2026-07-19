using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SC_DeckManager : MonoBehaviour
{
    public static SC_DeckManager instance { get; private set; }

    public const int max_cards = 3;
    private int m_used_cards = 0;
    public enum CardType { BOOST, LAST_NO_USE }
    [SerializeField] private List<SC_CardData> cards_list;
    private List<SC_CardData> m_draw_pile;

    

    public float m_current_multiplier = 1f;
    private float m_timer_multiplier = 0;

    public SC_CardData m_selected_card;
    private int m_selected_index = 0;

    [Header("References")]
    public SC_Car player;

    [Header("Canvas")]
    public Canvas canvas;
    public Transform cards_parents;
    private List<Image> im_cards_images;
    private List<Image> m_left_images;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }
    void Start()
    {
        m_left_images = new List<Image>();
        im_cards_images = new List<Image>();
        for (int i = 0; i < cards_list.Count && i < max_cards; i++)
        {
            GameObject ob = new GameObject($"Card image {i}");
            Image im = ob.AddComponent<Image>();
            im_cards_images.Add(im);
            m_left_images.Add(im);

            ob.transform.SetParent(cards_parents, false);
        }

        RestoreDrawPile();
    }

    private void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        float nearest_dist = 10000;
        SC_CardData nearest_card = null;
        for (int i = 0; i < m_left_images.Count; i++)
        {
            if (!m_left_images[i].gameObject.activeSelf)
            {
                continue;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_left_images[i].rectTransform,
                mousePos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint
            );

            Rect rect = m_left_images[i].rectTransform.rect;
            float dx = Mathf.Max(rect.xMin - localPoint.x, 0, localPoint.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - localPoint.y, 0, localPoint.y - rect.yMax);

            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            //Debug.Log($"Dist: {dist}");
            if (dist <= nearest_dist)
            {
                nearest_dist = dist;
                
                //Debug.Log($"Selected index: {indx}");
                if (i >= m_draw_pile.Count) break;

                nearest_card = m_draw_pile[i];
                m_selected_index = i;
            }
            im_cards_images[i].rectTransform.sizeDelta = Vector2.one * 100;
        }

        m_selected_card = nearest_card;
        if (m_selected_card != null && m_selected_index > -1)
        {
            m_left_images[m_selected_index].rectTransform.sizeDelta = Vector2.one * 200;
        }

        // Multiplier timer
        if (m_current_multiplier > 1)
        {
            m_timer_multiplier += Time.deltaTime;
            if (m_timer_multiplier > 3)
            {
                m_current_multiplier = 1;
                m_timer_multiplier = 0;
            }
        }
        
    }

    private void RestoreDrawPile()
    {
        m_left_images = new List<Image>(im_cards_images);
        m_draw_pile = new(cards_list);
        //Shuffle(m_draw_pile);
        for (int i = 0; i < m_draw_pile.Count; i++)
        {
            if (m_draw_pile.Count > max_cards)
                m_draw_pile.RemoveAt(m_draw_pile.Count - 1);
        }

        int length = im_cards_images.Count;

        for (int i = 0; i < m_draw_pile.Count && i < max_cards; i++)
        {
            m_left_images[i].gameObject.SetActive(true);
            m_left_images[i].sprite = m_draw_pile[i].card_sprite;
        }
    }
    private void RestoreHand()
    {
        m_left_images = new List<Image>(im_cards_images);
        m_draw_pile = new List<SC_CardData>();
        

        for (int i = m_used_cards; i < cards_list.Count && i < m_used_cards + max_cards; i++)
        {
            m_draw_pile.Add(cards_list[i]);
        }

        int length = im_cards_images.Count;

        for (int i = 0; i < m_draw_pile.Count && i < max_cards; i++)
        {
            m_left_images[i].gameObject.SetActive(true);
            m_left_images[i].sprite = m_draw_pile[i].card_sprite;
        }

        
    }

    public void UseCardInput(InputAction.CallbackContext con)
    {
        if (con.performed)
        {
            if (m_selected_card == null || m_selected_index < 0) return; /// Don't process the code if there isn't a card selected

            // Get the last card
            SC_CardData card = m_selected_card;
            
            //Debug.Log(card);
            if (card.card_effect == null)
            {
                Debug.LogWarning($"Card effect not added to card {card.name}");
                return;
            }

            // Activate the card
            card.card_effect.Activate(player, m_current_multiplier);
            Debug.Log(string.Format("Activated index: {0}", m_selected_index));

            m_left_images[m_selected_index].gameObject.SetActive(false);

            m_left_images.RemoveAt(m_selected_index);
            m_draw_pile.RemoveAt(m_selected_index);

            m_used_cards++;

            // Restore the full pile or only the hand
            if (m_used_cards >= cards_list.Count)
            {
                RestoreDrawPile();
                m_used_cards = 0;
            }
            else if (m_draw_pile.Count <= 0)
                RestoreHand();

            m_selected_card = null;
            m_selected_index = -1;
        }
    }

    public void SetMultiplier(float v)
    {
        if (v < 1) v = 1;
        m_current_multiplier = v;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            (list[i], list[j]) = (list[j], list[i]); // Swaps two elements without the need of a third variable
        }
    }
        
}