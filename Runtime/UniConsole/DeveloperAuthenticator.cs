using System;
using UnityEngine;

namespace UniCore.Console
{
    public enum TriggerResult
    {
        Nothing,
        RequestLogin,
        RequestOpenConsole
    }

    public class DeveloperAuthenticator
    {
        private const string k_devModePrefKey = "UniConsole_IsDeveloperMode";

        private readonly Func<ConsoleSettings> _settingsProvider;
        private bool _isDeveloperMode;
        private bool _isLoginOpen;
        private string _inputPassword = "";
        
        private int _currentTaps;
        private float _lastTapTime;
        private float _pressStartTime;
        private bool _isPressing;

        private bool _isDrawingCircle;
        private float _accumulatedAngle;
        private Vector2 _circleMin;
        private Vector2 _circleMax;
        private Vector2 _lastTouchPos;

        public bool IsDeveloperMode => _isDeveloperMode;
        public bool IsLoginOpen => _isLoginOpen;

        public DeveloperAuthenticator(Func<ConsoleSettings> settingsProvider)
        {
            _settingsProvider = settingsProvider;
            CheckSavedState();
        }

        private void CheckSavedState()
        {
            var savedState = PlayerPrefs.GetInt(k_devModePrefKey, 0);
            if (savedState != 1) return; 

            _isDeveloperMode = true;
        }

        public TriggerResult CheckTriggers()
        {
            if (_isLoginOpen) return TriggerResult.Nothing; 

            var settings = _settingsProvider.Invoke();
            
            var activeMode = !_isDeveloperMode ? settings.m_loginTriggerMode : settings.m_openTriggerMode;
            var activeTapCount = !_isDeveloperMode ? settings.m_loginTapCount : settings.m_openTapCount;
            var activeTapTimeout = !_isDeveloperMode ? settings.m_loginTapTimeout : settings.m_openTapTimeout;
            var activeLongPress = !_isDeveloperMode ? settings.m_loginLongPressDuration : settings.m_openLongPressDuration;

            if (activeMode == TriggerMode.None) return TriggerResult.Nothing;

            var isTouchDown = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            var isTouchUp = Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled));
            var isTouchMoved = Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved);
            var currentPos = GetInputPos();

            if (activeMode == TriggerMode.MultiTaps)
            {
                if (!isTouchDown) return TriggerResult.Nothing;

                if (Time.unscaledTime - _lastTapTime > activeTapTimeout) _currentTaps = 0;
                
                _currentTaps++;
                _lastTapTime = Time.unscaledTime;

                if (_currentTaps >= activeTapCount)
                {
                    _currentTaps = 0;
                    return TriggerSuccess();
                }
            }
            else if (activeMode == TriggerMode.LongPress)
            {
                if (isTouchDown)
                {
                    _isPressing = true;
                    _pressStartTime = Time.unscaledTime;
                }
                else if (isTouchUp) _isPressing = false;

                if (_isPressing && (Time.unscaledTime - _pressStartTime >= activeLongPress))
                {
                    _isPressing = false;
                    return TriggerSuccess();
                }
            }
            else if (activeMode == TriggerMode.DrawCircle)
            {
                if (isTouchDown)
                {
                    _isDrawingCircle = true;
                    _accumulatedAngle = 0f;
                    _circleMin = _circleMax = _lastTouchPos = currentPos;
                    return TriggerResult.Nothing;
                }

                if (!_isDrawingCircle) return TriggerResult.Nothing;

                if (isTouchUp)
                {
                    _isDrawingCircle = false;
                    return TriggerResult.Nothing;
                }

                if (isTouchMoved)
                {
                    _circleMin = Vector2.Min(_circleMin, currentPos);
                    _circleMax = Vector2.Max(_circleMax, currentPos);
                    var center = (_circleMin + _circleMax) * 0.5f;

                    var minRadius = Mathf.Min(Screen.width, Screen.height) * 0.1f;
                    if (Vector2.Distance(_circleMin, _circleMax) > minRadius)
                    {
                        var lastDir = _lastTouchPos - center;
                        var currentDir = currentPos - center;
                        
                        if (lastDir.sqrMagnitude > 1f && currentDir.sqrMagnitude > 1f)
                        {
                            _accumulatedAngle += Vector2.SignedAngle(lastDir, currentDir);
                        }

                        if (Mathf.Abs(_accumulatedAngle) >= 320f) 
                        {
                            _isDrawingCircle = false;
                            return TriggerSuccess();
                        }
                    }
                    _lastTouchPos = currentPos;
                }
            }
            return TriggerResult.Nothing;
        }

        private static Vector2 GetInputPos()
        {
            if (Input.touchCount > 0) return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        private TriggerResult TriggerSuccess()
        {
            if (!_isDeveloperMode)
            {
                OpenLogin();
                return TriggerResult.RequestLogin;
            }
            return TriggerResult.RequestOpenConsole;
        }

        private void OpenLogin()
        {
            _isLoginOpen = true;
            _currentTaps = 0;
            _isPressing = false;
            _isDrawingCircle = false;
            _inputPassword = "";
        }

        public void DrawLoginPanel(float virtualWidth, float virtualHeight, GUIStyle titleStyle)
        {
            if (!_isLoginOpen) return;

            var settings = _settingsProvider.Invoke();
            var rect = new Rect(virtualWidth / 2f - 200, virtualHeight / 2f - 100, 400, 200);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Developer Authentication", titleStyle); 
            
            _inputPassword = GUILayout.PasswordField(_inputPassword, '*', GUILayout.Height(40));
            GUILayout.Space(10);
            
            if (GUILayout.Button("Login", GUILayout.Height(50)))
            {
                if (_inputPassword == settings.m_password)
                {
                    _isDeveloperMode = true;
                    _isLoginOpen = false;
                    PlayerPrefs.SetInt(k_devModePrefKey, 1);
                    PlayerPrefs.Save();
                }
                _inputPassword = "";
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Close", GUILayout.Height(50)))
            {
                _isLoginOpen = false;
            }
            GUILayout.EndArea();
        }

        public void DisableDeveloperMode()
        {
            _isDeveloperMode = false;
            _isLoginOpen = false;
            PlayerPrefs.SetInt(k_devModePrefKey, 0);
            PlayerPrefs.Save();
        }
    }
}