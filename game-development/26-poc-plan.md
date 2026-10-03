# Module 26: Capstone: a no-middleware PoC plan

- **Goal:** turn the whole course into one plan: what a small team can build without buying middleware, in which order, and what each step must prove before the next one starts.
- **Prerequisites:** modules 00–25 (this module only summarises and links them).
- **Example:** none. The examples of the earlier modules are the PoC pieces; this module puts them in order.
- **Facts checked:** October 2026 (prices, licences and product status change; re-check before reuse)

## In short
Start with one question: if we buy no middleware, what can we build ourselves? The answer is almost everything an online RPG server needs. Each module's example proves one piece with deterministic tests. What remains genuinely hard is the 3D client (renderer, animation, editor tooling) and client-side anti-cheat. The plan below assembles the pieces in four phases: a headless rules core, an online server, content and operations, and finally a client. Each phase ends with a measurable exit test, so the team learns early if an assumption was wrong.

## 1. The concept
A **proof of concept (PoC)** answers one risky question cheaply, before the expensive work depends on it. It is not a prototype of the whole game. Good PoCs share three traits:

- **One question each.** "Can our own pathfinder answer a corner-to-corner query on a full-size map inside one tick?" is a PoC. "Build the navigation system" is not.
- **A pass/fail number.** A node budget, a byte count, a test that must stay green.
- **Throwaway by default.** Keep the code only if it already meets the quality bar. The course examples were written that way, so most of them can be kept.

**Integration order matters more than any single piece.** A deterministic loop has to exist before anything that relies on replay. Server authority has to exist before a client can be trusted with nothing. Persistence has to be transactional before items exist.

## 2. The design space
A plan for an online RPG varies along a few axes. Pick the option for each before phase 1, because they change the exit tests.

| Axis | Options | Choose by |
|---|---|---|
| Control | one character, or a party of several | genre; a party multiplies entities per player ([module 13](13-party-control.md)) |
| Combat | tab-target with cooldowns, or action combat | latency tolerance ([module 08](08-stats-combat.md), [module 09](09-action-combat.md)) |
| Client | thin web client, 2D engine, or a 3D engine | art budget and platform (module 22) |
| World | zones, instances, channels | population per area ([module 04](04-entities-world.md), [module 19](19-server-architecture.md)) |
| Content authoring | data only, data plus hooks, or a script language | who authors and how fast they iterate ([module 06](06-scripting.md)) |

## 3. What a small team replaces, and with what
Games have long licensed narrow, hard parts. This table lists the usual ones and what a team with no middleware budget puts in their place.

| Usually licensed | What it solves | No-middleware replacement | Proven in |
|---|---|---|---|
| Navigation middleware | Navmesh pathfinding, agent shapes, wall sliding | Own grid A*, radius inflation, line-of-sight smoothing, sliding, node budget; Recast/Detour (open source) if a navmesh is ever needed | [07](07-navigation.md) |
| Packet cipher | Hiding the protocol | TLS from the platform, plus server-side validation and sequence numbers | [17](17-networking.md), [21](21-security.md) |
| Client anti-cheat | Bots, speed hacks, memory edits | Server authority: movement validation, rate limits, replay protection | [21](21-security.md) |
| Mobile one-time password | Account second factor | TOTP (RFC 6238), a small open standard | [21](21-security.md) |
| Compression library | Smaller payloads | The standard library, used only where measurement shows a gain | [17](17-networking.md), [20](20-persistence.md) |
| Embedded scripting language | Content scripting | C# content hooks checked at load, plus data formulas | [06](06-scripting.md) |
| Vegetation, skeletal animation, audio, fonts, ray collision, graphics API | The 3D client | Not replaced. A thin client in the course; a licensed or open-source engine for a real 3D client | [22](22-client.md) |

## 4. Evaluation: what the course proved, and what it did not
### Proven by the examples
Every row is a passing, deterministic test suite in `examples/`.

| Piece | Module | The evidence |
|---|---|---|
| Fixed-tick loop, replay | [03](03-game-loop.md) | Same seed and command log give an identical state hash; a timer fires on exactly the expected tick |
| World and visibility | [04](04-entities-world.md) | Grid area-of-interest queries return exactly the expected entities; stale handles are rejected |
| Derived stats | [05](05-data-properties.md) | Changing one base stat recomputes only its dependants; cycles are rejected at load |
| Content hooks | [06](06-scripting.md) | A misspelled hook fails at load with a precise message |
| Pathfinding | [07](07-navigation.md) | A large-grid query stays inside a fixed node budget |
| Combat | [08](08-stats-combat.md) | Documented formulas reproduced exactly; a byte-identical attack log per seed |
| Skills and buffs | [10](10-skills-buffs.md) | Exact damage per skill level; a modifier disappears on its expiry tick |
| AI | [12](12-ai.md) | Aggro, threat switching, leash; a think interval respected exactly |
| Party control | [13](13-party-control.md) | Formation slots at exact coordinates for a given heading; squad recall |
| Economy | [14](14-items-economy.md) | Duplicate grants ignored; balances equal the sum of the ledger |
| Quests and missions | [15](15-quests-missions.md) | Chains unlock in order; a mission ends on its exact expiry tick |
| Client-server protocol | [17](17-networking.md) | Handshake, intents applied on the next tick, rate limiting, over a real WebSocket |
| State sync | [18](18-state-sync.md) | One Enter with full state, small position deltas, zero bytes for idle entities |
| Server topology | [19](19-server-architecture.md) | Tampered or expired tokens rejected; a character is never in two zones |
| Persistence | [20](20-persistence.md) | A crash between steps never leaves a partial trade; retries apply once |
| Security | [21](21-security.md) | RFC 6238 test vectors pass; speed hacks and replays are rejected |
| Content pipeline | [24](24-pipeline-testing.md) | Broken references reported with their location; a pinned, reproducible balance simulation |

### Not proven yet
- **Scale.** The examples run one zone in one process. Nobody has measured a zone with hundreds of players and thousands of monsters at the target tick rate.
- **The pieces together.** Each example stands alone. A single zone loop that runs combat, AI, skills, sync and persistence at once has not been built.
- **A real database.** The persistence example uses an in-memory store with a PostgreSQL schema beside it. The same tests have not run against PostgreSQL.
- **The 3D client.** Out of scope for the course. It is where a team should expect to buy or adopt an engine.
- **Client anti-cheat.** Deliberately not built. Server authority covers the most damaging cheats; bots that play legally remain a detection and operations problem.

## 5. Build or buy: the overall verdict
- **Build** everything that encodes the game's rules or its server: loop, world, properties, combat, skills, AI, party control, economy, quests, protocol, sync, topology, persistence rules and the content pipeline. Engines do not provide these for a persistent online world, and the course examples show each one fits in hundreds, not tens of thousands, of lines.
- **Use open source or the platform** for standard problems: TLS, compression, TOTP, a database, and Recast/Detour if terrain ever needs a real navmesh.
- **Buy or adopt** a 3D client engine (Unreal, Unity or Godot) when the product needs one, and a client anti-cheat only if the game's market demands it.
- **AI-assisted development** moves the line toward build for well-specified, testable pieces like these. It does not remove the need for deep specialists on a production renderer or editor tooling (as of October 2026).

## 6. The plan
Four phases. Each one reuses the course examples and ends with an exit test that must pass before the next phase starts.

```mermaid
flowchart LR
    P1["Phase 1<br/>Headless rules core"] --> P2["Phase 2<br/>Online server"]
    P2 --> P3["Phase 3<br/>Content and operations"]
    P3 --> P4["Phase 4<br/>Client"]
```

### Phase 1: the headless rules core
- **Combine:** the loop ([03](03-game-loop.md)), world and visibility ([04](04-entities-world.md)), properties ([05](05-data-properties.md)), combat ([08](08-stats-combat.md)), skills and buffs ([10](10-skills-buffs.md)), AI ([12](12-ai.md)), party control ([13](13-party-control.md)) and pathfinding ([07](07-navigation.md)) into **one zone simulation**, with no network and no database.
- **Exit test:** a scripted fight with a few players and 50 monsters runs 10,000 ticks, replays to the same state hash from its seed and command log, and stays inside the tick budget measured on target hardware.
- **Then scale it:** hundreds of players and thousands of monsters in one zone. Record the tick time and decide whether the planned tick rate holds.

### Phase 2: the online server
- **Add:** the WebSocket protocol ([17](17-networking.md)), per-observer sync ([18](18-state-sync.md)), login, lobby and zone hand-off as a modular monolith ([19](19-server-architecture.md)), real PostgreSQL behind the persistence rules ([20](20-persistence.md)) and the security checks ([21](21-security.md)).
- **Exit test:** 200 bot clients (the module 17 bot) log in, move, fight and trade for one hour. No partial trade survives a forced server kill, no duplicate item appears in the ledger, and bandwidth per player stays under the budget set in module 18.

### Phase 3: content and operations
- **Add:** content hooks ([06](06-scripting.md)), items and economy ([14](14-items-economy.md)), quests and missions ([15](15-quests-missions.md)), the validating content pipeline and localisation ([24](24-pipeline-testing.md)), and the live-operations basics ([25](25-live-ops.md)): hot reload, date-gated events, telemetry, an append-only audit log.
- **Exit test:** a designer changes a skill, a drop table and a quest in data; the validator catches a deliberately broken reference; the change reloads into a running zone without a restart; the audit log explains every item granted during the test.

### Phase 4: the client
- **Decide:** keep the thin web client ([22](22-client.md)) for a light game, or adopt an engine for a 3D one. The protocol and the state mirror stay your own code either way.
- **Exit test:** a player controls a party through the real server with interpolation and no client authority, and the game still works when the client is replaced by the bot.

## 7. Trade-offs and pitfalls
| Risk | Why it matters | Early signal |
|---|---|---|
| Tick budget | Several characters per player multiply the entity count | Phase 1 scale run |
| Content velocity | C# hooks may be too slow for designers compared to a script language | Phase 3 designer test |
| Economy | Duplication and inflation kill trust | Phase 2 ledger checks, Phase 3 telemetry |
| Client cost | A 3D client is the largest single cost | Phase 4 decision |
| Big-bang integration | Pieces that pass alone can fail together | Phase 1 combined run |

## Key takeaways
- Without middleware, a small team can build the whole online RPG server; the course examples prove each piece with deterministic tests.
- The narrow, hard parts studios once licensed can, as of October 2026, be replaced by open standards, open source and the platform.
- The two things still worth buying or adopting are a 3D client engine and, only if the market demands it, client anti-cheat.
- Integration and scale are the unproven parts. Phase 1 exists to measure them before anything else depends on them.
- Each phase ends with a hard exit test: replay identity, no partial trades, safe hot reload, a client with no authority.
- Keep the loop deterministic and the server authoritative from day one; every later phase relies on both.

## Further reading
- Glenn Fiedler, *Fix Your Timestep!*: https://gafferongames.com/post/fix_your_timestep/
- Gabriel Gambetta, *Client-Server Game Architecture*: https://www.gabrielgambetta.com/client-server-game-architecture.html
- Recast & Detour: https://github.com/recastnavigation/recastnavigation
- RFC 6238, TOTP: https://www.rfc-editor.org/rfc/rfc6238
- Robert Nystrom, *Game Programming Patterns*: https://gameprogrammingpatterns.com/
