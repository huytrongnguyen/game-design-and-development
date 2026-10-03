# Game Design Fundamentals: designing an online RPG

A focused course on how online games, and online RPGs in particular, are designed. Each module explains one part of the job, lays out the **design space** (the common options and which kinds of game use which), shows **how to tune** the choice (numbers, curves, playtest signals), and ends with a **worked example**: a filled-in design for one fictional course game, plus a small runnable simulation where the topic is numeric.

- **Audience:** product owners, producers and programmers new to game design, and junior designers who want the whole picture of an online RPG.
- **Not tied to one game.** Real games appear only as examples of features any player can see.
- **Built to last.** Every module stands on its own, states when its facts were checked, and ends with slide-ready key takeaways. The folder is self-contained, so it can be copied as-is to another repository or exported to slides or PDF.

## The course game
Every worked example designs part of the same fictional game, so the examples add up to one coherent design by the end of the course. It is deliberately the **most common shape of online RPG**, so nothing in it leans on one studio's signature: **a small online fantasy RPG for PC and mobile, where each player controls one hero, picks a class, fights in real time, groups up with other players, and plays in a shared world with a free-to-play business model.** Rarer shapes (controlling several characters at once, squad or hero-collection games, turn-based or auto-battle combat) are covered in the design space of the module they belong to, as alternatives to the course game's choice. Each module also shows how its design would change for a different kind of game.

## Simulations (numeric modules only)
Design numbers are checked by simulation before players see them. The numeric modules ship a small C# / .NET 10 project in `examples/NN-topic/` with xUnit tests (`dotnet test examples/NN-topic`), the same stack as the Game Development course. The data lives in a JSON or CSV file a designer can edit without touching code.

## Modules

| # | Module | Worked example | Sim |
|---|---|---|---|
| **1. Foundations** ||||
| 00 | [What game design is](00-what-game-design-is.md): roles, design vs development, the document hierarchy | The course game's one-page pitch | |
| 01 | [The player experience](01-player-experience.md): MDA, aesthetics, player motivation models | Target players and the feelings to deliver | |
| 02 | [Vision, pillars and loops](02-vision-pillars-loops.md): core, session and meta loops | Vision, 4 pillars, the three loops | |
| 03 | [Studying a reference game as a designer](03-studying-a-reference-game.md): teardowns from play and public information | A teardown template, filled for one public game | |
| 04 | [Design documents that stay alive](04-design-documents.md): one-pagers, GDD, feature specs, data tables, decision records | The course game's document map and one feature spec | |
| **2. Core gameplay** ||||
| 05 | [Controls, camera and game feel](05-controls-camera-feel.md) | Control scheme for one hero on PC and touch | |
| 06 | [Combat design](06-combat.md): tab-target vs action, readability, time-to-kill, pacing | Combat model and TTK targets | `06-ttk` |
| 07 | [Classes and roles](07-classes-roles.md): the trinity, build diversity, class trees | The class roster and role matrix | |
| 08 | [Skills and abilities](08-skills.md): resources, cooldowns, combos, stances, status effects | One class's full skill kit | |
| 09 | [One hero, a party or a roster](09-party-roster.md): player parties, AI companions, multi-character control, hero collection, recruitment vs gacha | Party rules for the course game; the companion and roster variants compared | |
| 10 | [Enemies, bosses and encounters](10-enemies-encounters.md): archetypes, behaviour, boss phases | A field pack, an elite and a three-phase boss | |
| **3. Progression and economy** ||||
| 11 | [Progression and power curves](11-progression.md): levels, XP curves, vertical vs horizontal | The level curve and pacing plan | `11-curves` |
| 12 | [Itemization and loot](12-items-loot.md): rarity, affixes, enhancement, sets, drop tables | Gear tiers and a drop table | `12-loot` |
| 13 | [Economy design](13-economy.md): currencies, faucets and sinks, trading, inflation | Currency map and a 90-day economy model | `13-economy` |
| 14 | [Monetization design](14-monetization.md): business models, fairness, ethics, regulation | Business model and the shop's red lines | `14-gacha-odds` |
| **4. World, content and story** ||||
| 15 | [World and level design](15-world-level.md): regions, fields, dungeons, towns, flow | One region: field, town and dungeon | |
| 16 | [Quest and mission design](16-quests.md): quest types, chains, gating, rewards | A story quest chain that unlocks a class advancement | |
| 17 | [Narrative design for a live game](17-narrative.md): serial story, characters, lore, how story drives systems | Story structure and character sheets | |
| **5. Social, PvP and endgame** ||||
| 18 | [Social systems](18-social.md): parties, guilds, chat, trading, cooperation vs competition | Guild and party features | |
| 19 | [PvP and competitive design](19-pvp.md): duels, arenas, territory war, PvP balance | One PvP mode with its rules | |
| 20 | [Endgame and retention](20-endgame-retention.md): dailies, raids, seasons, collection | The endgame loop and its weekly schedule | |
| **6. Craft and process** ||||
| 21 | [Balancing](21-balancing.md): spreadsheets, formulas, simulation, power budgets | A balance sheet for the class roster | `21-balance` |
| 22 | [UX, UI and onboarding](22-ux-onboarding.md): HUD, readability, the first hour | The first-hour flow | |
| 23 | [PC, mobile and browser](23-platforms.md): session length, input, auto-play, cross-play | Platform rules for the course game | |
| 24 | [Prototyping, playtesting and metrics](24-prototype-playtest.md) | A paper prototype and a playtest plan | |
| 25 | [Designing a live game](25-live-design.md): updates, events, balance patches, talking to players | A one-year content roadmap | |
| 26 | [Capstone: the design package](26-design-package.md) | The course game's complete design package, assembled | |
