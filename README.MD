# HvH.gg rapid fire fix (1.0.2)
![img](https://du.hurenso.hn/r/N5TTiI.png)

# About
CounterStrikeSharp plugin to handle rapid fire in CS2.

# Fix Methods
- 0 => Allows rapid fire
- 1 => Blocks rapid fire completely
- 2 => Reflects damage scaled (damage * ReflectScale)
- 3 => Reflects damage scaled, but keeps the player alive

# Reload Config
You can reload the config by using the `hvh_config_reload` command.

# Dependencies
[Metamod:Source (2.x)](https://www.sourcemm.net/downloads.php/?branch=master)  
[CounterStrikeSharp(v167)](https://github.com/roflmuffin/CounterStrikeSharp/releases)

# Installation
1. Install Metamod:Source and CounterStrikeSharp
2. Place the `addons` folder in your servers `game/csgo/` directory  
   ![extract](https://du.hurenso.hn/r/0NyFPY.png)
3. Edit the config file to your liking  
   3.1. Located at `addons/counterstrikesharp/configs/plugins/RapidFireFix/RapidFireFix.json`
4. Restart your server

# Config
```json
{
  "FixMethod": 1, // 0 allows rapid fire, 1 blocks rapid fire, 2 reflects damage scaled, 3 reflects damage but keeps the player alive 
  "ReflectScale": 1, // damage reflect percentage (0.0 - 1.0)
  "ConfigVersion": 1, // do not change
}
```

# Credits
- [Metamod:Source](https://www.sourcemm.net/)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [Discord.Net](https://github.com/discord-net/Discord.Net)
- [MagicBulletFix](https://github.com/CS2Plugins/MagicBulletFix)