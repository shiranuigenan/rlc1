using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace ContainerPacking
{
    /// <summary>
    /// The container packing service.
    /// </summary>
    public static class PackingService
    {
        /// <summary>
        /// Attempts to pack the specified container with the specified items.
        /// </summary>
        /// <param name="container">container to pack.</param>
        /// <param name="itemsToPack">The items to pack.</param>
        /// <returns>A container packing result with lists of the packed and unpacked items.</returns>
        public static AlgorithmPackingResult Pack(Container container, List<Item> itemsToPack)
        {
            var algorithm = new EB_AFIT();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var algorithmResult = algorithm.Run(container, itemsToPack);
            stopwatch.Stop();

            algorithmResult.PackTimeInMilliseconds = stopwatch.ElapsedMilliseconds;

            var containerVolume = container.Length * container.Width * container.Height;
            var itemVolumePacked = algorithmResult.PackedItems.Sum(i => i.Volume);
            var itemVolumeUnpacked = algorithmResult.UnpackedItems.Sum(i => i.Volume);

            algorithmResult.PercentContainerVolumePacked = Math.Round(itemVolumePacked / containerVolume * 100, 2);
            algorithmResult.PercentItemVolumePacked = Math.Round(itemVolumePacked / (itemVolumePacked + itemVolumeUnpacked) * 100, 2);

            return algorithmResult;
        }
        public static void PackingResultToPng(Container container, AlgorithmPackingResult packingResult)
        {
            var xCell = (int)Math.Ceiling(container.Length / 5);
            var yCell = (int)Math.Ceiling(container.Width / 5);
            var zCell = (int)Math.Ceiling(container.Height / 5);

            var cells = new byte[xCell, yCell, zCell];

            foreach (var item in packingResult.PackedItems)
            {
                //Y ile Z yer değiştirildi, algoritmanın c# uyarlamasında bir sorun olabilir!!!
                var x1 = (int)Math.Ceiling(item.CoordX / 5);
                var y1 = (int)Math.Ceiling(item.CoordZ / 5);
                var z1 = (int)Math.Ceiling(item.CoordY / 5);

                var x2 = x1 + (int)Math.Ceiling(item.PackDimX / 5);
                var y2 = y1 + (int)Math.Ceiling(item.PackDimZ / 5);
                var z2 = z1 + (int)Math.Ceiling(item.PackDimY / 5);

                for (int x = x1; x < x2; x++)
                    for (int y = y1; y < y2; y++)
                        for (int z = z1; z < z2; z++)
                            cells[x, y, z] = (byte)item.ID;
            }

            RenderCellsIsometric(cells);
        }
        public static void RenderCellsIsometric(byte[,,] cells)
        {
            var xDim = cells.GetLength(0);
            var yDim = cells.GetLength(1);
            var zDim = cells.GetLength(2);

            var imageWidth = 4 * (xDim + yDim);
            var imageHeight = 2 * (xDim + yDim) + 5 * zDim - 1;

            var pixels = new byte[imageHeight, imageWidth];

            for (int z = 0; z < zDim; z++)
                for (int y = 0; y < yDim; y++)
                    for (int x = 0; x < xDim; x++)
                        DrawCell(pixels, (yDim - 1) * 4 + x * 4 - y * 4, 5 * (zDim - 1) + x * 2 + y * 2 - 5 * z, cells[x, y, z]);

            var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

            using (var bitmap = new Bitmap(imageWidth, imageHeight, imageWidth, PixelFormat.Format8bppIndexed, handle.AddrOfPinnedObject()))
            {
                bitmap.Palette = GlobalPalette();
                bitmap.Save("result.png", ImageFormat.Png);
            }
        }
        private static byte[,] cellUnit = new byte[8, 8] {
        { 0, 0, 1, 1, 1, 1, 0, 0 },
        { 1, 1, 1, 1, 1, 1, 1, 1 },
        { 2, 2, 1, 1, 1, 1, 3, 3 },
        { 2, 2, 2, 2, 3, 3, 3, 3 },
        { 2, 2, 2, 2, 3, 3, 3, 3 },
        { 2, 2, 2, 2, 3, 3, 3, 3 },
        { 2, 2, 2, 2, 3, 3, 3, 3 },
        { 0, 0, 2, 2, 3, 3, 0, 0 }};
        private static void DrawCell(byte[,] canvas, int x, int y, int id)
        {
            try
            {
                if (id < 1) id = 1;
                if (id > 48) id = 48;

                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 8; j++)
                        if (cellUnit[j, i] != 0)
                            canvas[y + j, x + i] = (byte)(3 * (id - 1) + cellUnit[j, i]);

            }
            catch (System.Exception)
            {
                ;
            }

        }
        private static Bitmap paletteImage = null;
        private static ColorPalette GlobalPalette()
        {
            if (paletteImage == null)
            {
                if (!File.Exists("palette.gif"))
                    GeneratePalette();

                paletteImage = new Bitmap("palette.gif");
            }

            return paletteImage.Palette;
        }
        private static void GeneratePalette()
        {
            var pixel = 0;

            var handle = GCHandle.Alloc(pixel, GCHandleType.Pinned);
            var bitmap = new Bitmap(1, 1, 4, PixelFormat.Format8bppIndexed, handle.AddrOfPinnedObject());

            //tonlar
            var t = new byte[] { 0, 25, 51, 76, 102, 127, 153, 179, 204, 230, 255 };
            //sapmalar
            var s = new byte[] { 1, 5, 3, 7, 2, 4, 6, 8 };
            //palet index
            var k = 0;

            var p = bitmap.Palette;
            p.Entries[k++] = Color.FromArgb(0, 0, 0, 0);//transparent color

            //item renkleri
            for (int j = 0; j < 8; j++)
            {
                var i = s[j];

                p.Entries[k++] = Color.FromArgb(t[9], t[i], t[1]);
                p.Entries[k++] = Color.FromArgb(t[8], t[i - 1], t[0]);
                p.Entries[k++] = Color.FromArgb(t[10], t[i + 1], t[2]);

                p.Entries[k++] = Color.FromArgb(t[10 - i], t[9], t[1]);
                p.Entries[k++] = Color.FromArgb(t[9 - i], t[8], t[0]);
                p.Entries[k++] = Color.FromArgb(t[11 - i], t[10], t[1]);

                p.Entries[k++] = Color.FromArgb(t[1], t[9], t[i]);
                p.Entries[k++] = Color.FromArgb(t[0], t[8], t[i - 1]);
                p.Entries[k++] = Color.FromArgb(t[2], t[10], t[i + 1]);

                p.Entries[k++] = Color.FromArgb(t[1], t[10 - i], t[9]);
                p.Entries[k++] = Color.FromArgb(t[0], t[9 - i], t[8]);
                p.Entries[k++] = Color.FromArgb(t[2], t[11 - i], t[10]);

                p.Entries[k++] = Color.FromArgb(t[i], t[1], t[9]);
                p.Entries[k++] = Color.FromArgb(t[i - 1], t[0], t[8]);
                p.Entries[k++] = Color.FromArgb(t[i + 1], t[2], t[10]);

                p.Entries[k++] = Color.FromArgb(t[9], t[1], t[10 - i]);
                p.Entries[k++] = Color.FromArgb(t[8], t[0], t[9 - i]);
                p.Entries[k++] = Color.FromArgb(t[10], t[2], t[11 - i]);
            }

            //siyah beyaz tonlar
            var d = (256 - 1e-13) / 110;
            for (int i = 0; i < 111; i++)
            {
                var a = (int)(d * i);
                p.Entries[k++] = Color.FromArgb(a, a, a);
            }

            bitmap.Palette = p;

            bitmap.Save("palette.gif", ImageFormat.Gif);
        }
    }
}
