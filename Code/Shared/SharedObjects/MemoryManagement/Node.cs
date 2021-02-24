// MIT License
//
// Copyright (c) 2021 Oleg Mikhailov
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.IO.MemoryMappedFiles;

namespace CorpusCallosum.SharedObjects.MemoryManagement
{
    internal struct Node
    {
        public Node(long next, long lenght)
        {
            _next = next;

            _length = lenght;
        }

        private long _next; // backing fields in structs are required to get around a bug in UWP compiler

        public long Next
        {
            get
            {
                return _next;
            }
            set
            {
                _next = value;
            }
        }

        private long _length;

        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        public static Node Read(MemoryMappedViewAccessor view, long offset = 0)
        {
#if DOTNETSTANDARD_1_3
            return new Node(view.ReadInt64(offset), view.ReadInt64(offset + sizeof(long)));
#else
            Node result;

            view.Read(offset, out result);

            return result;
#endif
        }

        public static void Write(MemoryMappedViewAccessor view, ref Node node, long offset = 0)
        {
#if DOTNETSTANDARD_1_3
            view.Write(offset, node.Next);
            
            view.Write(offset + sizeof(long), node.Length);
#else
            view.Write(offset, ref node);
#endif
        }

        public static Node Read(MemoryMappedFile file, long offset)
        {
            using (var view = file.CreateViewAccessor(offset, sizeof(long) + sizeof(long))) return Read(view);
        }

        public static void Write(MemoryMappedFile file, long offset, ref Node node)
        {
            using (var view = file.CreateViewAccessor(offset, sizeof(long) + sizeof(long))) Write(view, ref node);
        }
    }
}
