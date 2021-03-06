﻿using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedServerClient;

namespace ConsoleClient
{
    class Program
    {
        static void Main_(string[] args)
        {
            Console.WriteLine("Hello world from Console Client!");

            var url = @"http://localhost:27427";
            var hubConnection = new HubConnection(url);

            var hubName = @"stockTicker"; //"StockTickerHub"
            IHubProxy stockTickerHubProxy = hubConnection.CreateHubProxy(hubName);
            stockTickerHubProxy.On<Stock>("UpdateStockPrice", stock => Console.WriteLine("Stock update for {0} new price {1}", stock.Symbol, stock.Price));



            //QueryString can be used to pass information
            // between client vs. server: https://www.asp.net/signalr/overview/guide-to-the-api/hubs-api-guide-net-client

            //Specify header into connection
            hubConnection.Headers.Add("key", "value");

            //Client certificate can be added like this
            //hubConnection.AddClientCertificate()

            hubConnection.Start().Wait();

            //Types of transports - can specify in the Connection.Start method:
            // + LongPollingTransport
            // + ServerSentEventsTransport
            // + WebSocketTransport
            // + AutoTransport
            // + ForeverFrame - can only be used by the browser


            //Handle connection life time events:
            // + Received
            // + ConnectionSlow
            // + Reconnection
            // + Reconnected
            // + StateChanged
            // + Closed 
            hubConnection.ConnectionSlow += () => Console.WriteLine("Connection problems.");

            //Handle an error raised by SignalR server
            hubConnection.Error += ex => Console.WriteLine("SignalR error: {0}", ex.Message);


            // ==============================
            // Enable client side logging
            // ==============================
            hubConnection.TraceLevel = TraceLevels.All;
            hubConnection.TraceWriter = Console.Out;



            // ==============================
            // Handle error from server side method invocation
            // ==============================
            try
            {
                IEnumerable<Stock> allStocks = stockTickerHubProxy.Invoke<IEnumerable<Stock>>("GetAllStocks").Result;
                foreach (Stock stock in allStocks)
                {
                    Console.WriteLine("Symbol: {0} price: {1}", stock.Symbol, stock.Price);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error invoking GetAllStocks: {0}", ex.Message);
            }



            // ==============================
            // Call some methods on the server
            // ==============================

            //Call a method on the server - with server returning nothing
            stockTickerHubProxy.Invoke("JoinGroup", "SomeRandomGroup");

            //Call a method on the server - with server return some value
            var stocks = stockTickerHubProxy.Invoke<IEnumerable<Stock>>("AddStock", new Stock() { Symbol = "XYZ" }).Result;


            // ==============================
            // Some local methods that the server can call
            // ==============================

            // Method without params: Notify
            stockTickerHubProxy.On("Notify", () => Console.WriteLine("Notified!"));

            // With some params - string typing: UpdateStockPrice
            stockTickerHubProxy.On<Stock>("UpdateStockPrice", stock =>
                                Console.WriteLine("Symbol {0} Price {1}", stock.Symbol, stock.Price)
                            );

            // With some params - dynamic typing: UpdateStockPrice
            stockTickerHubProxy.On("UpdateStockPrice", stock =>
                                Console.WriteLine("Symbol {0} Price {1}", stock.Symbol, stock.Price)
                            );

            Console.ReadLine();
        }


        static void Main__(string[] args)
        {
            var url = @"http://localhost:8080/signalr";
            var hubName = @"myHub";

            var hubConnection = new HubConnection(url, new Dictionary<string, string>
            {
                { "token", "NetClient"}
            });

            var hubProxy = hubConnection.CreateHubProxy(hubName);

            //Listen to the event from the server
            hubProxy.On("addMessage", (string name, string message) =>
            {
                Console.WriteLine($"{name}: {message}");
            });

            hubConnection.Start().Wait();

            do
            {
                var line = Console.ReadLine();

                if (line.Contains(":")) //send direct
                {
                    var parts = line.Split(':');
                    hubProxy.Invoke("SendToUser", parts[0], "NetClient", parts[1]);
                }
                else //send to all
                {
                    hubProxy.Invoke("Send", "NetClient", line);
                }
            } while (true);
        }

        static void Main(string[] args)
        {
            //Load the list of users
            var users = File.ReadAllLines(@"Data\users.txt")
                            .Select(foo => foo.Trim().ToLower().Replace(' ', '.'))
                            .ToList();

            //Initiate a number of client to broadcast random messages
            var random = new Random();
            var clients = Enumerable.Range(0, 10).Select(foo => new AutomatedClient(users.GetRandom(random), random));
            foreach (var item in clients)
            {
                item.Start(false);
            }

            //This guy will broadcast, and listen as well
            new AutomatedClient(users.GetRandom(random), random).Start(true);

            Console.ReadLine();
        }
    }
}