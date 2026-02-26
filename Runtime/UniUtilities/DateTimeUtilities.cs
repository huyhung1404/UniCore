using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniCore.Utilities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TicksAttribute : PropertyAttribute
    {
        public bool IsUTC { get; }

        public TicksAttribute(bool isUTC = false)
        {
            IsUTC = isUTC;
        }
    }

    public static class DateTimeUtilities
    {
        public const string k_Format = "yyyy-MM-dd HH:mm:ss";

        public static DateTime ToDateTime(this long time) => new DateTime(time);

        public static string ToText(this DateTime dt) => dt.ToString(k_Format);

        public static DateTime ToDateTime(this string text) => DateTime.ParseExact(text, k_Format, CultureInfo.InvariantCulture, DateTimeStyles.None);

        public static DateTime StartOfDay(this DateTime dt) => dt.Date;

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            var diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime DayOfWeek(this DateTime dt, DayOfWeek dayOfWeek)
        {
            var dayOfWeekInt = (int)dayOfWeek;
            if (dayOfWeekInt == 0) dayOfWeekInt = 7;
            var dtDayOfWeekInt = (int)dt.DayOfWeek;
            if (dtDayOfWeekInt == 0) dtDayOfWeekInt = 7;
            var daysToAdd = dayOfWeekInt - dtDayOfWeekInt;
            return dt.AddDays(daysToAdd);
        }

        public static double SecondsBetween(this DateTime a, DateTime b) => (a - b).TotalSeconds;

        public static double MinutesBetween(this DateTime a, DateTime b) => (a - b).TotalMinutes;

        public static double HoursBetween(this DateTime a, DateTime b) => (a - b).TotalHours;

        public static double DaysBetween(this DateTime a, DateTime b) => (a - b).TotalDays;

        public static bool IsSameDay(this DateTime a, DateTime b) => a.Date == b.Date;

        #region Count Down

        public class CountDownBuilder
        {
            internal TMP_Text text;
            internal double second;
            internal bool isRealtime = true;
            internal float interval = 1;
            internal Action onCompleted;
            internal ICountDownTick onTick;
        }

        public static CountDownBuilder CountDown(this TMP_Text text, double second)
        {
            return new CountDownBuilder { text = text, second = second };
        }

        public static CountDownBuilder SetUpdate(this CountDownBuilder builder, bool isRealTime)
        {
            builder.isRealtime = isRealTime;
            return builder;
        }

        public static CountDownBuilder SetInterval(this CountDownBuilder builder, float interval)
        {
            builder.interval = interval;
            return builder;
        }

        public static CountDownBuilder OnCompleted(this CountDownBuilder builder, Action onCompleted)
        {
            builder.onCompleted = onCompleted;
            return builder;
        }

        public static CountDownBuilder OnTick(this CountDownBuilder builder, ICountDownTick onTick)
        {
            builder.onTick = onTick;
            return builder;
        }

        public static CountDownBuilder ToDayTime(this CountDownBuilder builder, char separator = ' ')
        {
            builder.onTick = new CountDownTickDayTime(separator);
            return builder;
        }

        public static CountDownBuilder ToDayTimeWithDay(this CountDownBuilder builder, char separator = ' ')
        {
            builder.onTick = new CountDownTickDayTimeWithDay(separator);
            return builder;
        }

        public static CountDown Build(this CountDownBuilder builder)
        {
            return new CountDown(builder.text,
                builder.second,
                builder.isRealtime,
                builder.interval,
                builder.onCompleted,
                builder.onTick ?? new CountDownTickDayTime(' ')
            );
        }

        #endregion
    }

    public interface ICountDownTick
    {
        public void OnTick(TMP_Text text, in TimeSpan remainingTime);

        internal static int WriteNumber(char[] buffer, long value, int index)
        {
            if (value == 0)
            {
                buffer[index] = '0';
                return index + 1;
            }

            var start = index;
            while (value > 0)
            {
                buffer[index++] = (char)('0' + (value % 10));
                value /= 10;
            }

            Array.Reverse(buffer, start, index - start);
            return index;
        }

        internal static void WriteTwoDigits(char[] buffer, int value, int index)
        {
            buffer[index] = (char)('0' + value / 10);
            buffer[index + 1] = (char)('0' + value % 10);
        }
    }

    public class CountDownTickDayTime : ICountDownTick
    {
        private readonly char _separator;
        private readonly char[] _buffer = new char[16];

        public CountDownTickDayTime(char separator)
        {
            _separator = separator;
        }

        public void OnTick(TMP_Text text, in TimeSpan remainingTime)
        {
            int length;

            if (remainingTime.TotalHours >= 1)
            {
                var hours = (long)remainingTime.TotalHours;
                var minutes = remainingTime.Minutes;

                length = ICountDownTick.WriteNumber(_buffer, hours, 0);
                _buffer[length++] = _separator;
                ICountDownTick.WriteTwoDigits(_buffer, minutes, length);
            }
            else
            {
                var minutes = remainingTime.Minutes;
                var seconds = remainingTime.Seconds;

                length = ICountDownTick.WriteNumber(_buffer, minutes, 0);
                _buffer[length++] = _separator;
                ICountDownTick.WriteTwoDigits(_buffer, seconds, length);
            }

            length += 2;

            text.SetCharArray(_buffer, 0, length);
        }
    }

    public class CountDownTickDayTimeWithDay : ICountDownTick
    {
        private readonly char _separator;
        private readonly char[] _buffer = new char[20];

        public CountDownTickDayTimeWithDay(char separator)
        {
            _separator = separator;
        }

        public void OnTick(TMP_Text text, in TimeSpan remainingTime)
        {
            int length;

            if (remainingTime.Days > 0)
            {
                length = ICountDownTick.WriteNumber(_buffer, remainingTime.Days, 0);
                _buffer[length++] = _separator;
                ICountDownTick.WriteTwoDigits(_buffer, remainingTime.Hours, length);
            }
            else if (remainingTime.TotalHours >= 1)
            {
                var hours = (long)remainingTime.TotalHours;
                length = ICountDownTick.WriteNumber(_buffer, hours, 0);
                _buffer[length++] = _separator;
                ICountDownTick.WriteTwoDigits(_buffer, remainingTime.Minutes, length);
            }
            else
            {
                length = ICountDownTick.WriteNumber(_buffer, remainingTime.Minutes, 0);
                _buffer[length++] = _separator;
                ICountDownTick.WriteTwoDigits(_buffer, remainingTime.Seconds, length);
            }

            length += 2;

            text.SetCharArray(_buffer, 0, length);
        }
    }

    public class CountDown
    {
        private readonly TMP_Text _text;
        private readonly bool _isRealtime;
        private readonly float _interval;
        private TimeSpan _timeSpan;
        private Action _onCompleted;
        private ICountDownTick _onTick;
        private Coroutine _coroutine;
        private float _startTime;
        private WaitForSecondsRealtime _waitRealtime;
        private WaitForSeconds _wait;

        public CountDown(TMP_Text text, double second, bool isRealtime, float interval, Action onCompleted, ICountDownTick onTick)
        {
            _text = text;
            _isRealtime = isRealtime;
            _interval = interval;
            _onCompleted = onCompleted;
            _onTick = onTick;
            _timeSpan = TimeSpan.FromSeconds(second);
        }

        public void Start()
        {
            if (_coroutine != null)
            {
                Stop();
            }
            else
            {
                _startTime = _isRealtime ? Time.realtimeSinceStartup : Time.time;
                if (_isRealtime) _waitRealtime = new WaitForSecondsRealtime(_interval);
                else _wait = new WaitForSeconds(_interval);
            }

            _coroutine = _text.StartCoroutine(IECountDown());
        }

        public void Stop()
        {
            if (_coroutine == null) return;
            _text.StopCoroutine(_coroutine);
        }

        private IEnumerator IECountDown()
        {
            while (_timeSpan.TotalSeconds > 0)
            {
                _onTick.OnTick(_text, _timeSpan);
                yield return _isRealtime ? _waitRealtime : _wait;
                var currentTime = _isRealtime ? Time.realtimeSinceStartup : Time.time;
                var duration = currentTime - _startTime;
                _timeSpan -= TimeSpan.FromSeconds(duration);
                _startTime = currentTime;
            }

            _onTick.OnTick(_text, TimeSpan.Zero);

            _onCompleted?.Invoke();
            Stop();
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TicksAttribute))]
    public class TicksDrawer : PropertyDrawer
    {
        private static readonly Color s_utcColor = new Color(0.4f, 0.8f, 1f);
        private static readonly Color s_localColor = new Color(0.6f, 1f, 0.6f);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (fieldInfo.FieldType != typeof(long))
            {
                EditorGUI.LabelField(position, label.text, "[Ticks] requires long field.");
                return;
            }

            var attr = (TicksAttribute)attribute;
            var ticks = property.longValue;

            string displayText;

            try
            {
                var dt = new DateTime(ticks, attr.IsUTC ? DateTimeKind.Utc : DateTimeKind.Local);
                displayText = dt.ToString(DateTimeUtilities.k_Format);
            }
            catch
            {
                property.longValue = 0;
                displayText = new DateTime(0).ToString(DateTimeUtilities.k_Format);
            }

            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = position;
            fieldRect.width -= 55;

            var buttonRect = position;
            buttonRect.x += position.width - 50;
            buttonRect.width = 50;

            var input = EditorGUI.TextField(fieldRect, label, displayText);

            if (string.IsNullOrWhiteSpace(input))
            {
                property.longValue = 0;
            }
            else if (DateTime.TryParseExact(input, DateTimeUtilities.k_Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                parsed = DateTime.SpecifyKind(parsed, attr.IsUTC ? DateTimeKind.Utc : DateTimeKind.Local);
                property.longValue = parsed.Ticks;
            }

            var prevColor = GUI.color;
            GUI.color = attr.IsUTC ? s_utcColor : s_localColor;

            var labelBtn = attr.IsUTC ? "UTC" : "Local";
            if (GUI.Button(buttonRect, new GUIContent(labelBtn, "Set current time")))
            {
                property.longValue = (attr.IsUTC ? DateTime.UtcNow : DateTime.Now).Ticks;
            }

            GUI.color = prevColor;

            EditorGUI.EndProperty();
        }
    }
#endif
}