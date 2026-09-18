using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class UIHint : MonoBehaviour
{
    [SerializeField, Expandable] UIHintSO _uiHintSo;

    [SerializeField] Image _uiBackground;
    [SerializeField] Image _uiHint;

    [SerializeField] InputActionReference _actionReference;

    InputDevice _lastUsedDevice;

    void OnEnable()
    {
        InputSystem.onEvent += OnInputSystemEvent;
        RefreshHint();
    }

    void OnDisable()
    {
        InputSystem.onEvent -= OnInputSystemEvent;
    }

    void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (device == _lastUsedDevice) return;
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

        //TODO Add a potential delay in order to avoid multiple updates
        _lastUsedDevice = device;
        RefreshHint();
    }

    void RefreshHint()
    {
        if (_lastUsedDevice is Touchscreen)
        {
            _uiBackground.enabled = false;
            _uiHint.enabled = false;
            return;
        }
        else
        {
            _uiBackground.enabled = true;
            _uiHint.enabled = true;
        }

        var bindingName = GetBindingNameForCurrentDevice();
        var s = _uiHintSo.GetSpriteForInput(bindingName);
        if(s==null) Debug.LogError($"No binding image for {bindingName}", this);
        _uiHint.sprite = s;
    }

    string GetBindingNameForCurrentDevice()
    {
        var group = GetControlSchemeGroup(_lastUsedDevice);

        return string.IsNullOrEmpty(group)
            ? _actionReference.action.GetBindingDisplayString()
            : _actionReference.action.GetBindingDisplayString(InputBinding.MaskByGroup(group));
    }

    string GetControlSchemeGroup(InputDevice device)
    {
        if (device == null) return null;

        var asset = _actionReference.action.actionMap.asset;
        foreach (var scheme in asset.controlSchemes)
        {
            if (scheme.SupportsDevice(device))
                return scheme.bindingGroup;
        }

        return null;
    }
}
