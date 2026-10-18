# Chicken Eater

- [Français](#français)
- [English](#english)

---

## Français

Plugin [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) pour CS2 : on mange les poulets.

### Fonctionnalités

- **Poulet vivant** : viser un poulet de près et appuyer sur **E**. +20 PV par défaut, sans jamais
  dépasser 100 ; un joueur déjà à 100 PV ou plus ne gagne rien (et ne perd rien s'il est au-dessus).
  Le poulet meurt comme quand on le tue : son cri de mort et ses plumes, plus une tache de sang au sol.
- **Poulet rôti** : un poulet tué par le feu d'une molotov ou d'une incendiaire laisse un poulet rôti
  au sol pendant 60 s. Le manger avec **E** donne +50 PV par défaut, avec le même plafond, sans sang.
- Les sons d'un repas ne sont entendus que par les joueurs proches (200 unités, environ 5 m).
- Un message dans le chat, pour celui qui mange seulement.

### Commandes

Réservées aux admins (`@css/generic`) :

| Commande | Effet |
|---|---|
| `!sp` | pose un poulet vivant devant soi |
| `!spc` | pose un poulet rôti devant soi |

### Installation

1. Télécharger `cs2-chicken-eater-<version>.zip` dans les [releases](../../releases).
2. Extraire le dossier `ChickenEater` dans `game/csgo/addons/counterstrikesharp/plugins/`.
3. Redémarrer le serveur (ou changer de map) : les modèles et effets sont préchargés au chargement de
   la map.

### Configuration

`game/csgo/addons/counterstrikesharp/configs/plugins/ChickenEater/ChickenEater.json`, créé au premier
chargement. Les PV donnés par chaque poulet se règlent ici :

| Clé | Défaut | Rôle |
|---|---|---|
| `live_chicken_health` | `20` | PV donnés par un poulet vivant |
| `cooked_chicken_health` | `50` | PV donnés par un poulet rôti |
| `max_health` | `100` | plafond : jamais au-dessus, rien à partir de là |
| `language` | `fr` | `fr` ou `en` |
| `eat_distance` | `100` | portée depuis les yeux du joueur (unités du jeu) |
| `hearing_distance` | `200` | seuls les joueurs à cette distance entendent un repas |
| `live_eat_sounds` | `["Chicken.Death"]` | sons joués par le poulet mangé vivant |
| `cooked_eat_sounds` | `["Bounce.Flesh"]` | sons joués par le joueur qui mange un poulet rôti |
| `blood_effect` | `particles/blood_impact/blood_impact_high_ground_decal.vpcf` | tache de sang au sol |
| `feathers_effect` | `particles/critters/chicken/chicken_gone_feathers.vpcf` | plumes (`""` = aucune) |
| `blood_lifetime_seconds` | `30` | durée des effets de sang et de plumes |
| `cooked_chicken_model` | `models/chicken/chicken_roasted.vmdl` | modèle du poulet rôti |
| `cooked_chicken_lifetime_seconds` | `60` | durée du poulet rôti au sol (`0` = jusqu'à la fin de la manche) |
| `chat_messages` | `true` | message dans le chat à chaque repas |

Après une modification : `css_plugins reload ChickenEater`.

### Notes

- `Chicken.Death` reste muet quand c'est un joueur qui le joue : le poulet mangé le joue donc lui-même,
  caché pendant 1,5 s le temps de son cri, puis retiré.
- Chargé en cours de map (première installation), le plugin laisse manger les poulets vivants mais
  attend la map suivante pour le sang, le poulet rôti et les commandes : un modèle non préchargé peut
  faire planter le serveur. Un simple rechargement du plugin, lui, garde tout actif.

### Compiler soi-même

```bash
dotnet publish -c Release
```

Le plugin se trouve dans `src/bin/Release/net10.0/publish/`. Il est compilé contre CounterStrikeSharp
`1.0.376`, épinglé dans `src/ChickenEater.csproj`. Pousser un tag égal à la version de `src/Version.cs`
publie la release sur GitHub.

---

## English

[CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) plugin for CS2: eat the chickens.

### Features

- **Live chicken**: aim at a chicken up close and press **E**. +20 HP by default, never above 100;
  a player already at 100 HP or more gains nothing (and loses nothing above it). The chicken dies like
  when it is killed: its death cry and feathers, plus a blood stain on the ground.
- **Roast chicken**: a chicken killed by molotov or incendiary fire leaves a roast chicken on the
  ground for 60 s. Eating it with **E** gives +50 HP by default, same cap, no blood.
- Meal sounds only reach nearby players (200 units, about 5 m).
- A chat message, for the eater only.

### Commands

Admins only (`@css/generic`):

| Command | Effect |
|---|---|
| `!sp` | places a live chicken in front of you |
| `!spc` | places a roast chicken in front of you |

### Installation

1. Download `cs2-chicken-eater-<version>.zip` from the [releases](../../releases).
2. Extract the `ChickenEater` folder into `game/csgo/addons/counterstrikesharp/plugins/`.
3. Restart the server (or change map): models and effects are precached when the map loads.

### Configuration

`game/csgo/addons/counterstrikesharp/configs/plugins/ChickenEater/ChickenEater.json`, created on first
load. `live_chicken_health` (20) and `cooked_chicken_health` (50) set the HP of each chicken,
`max_health` (100) the cap; the other keys are described in the French table above.

After a change: `css_plugins reload ChickenEater`.

### Build

```bash
dotnet publish -c Release
```

Output in `src/bin/Release/net10.0/publish/`. Built against CounterStrikeSharp `1.0.376`, pinned in
`src/ChickenEater.csproj`. Pushing a tag equal to the version in `src/Version.cs` publishes the
GitHub release.
