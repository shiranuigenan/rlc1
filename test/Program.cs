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
                new Item(1, 10, 20, 30, 28),
                new Item(2, 20, 30, 40, 28),
                new Item(3, 30, 40, 50, 28),
                new Item(4, 40, 50, 60, 28),
            };

            var result = PackingService.Pack(container, itemsToPack);
            PackingService.PackingResultToPng(container, result);
        }
    }
}
