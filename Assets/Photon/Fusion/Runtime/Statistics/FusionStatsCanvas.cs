namespace Fusion.Statistics {
  using UnityEngine;
  using UnityEngine.Events;
  using UnityEngine.EventSystems;
  using UnityEngine.UI;

  public class FusionStatsCanvas : MonoBehaviour, IDragHandler, IEndDragHandler {
    [Header("General References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasScaler _canvasScaler;
    [SerializeField] private RectTransform _canvasPanel;

    [Space] [Header("Panel References")] 
    [SerializeField] private GameObject _contentPanel;
    [SerializeField] private GameObject _bottomPanel;
    
    [Space] [Header("Misc")] 
    [SerializeField] private Button _hideButton;
    [SerializeField] private Button _closeButton;

    [Space] [Header("World Anchor Panel Settings")] 
    [SerializeField] private FusionStatsConfig _config;
    
    private bool _isColapsed => !_contentPanel.activeSelf;
    
    private Vector2 _canvasPanelOriginPos;

    internal void SetupStatsCanvas(FusionStatistics fusionStatistics, UnityAction closeButtonAction) {
      _canvasPanelOriginPos = _canvasPanel.anchoredPosition;
      
      //Setup buttons
      _closeButton.onClick.RemoveAllListeners();
      _closeButton.onClick.AddListener(closeButtonAction);
      
      _hideButton.onClick.RemoveAllListeners();
      _hideButton.onClick.AddListener(ToggleHide);
      
      // Setup runner statistics ref
      _config.SetupStatisticReference(fusionStatistics);
    }


    public void OnDrag(PointerEventData eventData) {
      _canvasPanel.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData) {
      if (CheckDraggableRectVisibility(_canvasPanel) == false)
        SnapPanelBackToOriginPos();
    }
    
    public void SnapPanelBackToOriginPos() {
      _canvasPanel.anchoredPosition = _canvasPanelOriginPos;
    }
    
    
    internal void ToggleHide() {
      var active = _contentPanel.activeSelf;
      _hideButton.transform.rotation = active ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
      _contentPanel.SetActive(!active);
      _bottomPanel.SetActive(!active);
    }
    
    // Better offscreen check for later.
    private bool CheckDraggableRectVisibility(RectTransform rectTransform) {
      var anchoredPos = rectTransform.anchoredPosition;
      
      if (anchoredPos.x >= rectTransform.rect.width || anchoredPos.x <= -_canvasScaler.referenceResolution.x)
        return false;
      
      if (anchoredPos.y >= (_isColapsed ? 50 : rectTransform.rect.height) || anchoredPos.y <= -_canvasScaler.referenceResolution.y)
        return false;

      return true;
    }
  }
}