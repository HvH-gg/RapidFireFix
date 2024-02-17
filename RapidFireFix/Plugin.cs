using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace RapidFireFix;

public class Plugin : BasePlugin
{
    public override string ModuleName => "HvH.gg rapid fire fix";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "imi-tat0r";
    
    private readonly Dictionary<uint, int> _playerShotInfo = new();
    private readonly HashSet<uint> _rapidFireBlockUserIds = new();
    private readonly Dictionary<uint, float> _rapidFireBlockWarnings = new();
    
    private static readonly string ChatPrefix = $"[{ChatColors.Red}Hv{ChatColors.DarkRed}H{ChatColors.Default}.gg]";

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        Console.WriteLine("[HvH.gg] Start loading HvH.gg rapid fire fix plugin");

        RegisterListener<Listeners.OnMapStart>(name =>
        {
            _playerShotInfo.Clear();
        });

        RegisterEventHandler<EventWeaponFire>((@event, info) =>
        {
            if (@event.Userid is not { IsValid: true, IsHLTV: false, IsBot: false, UserId: not null, SteamID: >0 })
                return HookResult.Continue;
            
            var index = @event.Userid.Pawn.Index;
            
            if (!_playerShotInfo.TryGetValue(index, out var nextAttackTickCount))
                return HookResult.Continue;
            
            _playerShotInfo[index] = @event.Userid.Pawn.Value?.WeaponServices?.ActiveWeapon.Value?.NextPrimaryAttackTick ?? 0;
            
            if (nextAttackTickCount <= Server.TickCount)
                return HookResult.Continue;
            
            // clear list every frame (in case of misses)
            if (_rapidFireBlockUserIds.Count == 0)
                Server.NextFrame(_rapidFireBlockUserIds.Clear);
            
            _rapidFireBlockUserIds.Add(index);
            
            // skip warning if we already warned this player in the last 3 seconds
            if (!_rapidFireBlockWarnings.TryGetValue(index, out var lastWarningTime) ||
                lastWarningTime + 3 <= Server.CurrentTime) 
                return HookResult.Continue;
            
            // warn player
            Server.PrintToChatAll($"{ChatPrefix} {ChatColors.Red}{@event.Userid.PlayerName}{ChatColors.Default} tried using {ChatColors.Red}rapid fire{ChatColors.Default}!");
            _rapidFireBlockWarnings[index] = Server.CurrentTime;

            return HookResult.Continue;
        });
        
        // friendly fire + part of magic bullet restriction
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook((h) =>
        {
            var damageInfo = h.GetParam<CTakeDamageInfo>(1);

            // attacker is invalid
            if (damageInfo.Attacker.Value == null)
                return HookResult.Continue;

            // attacker is not in the list
            if (!_rapidFireBlockUserIds.Contains(damageInfo.Attacker.Index))
                return HookResult.Continue;

            // set damage to 0
            damageInfo.Damage = 0;
        
            Console.WriteLine("[HvH.gg] Blocked rapid fire");

            return HookResult.Changed;
        }, HookMode.Pre);
        
        Console.WriteLine("[HvH.gg] Finished loading HvH.gg rapid fire fix plugin");
    }
}