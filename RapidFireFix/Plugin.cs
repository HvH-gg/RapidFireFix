using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace RapidFireFix;

// https://github.com/CS2Plugins/MagicBulletFix/blob/main/MagicBulletFix.cs
public enum FixMethod
{
    Allow = 0,
    Ignore,
    Reflect,
    ReflectSafe
}

public class RapidFireFixConfig : BasePluginConfig
{
    [JsonPropertyName("FixMethod")] public FixMethod FixMethod { get; set; } = FixMethod.Ignore;
    [JsonPropertyName("ReflectScale")] public float ReflectScale { get; set; } = 1f;
}

public class Plugin : BasePlugin, IPluginConfig<RapidFireFixConfig>
{
    public override string ModuleName => "HvH.gg rapid fire fix";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "imi-tat0r";
    
    public RapidFireFixConfig Config { get; set; } = new();
    
    private readonly Dictionary<uint, int> _lastPlayerShotTick = new();
    private readonly HashSet<uint> _rapidFireBlockUserIds = new();
    private readonly Dictionary<uint, float> _rapidFireBlockWarnings = new();
    
    private static readonly string ChatPrefix = $"[{ChatColors.Red}Hv{ChatColors.DarkRed}H{ChatColors.Default}.gg]";

    public void OnConfigParsed(RapidFireFixConfig config)
    {
        Config = config;
    }
    
    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        Console.WriteLine("[HvH.gg] Start loading HvH.gg rapid fire fix plugin");

        RegisterListener<Listeners.OnMapStart>(name =>
        {
            _lastPlayerShotTick.Clear();
        });

        RegisterListener<Listeners.OnClientDisconnect>(client =>
        {
            var entityFromSlot = Utilities.GetPlayerFromSlot(client);
            _lastPlayerShotTick.Remove(entityFromSlot.Pawn.Index);
            _rapidFireBlockUserIds.Remove(entityFromSlot.Pawn.Index);
            _rapidFireBlockWarnings.Remove(entityFromSlot.Pawn.Index);
        });

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            _lastPlayerShotTick.Remove(@event.Userid.Pawn.Index);
            _rapidFireBlockUserIds.Remove(@event.Userid.Pawn.Index);
            _rapidFireBlockWarnings.Remove(@event.Userid.Pawn.Index);
            
            return HookResult.Continue;
        });
        
        RegisterEventHandler<EventWeaponFire>((@event, info) =>
        {
            if (@event.Userid is not { IsValid: true, IsHLTV: false, IsBot: false, UserId: not null, SteamID: >0 })
                return HookResult.Continue;
            
            var nextPrimaryAttackTick = @event.Userid.Pawn.Value?.WeaponServices?.ActiveWeapon.Value?.NextPrimaryAttackTick ?? 0;
            var index = @event.Userid.Pawn.Index;
            
            if (!_lastPlayerShotTick.TryGetValue(index, out var lastShotTick))
            {
                _lastPlayerShotTick[index] = Server.TickCount;
                return HookResult.Continue;
            }
            
            _lastPlayerShotTick[index] = Server.TickCount;

            // this is ghetto but should work for now
            if (nextPrimaryAttackTick > lastShotTick)
                return HookResult.Continue;

            Console.WriteLine($"[HvH.gg] Detected rapid fire from {@event.Userid.PlayerName}");
            
            // clear list every frame (in case of misses)
            if (_rapidFireBlockUserIds.Count == 0)
                Server.NextFrame(_rapidFireBlockUserIds.Clear);
            
            _rapidFireBlockUserIds.Add(index);
            
            // skip warning if we already warned this player in the last 3 seconds
            if (_rapidFireBlockWarnings.TryGetValue(index, out var lastWarningTime) &&
                lastWarningTime + 3 > Server.CurrentTime) 
                return HookResult.Continue;
            
            // warn player
            Server.PrintToChatAll($"{ChatPrefix} Player {ChatColors.Red}{@event.Userid.PlayerName}{ChatColors.Default} tried using {ChatColors.Red}rapid fire{ChatColors.Default}!");
            _rapidFireBlockWarnings[index] = Server.CurrentTime;

            return HookResult.Continue;
        });
        
        
        // block damage if attacker is in the list
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook((h) =>
        {
            var damageInfo = h.GetParam<CTakeDamageInfo>(1);

            // attacker is invalid
            if (damageInfo.Attacker.Value == null)
                return HookResult.Continue;

            // attacker is not in the list
            if (!_rapidFireBlockUserIds.Contains(damageInfo.Attacker.Index))
                return HookResult.Continue;
            
            // set damage according to config
            switch (Config.FixMethod)
            {
                case FixMethod.Allow:
                    break;
                case FixMethod.Ignore:
                    damageInfo.Damage = 0;
                    break;
                case FixMethod.Reflect:
                case FixMethod.ReflectSafe:
                    damageInfo.Damage *= Config.ReflectScale;
                    h.SetParam<CEntityInstance>(0, damageInfo.Attacker.Value);
                    if (Config.FixMethod == FixMethod.ReflectSafe)
                        damageInfo.DamageFlags |= TakeDamageFlags_t.DFLAG_PREVENT_DEATH; //https://docs.cssharp.dev/api/CounterStrikeSharp.API.Core.TakeDamageFlags_t.html
                    break;
            }

            return HookResult.Changed;
        }, HookMode.Pre);
        
        Console.WriteLine("[HvH.gg] Finished loading HvH.gg rapid fire fix plugin");
    }
}