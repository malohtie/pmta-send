using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        static void test()
        {
            Console.WriteLine("Hi4444");
            Thread.Sleep(3000);
            Console.WriteLine("After");
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hi");
            Console.WriteLine("Hi222");

            Console.WriteLine("Hi333");

            Task.Run(() => test());
            Console.WriteLine("Finish");
            Console.ReadLine();

        }
    }
}
