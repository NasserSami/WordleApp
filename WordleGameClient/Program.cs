namespace WordleGameClient
{
    internal class Program
    {
        static void Main( string[] args )
        {
            WordServer wordServer = new WordServer();

            WordleGameServer wordleGameServer = new WordleGameServer();

            Console.WriteLine(WordleGameServer.Word);

        }
    }
}
