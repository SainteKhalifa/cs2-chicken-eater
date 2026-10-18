# Chicken Eater

- [Français](#français)
- [English](#english)

---

## Français

Ce plugin [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) pour CS2 permet aux joueurs de manger les poulets.

### Fonctionnement

**Poulet vivant**

- Le joueur s'approche d'un poulet, le vise et appuie sur **E** pour le manger.
- Il gagne 20 PV (réglable), sans jamais dépasser 100 PV.
- S'il a déjà 100 PV ou plus, il ne gagne rien, mais il ne perd rien non plus.
- Le poulet meurt comme quand on le tue : il pousse son cri de mort, ses plumes volent et une tache de sang reste au sol.

**Poulet rôti**

- Un poulet tué par le feu d'une molotov ou d'une grenade incendiaire laisse un poulet rôti au sol.
- Le poulet rôti reste au sol pendant 60 secondes.
- Le joueur le mange de la même façon, avec **E**. Il gagne 50 PV (réglable), toujours sans dépasser 100 PV.
- Manger un poulet rôti ne laisse pas de sang.

**Sons et messages**

- Seuls les joueurs proches entendent le bruit du repas : à moins de 200 unités, soit environ 5 mètres.
- Le joueur qui mange reçoit un message dans le chat, par exemple : « Miam, un poulet cru ! +20 PV (87 PV) ».

### Commandes

Ces commandes sont réservées aux admins (droit `@css/generic`). Elles servent surtout à tester le plugin sur une map sans poulets.

| Commande | Ce qu'elle fait |
|---|---|
| `!sp` | Fait apparaître un poulet vivant devant soi. |
| `!spc` | Fait apparaître un poulet rôti devant soi. |

### Installation

1. Télécharger le fichier `cs2-chicken-eater-<version>.zip` sur la page des [releases](../../releases).
2. Copier le dossier `ChickenEater` du zip dans `game/csgo/addons/counterstrikesharp/plugins/`.
3. Redémarrer le serveur ou changer de map. Le plugin a besoin d'un chargement de map pour préparer le modèle du poulet rôti et les effets de sang.

### Configuration

Le fichier de configuration est créé automatiquement au premier démarrage du plugin :

`game/csgo/addons/counterstrikesharp/configs/plugins/ChickenEater/ChickenEater.json`

C'est dans ce fichier qu'on choisit le nombre de PV donnés par chaque poulet.

| Réglage | Valeur par défaut | À quoi il sert |
|---|---|---|
| `live_chicken_health` | `20` | Nombre de PV gagnés en mangeant un poulet vivant. |
| `cooked_chicken_health` | `50` | Nombre de PV gagnés en mangeant un poulet rôti. |
| `max_health` | `100` | Nombre maximum de PV. Manger ne fait jamais dépasser cette valeur, et un joueur qui l'a déjà atteinte ne gagne rien. |
| `language` | `fr` | Langue des messages du chat : `fr` pour le français, `en` pour l'anglais. |
| `eat_distance` | `100` | Distance maximale entre le joueur et le poulet pour pouvoir le manger, en unités du jeu. |
| `hearing_distance` | `200` | Distance jusqu'à laquelle les autres joueurs entendent le repas, en unités du jeu (200 unités font environ 5 mètres). |
| `live_eat_sounds` | `["Chicken.Death"]` | Sons joués quand on mange un poulet vivant. Par défaut, c'est le cri de mort du poulet. |
| `cooked_eat_sounds` | `["Bounce.Flesh"]` | Sons joués quand on mange un poulet rôti. |
| `blood_effect` | `particles/blood_impact/blood_impact_high_ground_decal.vpcf` | Effet utilisé pour la tache de sang au sol. |
| `feathers_effect` | `particles/critters/chicken/chicken_gone_feathers.vpcf` | Effet utilisé pour les plumes. Mettre `""` pour ne pas afficher de plumes. |
| `blood_lifetime_seconds` | `30` | Durée d'affichage du sang et des plumes, en secondes. |
| `cooked_chicken_model` | `models/chicken/chicken_roasted.vmdl` | Modèle 3D utilisé pour le poulet rôti. |
| `cooked_chicken_lifetime_seconds` | `60` | Temps pendant lequel le poulet rôti reste au sol, en secondes. Mettre `0` pour qu'il reste jusqu'à la fin de la manche. |
| `chat_messages` | `true` | Affiche un message dans le chat quand un joueur mange un poulet. Mettre `false` pour ne plus afficher ces messages. |

Après avoir modifié le fichier, taper `css_plugins reload ChickenEater` dans la console du serveur pour appliquer les changements.

### Bon à savoir

- Le cri de mort du poulet ne peut être joué que par le poulet lui-même. Quand un joueur le mange, le poulet devient donc invisible, puis disparaît au bout d'une seconde et demie, le temps de son cri.
- À la toute première installation, si le plugin est chargé en pleine partie, le sang, le poulet rôti et les commandes ne marchent qu'à partir de la map suivante. C'est une sécurité : utiliser un modèle qui n'a pas été préparé peut faire planter le serveur. Recharger le plugin plus tard ne pose pas ce problème.

### Compiler le plugin soi-même

```bash
dotnet publish -c Release
```

Le plugin compilé se trouve dans `src/bin/Release/net10.0/publish/`. Il est compilé pour CounterStrikeSharp `1.0.376`, une version fixée dans `src/ChickenEater.csproj`.

Pour publier une nouvelle version sur GitHub, il suffit de pousser un tag qui porte le même numéro que dans `src/Version.cs`.

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
