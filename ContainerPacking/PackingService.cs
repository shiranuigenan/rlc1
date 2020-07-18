using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
    }
}
