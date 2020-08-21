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
        private static float cos30 = (float)Math.Cos(Math.PI / 6.0);
        public static PointF toIsometric(float x, float y, float z)
        {
            return new PointF(0.5f * (x - y) * cos30, 0.25f * (x + y) + 0.5f * z);
        }
        public static void DrawBox(Graphics g, float x, float y, float z, float length, float width, float height, int id, bool solid = true)
        {
            if (id < 1) id = 1;
            if (id > 192) id = 192;

            var isometricPos0 = toIsometric(x, y, z);
            var isometricPos1 = toIsometric(x, y, z + height);
            var isometricPos2 = toIsometric(x, y + width, z + height);
            var isometricPos3 = toIsometric(x, y + width, z);
            var isometricPos4 = toIsometric(x + length, y, z);
            var isometricPos5 = toIsometric(x + length, y, z + height);
            var isometricPos6 = toIsometric(x + length, y + width, z + height);
            var isometricPos7 = toIsometric(x + length, y + width, z);

            var palette = ItemPalette();
            if (solid)
                using (var brush = new SolidBrush(palette.Entries[id - 1]))
                {
                    g.FillPolygon(brush, new PointF[] {
                isometricPos0,
                isometricPos4,
                isometricPos5,
                isometricPos6,
                isometricPos2,
                isometricPos3 });
                }

            using (var pen = new Pen(Color.FromArgb(palette.Entries[id - 1].R / 2, palette.Entries[id - 1].G / 2, palette.Entries[id - 1].B / 2)))
            {
                g.DrawLine(pen, isometricPos2, isometricPos3);
                g.DrawLine(pen, isometricPos3, isometricPos0);
                g.DrawLine(pen, isometricPos0, isometricPos4);
                g.DrawLine(pen, isometricPos3, isometricPos7);
                g.DrawLine(pen, isometricPos2, isometricPos6);
                g.DrawLine(pen, isometricPos4, isometricPos5);
                g.DrawLine(pen, isometricPos5, isometricPos6);
                g.DrawLine(pen, isometricPos6, isometricPos7);
                g.DrawLine(pen, isometricPos7, isometricPos4);
            }

            // var f = new Font(FontFamily.GenericMonospace, 20.0f);

            // g.DrawString("0", f, Brushes.Green, isometricPos0);
            // g.DrawString("1", f, Brushes.Green, isometricPos1);
            // g.DrawString("2", f, Brushes.Green, isometricPos2);
            // g.DrawString("3", f, Brushes.Green, isometricPos3);
            // g.DrawString("4", f, Brushes.Green, isometricPos4);
            // g.DrawString("5", f, Brushes.Green, isometricPos5);
            // g.DrawString("6", f, Brushes.Green, isometricPos6);
            // g.DrawString("7", f, Brushes.Green, isometricPos7);

        }
        public static void RenderPackingResult(Container container, AlgorithmPackingResult packingResult)
        {
            var isometricMaxX = toIsometric((float)container.Length, 0, 0);
            var isometricMinX = toIsometric(0, (float)container.Width, 0);
            var isometricMaxY = toIsometric((float)container.Length, (float)container.Width, (float)container.Height);

            var border = 18;

            var imageWidth = (int)Math.Ceiling(2 * border + isometricMaxX.X - isometricMinX.X);
            var imageHeight = (int)Math.Ceiling(2 * border + isometricMaxY.Y);

            using (var b = new Bitmap(imageWidth, imageHeight))
            {
                using (var g = Graphics.FromImage(b))
                {
                    g.TranslateTransform(border - isometricMinX.X, border);

                    // int k = 1;
                    // for (int z = 1; z >= 0; z--)
                    //     for (int x = 0; x < 6; x++)
                    //         for (int y = 0; y < 4; y++)
                    //             DrawBox(g, x * 50, y * 50, z * 50, 50, 50, 50, k++);

                    //DrawBox(g, 0, 0, 0, (float)container.Length, (float)container.Width, (float)container.Height, 6);

                    int p = 0;
                    foreach (var item in packingResult.PackedItems.OrderByDescending(x => x.CoordY).OrderBy(x => x.CoordZ).OrderBy(x => x.CoordX).ToList())
                    {
                        DrawBox(g,
                        (float)item.CoordX,
                        (float)item.CoordZ,
                        (float)container.Height - (float)item.CoordY,
                        (float)item.PackDimX,
                        (float)item.PackDimZ,
                        (float)item.PackDimY,
                        item.ID);
                        b.Save("a" + (p++).ToString("D2") + ".png", ImageFormat.Png);
                    }
                }
                //b.Save("a.png", ImageFormat.Png);
            }
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
        private static ColorPalette ItemPalette()
        {
            if (paletteImage == null)
            {
                if (!File.Exists("palette.gif"))
                    GeneratePalette2();

                paletteImage = new Bitmap("palette.gif");
            }

            return paletteImage.Palette;
        }
        private static void GeneratePalette2()
        {
            var pixel = new byte[256];
            for (int j = 0; j < 32; j++)
                for (int i = 0; i < 6; i++)
                    pixel[j * 8 + i] = (byte)(j * 6 + i);

            var handle = GCHandle.Alloc(pixel, GCHandleType.Pinned);
            using (var bitmap = new Bitmap(6, 32, 8, PixelFormat.Format8bppIndexed, handle.AddrOfPinnedObject()))
            {
                //tonlar
                var t = new byte[] { 0, 7, 15, 23, 31, 39, 47, 55, 63, 71, 79, 87, 95, 103, 111, 119, 127, 135, 143, 151, 159, 167, 175, 183, 191, 199, 207, 215, 223, 231, 239, 247, 255 };
                //sapmalar
                var s = new byte[] { 0, 16, 8, 24, 4, 12, 20, 28, 2, 6, 10, 14, 18, 22, 26, 30, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };
                //palet index
                var k = 0;

                var p = bitmap.Palette;

                //item renkleri
                for (int j = 0; j < 32; j++)
                {
                    var i = s[j];

                    p.Entries[k++] = Color.FromArgb(t[32], t[i], t[0]);
                    p.Entries[k++] = Color.FromArgb(t[32 - i], t[32], t[0]);
                    p.Entries[k++] = Color.FromArgb(t[0], t[32], t[i]);
                    p.Entries[k++] = Color.FromArgb(t[0], t[32 - i], t[32]);
                    p.Entries[k++] = Color.FromArgb(t[i], t[0], t[32]);
                    p.Entries[k++] = Color.FromArgb(t[32], t[0], t[32 - i]);
                }

                //kalan renkleri sil
                for (int i = 0; i < 64; i++)
                    p.Entries[k++] = Color.FromArgb(0, 0, 0);

                bitmap.Palette = p;

                bitmap.Save("palette.gif", ImageFormat.Gif);
            }
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
