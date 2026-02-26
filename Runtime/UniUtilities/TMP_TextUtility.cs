//#define TMP_ROUND_DECIMALS // When defined, float and double values are rounded using Math.round

namespace TMPro
{
    public static class TMP_TextUtility
    {
        private static readonly char[] s_arr = new char[64];

        private static int s_charIndex;

        public static void SetText(this TMP_Text text, sbyte number)
        {
            SetText(text, (int)number);
        }

        public static void SetText(this TMP_Text text, sbyte number, string prefix, string postfix)
        {
            SetText(text, (int)number, prefix, postfix);
        }

        public static void SetText(this TMP_Text text, byte number)
        {
            SetText(text, (uint)number);
        }

        public static void SetText(this TMP_Text text, byte number, string prefix, string postfix)
        {
            SetText(text, (uint)number, prefix, postfix);
        }

        public static void SetText(this TMP_Text text, short number)
        {
            SetText(text, (int)number);
        }

        public static void SetText(this TMP_Text text, short number, string prefix, string postfix)
        {
            SetText(text, (int)number, prefix, postfix);
        }

        public static void SetText(this TMP_Text text, ushort number)
        {
            SetText(text, (uint)number);
        }

        public static void SetText(this TMP_Text text, ushort number, string prefix, string postfix)
        {
            SetText(text, (uint)number, prefix, postfix);
        }

        public static void SetText(this TMP_Text text, int number)
        {
            SetInteger(number);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, int number, string prefix, string postfix)
        {
            AddStringToArray(postfix);
            SetInteger(number);
            AddStringToArray(prefix);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, uint number)
        {
            SetInteger(number);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, uint number, string prefix, string postfix)
        {
            AddStringToArray(postfix);
            SetInteger(number);
            AddStringToArray(prefix);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, long number)
        {
            SetInteger(number);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, long number, string prefix, string postfix)
        {
            AddStringToArray(postfix);
            SetInteger(number);
            AddStringToArray(prefix);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, ulong number)
        {
            SetInteger(number);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, ulong number, string prefix, string postfix)
        {
            AddStringToArray(postfix);
            SetInteger(number);
            AddStringToArray(prefix);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, float number, int precision = 2)
        {
            SetFloat(number, precision);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, float number, string prefix, string postfix, int precision = 2)
        {
            AddStringToArray(postfix);
            SetFloat(number, precision);
            AddStringToArray(prefix);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, double number, int precision = 2)
        {
            SetFloat(number, precision);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        public static void SetText(this TMP_Text text, double number, string prefix, string postfix, int precision = 2)
        {
            AddStringToArray(postfix);
            SetFloat(number, precision);
            AddStringToArray(prefix);

            ReverseArray();
            text.SetCharArray(s_arr, 0, s_charIndex);
            s_charIndex = 0;
        }

        private static void SetInteger(int number)
        {
            var isNegativeNumber = number < 0;
            if (number == 0)
                s_arr[s_charIndex++] = '0';
            else
            {
                if (isNegativeNumber)
                    number = -number;

                while (number > 0)
                {
                    s_arr[s_charIndex++] = (char)('0' + number % 10);
                    number /= 10;
                }

                if (isNegativeNumber)
                    s_arr[s_charIndex++] = '-';
            }
        }

        private static void SetInteger(uint number)
        {
            if (number == 0)
                s_arr[s_charIndex++] = '0';
            else
            {
                while (number > 0)
                {
                    s_arr[s_charIndex++] = (char)('0' + (number % 10));
                    number /= 10;
                }
            }
        }

        private static void SetInteger(long number)
        {
            if (number == 0)
                s_arr[s_charIndex++] = '0';
            else
            {
                var isNegativeNumber = number < 0;
                if (isNegativeNumber)
                    number = -number;

                while (number > 0)
                {
                    s_arr[s_charIndex++] = (char)('0' + (number % 10));
                    number /= 10;
                }

                if (isNegativeNumber)
                    s_arr[s_charIndex++] = '-';
            }
        }

        private static void SetInteger(ulong number)
        {
            if (number == 0)
                s_arr[s_charIndex++] = '0';
            else
            {
                while (number > 0)
                {
                    s_arr[s_charIndex++] = (char)('0' + (number % 10));
                    number /= 10;
                }
            }
        }

        private static void SetFloat(float number, int precision)
        {
            var isNegativeNumber = number < 0f;
            if (isNegativeNumber) number = -number;

            var integer = (uint)number;
            var fraction = number - integer;

#if TMP_ROUND_DECIMALS
		fraction = (float) System.Math.Round( fraction, precision );
#endif

            s_charIndex += precision;
            var truncateAmount = 0;
            for (var i = 0; i < precision; i++)
            {
                // Floating point arithmetics are not deterministic, try to handle
                // edge cases properly via these if conditions
                if (fraction >= 0f)
                    fraction *= 10f;
                else
                    fraction *= -10f;

                var digit = (int)fraction;
                if (digit <= 0)
                {
                    digit = 0;
                    truncateAmount++;
                }
                else
                {
                    if (digit > 9)
                        digit = 9;

                    truncateAmount = 0;
                }

                s_arr[--s_charIndex] = (char)('0' + digit);
                fraction -= digit;
            }

            // Assert: if truncateAmount == precision, then fraction consists of all 0's,
            // so don't include the fraction in the text at all
            if (truncateAmount < precision)
            {
                if (truncateAmount > 0)
                {
                    // Truncate redundant 0's
                    for (int i = s_charIndex + truncateAmount, j = i + precision; i < j; i++)
                        s_arr[i - truncateAmount] = s_arr[i];
                }

                s_charIndex += precision - truncateAmount;
                s_arr[s_charIndex++] = '.';
            }
            else if (integer == 0) // Rare case: -0.0001 with precision 2 results in -0, this line fixes it
                isNegativeNumber = false;

            SetInteger(integer);

            if (isNegativeNumber)
                s_arr[s_charIndex++] = '-';
        }

        private static void SetFloat(double number, int precision)
        {
            var isNegativeNumber = number < 0f;
            if (isNegativeNumber)
                number = -number;

            var integer = (ulong)number;
            var fraction = number - integer;

#if TMP_ROUND_DECIMALS
		fraction = System.Math.Round( fraction, precision );
#endif

            s_charIndex += precision;
            var truncateAmount = 0;
            for (var i = 0; i < precision; i++)
            {
                // Floating point arithmetics are not deterministic, try to handle
                // edge cases properly via these if conditions
                if (fraction >= 0.0)
                    fraction *= 10.0;
                else
                    fraction *= -10.0;

                var digit = (int)fraction;
                if (digit <= 0)
                {
                    digit = 0;
                    truncateAmount++;
                }
                else
                {
                    if (digit > 9)
                        digit = 9;

                    truncateAmount = 0;
                }

                s_arr[--s_charIndex] = (char)('0' + digit);
                fraction -= digit;
            }

            // Assert: if truncateAmount == precision, then fraction consists of all 0's,
            // so don't include the fraction in the text at all
            if (truncateAmount < precision)
            {
                if (truncateAmount > 0)
                {
                    // Truncate redundant 0's
                    for (int i = s_charIndex + truncateAmount, j = i + precision; i < j; i++)
                        s_arr[i - truncateAmount] = s_arr[i];
                }

                s_charIndex += precision - truncateAmount;
                s_arr[s_charIndex++] = '.';
            }
            else if (integer == 0) // Rare case: -0.0001 with precision 2 results in -0, this line fixes it
                isNegativeNumber = false;

            SetInteger(integer);

            if (isNegativeNumber)
                s_arr[s_charIndex++] = '-';
        }

        private static void AddStringToArray(string str)
        {
            if (string.IsNullOrEmpty(str)) return;

            for (var i = str.Length - 1; i >= 0; i--)
                s_arr[s_charIndex++] = str[i];
        }

        private static void ReverseArray()
        {
            for (var i = s_charIndex / 2 - 1; i >= 0; i--)
            {
                var j = s_charIndex - 1 - i;
                (s_arr[i], s_arr[j]) = (s_arr[j], s_arr[i]);
            }
        }
    }
}