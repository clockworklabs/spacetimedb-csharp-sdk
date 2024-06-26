# SpacetimeDB SDK for C#

## Overview

This repository contains the [C#](https://learn.microsoft.com/en-us/dotnet/csharp/) SDK for SpacetimeDB. The SDK allows to interact with the database server and is prepared to work with code generated from a SpacetimeDB backend code.

## Documentation

The C# SDK has a [Quick Start](https://spacetimedb.com/docs/client-languages/csharp/csharp-sdk-quickstart-guide) guide and a [Reference](https://spacetimedb.com/docs/client-languages/csharp/csharp-sdk-reference).

## Installation

The SDK is available as a [NuGet Package](https://www.nuget.org/packages/SpacetimeDB.ClientSDK). To install it, follow these steps:

1. Open the NuGet package manager in Visual Studio.
2. Search for `SpacetimeDB.ClientSDK`.
3. Click the install button.

Alternatively, it can be installed on the command line using the `dotnet` command:

```bash
dotnet add package SpacetimeDB.ClientSDK
```

## Usage

### Access the SpacetimeDB Client

The SpacetimeDB client is created automatically as a singleton and accessible via the `SpacetimeDBClient.instance` property.

### Connecting to SpacetimeDB

To connect to SpacetimeDB, you need to call the `Connect` method on the `SpacetimeDBClient` class. The `Connect` method takes the following parameters:

- `token`: The authentication token to use to connect to SpacetimeDB. This token is generated by the backend code and is used to authenticate the client.
- `hostName`: The hostname of the SpacetimeDB server. This is the same hostname that you use to connect to the SpacetimeDB web interface.
- `moduleAddress`: The address of the module to connect to. This is the same address that you use to connect to the SpacetimeDB web interface.
- `sslEnabled`: Whether to use SSL to connect to SpacetimeDB. This is the same value that you use to connect to the SpacetimeDB web interface.

Example: 

```csharp
using SpacetimeDB;

SpacetimeDBClient.instance.Connect(TOKEN, HOST, DBNAME, SSL_ENABLED);
```

### AuthToken optional helper class

The `AuthToken` class is a helper class that can be used to store the local client's authentication token locally to your user's home directory.

```csharp
using SpacetimeDB;

AuthToken.Init(".spacetime_csharp_quickstart");

SpacetimeDBClient.instance.Connect(AuthToken.Token, HOST, DBNAME, SSL_ENABLED);

void OnIdentityReceived(string authToken, Identity identity)
{
    local_identity = identity;
    AuthToken.SaveToken(authToken);
}
SpacetimeDBClient.instance.onIdentityReceived += OnIdentityReceived;
```

### Subscribing to tables

To subscribe to a table, you need to call the `Subscribe` method on the `SpacetimeDBClient` class. The `Subscribe` method takes a list of queries as a parameter. The queries are the same queries that you use to subscribe to tables in the SpacetimeDB web interface.

### Listening to events

To listen to events, you need to register callbacks on the `SpacetimeDBClient` class. The following callbacks are available:

- `onConnect`: Called when the client connects to SpacetimeDB.
- `onConnectError`: Called when the client fails to connect to SpacetimeDB.
- `onDisconnect`: Called when the client disconnects from SpacetimeDB.
- `onIdentityReceived`: Called when the client receives its identity from SpacetimeDB.
- `onSubscriptionApplied`: Called when the client receives the initial data from SpacetimeDB after subscribing to tables.

You can register for row update events on a table. To do this, you need to register callbacks on the table class. The following callbacks are available:

- `OnInsert`: Called when a row is inserted into the table.
- `OnUpdate`: Called when a row is updated in the table.
- `OnBeforeDelete`: Called before a row is deleted from the table.
- `OnDelete`: Called when a row is deleted from the table.

Example:

```csharp
using SpacetimeDB.Types;

PlayerComponent.OnInsert += PlayerComponent_OnInsert;
PlayerComponent.OnUpdate += PlayerComponent_OnUpdate;
PlayerComponent.OnDelete += PlayerComponent_OnDelete;
PlayerComponent.OnBeforeDelete += PlayerComponent_OnBeforeDelete;
```

You can register for reducer call updates as well.

- `OnREDUCEREvent`: Called when a reducer call is received from SpacetimeDB. (If a) you are subscribed to the table that the reducer modifies or b) You called the reducer and it failed)

Example:

```csharp
using SpacetimeDB.Types;

Reducer.OnMovePlayerEvent += Reducer_OnMovePlayerEvent;
```
 
### Accessing the client cache

The client cache is a local cache of the data that the client has received from SpacetimeDB. The client cache is automatically updated when the client receives updates from SpacetimeDB. 

When you run the CLI generate command, SpacetimeDB will automatically generate a class for each table in your database. These classes are generated in the `SpacetimeDB.Types` namespace. Each class contains a set of static methods that allow you to query the client cache. The following methods are available:

- `int Count()`: Returns the number of rows in the table.
- `IEnumerable<TableRow> Iter()`: Returns an iterator over the table.
- `IEnumerable<TableRow> FilterByCOLUMN(ColumnValue)`: Filters the table by the specified column value.
- `TableRow? FindByCOLUMN(ColumnValue)`: Finds a single item by the specifed column value.
- `IEnumerable<TableRow> Query(Func<TableRow, bool>)`: Filters the table with the specified predicate.

### Calling Reducers

To call a reducer, you need to call the autogenerated method on the `Reducer` class. The autogenerated method takes the reducer arguments as parameters. The reducer arguments are the same arguments that are expected in your server module.

Example:

```csharp
using SpacetimeDB.Types;

Reducer.MovePlayer(new StdbVector2(0.0f, 0.0f), new StdbVector2(1.0f, 1.0f));
```
