using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using SpacetimeDB;
using SpacetimeDB.Types;

static class Program
{
    enum NextTask
    {
        None,
        Subscribe,
        Insert,
        List,
        Clear,
        Disconnect,
    }

    readonly static Stopwatch stopwatch = new();

    readonly static string[] testStrings = Enumerable.Range(0, count).Select(i => $"Test Msg{i,92}").ToArray();

    const int count = 50000;

    static void PrintSpeed(string msg, string unit)
    {
        var elapsed = stopwatch.Elapsed;
        Console.WriteLine($"{msg}: {count / elapsed.TotalSeconds:N0} {unit}s/sec");
    }

    static void Main()
    {
        AuthToken.Init(".spacetime_csharp_quickstart");

        // create the client, pass in a logger to see debug messages
        SpacetimeDBClient.CreateInstance(new ConsoleLogger());

        var client = SpacetimeDBClient.instance;

        {
            client.onConnect += () =>
            {
                Console.WriteLine("Connected; subscribing");
                SpacetimeDBClient.instance.Subscribe(new List<string> { "SELECT * FROM Message" });
            };

            var subscribed = false;

            client.onSubscriptionApplied += () =>
            {
                Console.WriteLine("Subscribed; starting benchmarks");
                subscribed = true;
            };

            client.Connect(AuthToken.Token, "http://localhost:3000", "benchmark");

            while (!subscribed)
            {
                client.Update();
            }
        }

        {
            stopwatch.Restart();
            foreach (var s in testStrings)
            {
                Reducer.Noop(s);
            }
            PrintSpeed("Reducer call speed", "call");
        }

        {
            var leftoverCount = count;

            Message.OnInsert += (message, dbEvent) =>
            {
                // Start counting inserts only from the first one to amortize reducer roundtrip
                // (also because there is no way to wait for the previous noop reducer to finish
                // because STDB only emits events for reducers that insert/delete items).
                if (leftoverCount == count) {
                    stopwatch.Restart();
                }
                if (--leftoverCount == 0)
                {
                    PrintSpeed("Insertion handling speed", "item");
                }
            };

            Reducer.InsertAll(count);

            while (leftoverCount > 0)
            {
                client.Update();
            }
        }

        {
            var leftoverCount = count;

            Message.OnDelete += (message, dbEvent) =>
            {
                // Start counting inserts only from the first one to amortize reducer roundtrip.
                if (leftoverCount == count) {
                    stopwatch.Restart();
                }
                if (--leftoverCount == 0)
                {
                    PrintSpeed("Deletion handling speed", "item");
                }
            };

            Reducer.ClearAll();

            while (leftoverCount > 0)
            {
                client.Update();
            }
        }

        client.Close();
    }
}
