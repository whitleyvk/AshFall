# Architecture boundary

## Minimal product core

Keep RobustToolbox and the SS14 ECS, networking, maps, atmosphere, power, interaction, inventory, damage, medical,
access, jobs, preferences, localization, admin, and prototype infrastructure. Keep one modular station, a curated job
set, recovery logistics, and station-bound salvage access.

ASHFALL-owned code lives under `Ashfall` paths and namespaces while the fork is reduced. Once a retained
subsystem has a stable boundary, it may be renamed or moved deliberately; compatibility with endless upstream merges
is not a product goal.

ASHFALL is the project and server name. Ashen Industrial is the former in-world corporate owner, not the project name.
Degradation is a game mode. Russian is the primary product language, while both brand names remain untranslated.

## Removal candidates

Delete rather than archive unused upstream game modes, antagonist implementations, maps, roles, species, event pools,
vending/catalog clutter, construction recipes, ghost roles, shuttle collections, unused locales, hub deployment files,
and their assets. Remove dependencies from prototypes first, then systems, resources, tests, and infrastructure.
Every deletion batch must leave prototypes loadable and Server, Shared, and Client buildable.

Do not import Frontier's sector, deeds, fleets, ship economy, or permanent free-flight loop. Hullrot and RMC may donate
small ideas after a license review, never their product architecture wholesale.

## Degradation contract

The scenario manifest is deterministic from profile, population, and seed. Fault state is represented by the physical
world; the manifest records what was selected but never repairs the station by toggling a flag. Unsafe faults require
map zones, reachable recovery sources, completion detectors, and automated anti-softlock validation before activation.
