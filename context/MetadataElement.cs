using System;
using System.Text;
using Health.Direct.Context.Utils;
using MimeKit.Utils;

namespace Health.Direct.Context
{
    public class MetadataElement
    {
        internal static readonly byte[] Colon = { (byte)':' };

        string textValue;
        byte[] rawValue;

        /// <summary>
        /// Gets the stream offset of the beginning of the MetadataElement.
        /// </summary>
        /// <remarks>
        /// If the offset is set, it refers to the byte offset where it
        /// was found in the stream it was parsed from.
        /// </remarks>
        /// <value>The stream offset.</value>
        public long? Offset
        {
            get; internal set;
        }

        public string Field
        {
            get; private set;
        }

        public string Value
        {
            get
            {
                if (textValue == null)
                    textValue = Unfold(Rfc2047.DecodeText(rawValue));

                return textValue;
            }
            set
            {
                SetValue(Encoding.UTF8, value);
            }
        }

        public MetadataElement(string parameter, string value)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            if (parameter.Length == 0)
                throw new ArgumentException("Metadata parameter names are not allowed to be empty.", nameof(parameter));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Field = parameter;
            textValue = value;
        }

        protected internal MetadataElement(byte[] field, byte[] value, bool invalid)
        {
            var chars = new char[field.Length];
            int count = 0;

            while (count < field.Length && (invalid || !field[count].IsBlank()))
            {
                chars[count] = (char)field[count];
                count++;
            }
            
            rawValue = value;

            Field = new string(chars, 0, count);
            //Id = Field.ToHeaderId(); // could be used to identify ContentElements 
            IsInvalid = invalid;
        }

        internal bool IsInvalid
        {
            get; private set;
        }

        public void SetValue(Encoding encoding, string value)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            textValue = Unfold(value.Trim());
        }

        public static unsafe string Unfold(string text)
        {
            int startIndex;
            int endIndex;
            int i = 0;

            if (text == null)
                return string.Empty;

            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;

            if (i == text.Length)
                return string.Empty;

            startIndex = i;
            endIndex = i;

            while (i < text.Length)
            {
                if (!char.IsWhiteSpace(text[i++]))
                    endIndex = i;
            }

            int count = endIndex - startIndex;
            char[] chars = new char[count];

            fixed (char* outbuf = chars)
            {
                char* outptr = outbuf;

                for (i = startIndex; i < endIndex; i++)
                {
                    if (text[i] != '\r' && text[i] != '\n')
                    {
                        if (i > 0 && text[i - 1] == '\n' && text[i] == ' ')
                        {
                            continue;
                        }

                        *outptr++ = text[i];
                    }
                }

                count = (int)(outptr - outbuf);
            }

            return new string(chars, 0, count);
        }

        static bool IsAsciiAtom(byte c)
        {
            return c.IsAsciiAtom();
        }

        static bool IsControl(byte c)
        {
            return c.IsCtrl();
        }

        static bool IsBlank(byte c)
        {
            return c.IsBlank();
        }


        internal static unsafe bool TryParse(byte* input, int length, bool strict, out MetadataElement header)
        {
            byte* inend = input + length;
            byte* start = input;
            byte* inptr = input;
            var invalid = false;

            // find the end of the field name
            if (strict)
            {
                while (inptr < inend && IsAsciiAtom(*inptr))
                    inptr++;
            }
            else
            {
                while (inptr < inend && *inptr != (byte)':' && !IsControl(*inptr))
                    inptr++;
            }

            while (inptr < inend && IsBlank(*inptr))
                inptr++;

            if (inptr == inend || *inptr != ':')
            {
                if (strict)
                {
                    header = null;
                    return false;
                }

                invalid = true;
                inptr = inend;
            }

            var field = new byte[(int)(inptr - start)];
            fixed (byte* outbuf = field)
            {
                byte* outptr = outbuf;

                while (start < inptr)
                    *outptr++ = *start++;
            }

            byte[] value;

            if (inptr < inend)
            {
                inptr++;

                int count = (int)(inend - inptr);
                value = new byte[count];

                fixed (byte* outbuf = value)
                {
                    byte* outptr = outbuf;

                    while (inptr < inend)
                        *outptr++ = *inptr++;
                }
            }
            else
            {
                value = new byte[0];
            }

            header = new MetadataElement(field, value, invalid);

            return true;
        }
	}
}
