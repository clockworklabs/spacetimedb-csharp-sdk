using SpacetimeDB;
using SpacetimeDB.Types;
using System.Collections.Concurrent;
using System;
using System.Diagnostics;

static class Program {
    enum NextTask {
        None,
        Subscribe,
        Insert,
        List,
        Clear,
        Disconnect,
    }

    static void Main()
    {
        AuthToken.Init(".spacetime_csharp_quickstart");

        // create the client, pass in a logger to see debug messages
        SpacetimeDBClient.CreateInstance(new ConsoleLogger());

        var client = SpacetimeDBClient.instance;
        var stopwatch = new Stopwatch();
        var count = 50000;
        var leftoverCount = count;
        var nextTask = NextTask.None;

        User.OnInsert += (user, dbEvent) => {
            leftoverCount--;
            if (leftoverCount == 0) {
                stopwatch.Stop();
                Console.WriteLine($"Insertion speed: {count / stopwatch.Elapsed.TotalSeconds:N0} items/sec");
                nextTask = NextTask.List;
            }
        };

        User.OnDelete += (user, dbEvent) => {
            leftoverCount--;
            if (leftoverCount == 0) {
                stopwatch.Stop();
                Console.WriteLine($"Deletion speed: {count / stopwatch.Elapsed.TotalSeconds:N0} items/sec");
                nextTask = NextTask.Disconnect;
            }
        };

        client.onConnect += () => {
            Console.WriteLine("Connected; subscribing");
            nextTask = NextTask.Subscribe;
        };

        client.onIdentityReceived += (authToken, identity, address) => {
            Console.WriteLine("Received identity");
            AuthToken.SaveToken(authToken);
        };

        client.onSubscriptionApplied += () => {
            Console.WriteLine("Subscribed; starting benchmarks");
            nextTask = NextTask.Insert;
        };

        new Thread(() => {
            client.Connect(AuthToken.Token, "http://localhost:3000", "benchmark");
            while (nextTask != NextTask.Disconnect) {
                switch (nextTask) {
                    case NextTask.Subscribe:
                        nextTask = NextTask.None;
                        SpacetimeDBClient.instance.Subscribe(new List<string> { "SELECT * FROM User" });
                        break;
                    case NextTask.Insert:
                        nextTask = NextTask.None;
                        leftoverCount = count;
                        var strings = new List<string>(count);
                        for (var i = 0; i < count; i++) {
                            // just an easy way to get short random alphabetical strings in C#
                            strings.Add(Path.GetRandomFileName());
                        }
                        stopwatch.Restart();
                        for (var i = 0; i < count; i++) {
                            Reducer.Insert(strings[i], (byte)(i % 100));
                        }
                        break;
                    case NextTask.List:
                        stopwatch.Restart();
                        var localCount = 100;
                        for (var i = 0; i < localCount; i++) {
                            // iterate with some arbitrary field filter
                            foreach (var user in User.FilterByAge(50)) {
                                GC.KeepAlive(user);
                            }
                        }
                        stopwatch.Stop();
                        Console.WriteLine($"Query speed: {(count * localCount) / stopwatch.Elapsed.TotalSeconds:N0} items/sec");
                        nextTask = NextTask.Clear;
                        break;
                    case NextTask.Clear:
                        nextTask = NextTask.None;
                        leftoverCount = count;
                        stopwatch.Restart();
                        Reducer.ClearAll();
                        break;
                }
                client.Update();
            }
            client.Close();
        }).Start();
    }
}
