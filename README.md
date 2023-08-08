# Networking Library for Games Development

An easy to use C# based networking library, designed initially for the MonoGame framework but with the flexibility to be used across various C# based engines, built to provide beginner game developers the tools required to build networked multiplayer games in peer-to-peer sessions. The library handles synchronisation of game objects, connection handling / management and diagnostic information. All without requiring any experience of networking from the developer.

***Note: this should only be used to experiment and should definitely not be used in any game that could potentially reach a consumer. Security and privacy issues are likely, but have not been tested or found. Only use in "real" games if you are confident that you can extend the capabilities of the library. The performance also needs work. There we go, lawsuit avoided!***

An in-depth look at the design and implementation of the library can be found in the [final report.](https://github.com/henrypaul2001/Networking_Library_for_Games_Development/blob/master/Final%20report.pdf "Final Report")
The report is very large, but covers every step of this project including: research, planning, design, implementation, testing and evaluations. It will give an insight into why certain decisions were made and how the library works.

## Overview

There are three main pillars to the library: the network manager, the networked game object and the networked variable. These are the three things you will be interacting with the most.

### Network Manager

An abstract class, developer must create a game specific network manager inheriting from this one to implement specific behaviours for events.

- Step 1: Create game network manager instance, specify protocol ID and local port number
- Step 2: Call "ConnectLocalClientToHost" from the base class with a target IP and port number
- Step 3: Call the "Update" method from the base class every frame in the games update loop
- Step 4: Call "SendLocalObjects" in the ConnectionEstablished override method if you wish to have networked objects constructed remotely upon connection

### Networked Game Object

Any game object you wish to synchronise needs to derive from this class and must have both a local constructor and a remote constructor.

When constructing locally, any potentially dynamic parameters used to create the object need to be specified in the construction properties dictionary.

Create a local instnace of this object and the library will handle the rest, but don't forget your networked variables!

### Networked Variable

Without any networked variables, your networked game object is essentially useless.

If a type implements IConvertible, it can be used as a networked variable.

To use, simply do the following:

```
[NetworkedVariable]
string "iAmANetworkedVariable";
```

## Features

| What it does                                                     | What it does not                               |
| ---------------------------------------------------------------- | ---------------------------------------------- |
| Establish and manage connections                                 | Provide matchmaking services                   |
| Handle timeouts                                                  | Client-server sessions                         |
| Automatically synchronise networked variables and game objects   | Client-side prediction                         |
| Calculate connection diagnostics such as packet loss and RTT     | Dynamically control flow of packets            |
| Peer-to-peer sessions                                            | Provide any database / player profile services |
