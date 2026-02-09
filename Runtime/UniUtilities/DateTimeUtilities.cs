using System;
using System.Globalization;
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
        public const string FORMAT = "yyyy-MM-dd HH:mm:ss";

        public static DateTime ToDateTime(this long time) => new DateTime(time);

        public static string ToText(this DateTime dt) => dt.ToString(FORMAT);

        public static DateTime ToDateTime(this string text) => DateTime.ParseExact(text, FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None);

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
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TicksAttribute))]
    public class TicksDrawer : PropertyDrawer
    {
        private static readonly Color k_UtcColor = new Color(0.4f, 0.8f, 1f);
        private static readonly Color k_LocalColor = new Color(0.6f, 1f, 0.6f);

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
                displayText = dt.ToString(DateTimeUtilities.FORMAT);
            }
            catch
            {
                property.longValue = 0;
                displayText = new DateTime(0).ToString(DateTimeUtilities.FORMAT);
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
            else if (DateTime.TryParseExact(input, DateTimeUtilities.FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                parsed = DateTime.SpecifyKind(parsed, attr.IsUTC ? DateTimeKind.Utc : DateTimeKind.Local);
                property.longValue = parsed.Ticks;
            }

            var prevColor = GUI.color;
            GUI.color = attr.IsUTC ? k_UtcColor : k_LocalColor;

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