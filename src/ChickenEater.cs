using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace ChickenEater
{
    public class ChickenEaterConfig : BasePluginConfig
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "fr";  // "fr" ou "en"

        [JsonPropertyName("live_chicken_health")]
        public int LiveChickenHealth { get; set; } = 20;

        [JsonPropertyName("cooked_chicken_health")]
        public int CookedChickenHealth { get; set; } = 50;

        // Eating never takes a player above this, and gives nothing at or above it
        [JsonPropertyName("max_health")]
        public int MaxHealth { get; set; } = 100;

        // From the player's eyes to the chicken, in game units
        [JsonPropertyName("eat_distance")]
        public float EatDistance { get; set; } = 100f;

        [JsonPropertyName("blood_effect")]
        public string BloodEffect { get; set; } = "particles/blood_impact/blood_impact_high_ground_decal.vpcf";

        [JsonPropertyName("blood_lifetime_seconds")]
        public float BloodLifetimeSeconds { get; set; } = 30f;

        [JsonPropertyName("cooked_chicken_model")]
        public string CookedChickenModel { get; set; } = "models/chicken/chicken_roasted.vmdl";

        // How long a roast stays on the ground (0 = until the round ends)
        [JsonPropertyName("cooked_chicken_lifetime_seconds")]
        public float CookedChickenLifetimeSeconds { get; set; } = 60f;

        // Played by the chicken itself, like when it is killed: Chicken.Death stays
        // silent when a player emits it
        [JsonPropertyName("live_eat_sounds")]
        public List<string> LiveEatSounds { get; set; } = new() { "Chicken.Death" };

        // Played from the player who eats
        [JsonPropertyName("cooked_eat_sounds")]
        public List<string> CookedEatSounds { get; set; } = new() { "Bounce.Flesh" };

        // Only players this close hear a meal (game units, 200 ≈ 5 m)
        [JsonPropertyName("hearing_distance")]
        public float HearingDistance { get; set; } = 200f;

        // The feathers of a chicken's death, beside the blood ("" = none)
        [JsonPropertyName("feathers_effect")]
        public string FeathersEffect { get; set; } = "particles/critters/chicken/chicken_gone_feathers.vpcf";

        [JsonPropertyName("chat_messages")]
        public bool ChatMessages { get; set; } = true;
    }

    public partial class ChickenEater : BasePlugin, IPluginConfig<ChickenEaterConfig>
    {
        public override string ModuleName => "Chicken Eater";
        public override string ModuleAuthor => "SainteKhalifa";
        public override string ModuleDescription => "Eat chickens with E: alive for a few HP and a blood stain, roasted for more";

        private const string ChickenModel = "models/chicken/chicken.vmdl";
        private const string BloodSpray = "particles/blood_impact/blood_impact_heavy.vpcf";
        // What other_death reports for a chicken killed by a molotov or incendiary fire
        private static readonly string[] FireWeapons = { "inferno", "molotov", "incgrenade" };
        // E also opens doors, picks up weapons and defuses: one press, one chicken
        private const float EatCooldown = 0.5f;
        private const float StandingEyeHeight = 64f;
        private const float CrouchingEyeHeight = 46f;
        // Within about 40° of where the player looks
        private const float AimCone = 0.75f;
        // How long an eaten chicken stays, hidden, for its death cry to play out
        private const float DeathCryDuration = 1.5f;

        // ChatColors keeps old spellings beside the new ones (DarkRed and Darkred):
        // once lowercased they collide, so the first one wins
        private static readonly Dictionary<string, string> ColorTags = typeof(ChatColors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .GroupBy(field => "{" + field.Name.ToLowerInvariant() + "}")
            .ToDictionary(group => group.Key, group => group.First().GetValue(null)?.ToString() ?? "");

        public ChickenEaterConfig Config { get; set; } = new();

        private readonly List<CDynamicProp> _roasted = new();
        private readonly Dictionary<int, float> _lastEat = new();
        private readonly HashSet<uint> _eaten = new();
        private Dictionary<string, string> _translations = new();
        // Models and effects are only usable once a map has loaded with the plugin:
        // set by the precache listener, false after a load in the middle of a map
        private bool _resourcesReady;

        public void OnConfigParsed(ChickenEaterConfig config)
        {
            Config = config;
        }

        public override void Load(bool hotReload)
        {
            LoadTranslations();

            // A map without chickens loads neither the models nor the blood effects:
            // a roast or a spawned chicken would stay invisible there
            RegisterListener<Listeners.OnServerPrecacheResources>(manifest =>
            {
                manifest.AddResource(ChickenModel);
                manifest.AddResource(Config.CookedChickenModel);
                manifest.AddResource(Config.BloodEffect);
                manifest.AddResource(BloodSpray);
                if (Config.FeathersEffect.Length > 0)
                    manifest.AddResource(Config.FeathersEffect);
                _resourcesReady = true;
                WritePrecacheStamp();
            });
            // Reloaded in the middle of a map: the previous instance precached for it
            if (hotReload)
                _resourcesReady = ReadPrecacheStamp() == PrecacheStamp();
            RegisterListener<Listeners.OnPlayerButtonsChanged>(OnButtonsChanged);
            RegisterListener<Listeners.OnClientDisconnect>(slot => _lastEat.Remove(slot));
            // Entity indexes start over with the map: an old one would hide a new chicken
            RegisterListener<Listeners.OnMapStart>(_ => _eaten.Clear());
            RegisterEventHandler<EventOtherDeath>(OnOtherDeath);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
        }

        private void OnButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if ((pressed & PlayerButtons.Use) == 0 || !player.IsValid || player.IsBot)
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null)
                return;

            var now = Server.CurrentTime;
            if (_lastEat.TryGetValue(player.Slot, out var last) && now - last < EatCooldown)
                return;

            var food = FindFood(pawn, out var cooked);
            if (food == null)
                return;

            _lastEat[player.Slot] = now;
            Eat(player, pawn, food, cooked);
        }

        // The chicken or roast the player looks at, the closest one within reach
        private CBaseEntity? FindFood(CCSPlayerPawn pawn, out bool cooked)
        {
            var origin = pawn.AbsOrigin!;
            var ducking = (pawn.Flags & (uint)PlayerFlags.FL_DUCKING) != 0;
            float ex = origin.X, ey = origin.Y, ez = origin.Z + (ducking ? CrouchingEyeHeight : StandingEyeHeight);

            var angles = pawn.EyeAngles;
            var pitch = angles.X * MathF.PI / 180f;
            var yaw = angles.Y * MathF.PI / 180f;
            float fx = MathF.Cos(pitch) * MathF.Cos(yaw), fy = MathF.Cos(pitch) * MathF.Sin(yaw), fz = -MathF.Sin(pitch);

            CBaseEntity? best = null;
            var bestCooked = false;
            var bestDistance = Config.EatDistance;

            void Consider(CBaseEntity entity, bool isCooked)
            {
                if (!entity.IsValid)
                    return;
                var position = entity.AbsOrigin;
                if (position == null)
                    return;

                // Aim at the body, not at the feet
                float dx = position.X - ex, dy = position.Y - ey, dz = position.Z + 8f - ez;
                var distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
                if (distance < 1f || distance > bestDistance)
                    return;
                if ((dx * fx + dy * fy + dz * fz) / distance < AimCone)
                    return;

                best = entity;
                bestCooked = isCooked;
                bestDistance = distance;
            }

            foreach (var chicken in Utilities.FindAllEntitiesByDesignerName<CChicken>("chicken"))
            {
                if (!_eaten.Contains(chicken.Index))
                    Consider(chicken, false);
            }

            _roasted.RemoveAll(roast => !roast.IsValid);
            foreach (var roast in _roasted)
                Consider(roast, true);

            cooked = bestCooked;
            return best;
        }

        private void Eat(CCSPlayerController player, CCSPlayerPawn pawn, CBaseEntity food, bool cooked)
        {
            var position = food.AbsOrigin!;
            float x = position.X, y = position.Y, z = position.Z;

            var gained = Heal(pawn, cooked ? Config.CookedChickenHealth : Config.LiveChickenHealth);
            var listeners = NearbyPlayers(pawn);

            if (cooked)
            {
                if (food is CDynamicProp roast)
                    _roasted.Remove(roast);
                food.Remove();
                foreach (var sound in Config.CookedEatSounds)
                    pawn.EmitSound(sound, listeners);
            }
            else
            {
                // A chicken's own death: it cries itself and its feathers burst. Removed
                // right away it would cut its cry short, so it is hidden until then
                foreach (var sound in Config.LiveEatSounds)
                    food.EmitSound(sound, listeners);
                SpawnBlood(x, y, z);
                Hide(food);
                var index = food.Index;
                _eaten.Add(index);
                AddTimer(DeathCryDuration, () =>
                {
                    _eaten.Remove(index);
                    if (food.IsValid)
                        food.Remove();
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }

            if (Config.ChatMessages)
                player.PrintToChat(Message(gained == 0 ? "ateFull" : cooked ? "ateCooked" : "ateLive", gained, pawn.Health));
        }

        // Some sounds (the UI ones) are not positional: without this limit they would
        // reach the whole server
        private RecipientFilter NearbyPlayers(CCSPlayerPawn eater)
        {
            var filter = new RecipientFilter();
            var origin = eater.AbsOrigin!;
            foreach (var player in Utilities.GetPlayers())
            {
                var position = player.PlayerPawn.Value?.AbsOrigin;
                if (player.IsBot || position == null)
                    continue;
                float dx = position.X - origin.X, dy = position.Y - origin.Y, dz = position.Z - origin.Z;
                if (dx * dx + dy * dy + dz * dz <= Config.HearingDistance * Config.HearingDistance)
                    filter.Add(player);
            }
            return filter;
        }

        // A hot reload gets a new instance, the stamp tells it the map is already precached:
        // same server process and same map as the last precache
        private static string PrecacheStampPath => Path.Combine(Path.GetTempPath(), "chicken_eater_precache");

        private static string PrecacheStamp() => $"{Environment.ProcessId}|{Server.MapName}";

        private void WritePrecacheStamp()
        {
            try
            {
                File.WriteAllText(PrecacheStampPath, PrecacheStamp());
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Without it, a reload waits for the next map before roasts come back
                Logger.LogWarning("Chicken Eater: cannot write {Path}: {Message}", PrecacheStampPath, e.Message);
            }
        }

        private static string ReadPrecacheStamp()
        {
            try
            {
                return File.ReadAllText(PrecacheStampPath);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return "";
            }
        }

        // Never above max_health, and nothing for a player already at or above it: some
        // get more than 100 HP from other plugins, eating must not take any from them
        private int Heal(CCSPlayerPawn pawn, int amount)
        {
            var health = pawn.Health;
            if (amount <= 0 || health >= Config.MaxHealth)
                return 0;

            var healed = Math.Min(Config.MaxHealth, health + amount);
            pawn.Health = healed;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            return healed - health;
        }

        // A splash, the stain it leaves on the ground and the feathers of a chicken's death
        private void SpawnBlood(float x, float y, float z)
        {
            if (!_resourcesReady)
                return;
            SpawnParticle(BloodSpray, x, y, z + 8f);
            SpawnParticle(Config.BloodEffect, x, y, z + 2f);
            if (Config.FeathersEffect.Length > 0)
                SpawnParticle(Config.FeathersEffect, x, y, z + 10f);
        }

        // Invisible but still there, for its cry to play out
        private static void Hide(CBaseEntity entity)
        {
            if (entity is not CBaseModelEntity model)
                return;
            model.RenderMode = RenderMode_t.kRenderTransAlpha;
            model.Render = Color.FromArgb(0, 255, 255, 255);
            Utilities.SetStateChanged(model, "CBaseModelEntity", "m_nRenderMode");
            Utilities.SetStateChanged(model, "CBaseModelEntity", "m_clrRender");
        }

        private void SpawnParticle(string effect, float x, float y, float z)
        {
            var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle == null)
                return;

            particle.EffectName = effect;
            particle.StartActive = true;
            // Facing up, like an impact on the floor
            particle.Teleport(new Vector(x, y, z), new QAngle(-90f, 0f, 0f), new Vector(0f, 0f, 0f));
            particle.DispatchSpawn();
            particle.AcceptInput("Start");
            AddTimer(Config.BloodLifetimeSeconds, () =>
            {
                if (particle.IsValid)
                    particle.Remove();
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        // A chicken killed by fire leaves a roast in its place
        private HookResult OnOtherDeath(EventOtherDeath @event, GameEventInfo info)
        {
            var weapon = @event.Weapon ?? "";
            if (@event.Othertype != "chicken" || !FireWeapons.Any(fire => weapon.Contains(fire)))
                return HookResult.Continue;

            var chicken = Utilities.GetEntityFromIndex<CChicken>(@event.Otherid);
            if (chicken == null || !chicken.IsValid || chicken.AbsOrigin == null)
                return HookResult.Continue;

            float x = chicken.AbsOrigin.X, y = chicken.AbsOrigin.Y, z = chicken.AbsOrigin.Z;
            var yaw = chicken.AbsRotation?.Y ?? 0f;
            // The chicken is still being killed: the roast takes its place on the next frame
            Server.NextFrame(() => SpawnRoasted(x, y, z, yaw));
            return HookResult.Continue;
        }

        private CDynamicProp? SpawnRoasted(float x, float y, float z, float yaw)
        {
            // A model the map has not precached can bring the server down
            if (!_resourcesReady)
                return null;
            var roast = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (roast == null)
                return null;

            roast.Teleport(new Vector(x, y, z), new QAngle(0f, yaw, 0f), new Vector(0f, 0f, 0f));
            roast.DispatchSpawn();
            roast.SetModel(Config.CookedChickenModel);
            _roasted.Add(roast);

            if (Config.CookedChickenLifetimeSeconds > 0)
            {
                AddTimer(Config.CookedChickenLifetimeSeconds, () =>
                {
                    if (roast.IsValid)
                        roast.Remove();
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
            return roast;
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // Last round's roasts: whatever the map cleanup left behind goes too
            foreach (var roast in _roasted)
            {
                if (roast.IsValid)
                    roast.Remove();
            }
            _roasted.Clear();
            _eaten.Clear();
            return HookResult.Continue;
        }

        [ConsoleCommand("css_sp", "Fait apparaître un poulet vivant devant soi (test)")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/generic")]
        public void OnChickenCommand(CCSPlayerController? player, CommandInfo command) => SpawnInFront(player, false);

        [ConsoleCommand("css_spc", "Fait apparaître un poulet rôti devant soi (test)")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/generic")]
        public void OnRoastCommand(CCSPlayerController? player, CommandInfo command) => SpawnInFront(player, true);

        // For maps without chickens: one a few steps ahead, facing the player
        private void SpawnInFront(CCSPlayerController? player, bool cooked)
        {
            var pawn = player?.PlayerPawn.Value;
            if (player == null || pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                return;
            if (!_resourcesReady)
            {
                player.PrintToChat(Message("notReady"));
                return;
            }

            var yawDegrees = pawn.EyeAngles.Y;
            var yaw = yawDegrees * MathF.PI / 180f;
            float x = pawn.AbsOrigin.X + MathF.Cos(yaw) * 60f, y = pawn.AbsOrigin.Y + MathF.Sin(yaw) * 60f, z = pawn.AbsOrigin.Z;

            var spawned = false;
            if (cooked)
            {
                spawned = SpawnRoasted(x, y, z, yawDegrees + 180f) != null;
            }
            else
            {
                var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
                if (chicken != null)
                {
                    chicken.Teleport(new Vector(x, y, z), new QAngle(0f, yawDegrees + 180f, 0f), new Vector(0f, 0f, 0f));
                    chicken.DispatchSpawn();
                    spawned = true;
                }
            }
            player.PrintToChat(Message(spawned ? (cooked ? "spawnedCooked" : "spawned") : "spawnFailed"));
        }

        private string Message(string key, params object[] args)
        {
            var text = _translations.TryGetValue(key, out var value) ? value : key;
            var prefix = _translations.TryGetValue("prefix", out var tag) ? tag : "[Poulet]";
            try
            {
                text = string.Format(text, args);
            }
            catch (FormatException)
            {
                // A broken translation still shows, without its numbers
            }
            return $" {prefix} {text}";
        }

        // Read the language file matching the config, independent of server culture
        private void LoadTranslations()
        {
            var language = Config.Language.ToLowerInvariant() == "en" ? "en" : "fr";
            var path = Path.Combine(ModuleDirectory, "lang", $"{language}.json");
            _translations = new Dictionary<string, string>();
            if (!File.Exists(path))
                return;

            try
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
                foreach (var (key, value) in raw ?? new Dictionary<string, string>())
                {
                    var text = value;
                    foreach (var (colorTag, color) in ColorTags)
                        text = text.Replace(colorTag, color, StringComparison.OrdinalIgnoreCase);
                    _translations[key] = text;
                }
            }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
            {
                // Messages then show their keys instead of the whole plugin failing to load
                Logger.LogError(e, "Chicken Eater: cannot read {Path}", path);
            }
        }
    }
}
