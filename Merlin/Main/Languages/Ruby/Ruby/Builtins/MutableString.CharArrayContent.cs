﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using Microsoft.Scripting.Utils;
using System.Text;
using IronRuby.Runtime;
using System.Diagnostics;
using System.IO;

namespace IronRuby.Builtins {
    public partial class MutableString {
        /// <summary>
        /// Mutable character array. 
        /// All indices and counts are in characters. Surrogate pairs are treated as 2 separate characters.
        /// </summary>
        [Serializable]
        private sealed class CharArrayContent : Content {
            private char[]/*!*/ _data;
            private int _count;

            public CharArrayContent(char[]/*!*/ data, MutableString owner)
                : this(data, data.Length, owner) {
            }

            public CharArrayContent(char[]/*!*/ data, int count, MutableString owner) 
                : base(owner) {
                Assert.NotNull(data);
                Debug.Assert(count >= 0 && count <= data.Length);
                _data = data;
                _count = count;
            }

            internal BinaryContent/*!*/ SwitchToBinary() {
                return SwitchToBinary(0);
            }

            private BinaryContent/*!*/ SwitchToBinary(int additionalCapacity) {
                var bytes = DataToBytes(additionalCapacity);
                return WrapContent(bytes, bytes.Length - additionalCapacity);
            }

            private byte[]/*!*/ DataToBytes(int additionalCapacity) {
                if (_count == 0) {
                    return (additionalCapacity == 0) ? Utils.EmptyBytes : new byte[additionalCapacity];
                } else if (additionalCapacity == 0) {
                    return _owner._encoding.StrictEncoding.GetBytes(_data, 0, _count);
                } else {
                    var result = new byte[_owner._encoding.StrictEncoding.GetByteCount(_data, 0, _count) + additionalCapacity];
                    _owner._encoding.StrictEncoding.GetBytes(_data, 0, _count, result, 0);
                    return result;
                }
            }

            public char DataGetChar(int index) {
                Debug.Assert(index < _count);
                return _data[index];
            }

            public void DataSetChar(int index, char c) {
                Debug.Assert(index < _count);
                _data[index] = c;
            }

            #region GetHashCode, Length, Clone (read-only), Count

            public override int GetHashCode(out int binarySum) {
                return _data.GetValueHashCode(_count, out binarySum);
            }

            public override int GetBinaryHashCode(out int binarySum) {
                return _owner.IsBinaryEncoded ? GetHashCode(out binarySum) : SwitchToBinary().GetBinaryHashCode(out binarySum);
            }

            public override bool IsBinary {
                get { return false; }
            }

            public int DataLength {
                get { return _count; }
            }

            public override int Count {
                get { return _count; }
                set {
                    if (_data.Length < value) {
                        Array.Resize(ref _data, Utils.GetExpandedSize(_data, value));
                    } else {
                        Utils.Fill(_data, _count, '\0', value - _count);
                    }
                    _count = value;
                }
            }

            public override bool IsEmpty {
                get { return _count == 0; }
            }

            public override int GetCharCount() {
                return _count;
            }

            public override int GetByteCount() {
                return (_owner.HasByteCharacters) ? _count : (_count == 0) ? 0 : SwitchToBinary().GetByteCount();
            }

            public override void SwitchToBinaryContent() {
                SwitchToBinary();
            }

            public override void SwitchToStringContent() {
                // nop
            }

            public override void SwitchToMutableContent() {
                // nop
            }

            public override Content/*!*/ Clone() {
                return new CharArrayContent(Utils.GetSlice(_data, 0, _count), _owner);
            }

            public override void TrimExcess() {
                Utils.TrimExcess(ref _data, _count);
            }

            public override int GetCapacity() {
                return _data.Length;
            }

            public override void SetCapacity(int capacity) {
                if (capacity < _count) {
                    throw new InvalidOperationException();
                }
                Array.Resize(ref _data, capacity);
            }

            #endregion

            #region Conversions (read-only)

            public override string/*!*/ ConvertToString() {
                return GetStringSlice(0, _count);
            }

            public override byte[]/*!*/ ConvertToBytes() {
                var binary = SwitchToBinary();
                return binary.GetBinarySlice(0, binary.GetByteCount());
            }

            public override string/*!*/ ToString() {
                return new String(_data, 0, _count);
            }

            public override byte[]/*!*/ ToByteArray() {
                return DataToBytes(0);
            }

            internal override byte[]/*!*/ GetByteArray(out int count) {
                return SwitchToBinary().GetByteArray(out count);
            }

            public override Content/*!*/ EscapeRegularExpression() {
                // TODO:
                StringBuilder sb = RubyRegex.EscapeToStringBuilder(ToString());
                return (sb != null) ? new CharArrayContent(sb.ToString().ToCharArray(), _owner) : this;
            }

            public override void CheckEncoding() {
                _owner._encoding.StrictEncoding.GetByteCount(_data, 0, _count);
            }

            #endregion

            #region CompareTo (read-only)

            public override int OrdinalCompareTo(string/*!*/ str) {
                return _data.ValueCompareTo(_count, str);
            }

            internal int OrdinalCompareTo(char[]/*!*/ chars, int count) {
                return _data.ValueCompareTo(_count, chars, count);
            }

            // this <=> content
            public override int OrdinalCompareTo(Content/*!*/ content) {
                return content.ReverseOrdinalCompareTo(this);
            }

            // content.bytes <=> this.chars
            public override int ReverseOrdinalCompareTo(BinaryContent/*!*/ content) {
                return SwitchToBinary().ReverseOrdinalCompareTo(content);
            }

            // content.chars <=> this.chars
            public override int ReverseOrdinalCompareTo(CharArrayContent/*!*/ content) {
                return content.OrdinalCompareTo(_data, _count);
            }

            // content.chars <=> this.chars
            public override int ReverseOrdinalCompareTo(StringContent/*!*/ content) {
                return content.OrdinalCompareTo(_data, _count);
            }

            #endregion

            #region Slices (read-only)

            public override char GetChar(int index) {
                if (index >= _count) {
                    throw new IndexOutOfRangeException();
                }
                return _data[index];
            }

            public override byte GetByte(int index) {
                if (_owner.HasByteCharacters) {
                    if (index >= _count) {
                        throw new IndexOutOfRangeException();
                    }
                    return (byte)_data[index];
                }
                return SwitchToBinary().GetByte(index);
            }

            public override string/*!*/ GetStringSlice(int start, int count) {
                return new String(_data, start, count);
            }

            public override byte[]/*!*/ GetBinarySlice(int start, int count) {
                return SwitchToBinary().GetBinarySlice(start, count);
            }

            public override Content/*!*/ GetSlice(int start, int count) {
                return new CharArrayContent(_data.GetSlice(start, count), _owner);
            }

            public override IEnumerable<char>/*!*/ GetCharacters() {
                return Utils.Enumerate(_data, _count);
            }

            public override IEnumerable<byte>/*!*/ GetBytes() {
                if (_owner.HasByteCharacters) {
                    return Utils.EnumerateAsBytes(_data, _count);
                } else {
                    return SwitchToBinary().GetBytes();
                }
            }

            #endregion

            #region IndexOf (read-only)

            //
            // Searching for Unicode characters/strings (doesn't work correctly in Ruby 1.9.1):
            //
            // å == U+00E5 == (U+0061, U+030A)
            // string str = "combining mark: a\u030a";
            // Console.WriteLine(str.IndexOf("å")); // 16
            // Console.WriteLine(str.IndexOf('å')); // -1       
            //

            public override int IndexOf(char c, int start, int count) {
                return Array.IndexOf(_data, c, start, count);
            }

            public override int IndexOf(byte b, int start, int count) {
                return SwitchToBinary().IndexOf(b, start, count);
            }

            public override int IndexOf(string/*!*/ str, int start, int count) {
                // TODO: Unfortunately, BCL doesn't provide IndexOf on char[] (see CompareInfo):
                return ToString().IndexOf(str, start, count, StringComparison.Ordinal);
            }

            public override int IndexOf(byte[]/*!*/ bytes, int start, int count) {
                return SwitchToBinary().IndexOf(bytes, start, count);
            }

            public override int IndexIn(Content/*!*/ str, int start, int count) {
                return str.IndexOf(ToString(), start, count);
            }

            #endregion

            #region LastIndexOf (read-only)

            public override int LastIndexOf(char c, int start, int count) {
                return Array.LastIndexOf(_data, c, start, count);
            }

            public override int LastIndexOf(byte b, int start, int count) {
                return SwitchToBinary().LastIndexOf(b, start, count);
            }

            public override int LastIndexOf(string/*!*/ str, int start, int count) {
                // TODO: Unfortunately, BCL doesn't provide IndexOf on char[] (see CompareInfo):
                return ToString().LastIndexOf(str, start, count, StringComparison.Ordinal);
            }

            public override int LastIndexOf(byte[]/*!*/ bytes, int start, int count) {
                return SwitchToBinary().LastIndexOf(bytes, start, count);
            }

            public override int LastIndexIn(Content/*!*/ str, int start, int count) {
                return str.LastIndexOf(ToString(), start, count);
            }

            #endregion

            #region Append

            public override void Append(char c, int repeatCount) {
                _count = Utils.Append(ref _data, _count, c, repeatCount);
            }

            public override void Append(byte b, int repeatCount) {
                SwitchToBinary(repeatCount).Append(b, repeatCount);
            }

            public override void Append(string/*!*/ str, int start, int count) {
                _count = Utils.Append(ref _data, _count, str, start, count);
            }

            public override void Append(char[]/*!*/ chars, int start, int count) {
                _count = Utils.Append(ref _data, _count, chars, start, count);
            }

            public override void Append(byte[]/*!*/ bytes, int start, int count) {
                SwitchToBinary(count).Append(bytes, start, count);
            }

            public override void Append(Stream/*!*/ stream, int count) {
                SwitchToBinary(count).Append(stream, count);
            }

            public override void AppendFormat(IFormatProvider provider, string/*!*/ format, object[]/*!*/ args) {
                var formatted = String.Format(provider, format, args);
                Append(formatted, 0, formatted.Length);
            }

            // this + content[start, count]
            public override void Append(Content/*!*/ content, int start, int count) {
                content.AppendTo(this, start, count);
            }

            // content.bytes + this.chars[start, count]
            public override void AppendTo(BinaryContent/*!*/ content, int start, int count) {
                if (start > _count - count) {
                    throw new ArgumentOutOfRangeException("start");
                }

                content.AppendBytes(_data, start, count);
            }

            // content.chars + this.chars[start, count]
            public override void AppendTo(CharArrayContent/*!*/ content, int start, int count) {
                if (start > _count - count) {
                    throw new ArgumentOutOfRangeException("start");
                }

                content.Append(_data, start, count);
            }

            // content.chars + this.chars[start, count]
            public override void AppendTo(StringContent/*!*/ content, int start, int count) {
                if (start > _count - count) {
                    throw new ArgumentOutOfRangeException("start");
                }

                content.Append(_data, start, count);
            }

            #endregion

            #region Insert

            public override void Insert(int index, char c) {
                _count = Utils.InsertAt(ref _data, _count, index, c, 1);
            }

            public override void Insert(int index, byte b) {
                if (_owner.HasByteCharacters) {
                    Debug.Assert(b < 0x80 || _owner.IsBinaryEncoded);
                    Insert(index, (char)b);
                } else {
                    SwitchToBinary(1).Insert(index, b);
                }
            }

            public override void Insert(int index, string/*!*/ str, int start, int count) {
                _count = Utils.InsertAt(ref _data, _count, index, str, start, count);
            }

            public override void Insert(int index, char[]/*!*/ chars, int start, int count) {
                _count = Utils.InsertAt(ref _data, _count, index, chars, start, count);
            }

            public override void Insert(int index, byte[]/*!*/ bytes, int start, int count) {
                SwitchToBinary(count).Insert(index, bytes, start, count);
            }

            public override void InsertTo(Content/*!*/ str, int index, int start, int count) {
                str.Insert(index, _data, start, count);
            }

            // requires: encoding is ascii-identity
            public override void SetByte(int index, byte b) {
                if (_owner.HasByteCharacters) {
                    Debug.Assert(b < 0x80 || _owner.IsBinaryEncoded);
                    DataSetChar(index, (char)b);
                } else {
                    SwitchToBinary().SetByte(index, b);
                }
            }

            public override void SetChar(int index, char c) {
                DataSetChar(index, c);
            }

            #endregion

            #region Remove, Write

            public override void Remove(int start, int count) {
                _count = Utils.Remove(ref _data, _count, start, count);
            }

            public override void Write(int offset, byte[]/*!*/ value, int start, int count) {
                SwitchToBinary().Write(offset, value, start, count);
            }

            public override void Write(int offset, byte value, int repeatCount) {
                SwitchToBinary().Write(offset, value, repeatCount);
            }

            #endregion
        }
    }
}
