﻿//
// Copyright (c) 2008-2010, Kenneth Bell
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

namespace DiscUtils.Ext2
{
    using System;

    internal class SuperBlock : IByteArraySerializable
    {
        public const ushort Ext2Magic = 0xEF53;

        public uint InodesCount;
        public uint BlocksCount;
        public uint ReservedBlocksCount;
        public uint FreeBlocksCount;
        public uint FreeInodesCount;
        public uint FirstDataBlock;
        public uint LogBlockSize;
        public uint LogFragSize;
        public uint BlocksPerGroup;
        public uint FragsPerGroup;
        public uint InodesPerGroup;
        public uint MountTime;
        public uint WriteTime;
        public ushort MountCount;
        public ushort MaxMountCount;
        public ushort Magic;
        public ushort State;
        public ushort Errors;
        public ushort MinorRevisionLevel;
        public uint LastCheckTime;
        public uint CheckInterval;
        public uint CreatorOS;
        public uint RevisionLevel;
        public ushort DefaultReservedBlockUid;
        public ushort DefaultReservedBlockGid;

        public uint FirstInode;
        public ushort InodeSize;
        public ushort BlockGroupNumber;
        public uint CompatibleFeatures;
        public uint IncompatibleFeatures;
        public uint ReadOnlyCompatibleFeatures;
        public Guid UniqueId;
        public string VolumeName;
        public string LastMountPoint;
        public uint CompressionAlgorithmUsageBitmap;

        public byte PreallocateBlockCount;
        public byte DirPreallocateBlockCount;

        public Guid JournalSuperBlockUniqueId;
        public uint JournalInode;
        public uint JournalDevice;
        public uint LastOrphan;
        public uint[] HashSeed;
        public byte DefaultHashVersion;
        public uint DefaultMountOptions;
        public uint FirstMetablockBlockGroup;

        public uint BlockSize
        {
            get { return (uint)(1024 << (int)LogBlockSize); }
        }

        public int Size
        {
            get { return 1024; }
        }

        public int ReadFrom(byte[] buffer, int offset)
        {
            InodesCount = Utilities.ToUInt32LittleEndian(buffer, offset + 0);
            BlocksCount = Utilities.ToUInt32LittleEndian(buffer, offset + 4);
            ReservedBlocksCount = Utilities.ToUInt32LittleEndian(buffer, offset + 8);
            FreeBlocksCount = Utilities.ToUInt32LittleEndian(buffer, offset + 12);
            FreeInodesCount = Utilities.ToUInt32LittleEndian(buffer, offset + 16);
            FirstDataBlock = Utilities.ToUInt32LittleEndian(buffer, offset + 20);
            LogBlockSize = Utilities.ToUInt32LittleEndian(buffer, offset + 24);
            LogFragSize = Utilities.ToUInt32LittleEndian(buffer, offset + 28);
            BlocksPerGroup = Utilities.ToUInt32LittleEndian(buffer, offset + 32);
            FragsPerGroup = Utilities.ToUInt32LittleEndian(buffer, offset + 36);
            InodesPerGroup = Utilities.ToUInt32LittleEndian(buffer, offset + 40);
            MountTime = Utilities.ToUInt32LittleEndian(buffer, offset + 44);
            WriteTime = Utilities.ToUInt32LittleEndian(buffer, offset + 48);
            MountCount = Utilities.ToUInt16LittleEndian(buffer, offset + 52);
            MaxMountCount = Utilities.ToUInt16LittleEndian(buffer, offset + 54);
            Magic = Utilities.ToUInt16LittleEndian(buffer, offset + 56);
            State = Utilities.ToUInt16LittleEndian(buffer, offset + 58);
            Errors = Utilities.ToUInt16LittleEndian(buffer, offset + 60);
            MinorRevisionLevel = Utilities.ToUInt16LittleEndian(buffer, offset + 62);
            LastCheckTime = Utilities.ToUInt32LittleEndian(buffer, offset + 64);
            CheckInterval = Utilities.ToUInt32LittleEndian(buffer, offset + 68);
            CreatorOS = Utilities.ToUInt32LittleEndian(buffer, offset + 72);
            RevisionLevel = Utilities.ToUInt32LittleEndian(buffer, offset + 76);
            DefaultReservedBlockUid = Utilities.ToUInt16LittleEndian(buffer, offset + 80);
            DefaultReservedBlockGid = Utilities.ToUInt16LittleEndian(buffer, offset + 82);

            FirstInode = Utilities.ToUInt32LittleEndian(buffer, offset + 84);
            InodeSize = Utilities.ToUInt16LittleEndian(buffer, offset + 88);
            BlockGroupNumber = Utilities.ToUInt16LittleEndian(buffer, offset + 90);
            CompatibleFeatures = Utilities.ToUInt32LittleEndian(buffer, offset + 92);
            IncompatibleFeatures = Utilities.ToUInt32LittleEndian(buffer, offset + 96);
            ReadOnlyCompatibleFeatures = Utilities.ToUInt32LittleEndian(buffer, offset + 100);
            UniqueId = Utilities.ToGuidLittleEndian(buffer, offset + 104);
            VolumeName = Utilities.BytesToString(buffer, offset + 120, 16).Trim('\0');
            LastMountPoint = Utilities.BytesToString(buffer, offset + 136, 64).Trim('\0');
            CompressionAlgorithmUsageBitmap = Utilities.ToUInt32LittleEndian(buffer, offset + 200);

            PreallocateBlockCount = buffer[offset + 204];
            DirPreallocateBlockCount = buffer[offset + 205];

            JournalSuperBlockUniqueId = Utilities.ToGuidLittleEndian(buffer, offset + 208);
            JournalInode = Utilities.ToUInt32LittleEndian(buffer, offset + 224);
            JournalDevice = Utilities.ToUInt32LittleEndian(buffer, offset + 228);
            LastOrphan = Utilities.ToUInt32LittleEndian(buffer, offset + 232);
            HashSeed = new uint[4];
            HashSeed[0] = Utilities.ToUInt32LittleEndian(buffer, offset + 236);
            HashSeed[1] = Utilities.ToUInt32LittleEndian(buffer, offset + 240);
            HashSeed[2] = Utilities.ToUInt32LittleEndian(buffer, offset + 244);
            HashSeed[3] = Utilities.ToUInt32LittleEndian(buffer, offset + 248);
            DefaultHashVersion = buffer[offset + 252];
            DefaultMountOptions = Utilities.ToUInt32LittleEndian(buffer, offset + 256);
            FirstMetablockBlockGroup = Utilities.ToUInt32LittleEndian(buffer, offset + 260);

            return 1024;
        }

        public void WriteTo(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
