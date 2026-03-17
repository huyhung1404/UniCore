using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace UniCore.Editor.PrefsEditor
{
    public static class Plist
    {
        private static readonly List<int> offsetTable = new List<int>();
        private static List<byte> objectTable = new List<byte>();
        private static int refCount;
        private static int objRefSize;
        private static int offsetByteSize;
        private static long offsetTableOffset;

        #region Public Functions

        public static object readPlist(string path)
        {
            using var f = new FileStream(path, FileMode.Open, FileAccess.Read);
            return readPlist(f, plistType.Auto);
        }

        public static object readPlistSource(string source)
        {
            return readPlist(Encoding.UTF8.GetBytes(source));
        }

        public static object readPlist(byte[] data)
        {
            return readPlist(new MemoryStream(data), plistType.Auto);
        }

        public static plistType getPlistType(Stream stream)
        {
            var magicHeader = new byte[8];
            _ = stream.Read(magicHeader, 0, 8);
            return BitConverter.ToInt64(magicHeader, 0) == 3472403351741427810 ? plistType.Binary : plistType.Xml;
        }

        public static object readPlist(Stream stream, plistType type)
        {
            if (type == plistType.Auto)
            {
                type = getPlistType(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }

            if (type == plistType.Binary)
            {
                using var reader = new BinaryReader(stream);
                var data = reader.ReadBytes((int)reader.BaseStream.Length);
                return readBinary(data);
            }

            var xml = new XmlDocument
            {
                XmlResolver = null
            };
            xml.Load(stream);
            return readXml(xml);
        }

        public static void writeXml(object value, string path)
        {
            using var writer = new StreamWriter(path);
            writer.Write(writeXml(value));
        }

        public static void writeXml(object value, Stream stream)
        {
            using var writer = new StreamWriter(stream);
            writer.Write(writeXml(value));
        }

        public static string writeXml(object value)
        {
            using var ms = new MemoryStream();
            var xmlWriterSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true
            };

            using var xmlWriter = XmlWriter.Create(ms, xmlWriterSettings);
            xmlWriter.WriteStartDocument();
            //xmlWriter.WriteComment("DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" " + "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\"");
            xmlWriter.WriteDocType("plist", "-//Apple Computer//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);
            xmlWriter.WriteStartElement("plist");
            xmlWriter.WriteAttributeString("version", "1.0");
            compose(value, xmlWriter);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static void writeBinary(object value, string path)
        {
            using var writer = new BinaryWriter(new FileStream(path, FileMode.Create));
            writer.Write(writeBinary(value));
        }

        public static void writeBinary(object value, Stream stream)
        {
            using var writer = new BinaryWriter(stream);
            writer.Write(writeBinary(value));
        }

        public static byte[] writeBinary(object value)
        {
            offsetTable.Clear();
            objectTable.Clear();
            refCount = 0;
            objRefSize = 0;
            offsetByteSize = 0;
            offsetTableOffset = 0;

            var totalRefs = countObject(value) - 1;

            refCount = totalRefs;

            objRefSize = RegulateNullBytes(BitConverter.GetBytes(refCount)).Length;

            composeBinary(value);

            writeBinaryString("bplist00", false);

            offsetTableOffset = objectTable.Count;

            offsetTable.Add(objectTable.Count - 8);

            offsetByteSize = RegulateNullBytes(BitConverter.GetBytes(offsetTable[^1])).Length;

            var offsetBytes = new List<byte>();

            offsetTable.Reverse();

            for (var i = 0; i < offsetTable.Count; i++)
            {
                offsetTable[i] = objectTable.Count - offsetTable[i];
                var buffer = RegulateNullBytes(BitConverter.GetBytes(offsetTable[i]), offsetByteSize);
                Array.Reverse(buffer);
                offsetBytes.AddRange(buffer);
            }

            objectTable.AddRange(offsetBytes);

            objectTable.AddRange(new byte[6]);
            objectTable.Add(Convert.ToByte(offsetByteSize));
            objectTable.Add(Convert.ToByte(objRefSize));

            var a = BitConverter.GetBytes((long)totalRefs + 1);
            Array.Reverse(a);
            objectTable.AddRange(a);

            objectTable.AddRange(BitConverter.GetBytes((long)0));
            a = BitConverter.GetBytes(offsetTableOffset);
            Array.Reverse(a);
            objectTable.AddRange(a);

            return objectTable.ToArray();
        }

        #endregion

        #region Private Functions

        private static object readXml(XmlDocument xml)
        {
            if (xml.DocumentElement == null) return null;
            var rootNode = xml.DocumentElement.ChildNodes[0];
            return parse(rootNode);
        }

        private static object readBinary(byte[] data)
        {
            offsetTable.Clear();
            objectTable.Clear();
            refCount = 0;
            objRefSize = 0;
            offsetByteSize = 0;
            offsetTableOffset = 0;

            var bList = new List<byte>(data);

            var trailer = bList.GetRange(bList.Count - 32, 32);

            parseTrailer(trailer);

            objectTable = bList.GetRange(0, (int)offsetTableOffset);

            var offsetTableBytes = bList.GetRange((int)offsetTableOffset, bList.Count - (int)offsetTableOffset - 32);

            parseOffsetTable(offsetTableBytes);

            return parseBinary(0);
        }

        private static Dictionary<string, object> parseDictionary(XmlNode node)
        {
            var children = node.ChildNodes;
            if (children.Count % 2 != 0)
            {
                throw new DataMisalignedException("Dictionary elements must have an even number of child nodes");
            }

            var dict = new Dictionary<string, object>();

            for (var i = 0; i < children.Count; i += 2)
            {
                var kn = children[i];
                var vn = children[i + 1];

                if (kn.Name != "key")
                {
                    throw new ApplicationException("expected a key node");
                }

                var result = parse(vn);

                if (result != null)
                {
                    dict.Add(kn.InnerText, result);
                }
            }

            return dict;
        }

        private static List<object> parseArray(XmlNode node)
        {
            var array = new List<object>();

            foreach (XmlNode child in node.ChildNodes)
            {
                var result = parse(child);
                if (result != null)
                {
                    array.Add(result);
                }
            }

            return array;
        }

        private static void composeArray(List<object> value, XmlWriter writer)
        {
            writer.WriteStartElement("array");
            foreach (object obj in value)
            {
                compose(obj, writer);
            }

            writer.WriteEndElement();
        }

        private static object parse(XmlNode node)
        {
            switch (node.Name)
            {
                case "dict":
                    return parseDictionary(node);
                case "array":
                    return parseArray(node);
                case "string":
                    return node.InnerText;
                case "integer":
                    //  int result;
                    //int.TryParse(node.InnerText, System.Globalization.NumberFormatInfo.InvariantInfo, out result);
                    return Convert.ToInt32(node.InnerText, System.Globalization.NumberFormatInfo.InvariantInfo);
                case "real":
                    return Convert.ToDouble(node.InnerText, System.Globalization.NumberFormatInfo.InvariantInfo);
                case "false":
                    return false;
                case "true":
                    return true;
                case "null":
                    return null;
                case "date":
                    return XmlConvert.ToDateTime(node.InnerText, XmlDateTimeSerializationMode.Utc);
                case "data":
                    return Convert.FromBase64String(node.InnerText);
            }

            throw new ApplicationException($"Plist Node `{node.Name}' is not supported");
        }

        private static void compose(object value, XmlWriter writer)
        {
            if (value == null || value is string)
            {
                writer.WriteElementString("string", (string)value);
            }
            else if (value is int || value is long)
            {
                writer.WriteElementString("integer", ((int)value).ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
            }
            else if (value is Dictionary<string, object> ||
                     value.GetType().ToString().StartsWith("System.Collections.Generic.Dictionary`2[System.String"))
            {
                //Convert to Dictionary<string, object>
                var dic = value as Dictionary<string, object>;
                if (dic == null)
                {
                    dic = new Dictionary<string, object>();
                    IDictionary idic = (IDictionary)value;
                    foreach (var key in idic.Keys)
                    {
                        dic.Add(key.ToString(), idic[key]);
                    }
                }

                writeDictionaryValues(dic, writer);
            }
            else if (value is List<object> list)
            {
                composeArray(list, writer);
            }
            else if (value is byte[] bytes)
            {
                writer.WriteElementString("data", Convert.ToBase64String(bytes));
            }
            else if (value is float || value is double)
            {
                writer.WriteElementString("real", ((double)value).ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
            }
            else if (value is DateTime time)
            {
                var theString = XmlConvert.ToString(time, XmlDateTimeSerializationMode.Utc);
                writer.WriteElementString("date", theString); //, "yyyy-MM-ddTHH:mm:ssZ"));
            }
            else if (value is bool)
            {
                writer.WriteElementString(value.ToString().ToLower(), "");
            }
            else
            {
                throw new Exception($"Value type '{value.GetType()}' is unhandled");
            }
        }

        private static void writeDictionaryValues(Dictionary<string, object> dictionary, XmlWriter writer)
        {
            writer.WriteStartElement("dict");
            foreach (string key in dictionary.Keys)
            {
                object value = dictionary[key];
                writer.WriteElementString("key", key);
                compose(value, writer);
            }

            writer.WriteEndElement();
        }

        private static int countObject(object value)
        {
            int count = 0;
            switch (value.GetType().ToString())
            {
                case "System.Collections.Generic.Dictionary`2[System.String,System.Object]":
                    Dictionary<string, object> dict = (Dictionary<string, object>)value;
                    foreach (string key in dict.Keys)
                    {
                        count += countObject(dict[key]);
                    }

                    count += dict.Keys.Count;
                    count++;
                    break;
                case "System.Collections.Generic.List`1[System.Object]":
                    List<object> list = (List<object>)value;
                    foreach (object obj in list)
                    {
                        count += countObject(obj);
                    }

                    count++;
                    break;
                default:
                    count++;
                    break;
            }

            return count;
        }

        private static byte[] writeBinaryDictionary(Dictionary<string, object> dictionary)
        {
            List<byte> buffer = new List<byte>();
            List<byte> header = new List<byte>();
            List<int> refs = new List<int>();
            for (int i = dictionary.Count - 1; i >= 0; i--)
            {
                var o = new object[dictionary.Count];
                dictionary.Values.CopyTo(o, 0);
                composeBinary(o[i]);
                offsetTable.Add(objectTable.Count);
                refs.Add(refCount);
                refCount--;
            }

            for (int i = dictionary.Count - 1; i >= 0; i--)
            {
                var o = new string[dictionary.Count];
                dictionary.Keys.CopyTo(o, 0);
                composeBinary(o[i]); //);
                offsetTable.Add(objectTable.Count);
                refs.Add(refCount);
                refCount--;
            }

            if (dictionary.Count < 15)
            {
                header.Add(Convert.ToByte(0xD0 | Convert.ToByte(dictionary.Count)));
            }
            else
            {
                header.Add(0xD0 | 0xf);
                header.AddRange(writeBinaryInteger(dictionary.Count, false));
            }


            foreach (int val in refs)
            {
                byte[] refBuffer = RegulateNullBytes(BitConverter.GetBytes(val), objRefSize);
                Array.Reverse(refBuffer);
                buffer.InsertRange(0, refBuffer);
            }

            buffer.InsertRange(0, header);


            objectTable.InsertRange(0, buffer);

            return buffer.ToArray();
        }

        private static byte[] composeBinaryArray(List<object> objects)
        {
            var buffer = new List<byte>();
            var header = new List<byte>();
            var refs = new List<int>();

            for (var i = objects.Count - 1; i >= 0; i--)
            {
                composeBinary(objects[i]);
                offsetTable.Add(objectTable.Count);
                refs.Add(refCount);
                refCount--;
            }

            if (objects.Count < 15)
            {
                header.Add(Convert.ToByte(0xA0 | Convert.ToByte(objects.Count)));
            }
            else
            {
                header.Add(0xA0 | 0xf);
                header.AddRange(writeBinaryInteger(objects.Count, false));
            }

            foreach (var val in refs)
            {
                var refBuffer = RegulateNullBytes(BitConverter.GetBytes(val), objRefSize);
                Array.Reverse(refBuffer);
                buffer.InsertRange(0, refBuffer);
            }

            buffer.InsertRange(0, header);

            objectTable.InsertRange(0, buffer);

            return buffer.ToArray();
        }

        private static byte[] composeBinary(object obj)
        {
            byte[] value;
            switch (obj.GetType().ToString())
            {
                case "System.Collections.Generic.Dictionary`2[System.String,System.Object]":
                    value = writeBinaryDictionary((Dictionary<string, object>)obj);
                    return value;

                case "System.Collections.Generic.List`1[System.Object]":
                    value = composeBinaryArray((List<object>)obj);
                    return value;

                case "System.Byte[]":
                    value = writeBinaryByteArray((byte[])obj);
                    return value;

                case "System.Double":
                    value = writeBinaryDouble((double)obj);
                    return value;

                case "System.Int32":
                    value = writeBinaryInteger((int)obj, true);
                    return value;

                case "System.String":
                    value = writeBinaryString((string)obj, true);
                    return value;

                case "System.DateTime":
                    value = writeBinaryDate((DateTime)obj);
                    return value;

                case "System.Boolean":
                    value = writeBinaryBool((bool)obj);
                    return value;

                default:
                    return Array.Empty<byte>();
            }
        }

        public static byte[] writeBinaryDate(DateTime obj)
        {
            var buffer = new List<byte>(RegulateNullBytes(BitConverter.GetBytes(PlistDateConverter.ConvertToAppleTimeStamp(obj)), 8));
            buffer.Reverse();
            buffer.Insert(0, 0x33);
            objectTable.InsertRange(0, buffer);
            return buffer.ToArray();
        }

        public static byte[] writeBinaryBool(bool obj)
        {
            var buffer = new List<byte>(new[] { obj ? (byte)9 : (byte)8 });
            objectTable.InsertRange(0, buffer);
            return buffer.ToArray();
        }

        private static byte[] writeBinaryInteger(int value, bool write)
        {
            var buffer = new List<byte>(BitConverter.GetBytes((long)value));
            buffer = new List<byte>(RegulateNullBytes(buffer.ToArray()));
            while (Math.Abs(buffer.Count - Math.Pow(2, Math.Log(buffer.Count) / Math.Log(2))) > 0.0001f)
                buffer.Add(0);
            var header = 0x10 | (int)(Math.Log(buffer.Count) / Math.Log(2));

            buffer.Reverse();

            buffer.Insert(0, Convert.ToByte(header));

            if (write)
                objectTable.InsertRange(0, buffer);

            return buffer.ToArray();
        }

        private static byte[] writeBinaryDouble(double value)
        {
            var buffer = new List<byte>(RegulateNullBytes(BitConverter.GetBytes(value), 4));
            while (Math.Abs(buffer.Count - Math.Pow(2, Math.Log(buffer.Count) / Math.Log(2))) > 0.0001f)
                buffer.Add(0);
            var header = 0x20 | (int)(Math.Log(buffer.Count) / Math.Log(2));

            buffer.Reverse();

            buffer.Insert(0, Convert.ToByte(header));

            objectTable.InsertRange(0, buffer);

            return buffer.ToArray();
        }

        private static byte[] writeBinaryByteArray(byte[] value)
        {
            List<byte> buffer = new List<byte>(value);
            List<byte> header = new List<byte>();
            if (value.Length < 15)
            {
                header.Add(Convert.ToByte(0x40 | Convert.ToByte(value.Length)));
            }
            else
            {
                header.Add(0x40 | 0xf);
                header.AddRange(writeBinaryInteger(buffer.Count, false));
            }

            buffer.InsertRange(0, header);

            objectTable.InsertRange(0, buffer);

            return buffer.ToArray();
        }

        private static byte[] writeBinaryString(string value, bool head)
        {
            var buffer = new List<byte>();
            var header = new List<byte>();
            for (var index = 0; index < value.ToCharArray().Length; index++)
            {
                var chr = value.ToCharArray()[index];
                buffer.Add(Convert.ToByte(chr));
            }

            if (head)
            {
                if (value.Length < 15)
                {
                    header.Add(Convert.ToByte(0x50 | Convert.ToByte(value.Length)));
                }
                else
                {
                    header.Add(0x50 | 0xf);
                    header.AddRange(writeBinaryInteger(buffer.Count, false));
                }
            }

            buffer.InsertRange(0, header);

            objectTable.InsertRange(0, buffer);

            return buffer.ToArray();
        }

        private static byte[] RegulateNullBytes(byte[] value, int minBytes = 1)
        {
            Array.Reverse(value);
            var bytes = new List<byte>(value);
            for (int i = 0; i < bytes.Count; i++)
            {
                if (bytes[i] == 0 && bytes.Count > minBytes)
                {
                    bytes.Remove(bytes[i]);
                    i--;
                }
                else
                    break;
            }

            if (bytes.Count < minBytes)
            {
                int dist = minBytes - bytes.Count;
                for (int i = 0; i < dist; i++)
                    bytes.Insert(0, 0);
            }

            value = bytes.ToArray();
            Array.Reverse(value);
            return value;
        }

        private static void parseTrailer(List<byte> trailer)
        {
            offsetByteSize = BitConverter.ToInt32(RegulateNullBytes(trailer.GetRange(6, 1).ToArray(), 4), 0);
            objRefSize = BitConverter.ToInt32(RegulateNullBytes(trailer.GetRange(7, 1).ToArray(), 4), 0);
            byte[] refCountBytes = trailer.GetRange(12, 4).ToArray();
            Array.Reverse(refCountBytes);
            refCount = BitConverter.ToInt32(refCountBytes, 0);
            byte[] offsetTableOffsetBytes = trailer.GetRange(24, 8).ToArray();
            Array.Reverse(offsetTableOffsetBytes);
            offsetTableOffset = BitConverter.ToInt64(offsetTableOffsetBytes, 0);
        }

        private static void parseOffsetTable(List<byte> offsetTableBytes)
        {
            for (int i = 0; i < offsetTableBytes.Count; i += offsetByteSize)
            {
                byte[] buffer = offsetTableBytes.GetRange(i, offsetByteSize).ToArray();
                Array.Reverse(buffer);
                offsetTable.Add(BitConverter.ToInt32(RegulateNullBytes(buffer, 4), 0));
            }
        }

        private static object parseBinaryDictionary(int objRef)
        {
            var buffer = new Dictionary<string, object>();
            var refs = new List<int>();
            var count = getCount(offsetTable[objRef], out var refStartPosition);

            if (count < 15)
                refStartPosition = offsetTable[objRef] + 1;
            else
                refStartPosition = offsetTable[objRef] + 2 + RegulateNullBytes(BitConverter.GetBytes(count)).Length;

            for (var i = refStartPosition; i < refStartPosition + count * 2 * objRefSize; i += objRefSize)
            {
                var refBuffer = objectTable.GetRange(i, objRefSize).ToArray();
                Array.Reverse(refBuffer);
                refs.Add(BitConverter.ToInt32(RegulateNullBytes(refBuffer, 4), 0));
            }

            for (var i = 0; i < count; i++)
            {
                buffer.Add((string)parseBinary(refs[i]), parseBinary(refs[i + count]));
            }

            return buffer;
        }

        private static object parseBinaryArray(int objRef)
        {
            var buffer = new List<object>();
            var refs = new List<int>();
            var count = getCount(offsetTable[objRef], out var refStartPosition);

            if (count < 15)
                refStartPosition = offsetTable[objRef] + 1;
            else
                refStartPosition = offsetTable[objRef] + 2 + RegulateNullBytes(BitConverter.GetBytes(count)).Length;

            for (var i = refStartPosition; i < refStartPosition + count * objRefSize; i += objRefSize)
            {
                var refBuffer = objectTable.GetRange(i, objRefSize).ToArray();
                Array.Reverse(refBuffer);
                refs.Add(BitConverter.ToInt32(RegulateNullBytes(refBuffer, 4), 0));
            }

            for (var i = 0; i < count; i++)
            {
                buffer.Add(parseBinary(refs[i]));
            }

            return buffer;
        }

        private static int getCount(int bytePosition, out int newBytePosition)
        {
            var headerByte = objectTable[bytePosition];
            var headerByteTrail = Convert.ToByte(headerByte & 0xf);
            int count;
            if (headerByteTrail < 15)
            {
                count = headerByteTrail;
                newBytePosition = bytePosition + 1;
            }
            else
                count = (int)parseBinaryInt(bytePosition + 1, out newBytePosition);

            return count;
        }

        private static object parseBinary(int objRef)
        {
            var header = objectTable[offsetTable[objRef]];
            switch (header & 0xF0)
            {
                case 0:
                {
                    //If the byte is
                    //0 return null
                    //9 return true
                    //8 return false
                    return (objectTable[offsetTable[objRef]] == 0) ? null : objectTable[offsetTable[objRef]] == 9;
                }
                case 0x10:
                {
                    return parseBinaryInt(offsetTable[objRef]);
                }
                case 0x20:
                {
                    return parseBinaryReal(offsetTable[objRef]);
                }
                case 0x30:
                {
                    return parseBinaryDate(offsetTable[objRef]);
                }
                case 0x40:
                {
                    return parseBinaryByteArray(offsetTable[objRef]);
                }
                case 0x50: //String ASCII
                {
                    return parseBinaryAsciiString(offsetTable[objRef]);
                }
                case 0x60: //String Unicode
                {
                    return parseBinaryUnicodeString(offsetTable[objRef]);
                }
                case 0xD0:
                {
                    return parseBinaryDictionary(objRef);
                }
                case 0xA0:
                {
                    return parseBinaryArray(objRef);
                }
            }

            throw new Exception("This type is not supported");
        }

        public static object parseBinaryDate(int headerPosition)
        {
            byte[] buffer = objectTable.GetRange(headerPosition + 1, 8).ToArray();
            Array.Reverse(buffer);
            double appleTime = BitConverter.ToDouble(buffer, 0);
            DateTime result = PlistDateConverter.ConvertFromAppleTimeStamp(appleTime);
            return result;
        }

        private static object parseBinaryInt(int headerPosition)
        {
            return parseBinaryInt(headerPosition, out _);
        }

        private static object parseBinaryInt(int headerPosition, out int newHeaderPosition)
        {
            byte header = objectTable[headerPosition];
            int byteCount = (int)Math.Pow(2, header & 0xf);
            byte[] buffer = objectTable.GetRange(headerPosition + 1, byteCount).ToArray();
            Array.Reverse(buffer);
            //Add one to account for the header byte
            newHeaderPosition = headerPosition + byteCount + 1;
            return BitConverter.ToInt32(RegulateNullBytes(buffer, 4), 0);
        }

        private static object parseBinaryReal(int headerPosition)
        {
            var header = objectTable[headerPosition];
            var byteCount = (int)Math.Pow(2, header & 0xf);
            var buffer = objectTable.GetRange(headerPosition + 1, byteCount).ToArray();
            Array.Reverse(buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        private static object parseBinaryAsciiString(int headerPosition)
        {
            var charCount = getCount(headerPosition, out var charStartPosition);
            var buffer = objectTable.GetRange(charStartPosition, charCount);
            return buffer.Count > 0 ? Encoding.ASCII.GetString(buffer.ToArray()) : string.Empty;
        }

        private static object parseBinaryUnicodeString(int headerPosition)
        {
            var charCount = getCount(headerPosition, out var charStartPosition);
            charCount *= 2;

            var buffer = new byte[charCount];

            for (var i = 0; i < charCount; i += 2)
            {
                var one = objectTable.GetRange(charStartPosition + i, 1)[0];
                var two = objectTable.GetRange(charStartPosition + i + 1, 1)[0];

                if (BitConverter.IsLittleEndian)
                {
                    buffer[i] = two;
                    buffer[i + 1] = one;
                }
                else
                {
                    buffer[i] = one;
                    buffer[i + 1] = two;
                }
            }

            return Encoding.Unicode.GetString(buffer);
        }

        private static object parseBinaryByteArray(int headerPosition)
        {
            var byteCount = getCount(headerPosition, out var byteStartPosition);
            return objectTable.GetRange(byteStartPosition, byteCount).ToArray();
        }

        #endregion
    }

    public enum plistType
    {
        Auto,
        Binary,
        Xml
    }

    public static class PlistDateConverter
    {
        public const long k_TimeDifference = 978307200;

        public static long GetAppleTime(long unixTime)
        {
            return unixTime - k_TimeDifference;
        }

        public static long GetUnixTime(long appleTime)
        {
            return appleTime + k_TimeDifference;
        }

        public static DateTime ConvertFromAppleTimeStamp(double timestamp)
        {
            DateTime origin = new DateTime(2001, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        public static double ConvertToAppleTimeStamp(DateTime date)
        {
            DateTime begin = new DateTime(2001, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - begin;
            return Math.Floor(diff.TotalSeconds);
        }
    }
}