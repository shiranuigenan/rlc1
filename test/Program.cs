using System;
using System.Collections.Generic;
using ContainerPacking;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            var containers = new List<Container>();
            containers.Add(new Container(1, 100, 200, 300));

            var itemsToPack = new List<Item>();
            itemsToPack.Add(new Item(1, 10, 20, 30, 500));

            var algorithms = new List<int>();
            algorithms.Add((int)AlgorithmType.EB_AFIT);

            var result = PackingService.Pack(containers, itemsToPack, algorithms);
        }
    }
}
