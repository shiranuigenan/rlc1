using System;
using System.Collections.Generic;
using ContainerPacking;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container(1, 300, 200, 100);
            var itemsToPack = new List<Item>()
            {
                new Item(1, 150, 100, 50, 1),
            };

            var result = PackingService.Pack(container, itemsToPack);
            PackingService.RenderPackingResult(container, result);
        }
    }
}
