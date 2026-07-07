using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SC_DeckManager : MonoBehaviour
{
    public static SC_DeckManager instance { get; private set; }
    public enum CardType { BOOST, LAST_NO_USE }
    [SerializeField] private List<SC_CardData> cards_list;
    private List<SC_CardData> m_draw_pile;

    public float m_current_multiplier = 1f;

    [Header("References")]
    public SC_Car player;

    [Header("Canvas")]
    public Canvas canvas;
    public Transform cards_parents;
    private List<Image> im_cards_images;

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
        im_cards_images = new List<Image>();
        for (int i = 0; i < cards_list.Count; i++)
        {
            GameObject ob = new GameObject($"Card image {i}");
            im_cards_images.Add(ob.AddComponent<Image>());

            ob.transform.SetParent(cards_parents, false);
        }

        RestoreDrawPile();
    }

    private void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        for (int i = 0; i < im_cards_images.Count; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                im_cards_images[i].rectTransform,
                mousePos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint
            );

            Rect rect = im_cards_images[i].rectTransform.rect;
            float dx = Mathf.Max(rect.xMin - localPoint.x, 0, localPoint.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - localPoint.y, 0, localPoint.y - rect.yMax);

            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            Debug.Log($"Dist: {dist}");
            if (dist < 10)
            {
                im_cards_images[i].rectTransform.sizeDelta = Vector2.one * 110;
            }
            else
                im_cards_images[i].rectTransform.sizeDelta = Vector2.one * 100;
        }
        
    }

    private void RestoreDrawPile()
    {
        m_draw_pile = new(cards_list);
        Shuffle(m_draw_pile);

        int length = im_cards_images.Count;
        for (int i = 0; i < length && i < m_draw_pile.Count; i++)
        {
            im_cards_images[i].gameObject.SetActive(true);
            im_cards_images[i].sprite = m_draw_pile[i].card_sprite;
        }
    }    

    public void UseCardInput(InputAction.CallbackContext con)
    {
        if (con.performed)
        {
            // Get the last card
            SC_CardData card = m_draw_pile[0];
            m_draw_pile.RemoveAt(0);
            Debug.Log(card);
            if (card.card_effect == null)
            {
                Debug.LogWarning($"Card effect not added to card {card.name}");
                return;
            }

            // Activate the card
            card.card_effect.Activate(player, m_current_multiplier);
                /// Reset the multiplier
            //m_current_multiplier = 1;

            for (int i = 0; i < im_cards_images.Count; i++)
                if (im_cards_images[i].gameObject.activeSelf)
                {
                    im_cards_images[i].gameObject.SetActive(false);
                    break;
                }

            if (m_draw_pile.Count <= 0)
                RestoreDrawPile();
        }
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