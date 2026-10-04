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
| 02 | [Genres and formats](02-genres-formats.md): the genre map from the MMORPG to the open-world action RPG, the squad RPG and idle games; classic and 8-bit style games | Where the course game sits, and how it would change in three other genres | |
| 03 | [Vision, pillars and loops](03-vision-pillars-loops.md): core, session and meta loops | Vision, 4 pillars, the three loops | |
| 04 | [Studying a reference game as a designer](04-studying-a-reference-game.md): teardowns from play and public information | A teardown template, filled for one public game | |
| 05 | [Design documents that stay alive](05-design-documents.md): one-pagers, GDD, feature specs, data tables, decision records | The course game's document map and one feature spec | |
| **2. Core gameplay** ||||
| 06 | [Controls, camera and game feel](06-controls-camera-feel.md) | Control scheme for one hero on PC and touch | |
| 07 | [2D, 2.5D and 3D](07-dimensions-art-style.md): how dimension and art style change design; browser games | The course game's dimension and style, and what each alternative would change | |
| 08 | [Combat design](08-combat.md): tab-target vs action, readability, time-to-kill, pacing | Combat model and TTK targets | `08-ttk` |
| 09 | [Classes and roles](09-classes-roles.md): the trinity, build diversity, class trees | The class roster and role matrix | |
| 10 | [Skills and abilities](10-skills.md): resources, cooldowns, combos, stances, status effects | One class's full skill kit | |
| 11 | [One hero, a party or a roster](11-party-roster.md): player parties, AI companions, multi-character control, hero collection, recruitment vs gacha | Party rules for the course game; the companion and roster variants compared | |
| 12 | [Enemies, bosses and encounters](12-enemies-encounters.md): archetypes, behaviour, boss phases | A field pack, an elite and a three-phase boss | |
| 13 | [AI-driven characters](13-ai-driven-characters.md): NPCs and companions played by AI agents, guardrails, test bots | An AI-driven companion and NPC with persona, limits and fallbacks | |
| **3. Progression and economy** ||||
| 14 | [Progression and power curves](14-progression.md): levels, XP curves, vertical vs horizontal | The level curve and pacing plan | `14-curves` |
| 15 | [Itemization and loot](15-items-loot.md): rarity, affixes, enhancement, sets, drop tables | Gear tiers and a drop table | `15-loot` |
| 16 | [Economy design](16-economy.md): currencies, faucets and sinks, trading, inflation | Currency map and a 90-day economy model | `16-economy` |
| 17 | [Monetization design](17-monetization.md): business models, fairness, ethics, regulation | Business model and the shop's red lines | `17-gacha-odds` |
| **4. World, content and story** ||||
| 18 | [World and level design](18-world-level.md): regions, fields, dungeons, towns, flow, the open world | One region: field, town and dungeon | |
| 19 | [Quest and mission design](19-quests.md): quest types, chains, gating, rewards | A story quest chain that unlocks a class advancement | |
| 20 | [Narrative design for a live game](20-narrative.md): serial story, characters, lore, how story drives systems | Story structure and character sheets | |
| 21 | [One story world, many games](21-story-world-many-games.md): adapting a story world across genres, canon tiers across products | Three spin-off products from the course game's world | |
| **5. Social, PvP and endgame** ||||
| 22 | [Social systems](22-social.md): parties, guilds, chat, trading, cooperation vs competition | Guild and party features | |
| 23 | [PvP and competitive design](23-pvp.md): duels, arenas, territory war, PvP balance | One PvP mode with its rules | |
| 24 | [Endgame and retention](24-endgame-retention.md): dailies, raids, seasons, collection | The endgame loop and its weekly schedule | |
| **6. Craft and process** ||||
| 25 | [Balancing](25-balancing.md): spreadsheets, formulas, simulation, power budgets | A balance sheet for the class roster | `25-balance` |
| 26 | [UX, UI and onboarding](26-ux-onboarding.md): HUD, readability, the first hour | The first-hour flow | |
| 27 | [PC, mobile and browser](27-platforms.md): session length, input, auto-play, cross-play | Platform rules for the course game | |
| 28 | [Prototyping, playtesting and metrics](28-prototype-playtest.md) | A paper prototype and a playtest plan | |
| 29 | [Designing a live game](29-live-design.md): updates, events, balance patches, talking to players | A one-year content roadmap | |
| 30 | [Capstone: the design package](30-design-package.md) | The course game's complete design package, assembled | |
