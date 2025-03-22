using Grpc.Core;
using Grpc.Net.Client;
using WordleGameServer.Protos;
using WordServer.Protos;

namespace WordleGameClient
{
    internal class Program
    {
        static async Task Main( string[] args )
        {
            //currently testing responses from server, no implemention of game yet 
            try
            {

                var client = new DailyWordle.DailyWordleClient(GrpcChannel.ForAddress("https://localhost:7018"));

                //Access the word directly for testing
                var test = new DailyWord.DailyWordClient(GrpcChannel.ForAddress("https://localhost:7073"));

                var wordRequest = new WordRequest();
                var wordResponse = test.GetWord(wordRequest);
                Console.WriteLine($"Word of the day: {wordResponse.Word}");

                using (var call = client.Play())
                {

                    do
                    {
                        Console.WriteLine("Enter your guess:");
                        var guess = Console.ReadLine();

                        // Await the write operation
                        await call.RequestStream.WriteAsync(new GuessRequest { Guess = guess });

                        // Await MoveNext and check if there's a response available
                        bool hasResponse = await call.ResponseStream.MoveNext();
                        if (!hasResponse)
                        {
                            Console.WriteLine("No response received from server.");
                            break;
                        }

                        // Now it's safe to access Current
                        GuessResponse response = call.ResponseStream.Current;
                        Console.WriteLine(response);

                        if (response.IsCorrect)
                        {
                            Console.WriteLine("You guessed the word!");
                            Console.WriteLine(response);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Try again!");
                        }
                    } while (true);

                    // Don't forget to complete the request stream when done
                    await call.RequestStream.CompleteAsync();
                }

            }
            catch (RpcException)
            {
                Console.WriteLine("Error: Wordle Game Sever is currently unavailable!");
            }
        }
    }
}