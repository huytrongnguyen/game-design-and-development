# Game Development Fundamentals: building an online RPG

A focused course on how online games, and online RPGs in particular, are built. Each module explains one part of the job, lays out the **design space** (the common options and when each fits), weighs **build vs buy** (what engines, middleware and services give you, and what a small team can build itself with open tools and AI-assisted development), and then builds the idea as a small, runnable example.

- **Audience:** programmers new to game development, and producers or designers who want to understand how an online game is built.
- **Not tied to one game.** Examples of games are used only for features any player can see.
- **Built to last.** Every module stands on its own, states when its facts were checked, and ends with slide-ready key takeaways. The folder is self-contained, so it can be copied as-is to another repository or exported to slides or PDF.

## Example stack (chosen for teaching)
The examples have one job: **show how the server side of an online game works**, and how a client talks to it. Graphics and animation are out of scope.

| Option | For | Against |
|---|---|---|
| **C# / .NET 10** ✅ | Fast enough for a real game server; first-class WebSockets, `Channel<T>` and hosted services; deterministic tests with xUnit; skills transfer to Unity and Godot, which both script in C# | The garbage collector needs care in hot loops (module 03) |
| Go | Simple concurrency, small binaries | Weaker fit for rich gameplay object models; no engine uses it |
| Rust | Performance and safety; the Bevy ECS | A steep learning curve that would distract from the game concepts |
| TypeScript / Node | One language with the browser | Single-threaded; CPU-bound simulation is slow |
| C++ | What Unreal and most in-house engines use | Slow to write and teach; easy to get memory bugs |

**Choice:** a C# / .NET 10 server and rules core. Each example is a self-contained project with xUnit tests (`dotnet test examples/NN-topic`). The networking modules add a plain WebSocket endpoint with a small message protocol, a one-page HTML client in plain JavaScript, and a C# bot client. There is no rendering engine and no build step.

## Modules

| # | Module | Example (`examples/`) |
|---|---|---|
| **1. Foundations** |||
| 00 | [Game development fundamentals](00-fundamentals.md) | none |
| 01 | [What a game engine is, and build vs buy](01-engine-build-or-buy.md) | `01-mini-engine` |
| 02 | [Studying a reference game from public information](02-studying-a-reference-game.md) | none |
| **2. Core runtime** |||
| 03 | [The game loop and time](03-game-loop.md) | `03-game-loop` |
| 04 | [Entities, world and zones](04-entities-world.md) | `04-world` |
| 05 | [Data-driven design and property systems](05-data-properties.md) | `05-properties` |
| 06 | [Scripting: the engine/script boundary](06-scripting.md) | `06-scripting` |
| 07 | [Navigation and pathfinding](07-navigation.md) | `07-navigation` |
| **3. Gameplay systems** |||
| 08 | [Stats and combat resolution](08-stats-combat.md) | `08-combat` |
| 09 | [Action combat: hit detection, animation-driven skills and lag compensation](09-action-combat.md) | `09-action-combat` |
| 10 | [Skills, abilities and buffs](10-skills-buffs.md) | `10-skills` |
| 11 | [Character progression: levels, classes and advancement trees](11-progression.md) | `11-progression` |
| 12 | [Game AI](12-ai.md) | `12-ai` |
| 13 | [Party and companion control](13-party-control.md) | `13-party` |
| 14 | [Items, economy and rewards](14-items-economy.md) | `14-economy` |
| 15 | [Quests, missions and instances](15-quests-missions.md) | `15-quests` |
| 16 | [Party finder, matchmaking and instance queues](16-matchmaking.md) | `16-matchmaking` |
| **4. The online server** |||
| 17 | [Networking: protocol and client-server interaction](17-networking.md) | `17-net` |
| 18 | [State sync and interest management](18-state-sync.md) | `18-sync` |
| 19 | [MMO server architecture](19-server-architecture.md) | `19-topology` |
| 20 | [Persistence and transactions](20-persistence.md) | `20-persistence` |
| 21 | [Security and anti-cheat](21-security.md) | `21-security` |
| **5. Client, content and operations** |||
| 22 | [The client's job](22-client.md) | uses `17-net` |
| 23 | [World and content production: maps, tools and asset pipelines](23-world-content-production.md) | none |
| 24 | [Content pipeline, localisation and testing](24-pipeline-testing.md) | `24-pipeline` |
| 25 | [Live operations](25-live-ops.md) | none |
| 26 | [Capstone: a no-middleware PoC plan](26-poc-plan.md) | none |
