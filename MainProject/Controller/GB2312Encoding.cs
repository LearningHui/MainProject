﻿//
//  RYTUnZipper
//
//  Created by wang.ping on 10/19/12.
//  Copyright 2011 RYTong. All rights reserved.
//

using System;
using System.IO;
using System.Text;
using System.Windows;

namespace RYTong.Controller.GB2312
{
    public sealed class GB2312Encoding : Encoding
    {
        private const char LEAD_BYTE_CHAR = '\uFFFE';

        private static GB2312Encoding instance;
        public static GB2312Encoding Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GB2312Encoding();
                }
                return instance;
            }
        }

        private GB2312Encoding()
        {
            if (!BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("Not supported big endian platform.");
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            int byteCount = 0;
            ushort u;
            char c;

            for (int i = 0; i < count; index++, byteCount++, i++)
            {
                c = chars[index];
                u = Map.UnicodeToGB2312(c);
                if (u > 0xff)
                    byteCount++;
            }

            return byteCount;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int byteCount = 0;
            ushort u;
            char c;

            for (int i = 0; i < charCount; charIndex++, byteIndex++, byteCount++, i++)
            {
                c = chars[charIndex];
                u = Map.UnicodeToGB2312(c);
                if (u == 0 && c != 0)
                {
                    bytes[byteIndex] = 0x3f;    // 0x3f == '?'
                }
                else if (u < 0x100)
                {
                    bytes[byteIndex] = (byte)u;
                }
                else
                {
                    bytes[byteIndex] = (byte)((u >> 8) & 0xff);
                    byteIndex++;
                    byteCount++;
                    bytes[byteIndex] = (byte)(u & 0xff);
                }
            }

            return byteCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(bytes, index, count, null);
        }

        private int GetCharCount(byte[] bytes, int index, int count, GB2312Decoder decoder)
        {
            int charCount = 0;
            ushort u;
            char c;

            for (int i = 0; i < count; index++, charCount++, i++)
            {
                u = 0;
                if (decoder != null && decoder.pendingByte != 0)
                {
                    u = decoder.pendingByte;
                    decoder.pendingByte = 0;
                }

                u = (ushort)(u << 8 | bytes[index]);
                c = Map.GB2312ToUnicode(u);
                if (c == LEAD_BYTE_CHAR)
                {
                    if (i < count - 1)
                    {
                        index++;
                        i++;
                    }
                    else if (decoder != null)
                    {
                        decoder.pendingByte = bytes[index];
                        return charCount;
                    }
                }
            }

            return charCount;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex, null);
        }

        private int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, GB2312Decoder decoder)
        {
            int charCount = 0;
            ushort u;
            char c;

            for (int i = 0; i < byteCount; byteIndex++, charIndex++, charCount++, i++)
            {
                u = 0;
                if (decoder != null && decoder.pendingByte != 0)
                {
                    u = decoder.pendingByte;
                    decoder.pendingByte = 0;
                }

                u = (ushort)(u << 8 | bytes[byteIndex]);
                c = Map.GB2312ToUnicode(u);
                if (c == LEAD_BYTE_CHAR)
                {
                    if (i < byteCount - 1)
                    {
                        byteIndex++;
                        i++;
                        u = (ushort)(u << 8 | bytes[byteIndex]);
                        c = Map.GB2312ToUnicode(u);
                    }
                    else if (decoder == null)
                    {
                        c = '\0';
                    }
                    else
                    {
                        decoder.pendingByte = bytes[byteIndex];
                        return charCount;
                    }
                }
                if (c == 0 && u != 0)
                    chars[charIndex] = '?';
                else
                    chars[charIndex] = c;
            }

            return charCount;
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount");
            long count = charCount + 1;
            count *= 2;
            if (count > int.MaxValue)
                throw new ArgumentOutOfRangeException("charCount");
            return (int)count;

        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount");
            long count = byteCount + 3;
            if (count > int.MaxValue)
                throw new ArgumentOutOfRangeException("byteCount");
            return (int)count;
        }

        public override Decoder GetDecoder()
        {
            return new GB2312Decoder(this);
        }

        public override string WebName
        {
            get
            {
                return "gb2312";
            }
        }

        private static class Map
        {
            private static ushort[] _gb2312ToUnicode = null;
            private static ushort[] _unicodeToGb2312 = null;

            static Map()
            {
                _gb2312ToUnicode = new ushort[0x10000];
                _unicodeToGb2312 = new ushort[0x10000];

                var resource = Application.GetResourceStream(new Uri("Resources/gb2312.bin", UriKind.Relative));
                if (resource != null)
                {
                    using (BinaryReader reader = new BinaryReader(resource.Stream))
                    {
                        for (int i = 0; i < 0xffff; i++)
                        {
                            ushort u = reader.ReadUInt16();
                            _unicodeToGb2312[i] = u;
                        }
                        for (int i = 0; i < 0xffff; i++)
                        {
                            ushort u = reader.ReadUInt16();
                            _gb2312ToUnicode[i] = u;
                        }
                    }
                }
                else
                {
                    RYTong.LogLib.RYTLog.ShowMessage("gb2312.bin reference is null");
                }
            }

            public static char GB2312ToUnicode(ushort code)
            {
                return (char)_gb2312ToUnicode[code];
            }
            public static ushort UnicodeToGB2312(char ch)
            {
                return _unicodeToGb2312[ch];
            }
        }

        private sealed class GB2312Decoder : Decoder
        {
            private GB2312Decoder() { }
            private GB2312Encoding _encoding = null;

            public GB2312Decoder(GB2312Encoding encoding)
            {
                _encoding = encoding;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return _encoding.GetCharCount(bytes, index, count, this);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex, this);
            }

            public byte pendingByte;
        }
    }
}
