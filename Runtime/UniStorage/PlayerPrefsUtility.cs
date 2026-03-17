using System;
using System.Globalization;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

namespace UniCore.Storage
{
    public static class SimpleEncryption
    {
        private static byte[] s_key =
        {
            58, 123, 106, 37, 54, 106, 63, 69, 58, 116, 35, 125, 71, 49, 48, 109, 77, 37, 57, 104, 112, 53, 83, 61, 37, 125, 50, 44, 89, 50, 54, 67
        };

        private static RijndaelManaged s_provider;

        public static bool IsCustomKeyApplied { get; private set; }

        public static void SetCustomKey(string keyString)
        {
            SetCustomKey(Encoding.ASCII.GetBytes(keyString));
        }

        public static void SetCustomKey(byte[] key)
        {
            if (key.Length != 32)
            {
                throw new ArgumentException("Key must be exactly 32 bytes long (256 bit)");
            }

            s_key = key;

            IsCustomKeyApplied = true;
            SetupProvider();
        }

        private static RijndaelManaged SetupProvider()
        {
            s_provider = new RijndaelManaged();
            s_provider.Key = s_key;
            s_provider.Mode = CipherMode.ECB;
            return s_provider;
        }
        
        public static string EncryptString(string sourceString)
        {
            s_provider ??= SetupProvider();
            var encryptor = s_provider.CreateEncryptor();
            var sourceBytes = Encoding.UTF8.GetBytes(sourceString);
            var outputBytes = encryptor.TransformFinalBlock(sourceBytes, 0, sourceBytes.Length);
            return Convert.ToBase64String(outputBytes);
        }

        /// <summary>
        /// Decrypts the specified string from its specified encrypted value into the returned decrypted value using the
        /// key stored in SimpleEncryption
        /// </summary>
        public static string DecryptString(string sourceString)
        {
            s_provider ??= SetupProvider();
            var decryptor = s_provider.CreateDecryptor();
            var sourceBytes = Convert.FromBase64String(sourceString);
            var outputBytes = decryptor.TransformFinalBlock(sourceBytes, 0, sourceBytes.Length);
            return Encoding.UTF8.GetString(outputBytes);
        }

        /// <summary>
        /// Encrypts the specified float value and returns an encrypted string
        /// </summary>
        public static string EncryptFloat(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            var base64 = Convert.ToBase64String(bytes);
            return EncryptString(base64);
        }

        /// <summary>
        /// Encrypts the specified int value and returns an encrypted string
        /// </summary>
        public static string EncryptInt(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            var base64 = Convert.ToBase64String(bytes);
            return EncryptString(base64);
        }

        /// <summary>
        /// Encrypts the specified bool value and returns an encrypted string
        /// </summary>
        public static string EncryptBool(bool value)
        {
            var bytes = BitConverter.GetBytes(value);
            var base64 = Convert.ToBase64String(bytes);
            return EncryptString(base64);
        }

        /// <summary>
        /// Decrypts the encrypted string representing a float into the decrypted float
        /// </summary>
        public static float DecryptFloat(string sourceString)
        {
            var decryptedString = DecryptString(sourceString);
            var bytes = Convert.FromBase64String(decryptedString);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Decrypts the encrypted string representing an int into the decrypted int
        /// </summary>
        public static int DecryptInt(string sourceString)
        {
            var decryptedString = DecryptString(sourceString);
            var bytes = Convert.FromBase64String(decryptedString);
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Decrypts the encrypted string representing a bool into the decrypted bool
        /// </summary>
        public static bool DecryptBool(string sourceString)
        {
            var decryptedString = DecryptString(sourceString);
            var bytes = Convert.FromBase64String(decryptedString);
            return BitConverter.ToBoolean(bytes, 0);
        }
    }

    public static class PlayerPrefsUtility
    {
        public const string KEY_PREFIX = "ENC-";
        
        public const string VALUE_FLOAT_PREFIX = "0";
        public const string VALUE_INT_PREFIX = "1";
        public const string VALUE_STRING_PREFIX = "2";
        public const string VALUE_BOOL_PREFIX = "3";

        /// <summary>
        /// Determines if the specified player pref key refers to an encrypted record
        /// </summary>
        public static bool IsEncryptedKey(string key)
        {
            return key.StartsWith(KEY_PREFIX);
        }

        /// <summary>
        /// Decrypts the specified key
        /// </summary>
        public static string DecryptKey(string encryptedKey)
        {
            if (encryptedKey.StartsWith(KEY_PREFIX))
            {
                var strippedKey = encryptedKey[KEY_PREFIX.Length..];
                return SimpleEncryption.DecryptString(strippedKey);
            }
            else
            {
                throw new InvalidOperationException("Could not decrypt item, no match found in known encrypted key prefixes");
            }
        }

        /// <summary>
        /// Encrypted version of PlayerPrefs.SetFloat(), stored key and value is encrypted in player prefs
        /// </summary>
        public static void SetEncryptedFloat(string key, float value)
        {
            var encryptedKey = SimpleEncryption.EncryptString(key);
            var encryptedValue = SimpleEncryption.EncryptFloat(value);

            PlayerPrefs.SetString(KEY_PREFIX + encryptedKey, VALUE_FLOAT_PREFIX + encryptedValue);
        }

        /// <summary>
        /// Encrypted version of PlayerPrefs.SetInt(), stored key and value is encrypted in player prefs
        /// </summary>
        public static void SetEncryptedInt(string key, int value)
        {
            var encryptedKey = SimpleEncryption.EncryptString(key);
            var encryptedValue = SimpleEncryption.EncryptInt(value);

            PlayerPrefs.SetString(KEY_PREFIX + encryptedKey, VALUE_INT_PREFIX + encryptedValue);
        }

        /// <summary>
        /// Encrypted version of PlayerPrefs.SetString(), stored key and value is encrypted in player prefs
        /// </summary>
        public static void SetEncryptedString(string key, string value)
        {
            var encryptedKey = SimpleEncryption.EncryptString(key);
            var encryptedValue = SimpleEncryption.EncryptString(value);

            PlayerPrefs.SetString(KEY_PREFIX + encryptedKey, VALUE_STRING_PREFIX + encryptedValue);
        }

        /// <summary>
        /// Encrypted version of EditorPrefs.SetBool(), stored key and value is encrypted in player prefs
        /// </summary>
        public static void SetEncryptedBool(string key, bool value)
        {
            var encryptedKey = SimpleEncryption.EncryptString(key);
            var encryptedValue = SimpleEncryption.EncryptBool(value);

            PlayerPrefs.SetString(KEY_PREFIX + encryptedKey, VALUE_BOOL_PREFIX + encryptedValue);
        }

        /// <summary>
        /// Helper method that can handle any of the encrypted player pref types, returning a float, int or string based
        /// on what type of value has been stored.
        /// </summary>
        public static object GetEncryptedValue(string encryptedKey, string encryptedValue)
        {
            if (encryptedValue.StartsWith(VALUE_FLOAT_PREFIX))
            {
                return GetEncryptedFloat(SimpleEncryption.DecryptString(encryptedKey.Substring(KEY_PREFIX.Length)));
            }

            if (encryptedValue.StartsWith(VALUE_INT_PREFIX))
            {
                return GetEncryptedInt(SimpleEncryption.DecryptString(encryptedKey.Substring(KEY_PREFIX.Length)));
            }

            if (encryptedValue.StartsWith(VALUE_STRING_PREFIX))
            {
                return GetEncryptedString(SimpleEncryption.DecryptString(encryptedKey.Substring(KEY_PREFIX.Length)));
            }

            if (encryptedValue.StartsWith(VALUE_BOOL_PREFIX))
            {
                return GetEncryptedBool(SimpleEncryption.DecryptString(encryptedKey.Substring(KEY_PREFIX.Length)));
            }

            throw new InvalidOperationException("Could not decrypt item, no match found in known encrypted key prefixes");
        }

        /// <summary>
        /// Encrypted version of PlayerPrefs.GetFloat(), an unencrypted key is passed and the value is returned decrypted
        /// </summary>
        public static float GetEncryptedFloat(string key, float defaultValue = 0.0f)
        {
            var encryptedKey = KEY_PREFIX + SimpleEncryption.EncryptString(key);

            var fetchedString = PlayerPrefs.GetString(encryptedKey);

            if (!string.IsNullOrEmpty(fetchedString))
            {
                fetchedString = fetchedString.Remove(0, 1);

                return SimpleEncryption.DecryptFloat(fetchedString);
            }

            return defaultValue;
        }

        /// <summary>
        /// Encrypted version of PlayerPrefs.GetInt(), an unencrypted key is passed and the value is returned decrypted
        /// </summary>
        public static int GetEncryptedInt(string key, int defaultValue = 0)
        {
            var encryptedKey = KEY_PREFIX + SimpleEncryption.EncryptString(key);

            var fetchedString = PlayerPrefs.GetString(encryptedKey);

            if (!string.IsNullOrEmpty(fetchedString))
            {
                fetchedString = fetchedString.Remove(0, 1);

                return SimpleEncryption.DecryptInt(fetchedString);
            }

            return defaultValue;
        }

        /// <summary>
        /// Encrypted version of PlayerPrefs.GetString(), an unencrypted key is passed and the value is returned decrypted
        /// </summary>
        public static string GetEncryptedString(string key, string defaultValue = "")
        {
            var encryptedKey = KEY_PREFIX + SimpleEncryption.EncryptString(key);

            var fetchedString = PlayerPrefs.GetString(encryptedKey);

            if (!string.IsNullOrEmpty(fetchedString))
            {
                fetchedString = fetchedString.Remove(0, 1);

                return SimpleEncryption.DecryptString(fetchedString);
            }

            return defaultValue;
        }

        /// <summary>
        /// Encrypted version of EditorPrefs.GetBool(), an unencrypted key is passed and the value is returned decrypted
        /// </summary>
        public static bool GetEncryptedBool(string key, bool defaultValue = false)
        {
            var encryptedKey = KEY_PREFIX + SimpleEncryption.EncryptString(key);
            var fetchedString = PlayerPrefs.GetString(encryptedKey);

            if (string.IsNullOrEmpty(fetchedString)) return defaultValue;
            fetchedString = fetchedString.Remove(0, 1);
            return SimpleEncryption.DecryptBool(fetchedString);

        }

        /// <summary>
        /// Helper method to store a bool in PlayerPrefs (stored as an int)
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            if (value)
            {
                PlayerPrefs.SetInt(key, 1);
                return;
            }

            PlayerPrefs.SetInt(key, 0);
        }

        /// <summary>
        /// Helper method to retrieve a bool from PlayerPrefs (stored as an int)
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue;
            var value = PlayerPrefs.GetInt(key);
            return value != 0;
        }

        /// <summary>
        /// Helper method to store an enum value in PlayerPrefs (stored using the string name of the enum)
        /// </summary>
        public static void SetEnum(string key, Enum value)
        {
            PlayerPrefs.SetString(key, value.ToString());
        }

        /// <summary>
        /// Generic helper method to retrieve an enum value from PlayerPrefs and parse it from its stored string into the
        /// specified generic type. This method should generally be preferred over the non-generic equivalent
        /// </summary>
        public static T GetEnum<T>(string key, T defaultValue = default(T)) where T : struct
        {
            var stringValue = PlayerPrefs.GetString(key);

            if (!string.IsNullOrEmpty(stringValue))
            {
                return (T)Enum.Parse(typeof(T), stringValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Non-generic helper method to retrieve an enum value from PlayerPrefs (stored as a string). Default value must be
        /// passed, passing null will mean you need to do a null check where you call this method. Generally try to use the
        /// generic version of this method instead: GetEnum<T />
        /// </summary>
        public static object GetEnum(string key, Type enumType, object defaultValue)
        {
            var value = PlayerPrefs.GetString(key);
            return !string.IsNullOrEmpty(value) ? Enum.Parse(enumType, value) : defaultValue;
        }

        /// <summary>
        /// Helper method to store a DateTime (complete with its timezone) in PlayerPrefs as a string
        /// </summary>
        public static void SetDateTime(string key, DateTime value)
        {
            PlayerPrefs.SetString(key, value.ToString("o", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Helper method to retrieve a DateTime from PlayerPrefs (stored as a string) and return a DateTime complete with
        /// timezone (works with UTC and local DateTimes)
        /// </summary>
        public static DateTime GetDateTime(string key, DateTime defaultValue = new DateTime())
        {
            var stringValue = PlayerPrefs.GetString(key);

            return !string.IsNullOrEmpty(stringValue) ? DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : defaultValue;
        }

        /// <summary>
        /// Helper method to store a TimeSpan in PlayerPrefs as a string
        /// </summary>
        public static void SetTimeSpan(string key, TimeSpan value)
        {
            PlayerPrefs.SetString(key, value.ToString());
        }

        /// <summary>
        /// Helper method to retrieve a TimeSpan from PlayerPrefs (stored as a string)
        /// </summary>
        public static TimeSpan GetTimeSpan(string key, TimeSpan defaultValue = new TimeSpan())
        {
            var stringValue = PlayerPrefs.GetString(key);
            return !string.IsNullOrEmpty(stringValue) ? TimeSpan.Parse(stringValue) : defaultValue;
        }
    }
}