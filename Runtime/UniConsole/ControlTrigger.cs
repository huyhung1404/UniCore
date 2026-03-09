using UnityEngine;

namespace UniConsole
{
    public enum TriggerResult
    {
        Nothing,
        Request
    }

    public class ControlTrigger
    {
        private int _currentTaps;
        private float _lastTapTime;
        private bool _isPressing;
        private bool _isDrawingCircle;
        private float _pressStartTime;
        private float _accumulatedAngle;
        private Vector2 _circleMin;
        private Vector2 _circleMax;
        private Vector2 _lastTouchPos;
        private readonly TriggerMode _activeMode;
        private readonly int _activeTapCount;
        private readonly float _activeTapTimeout;
        private readonly float _activeLongPress;
        public bool IsOpen { get; set; }
        
        public ControlTrigger(TriggerMode activeMode, int activeTapCount, float activeTapTimeout, float activeLongPress)
        {
            _activeMode = activeMode;
            _activeTapCount = activeTapCount;
            _activeTapTimeout = activeTapTimeout;
            _activeLongPress = activeLongPress;
        }

        public TriggerResult CheckTriggers()
        {
            if (IsOpen || _activeMode == TriggerMode.None) return TriggerResult.Nothing;
            var isTouchDown = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            var isTouchUp = Input.GetMouseButtonUp(0) ||
                            (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled));
            var isTouchMoved = Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved);
            var currentPos = GetInputPos();

            if (_activeMode == TriggerMode.MultiTaps)
            {
                if (!isTouchDown) return TriggerResult.Nothing;

                if (Time.unscaledTime - _lastTapTime > _activeTapTimeout) _currentTaps = 0;

                _currentTaps++;
                _lastTapTime = Time.unscaledTime;

                if (_currentTaps >= _activeTapCount)
                {
                    _currentTaps = 0;
                    return TriggerSuccess();
                }
            }
            else if (_activeMode == TriggerMode.LongPress)
            {
                if (isTouchDown)
                {
                    _isPressing = true;
                    _pressStartTime = Time.unscaledTime;
                }
                else if (isTouchUp) _isPressing = false;

                if (_isPressing && (Time.unscaledTime - _pressStartTime >= _activeLongPress))
                {
                    _isPressing = false;
                    return TriggerSuccess();
                }
            }
            else if (_activeMode == TriggerMode.DrawCircle)
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
            Open();
            return TriggerResult.Request;
        }

        private void Open()
        {
            IsOpen = true;
            _currentTaps = 0;
            _isPressing = false;
            _isDrawingCircle = false;
        }
    }
}