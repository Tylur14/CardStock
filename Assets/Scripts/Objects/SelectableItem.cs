using System;
using System.Collections.Generic;
using System.Globalization;
using JimJam.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using JimJam.Interface;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SelectableItem : MonoBehaviour, IPointerDownHandler
{
    // Controls
    private Slider _rotationSlider;
    private Button _resetScaleButton, _resetPositionButton, _delete, _colorPickerToggle, _centerX, _centerY, _stretchX, _stretchY, _stretchFill;
    private TMP_InputField _scaleXInput, _scaleYInput, _objectNameInput;
    private Toggle _flipX, _flipY, _lock, _expose;
    
    // Refs
    private Selection _selectionBox;
    private RectTransform _parentRect;
    private RectTransform _rect;
    private Draggable _draggable;
    private ColorPicker _colorPicker;
    
    // Temp Data
    private Vector2 _boundsX;
    private Vector2 _boundsY;
    private Vector2 _resetScale, _resetPosition;
    private bool _colorSelectionLock;

    // Data
    private bool _isFlippedX;
    private bool _isFlippedY;
    private bool _isLocked;
    private int _rotation;
    protected string Color = "#ffffff";
    protected string Name;
    public bool isExposed;
    
    // Events
    [HideInInspector]
    public UnityEvent onSelect;
    public UnityEvent onNameChange;
    public UnityEvent onDelete;
    
    public static SelectableItem SelectedItem;
    
    protected virtual void Awake()
    {
        _parentRect             = transform.parent.GetComponent<RectTransform>();
        _draggable              = GetComponent<Draggable>();
        _rect                    = GetComponent<RectTransform>();
        _resetScale             = _rect.sizeDelta;
        _resetPosition          = _rect.anchoredPosition;
        
        _rotationSlider         = GameObject.FindWithTag("ElementRotationSlider").GetComponent<Slider>();
        _scaleXInput            = GameObject.FindWithTag("SetScaleX").GetComponent<TMP_InputField>();
        _scaleYInput            = GameObject.FindWithTag("SetScaleY").GetComponent<TMP_InputField>();
        _objectNameInput        = GameObject.FindWithTag("ObjectName").GetComponent<TMP_InputField>();
        _resetScaleButton       = GameObject.FindWithTag("ResetScale").GetComponent<Button>();
        _resetPositionButton    = GameObject.FindWithTag("ResetPosition").GetComponent<Button>();
        _delete                 = GameObject.FindWithTag("DeleteElement").GetComponent<Button>();
        _centerX                = GameObject.FindWithTag("CenterX").GetComponent<Button>();
        _centerY                = GameObject.FindWithTag("CenterY").GetComponent<Button>();
        _stretchX               = GameObject.FindWithTag("StretchX").GetComponent<Button>();
        _stretchY               = GameObject.FindWithTag("StretchY").GetComponent<Button>();
        _stretchFill            = GameObject.FindWithTag("StretchFill").GetComponent<Button>();
        _flipX                  = GameObject.FindWithTag("ImageFlipX").GetComponent<Toggle>();
        _flipY                  = GameObject.FindWithTag("ImageFlipY").GetComponent<Toggle>();
        _lock                   = GameObject.FindWithTag("ObjectLock").GetComponent<Toggle>();
        _expose                 = GameObject.FindWithTag("ObjectExpose").GetComponent<Toggle>();
        
        _selectionBox           = FindObjectOfType<Selection>();
        _colorPicker            = GameObject.FindWithTag("ColorPicker").GetComponent<ColorPicker>();
        
        _rotationSlider.minValue = 0;
        _rotationSlider.maxValue = 360;
        
        SetName(this.gameObject.name);
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(1))
            DeselectItem();
    }

    private void FixedUpdate()
    {
        if(SelectedItem==this)
            CheckBounds();
    }

    private void GetBounds()
    {
        var sizeDelta = _parentRect.sizeDelta;
        float horizontal = sizeDelta.x / 2 + sizeDelta.x / 2;
        float vertical = sizeDelta.y / 2 + sizeDelta.y / 2;
        _boundsX.x = -horizontal;
        _boundsX.y = horizontal;
        _boundsY.x = -vertical;
        _boundsY.y = vertical;
        
        SetRotation();
    }
    
    protected virtual void SetRotation()
    {
        _rotationSlider.value = _rect.rotation.eulerAngles.z;
    }

    protected virtual void CheckBounds()
    {
        var localPosition = _rect.localPosition;
        var pos = localPosition;
        pos.x = pos.x < _boundsX.x ? _boundsX.x : pos.x > _boundsX.y ? _boundsX.y : pos.x;
        pos.y = pos.y < _boundsY.x ? _boundsY.x : pos.y > _boundsY.y ? _boundsY.y : pos.y;
        
        _selectionBox.HighlightArea(_rect.sizeDelta,localPosition);
    }

    protected virtual void AssignCallbacks()
    {
        ClearCallbacks();
        
        _scaleXInput.text = _rect.sizeDelta.x.ToString(CultureInfo.CurrentCulture);
        _scaleYInput.text = _rect.sizeDelta.y.ToString(CultureInfo.CurrentCulture);
        _objectNameInput.text = Name;
        _flipX.isOn = _isFlippedX;
        _flipY.isOn = _isFlippedY;
        _lock.isOn  = _isLocked;
        _expose.isOn= isExposed;
         
        _scaleXInput.onValueChanged.AddListener(delegate { ScaleX(_scaleXInput.text); });
        _scaleYInput.onValueChanged.AddListener(delegate { ScaleY(_scaleYInput.text); });
        _objectNameInput.onValueChanged.AddListener(delegate { SetName(_objectNameInput.text); });
        _resetScaleButton.onClick.AddListener(ResetScale);
        _resetPositionButton.onClick.AddListener(ResetPosition);
        _delete.onClick.AddListener(DeleteItem);
        _centerX.onClick.AddListener(delegate { CenterAlignObject(0); });
        _centerY.onClick.AddListener(delegate { CenterAlignObject(1); });
        _stretchX.onClick.AddListener(delegate { Stretch(0); });
        _stretchY.onClick.AddListener(delegate { Stretch(1); });
        _stretchFill.onClick.AddListener(delegate { Stretch(2); });
        _stretchFill.onClick.AddListener(ResetPosition);
        
        _flipX.onValueChanged.AddListener(delegate { ToggleFlipX(_flipX.isOn); });
        _flipY.onValueChanged.AddListener(delegate { ToggleFlipY(_flipY.isOn); });
        _lock.onValueChanged.AddListener(delegate  { ToggleLock(_lock.isOn); });
        _expose.onValueChanged.AddListener(delegate  { ToggleExpose(_expose.isOn); });
        
        _rotationSlider.onValueChanged.AddListener(SelectedItem.Rotate);
        ToggleControls(true);
    }

    protected virtual void ClearCallbacks()
    {
        _rotationSlider.onValueChanged.RemoveAllListeners();
        _scaleXInput.onValueChanged.RemoveAllListeners();
        _scaleYInput.onValueChanged.RemoveAllListeners();
        _objectNameInput.onValueChanged.RemoveAllListeners();
        _resetScaleButton.onClick.RemoveAllListeners();
        _resetPositionButton.onClick.RemoveAllListeners();
        _flipX.onValueChanged.RemoveAllListeners();
        _flipY.onValueChanged.RemoveAllListeners();
        _lock.onValueChanged.RemoveAllListeners();
        _expose.onValueChanged.RemoveAllListeners();
        _delete.onClick.RemoveAllListeners();
        _centerX.onClick.RemoveAllListeners();
        _centerY.onClick.RemoveAllListeners();
        _stretchX.onClick.RemoveAllListeners();
        _stretchY.onClick.RemoveAllListeners();
        _stretchFill.onClick.RemoveAllListeners();
    }

    public virtual Element SaveElement()
    {
        return null;
    }

    protected Element GetBaseData()
    {
        var baseData = new Element();
        var anchoredPosition = _rect.anchoredPosition;
        var sizeDelta = _rect.sizeDelta;
        baseData.Layer = _rect.GetSiblingIndex();
        baseData.PositionX = anchoredPosition.x;
        baseData.PositionY = anchoredPosition.y;
        baseData.ScaleX = sizeDelta.x;
        baseData.ScaleY = sizeDelta.y;
        baseData.Rotation = _rotation;
        baseData.FlippedX = _isFlippedX;
        baseData.FlippedY = _isFlippedY;
        baseData.Locked   = _isLocked;
        baseData.Color = Color;
        baseData.Name = Name;
        baseData.Exposed = isExposed;
        return baseData;
    }
    
    public virtual void LoadElement(Element data)
    {
        Vector2 pos = new Vector2(data.PositionX, data.PositionY);
        Vector2 scale = new Vector2(data.ScaleX, data.ScaleY);
        _rect.anchoredPosition = pos;
        _rect.sizeDelta = scale;
        Color = data.Color;
        Name = data.Name;
        isExposed = data.Exposed;
        ToggleFlipX(data.FlippedX);
        ToggleFlipY(data.FlippedY);
        ToggleLock(data.Locked);
        Rotate(data.Rotation);
        SetName(Name);
    }
    
    public virtual void SelectItem()
    {
        if (SelectedItem == this) return;
        if (SelectedItem != null) SelectedItem.DeselectItem();
        SelectedItem = this;
        ColorUtility.TryParseHtmlString(Color, out var c);
        _colorPicker.color = c;
        
        onSelect.Invoke();
        AssignCallbacks();
        GetBounds();
    }

    public virtual void DeselectItem()
    {
        if (SelectedItem == null) return;
        SelectedItem.ToggleControls(false);
        SelectedItem = null;
        _selectionBox.Reset();
    }

    private void DeleteItem()
    {
        DeselectItem();
        Destroy(this.gameObject);
        
        onDelete.Invoke();
        CardController.instance.recentlySaved = false;
    }
    
    public void ChangeLayer(int dir)
    {
        var index = this.transform.GetSiblingIndex();
        index += dir;
        this.transform.SetSiblingIndex(index);
        
        CardController.instance.recentlySaved = false;
    }

    private void Rotate(float r)
    {
        if (SelectedItem != this) return;
        var rot = _rect.rotation.eulerAngles;
        rot.z = r;
        _rect.rotation = Quaternion.Euler(rot);
        _rotation = (int)rot.z;
        
        CardController.instance.recentlySaved = false;
    }

    private void Stretch(int fill)
    {
        if (fill == 0)
            ScaleX(_parentRect.sizeDelta.x.ToString(CultureInfo.CurrentCulture));
        else if (fill == 1)
            ScaleY(_parentRect.sizeDelta.y.ToString(CultureInfo.CurrentCulture));
        else if (fill == 2)
        {
            ScaleX(_parentRect.sizeDelta.x.ToString(CultureInfo.CurrentCulture));
            ScaleY(_parentRect.sizeDelta.y.ToString(CultureInfo.CurrentCulture));
        }
        _scaleXInput.text = _rect.sizeDelta.x.ToString(CultureInfo.CurrentCulture);
        _scaleYInput.text = _rect.sizeDelta.y.ToString(CultureInfo.CurrentCulture);
        
        CardController.instance.recentlySaved = false;
    }
    
    private void ScaleX(string incomingSizeX)
    {
        if (incomingSizeX == String.Empty) return;
        float s = float.Parse(incomingSizeX);
        if (SelectedItem != this) return;
        var scale = _rect.sizeDelta;
        scale.x = s;
        _rect.sizeDelta = scale;
        GetBounds();
        CardController.instance.recentlySaved = false;
    }
    
    private void ScaleY(string incomingSizeY)
    {
        if (incomingSizeY == String.Empty) return;
        float s = float.Parse(incomingSizeY);
        if (SelectedItem != this) return;
        var scale = _rect.sizeDelta;
        scale.y = s;
        _rect.sizeDelta = scale;
        GetBounds();
        CardController.instance.recentlySaved = false;
    }

    private void CenterAlignObject(int alignment)
    {
        var pos = _rect.anchoredPosition;
        if (alignment == 0)
            pos.x = 0;
        else if (alignment == 1)
            pos.y = 0;
        _rect.anchoredPosition = pos;
        CardController.instance.recentlySaved = false;
    }
    
    private void ToggleFlipX(bool state)
    {
        _isFlippedX = state;
        int mod = _isFlippedX ? -1 : 1;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * mod;
        transform.localScale = s;
        CardController.instance.recentlySaved = false;
    }

    private void ToggleFlipY(bool state)
    {
        _isFlippedY = state;
        int mod = _isFlippedY ? -1 : 1;
        var s = transform.localScale;
        s.y = Mathf.Abs(s.y) * mod;
        transform.localScale = s;
        CardController.instance.recentlySaved = false;
    }

    private void ToggleLock(bool state)
    {
        _draggable.enabled = !(_isLocked = state);
        CardController.instance.recentlySaved = false;
    }

    private void ToggleExpose(bool state)
    {
        isExposed = state;
    }

    public virtual void SetColor(string hexValue)
    {
        CardController.instance.recentlySaved = false;
    }

    public string GetName()
    {
        return Name;
    }
    
    public virtual void SetName(string incName)
    {
        Name = incName;
        onNameChange.Invoke();
    }
    
    private void ResetScale()
    {
        _rect.sizeDelta = _resetScale;
        _scaleXInput.text = _rect.sizeDelta.x.ToString(CultureInfo.CurrentCulture);
        _scaleYInput.text = _rect.sizeDelta.y.ToString(CultureInfo.CurrentCulture);
        CardController.instance.recentlySaved = false;
    }

    private void ResetPosition()
    {
        _rect.anchoredPosition = _resetPosition;
        CardController.instance.recentlySaved = false;
    }

    protected virtual void ToggleControls(bool state)
    {
        _rotationSlider.interactable = state;
        _scaleXInput.interactable = state; 
        _scaleYInput.interactable = state;
        _objectNameInput.interactable = state;
        _resetScaleButton.interactable = state;
        _resetPositionButton.interactable = state;
        _flipX.interactable = state;
        _flipY.interactable = state;
        _lock.interactable = state;
        _expose.interactable = state;
        _delete.interactable = state;
        _centerX.interactable = state; 
        _centerY.interactable = state;
        _stretchX.interactable = state;
        _stretchY.interactable = state;
        _stretchFill.interactable = state;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        SelectItem();
    }
}
