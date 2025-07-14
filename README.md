# MonoNet - Networking Library for Games Development

## Why am I showing you this?

As a programmer who’s been learning continuously since finishing this project, I'll be honest... I'm not exactly proud of the code quality here. So why am I showing you this?

Because this project taught me what might be the most valuable lesson I'll ever learn:

***You really can just learn anything, no matter how intimidating it might seem***

A few months before the academic year began, all final-year students had to choose an honours project topic. I had no idea what I was going to do. Thankfully, the department provided a list of project proposals from supervisors for students like me who were stuck.

There was one in particular that caught my eye: __"Networked Tower Defense Game"__. That idea eventually evolved into what you're seeing now, a generic networking library.

So, why did I choose it?

Because I was absolutely terrified of networking.

It was a topic that felt so far beyond what I was capable of, something I always assumed I'd never be able to do, especially not on my own. But that fear was exactly why I picked it. It was the perfect opportunity to challenge myself.

And it was a challenge. The code may not be great and the design may be flawed, but I learned a lot. Without this project, I would not be the programmer I am today.

This project taught me that no matter how complex a subject may seem, no matter how incapable, unprepared or unqualified you feel, you can learn it.

Physics simulation, graphics programming, engine development: all topics that used to intimidate me. But I brought the key lesson of this project along with me, and it has made all the difference.

## Intro

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
