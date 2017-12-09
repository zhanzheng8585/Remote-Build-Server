using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plus
{
    public interface ITest
    {
        bool test();
    }
    public class td : ITest
    {
        public bool test()
        {
            testplus n = new testplus();
            int a = 10;
            int b = 10;
            int c = n.plus(a, b);
            int d = a + b;
            if (c == d)
            {
                Console.Write("\n Test one has passed test, plus(10,10) == 20");
                return true;
            }
            else
            {
                Console.Write("\n Test one failed, plus(10,10) != 20");
                return false;
            }
        }
    }
}
