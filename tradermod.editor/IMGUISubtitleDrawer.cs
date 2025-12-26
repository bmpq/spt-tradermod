using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using tarkin.tradermod.shared;
using UnityEngine;

public class IMGUISubtitleDrawer : MonoBehaviour
{
    public TextAsset localeAsset;
    private Dictionary<string, string> _locale;

    public int fontSize = 24;
    public Color textColor = Color.white;
    public float bottomOffset = 50f;
    public float autoHideDuration = 5f;

    private string _currentText = "";
    private float _lastUpdateTime;
    private GUIStyle _textStyle;
    private GUIStyle _shadowStyle;
    private bool _stylesInitialized = false;

    private void Awake()
    {
        _locale = JsonConvert.DeserializeObject<Dictionary<string, string>>(localeAsset.text) ?? new Dictionary<string, string>();
    }

    void OnEnable()
    {
        EFT.AnimationSequencePlayer.SequencePlayer.OnSubtitleChanged += HandleSubtitleChanged;
        SubtitleBehaviour.OnSubtitleChange += HandleSubtitleChanged;
    }

    void OnDisable()
    { 
        EFT.AnimationSequencePlayer.SequencePlayer.OnSubtitleChanged -= HandleSubtitleChanged;
        SubtitleBehaviour.OnSubtitleChange -= HandleSubtitleChanged;
    }

    private void HandleSubtitleChanged(string subtitle)
    {
        if (!string.IsNullOrEmpty(subtitle))
        {
            _locale.TryGetValue(subtitle, out subtitle);
        }

        _currentText = subtitle;
        _lastUpdateTime = Time.time;
    }

    private void InitStyles()
    {
        _textStyle = new GUIStyle(GUI.skin.label);
        _textStyle.alignment = TextAnchor.LowerCenter;
        _textStyle.fontSize = fontSize;
        _textStyle.normal.textColor = textColor;
        _textStyle.wordWrap = true;
        _textStyle.richText = true;

        _shadowStyle = new GUIStyle(_textStyle);
        _shadowStyle.normal.textColor = Color.black;

        _stylesInitialized = true;
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(_currentText)) return;

        if (Time.time - _lastUpdateTime > autoHideDuration) return;

        if (!_stylesInitialized) InitStyles();

        float width = Screen.width * 0.8f;
        float height = 200f;
        float x = (Screen.width - width) / 2f;
        float y = Screen.height - height - bottomOffset;

        Rect contentRect = new Rect(x, y, width, height);

        Rect shadowRect = new Rect(contentRect);
        shadowRect.x += 2f;
        shadowRect.y += 2f;
        GUI.Label(shadowRect, _currentText, _shadowStyle);

        GUI.Label(contentRect, _currentText, _textStyle);
    }
}