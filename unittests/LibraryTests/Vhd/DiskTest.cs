﻿//
// Copyright (c) 2008, Kenneth Bell
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

using System;
using System.IO;
using NUnit.Framework;

namespace DiscUtils.Vhd
{
    [TestFixture]
    public class DiskTest
    {
        [Test]
        public void InitializeFixed()
        {
            MemoryStream ms = new MemoryStream();
            using (Disk disk = Disk.InitializeFixed(ms, 8 * 1024 * 1024))
            {
                Assert.IsNotNull(disk);
                Assert.That(disk.Geometry.Capacity > 7.5 * 1024 * 1024 && disk.Geometry.Capacity < 8 * 1024 * 1024);
                Assert.That(disk.Geometry.Capacity == disk.Content.Length);
            }

            // Check the stream is still valid
            ms.ReadByte();
            ms.Dispose();
        }

        [Test]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void InitializeFixedOwnStream()
        {
            MemoryStream ms = new MemoryStream();
            using (Disk disk = Disk.InitializeFixed(ms, true, 8 * 1024 * 1024))
            {
            }
            ms.ReadByte();
        }

        [Test]
        public void InitializeDynamic()
        {
            MemoryStream ms = new MemoryStream();
            using (Disk disk = Disk.InitializeDynamic(ms, 16 * 1024L * 1024 * 1024))
            {
                Assert.IsNotNull(disk);
                Assert.That(disk.Geometry.Capacity > 15.8 * 1024L * 1024 * 1024 && disk.Geometry.Capacity < 16 * 1024L * 1024 * 1024);
                Assert.That(disk.Geometry.Capacity == disk.Content.Length);
            }
            Assert.Greater(1 * 1024 * 1024, ms.Length);
        }

        [Test]
        public void InitializeDifferencing()
        {
            MemoryStream baseStream = new MemoryStream();
            MemoryStream diffStream = new MemoryStream();
            DiskImageFile baseFile = DiskImageFile.InitializeDynamic(baseStream, 16 * 1024L * 1024 * 1024);
            using (Disk disk = Disk.InitializeDifferencing(diffStream, baseFile, @"C:\TEMP\Base.vhd", @".\Base.vhd", DateTime.UtcNow))
            {
                Assert.IsNotNull(disk);
                Assert.That(disk.Geometry.Capacity > 15.8 * 1024L * 1024 * 1024 && disk.Geometry.Capacity < 16 * 1024L * 1024 * 1024);
                Assert.That(disk.Geometry.Capacity == disk.Content.Length);
                Assert.AreEqual(2, disk.Layers.Count);
            }
            Assert.Greater(1 * 1024 * 1024, diffStream.Length);
        }

        [Test]
        public void ConstructorDynamic()
        {
            Geometry geometry;
            MemoryStream ms = new MemoryStream();
            using (Disk disk = Disk.InitializeDynamic(ms, 16 * 1024L * 1024 * 1024))
            {
                geometry = disk.Geometry;
            }
            using (Disk disk = new Disk(ms))
            {
                Assert.AreEqual(geometry, disk.Geometry);
                Assert.IsNotNull(disk.Content);
            }
            using (Disk disk = new Disk(ms, true))
            {
                Assert.AreEqual(geometry, disk.Geometry);
                Assert.IsNotNull(disk.Content);
            }
        }
    }
}