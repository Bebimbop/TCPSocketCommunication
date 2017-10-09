using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    class Program
    {
        static void Main(string[] args)
        {
            ORTCPClient client = new ORTCPClient();

            client.Start();
        }
    }
}
