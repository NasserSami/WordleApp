using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WordleGameClient
{
    //temporary class to simulate a server, will be moved to gRPC service later
    public class WordleGameServer
    {
        public static string Word { get; private set; } = "";

        public WordleGameServer()
        {

            Word = new WordServer().GetWord();

        }
    }
}
