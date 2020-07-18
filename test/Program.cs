using System;
using System.Collections.Generic;
using ContainerPacking;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container(1, 100, 200, 300);
            var itemsToPack = new List<Item>() { new Item(1, 10, 20, 30, 500) };

            var result = PackingService.Pack(container, itemsToPack);
        }
    }
}
