using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DiscUtils.Fat
{

    /// <summary>
    /// Represents a long file name.
    /// </summary>
    internal class LongFileName
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LongFileName"/> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <exception cref="System.ArgumentNullException">filename</exception>
        /// <exception cref="System.ArgumentException">filename</exception>
        public LongFileName(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (filename.Length == 0) throw new ArgumentException(nameof(filename));

            Filename = filename;
        }


        /// <summary>
        /// Gets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        public string Filename { get; }
    }

    /// <summary>
    /// Responsible for:
    /// - reading the stream
    /// </summary>
    internal static class DirectoryEntryReader
    {
        private const int EntrySize = 32;

        private const byte DeletedEntry = 0xe5;

        /// <summary>
        /// Reads one or more directory entries from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.InvalidOperationException">Stream is not readable.</exception>
        public static byte[] Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (!stream.CanRead)
            {
                throw new InvalidOperationException("Stream is not readable.");
            }

            byte[] buffer = ReadOneEntry(stream);

            // buffer could contain a long or short entry
            // if the entry is deleted
            if (!IsDeletedEntry(buffer) && IsLongEntry(buffer))
            {
                return ReadLongEntry(buffer, stream);
            }

            return buffer;
        }

        private static bool IsDeletedEntry(byte[] buffer)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Length == EntrySize);

            return buffer[0] == DeletedEntry;
        }

        private static byte[] ReadOneEntry(Stream stream)
        {
            Debug.Assert(stream != null);

            return Utilities.ReadFully(stream, EntrySize);
        }

        private static bool IsLongEntry(byte[] buffer)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Length == EntrySize);

            return LongDirectoryEntry.Attributes == LongDirectoryEntry.GetAttributes(buffer);
        }

        /// <summary>
        /// Reads all of the directory entries that make up long directory entry 
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <remarks>
        /// 
        ///   [ part  n   ]
        ///   [     n - 1 ]
        ///   [     n - 2 ]
        ///   [      ...  ]
        ///   [       1   ]
        ///   [      fat  ]
        /// </remarks>
        private static byte[] ReadLongEntry(byte[] entry, Stream stream)
        {
            Debug.Assert(entry != null);
            Debug.Assert(entry.Length == EntrySize);

            if ((entry[0] & LongDirectoryEntry.Masks.LastLongFileNameEntry) == 0)
            {
                throw new IOException("Expected to be on last entry.");
            }

            // get the number of vfat parts
            int n = entry[0] & LongDirectoryEntry.Masks.LongFileNameEntryNumber;

            // allocate memory for all of the vfat entries plus the fat entry
            var segments = CreateSegments(n + 1);

            // the vfat entries are stored in decscending order (last one first) ie,  n, n-1, n-2, ..., 1
            // store each of the vfat entries in logical order: 1, 2, ... n
            BlockCopy(entry, segments[0]);

            // by reading 1..n, the fat entry will be read also
            for (int i = 1; i <= n; i++)
            {
                entry = ReadOneEntry(stream);
                BlockCopy(entry, segments[i]);
            }

            return segments[0].Array;
        }

        private static void BlockCopy(byte[] source, ArraySegment<byte> destination)
        {
            System.Buffer.BlockCopy(source, 0, destination.Array, destination.Offset, source.Length);
        }

        /// <summary>
        /// Creates the specified number of segments. All of the segments will share the same underlying buffer.
        /// </summary>
        /// <param name="n">The number of segments to create.</param>
        /// <returns></returns>
        private static ArraySegment<byte>[] CreateSegments(int n)
        {
            byte[] entries = new byte[n * EntrySize];

            ArraySegment<byte>[] segments = new ArraySegment<byte>[n];
            for (int offset = 0, i = 0; offset < entries.Length; offset += EntrySize, i++)
            {
                segments[i] = new ArraySegment<byte>(entries, offset, EntrySize);
            }

            return segments;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class DirectoryEntryEncoding
    {
        public byte[] GetBytes(DirectoryEntry entry, Action<ArraySegment<byte>> writeToAction)
        {
            int entryCount = entry.EntryCount;
            ArraySegment<byte>[] entries = CreateEmptyEntries(entry.EntryCount);

            if (entry.Name.HasLongFilename)
            {
                var encoding = new UnicodeEncoding();
                string longName = entry.Name.GetLongName();
                bool isDeleted = entry.Name.IsDeleted();
                var filename = encoding.GetBytes(PadLongFilename(longName));

                // if the the entry is deleted, the checksum will be wrong
                // but that does not matter as deleted entries are skipped
                var checksum = GetChecksum(entry);

                for (int i = 0; i < entryCount - 1; i++)
                {
                    // 3, 2, 1
                    int ordinal = (entryCount - i - 1);

                    EncodeEntry(entries[i], ordinal, filename, checksum, i == 0, isDeleted);
                }
            }

            // call back to the directory entry to write it's data to the last segment
            writeToAction(entries[entries.Length - 1]);

            return entries[0].Array;
        }

        public DirectoryEntry GetDirectoryEntry(FatFileSystemOptions options, Stream stream, FatType fileSystemFatVariant)
        {
            byte[] bytes = DirectoryEntryReader.Read(stream);

            ArraySegment<byte>[] directoryEntries = CreateEmptyEntries(bytes);

            string longFilename = string.Empty;

            if (1 < directoryEntries.Length)
            {
                longFilename = ReadLongFilename(directoryEntries);
            }

            int lastIndex = directoryEntries.Length - 1;
            DirectoryEntry directoryEntry = DirectoryEntry.CreateFrom(options, directoryEntries[lastIndex], longFilename, fileSystemFatVariant);
            return directoryEntry;

        }


        private static string ReadLongFilename(ArraySegment<byte>[] directoryEntries)
        {
            Encoding encoding = new UnicodeEncoding();

            var filenameBuilder = new StringBuilder(256); // file names cannot be longer than 255 characters

            for (int index = directoryEntries.Length - 2; index >= 0; index--)
            {
                var directoryEntry = directoryEntries[index];

                var name0_4 = encoding.GetChars(directoryEntry.Array, directoryEntry.Offset + 1, 5 * 2);
                var name5_10 = encoding.GetChars(directoryEntry.Array, directoryEntry.Offset + 14, 6 * 2);
                var name11_12 = encoding.GetChars(directoryEntry.Array, directoryEntry.Offset + 28, 2 * 2);

                filenameBuilder.Append(name0_4);
                filenameBuilder.Append(name5_10);
                filenameBuilder.Append(name11_12);
            }

            var beforeTrim = filenameBuilder.ToString();

            TrimLongFilename(filenameBuilder);

            var afterTrim = filenameBuilder.ToString();

            var afterPad = PadLongFilename(afterTrim);

            Debug.Assert(beforeTrim == afterPad);

            var filename = filenameBuilder.ToString();
            return filename;
        }

        private static void TrimLongFilename(StringBuilder name)
        {
            while (name.Length > 0)
            {
                int end = name.Length - 1;

                if (name[end] == '\0' || name[end] == '\uffff')
                {
                    name.Remove(end, 1);
                }
                else
                {
                    break;
                }
            }
        }

        private static string PadLongFilename(string name)
        {
            if (name.Length % 13 == 0)
                return name;

            int padLength = (13 - name.Length % 13);

            StringBuilder builder = new StringBuilder(name, name.Length + padLength);
            builder.Append('\0');

            while (--padLength > 0)
            {
                builder.Append('\uffff');
            }

            return builder.ToString();
        }

        public static byte FilenameChecksum(ArraySegment<byte> segment)
        {
            var buffer = segment.Array;
            var offset = segment.Offset;
            return FilenameChecksum(buffer, offset);
        }

        private static byte FilenameChecksum(byte[] buffer, int offset)
        {
            byte sum = 0;

            for (int i = 0; i < 11; i++)
            {
                sum = (byte)((((sum & 1) << 7) | ((sum & 0xfe) >> 1)) + buffer[i + offset]);
            }

            return sum;
        }


        private ArraySegment<byte>[] CreateEmptyEntries(int count)
        {
            byte[] buffer = new byte[count * 32];

            ArraySegment<byte>[] entries = new ArraySegment<byte>[count];
            for (int offset = 0, i = 0; offset < buffer.Length; offset += 32, i++)
            {
                entries[i] = new ArraySegment<byte>(buffer, offset, 32);
            }

            return entries;
        }

        private ArraySegment<byte>[] CreateEmptyEntries(byte[] buffer)
        {
            ArraySegment<byte>[] entries = new ArraySegment<byte>[buffer.Length / 32];
            for (int offset = 0, i = 0; offset < buffer.Length; offset += 32, i++)
            {
                entries[i] = new ArraySegment<byte>(buffer, offset, 32);
            }

            return entries;
        }

        private byte GetChecksum(DirectoryEntry directoryEntry)
        {
            byte[] buffer = new byte[11];
            directoryEntry.Name.GetBytes(buffer, 0);
            byte checksum = FilenameChecksum(buffer, 0);
            return checksum;
        }

        private void EncodeEntry(ArraySegment<byte> entry, int ordinal, byte[] filename, byte shortNameChecksum, bool isLastEntry, bool isDeleted)
        {
            var buffer = entry.Array;
            var offset = entry.Offset;

            if (!isDeleted)
            {
                buffer[offset + LongDirectoryEntry.Offsets.Ordinal] = (byte)ordinal;
                if (isLastEntry)
                {
                    buffer[offset + LongDirectoryEntry.Offsets.Ordinal] |= LongDirectoryEntry.Masks.LastLongFileNameEntry;
                }
            }
            else
            {
                buffer[offset + LongDirectoryEntry.Offsets.Ordinal] = 0xE5;
            }

            buffer[offset + LongDirectoryEntry.Offsets.Flags] = (byte)(FatAttributes.ReadOnly | FatAttributes.Hidden | FatAttributes.System | FatAttributes.VolumeId);
            buffer[offset + LongDirectoryEntry.Offsets.Checksum] = shortNameChecksum;

            int nameIndex = (ordinal - 1) * 13 * 2; // 2 bytes per character
            System.Buffer.BlockCopy(filename, nameIndex + 0, buffer, offset + 1, 5 * 2);
            System.Buffer.BlockCopy(filename, nameIndex + 5 * 2, buffer, offset + 14, 6 * 2);
            System.Buffer.BlockCopy(filename, nameIndex + 5 * 2 + 6 * 2, buffer, offset + 28, 2 * 2);
        }
    }

    internal static class LongDirectoryEntry
    {
        /// <summary>
        /// Attributes set on a long vfat directory entry (ReadOnly, Hidden, System and VolumeId)
        /// </summary>
        public static FatAttributes Attributes = FatAttributes.ReadOnly | FatAttributes.Hidden |
                                                         FatAttributes.System | FatAttributes.VolumeId;
        public static class Masks
        {
            public const int LastLongFileNameEntry = 0x40;
            /// <summary>
            /// Mask to the lower 6 bits (0x3f).
            /// </summary>
            public const int LongFileNameEntryNumber = 0x3f;
        }

        public static FatAttributes GetAttributes(byte[] buffer)
        {
            return (FatAttributes)buffer[Offsets.Flags];
        }

        public static class Offsets
        {
            public const int Ordinal = 0x00;
            public const int Flags = 0x0b;
            public const int Checksum = 0x0d;
        }
    }

}
