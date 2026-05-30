using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text slotText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private LayoutElement layoutElement;

    [Header("Visual")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color fixedColor = new Color(1f, 0.95f, 0.65f, 1f);
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.15f);

    private Transform _originalParent;
    private int _originalSiblingIndex;
    private GameObject _placeholder;

    private CardHandView _owner;
    private int _handIndex;
    private string _cardId;
    private CardDefinition _def;

    private RectTransform _rect;
    private Canvas _rootCanvas;

    private Vector2 _startAnchoredPos;
    private Vector3 _startScale;
    private int _startSiblingIndex;
    private bool _isDragging;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        _rootCanvas = GetComponentInParent<Canvas>();
        _startScale = Vector3.one;
    }

    private void EnsureRefs()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Bind(CardHandView owner, int handIndex, string cardId, CardDefinition def)
    {
        EnsureRefs();

        if (_rect == null)
        {
            Debug.LogError("[CardItemView] RectTransform reference is missing.");
            return;
        }

        _owner = owner;
        _handIndex = handIndex;
        _cardId = cardId;
        _def = def;

        transform.localScale = Vector3.one;
        _rect.anchoredPosition = Vector2.zero;

        if (slotText != null)
        {
            slotText.text = handIndex == NetworkPlayer.FixedHandIndex ? "0\nBASIC" : handIndex.ToString();
        }

        if (_def == null)
        {
            if (costText != null) costText.text = "-";
            if (nameText != null) nameText.text = handIndex == NetworkPlayer.FixedHandIndex ? "¶Ñ¹÷¶Ñ¹÷" : "(ºó ½½·Ô)";
            if (descText != null) descText.text = handIndex == NetworkPlayer.FixedHandIndex ? "°íÁ¤ Ä«µå ½½·Ô" : "";
            if (stateText != null) stateText.text = "";
            if (background != null) background.color = handIndex == NetworkPlayer.FixedHandIndex ? fixedColor : emptyColor;
        }
        else
        {
            if (costText != null) costText.text = _def.Cost.ToString();
            if (nameText != null) nameText.text = _def.CardName;
            if (descText != null) descText.text = _def.Description;
            if (stateText != null) stateText.text = _def.EffectType.ToString();
            if (background != null) background.color = handIndex == NetworkPlayer.FixedHandIndex ? fixedColor : normalColor;
        }

        RefreshState();
    }

    public void RefreshState()
    {
        if (canvasGroup == null)
            return;

        if (_def == null)
        {
            canvasGroup.alpha = _handIndex == NetworkPlayer.FixedHandIndex ? 0.45f : 0.25f;
            return;
        }

        bool canUse = _owner != null && _owner.CanUseCard(_handIndex, _def);
        canvasGroup.alpha = canUse ? 1f : 0.55f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_def == null || _isDragging)
            return;

        _owner?.ShowDetail(_def, _handIndex);
        MoveToHoverLayer();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isDragging)
            return;

        RestoreToHandRoot();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_def == null || _owner == null)
            return;

        if (!_owner.CanUseCard(_handIndex, _def))
            return;

        _isDragging = true;
        _startAnchoredPos = _rect.anchoredPosition;

        MoveToHoverLayer();

        if (layoutElement != null)
            layoutElement.ignoreLayout = true;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
        _rect.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        float movedY = _rect.anchoredPosition.y - _startAnchoredPos.y;
        bool shouldUse = movedY >= _owner.UseThresholdY;

        if (shouldUse)
        {
            _owner.TryUseCard(_handIndex);
        }

        RestoreCardVisual();
    }

    private void RestoreCardVisual()
    {
        _isDragging = false;

        if (layoutElement != null)
            layoutElement.ignoreLayout = false;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        RestoreToHandRoot();
        RefreshState();
    }

    private void MoveToHoverLayer()
    {
        if (_owner == null)
            return;

        if (_originalParent == null)
            _originalParent = transform.parent;

        _originalSiblingIndex = transform.GetSiblingIndex();

        CreatePlaceholder();

        transform.SetParent(_owner.HoverRoot, true);
        transform.SetAsLastSibling();
        transform.localScale = Vector3.one * hoverScale;
    }

    private void RestoreToHandRoot()
    {
        if (_originalParent == null)
            return;

        int restoreIndex = _originalSiblingIndex;

        if (_placeholder != null)
            restoreIndex = _placeholder.transform.GetSiblingIndex();

        transform.SetParent(_originalParent, true);
        transform.SetSiblingIndex(restoreIndex);
        transform.localScale = Vector3.one;
        _rect.anchoredPosition = Vector2.zero;

        RemovePlaceholder();
    }

    private void CreatePlaceholder()
    {
        if (_placeholder != null || _originalParent == null)
            return;

        _placeholder = new GameObject($"{name}_Placeholder", typeof(RectTransform), typeof(LayoutElement));
        _placeholder.transform.SetParent(_originalParent, false);
        _placeholder.transform.SetSiblingIndex(_originalSiblingIndex);

        RectTransform placeholderRect = _placeholder.GetComponent<RectTransform>();
        LayoutElement placeholderLayout = _placeholder.GetComponent<LayoutElement>();

        if (layoutElement != null)
        {
            placeholderLayout.preferredWidth = layoutElement.preferredWidth;
            placeholderLayout.preferredHeight = layoutElement.preferredHeight;
            placeholderLayout.minWidth = layoutElement.minWidth;
            placeholderLayout.minHeight = layoutElement.minHeight;
            placeholderLayout.flexibleWidth = layoutElement.flexibleWidth;
            placeholderLayout.flexibleHeight = layoutElement.flexibleHeight;
        }
        else
        {
            placeholderLayout.preferredWidth = _rect.rect.width;
            placeholderLayout.preferredHeight = _rect.rect.height;
        }

        placeholderRect.localScale = Vector3.one;
    }

    private void RemovePlaceholder()
    {
        if (_placeholder != null)
        {
            Destroy(_placeholder);
            _placeholder = null;
        }
    }
}