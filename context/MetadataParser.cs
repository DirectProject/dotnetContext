using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Health.Direct.Context.Utils;
using MimeKit.IO;

namespace Health.Direct.Context
{
    public class MetadataParser
    {
        const int ReadAheadSize = 128;
        const int BlockSize = 4096; // bigger than metadata expected.
        const int PadSize = 4;

        // I/O buffering
        readonly byte[] input = new byte[ReadAheadSize + BlockSize + PadSize];
        const int InputStart = ReadAheadSize;
        private int inputIndex;
        private int inputEnd;

        // message/rfc822 mbox markers (shouldn't exist, but sometimes do)
        byte[] preHeaderBuffer = new byte[128];
        int preHeaderLength;

        // metadata buffer
        byte[] metadataBuffer = new byte[512];
        long metadataOffset;
        int metadataIndex;

        readonly List<MetadataElement> metadataElements = new List<MetadataElement>();

        bool eos;
        
        Stream stream;
        long position;

        public MetadataParser(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            this.stream = stream;
            inputIndex = InputStart;
            inputEnd = InputStart;

            position = stream.CanSeek ? stream.Position : 0;
            metadataElements.Clear();
            metadataOffset = 0;
            metadataIndex = 0;
        }

        public List<MetadataElement> ParseMessage()
        {
            unsafe
            {
                fixed (byte* inbuf = input)
                {
                    return ParseMessage(inbuf);
                }
            }
        }

        // StepHeaders
        private unsafe List<MetadataElement> ParseMessage(byte* inbuf, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool scanningFieldName = true;
            bool checkFolded = false;
            bool midline = false;
            bool blank = false;
            bool valid = true;
            int left = 0;

            
            ResetRawMetadataData();
            metadataElements.Clear();

            ReadAhead(ReadAheadSize, 0, cancellationToken);

            do
            {
                if (!StepHeaders(inbuf, ref scanningFieldName, ref checkFolded, ref midline, ref blank, ref valid, ref left))
                    break;

                // BlockSize is 4096 much bigger than expected metadata size.
                // So shouldn't need to code the rest of this.
                //
                // var available = ReadAhead(left + 1, 0, cancellationToken);
                // ...
                //

            } while (true);


            return metadataElements;
        }

        static int NextAllocSize(int need)
        {
            // Always grow int to the next 64 bit block.
            // 4096 becomes 4096
            // 4090 becomes 4096
            // 4097 becomes 4160
            return (need + 63) & ~63;
        }

        void AppendRawHeaderData(int startIndex, int length)
        {
            int left = metadataBuffer.Length - metadataIndex;

            if (left < length)
                Array.Resize(ref metadataBuffer, NextAllocSize(metadataIndex + length));

            Buffer.BlockCopy(input, startIndex, metadataBuffer, metadataIndex, length);
            metadataIndex += length;
        }


        unsafe void ParseAndAppendHeader()
        {
            if (metadataIndex == 0)
                return;

            fixed (byte* buf = metadataBuffer)
            {
                if (MetadataElement.TryParse(buf, metadataIndex, false, out var metadataElement))
                {
                    metadataElement.Offset = metadataOffset;
                    metadataElements.Add(metadataElement);
                    metadataIndex = 0;
                }
            }
        }

        static bool IsControl(byte c)
        {
            return c.IsCtrl();
        }

        static bool IsBlank(byte c)
        {
            return c.IsBlank();
        }

        static unsafe bool IsEoln(byte* text)
        {
            if (*text == (byte)'\r')
                text++;

            return *text == (byte)'\n';
        }

        unsafe bool StepHeaders(byte* inbuf, ref bool scanningFieldName, ref bool checkFolded, ref bool midline, ref bool blank, ref bool valid, ref int left)
        {
            byte* inptr = inbuf + inputIndex;
            byte* inend = inbuf + inputEnd;
            bool needInput = false;
            long length;
            bool eoln;

            *inend = (byte)'\n';

            while (inptr < inend)
            {
                byte* start = inptr;

                // if we are scanning a new line, check for a folded header
                if (!midline && checkFolded && !IsBlank(*inptr))
                {
                    ParseAndAppendHeader();

                    metadataOffset = GetOffset((int)(inptr - inbuf));
                    scanningFieldName = true;
                    checkFolded = false;
                    blank = false;
                    valid = true;
                }

                eoln = IsEoln(inptr);
                if (scanningFieldName && !eoln)
                {
                    // scan and validate the field name
                    if (*inptr != (byte)':')
                    {
                        *inend = (byte)':';

                        while (*inptr != (byte)':')
                        {
                            // Blank spaces are allowed between the field name and
                            // the ':', but field names themselves are not allowed
                            // to contain spaces.
                            if (IsBlank(*inptr))
                            {
                                blank = true;
                            }
                            else if (blank || IsControl(*inptr))
                            {
                                valid = false;
                                break;
                            }

                            inptr++;
                        }

                        if (inptr == inend)
                        {
                            // we don't have enough input data; restore state back to the beginning of the line
                            left = (int)(inend - start);
                            inputIndex = (int)(start - inbuf);
                            needInput = true;
                            break;
                        }

                        *inend = (byte)'\n';
                    }
                    else
                    {
                        valid = false;
                    }

                    if (!valid)
                    {
                        if (metadataElements.Count == 0)
                        {
                            //First metadataElement with no value parts
                            inputIndex = (int)(start - inbuf);
                            metadataIndex = 0;
                            return false;
                        }
                    }
                }

                scanningFieldName = false;

                while (*inptr != (byte)'\n')
                    inptr++;

                if (inptr == inend)
                {
                    // we didn't manage to slurp up a full line, save what we have and refill our input buffer
                    length = inptr - start;

                    if (inptr > start)
                    {
                        // Note: if the last byte we got was a '\r', rewind a byte
                        inptr--;
                        if (*inptr == (byte)'\r')
                            length--;
                        else
                            inptr++;
                    }

                    if (length > 0)
                    {
                        AppendRawHeaderData((int)(start - inbuf), (int)length);
                        midline = true;
                    }

                    inputIndex = (int)(inptr - inbuf);
                    left = (int)(inend - inptr);
                    needInput = true;
                    break;
                }
                
                length = (inptr + 1) - start;

                if (!valid && metadataElements.Count == 0)
                {
                    if (length > 0 && preHeaderLength == 0)
                    {
                        if (inptr[-1] == (byte)'\r')
                            length--;
                        length--;

                        preHeaderLength = (int)length;

                        if (preHeaderLength > preHeaderBuffer.Length)
                            Array.Resize(ref preHeaderBuffer, NextAllocSize(preHeaderLength));

                        Buffer.BlockCopy(input, (int)(start - inbuf), preHeaderBuffer, 0, preHeaderLength);
                    }
                    scanningFieldName = true;
                    checkFolded = false;
                    blank = false;
                    valid = true;
                }
                else
                {
                    AppendRawHeaderData((int)(start - inbuf), (int)length);
                    checkFolded = true;
                }

                midline = false;
                inptr++;
            }

            if (!needInput)
            {
                ParseAndAppendHeader(); // Last load.  No boundary like MimeParser where I modeled this routine from.
                
                //todo: can I remove these two lines.
                inputIndex = (int)(inptr - inbuf);
                left = (int)(inend - inptr);

                return false;
            }

            return true;
        }


        long GetOffset(int index)
        {
            if (position == -1)
                return -1;

            return position - (inputEnd - index);
        }

        void ResetRawMetadataData()
        {
            metadataIndex = 0;
        }

        int ReadAhead(int atleast, int save, CancellationToken cancellationToken)
        {
            int nread, left, start, end;

            if (!AlignReadAheadBuffer(atleast, save, out left, out start, out end))
                return left;

            // use the cancellable stream interface if available...
            var cancellable = stream as ICancellableStream;
            if (cancellable != null)
            {
                nread = cancellable.Read(input, start, end - start, cancellationToken);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                nread = stream.Read(input, start, end - start);
            }

            if (nread > 0)
            {
                inputEnd += nread;
                position += nread;
            }
            else
            {
                eos = true;
            }

            return inputEnd - inputIndex;
        }

        bool AlignReadAheadBuffer(int atleast, int save, out int left, out int start, out int end)
        {
            left = inputEnd - inputIndex;
            start = InputStart;
            end = inputEnd;

            if (left >= atleast || eos)
                return false;

            left += save;

            if (left > 0)
            {
                int index = inputIndex - save;

                // attempt to align the end of the remaining input with ReadAheadSize
                if (index >= start)
                {
                    start -= Math.Min(ReadAheadSize, left);
                    Buffer.BlockCopy(input, index, input, start, left);
                    index = start;
                    start += left;
                }
                else if (index > 0)
                {
                    int shift = Math.Min(index, end - start);
                    Buffer.BlockCopy(input, index, input, index - shift, left);
                    index -= shift;
                    start = index + left;
                }
                else
                {
                    // we can't shift...
                    start = end;
                }

                inputIndex = index + save;
                inputEnd = start;
            }
            else
            {
                inputIndex = start;
                inputEnd = start;
            }

            end = input.Length - PadSize;

            return true;
        }
    }
}