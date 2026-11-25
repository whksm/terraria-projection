# 泰拉瑞亚 TileID → 中文名称对照表

来源：`Terraria.exe`（1.4.5.8 原版）内嵌资源 `Terraria.Localization.Content.zh-Hans.Items.json` / `en-US.Items.json` 的物品名；
方块名取「能放置出该 tile 的物品」（`Item.SetDefaults` 中 `createTile == id`，由反编译 `Terraria.Item` 的 `SetDefaults1..5` 解析得到）。

共 754 个 tile（0..753），其中 519 个有对应放置物品名；无放置物品的多为自然生成/装饰块，名称回退为 TileID 常量名。

## 你蓝图里用到的 44 种（按出现次数降序）

| # | TileID | 次数 | 中文名 | 英文名 | 放置物品 | TileID 常量 |
|---:|---:|---:|---|---|---|---|
| 1 | 1 | 3217 | 石块 | Stone Block | StoneBlock | Stone |
| 2 | 53 | 3037 | 沙块 | Sand Block | SandBlock | Sand |
| 3 | 161 | 2998 | 冰雪块 | Ice Block | IceBlock | IceBlock |
| 4 | 472 | 1965 | 铁砖 | Iron Brick | IronBrick | IronBrick |
| 5 | 2 | 1080 | 草种子 | Grass Seeds | GrassSeeds | Grass |
| 6 | 44 | 985 | 粉砖 | Pink Brick | PinkBrick | PinkDungeonBrick |
| 7 | 40 | 934 | 粘土块 | Clay Block | ClayBlock | ClayBlock |
| 8 | 234 | 768 | 猩红沙块 | Crimsand Block | CrimsandBlock | Crimsand |
| 9 | 382 | 709 | （无对应物品） | Vine Flowers |  | VineFlowers |
| 10 | 203 | 598 | 猩红石块 | Crimstone Block | CrimstoneBlock | Crimstone |
| 11 | 21 | 418 | 宝箱 | Chest | Chest | Containers |
| 12 | 19 | 305 | 木平台 | Wood Platform | WoodPlatform | Platforms |
| 13 | 225 | 278 | 蜂巢魔棒 | Hive Wand | HiveWand | Hive |
| 14 | 5 | 278 | （无对应物品） | Trees |  | Trees |
| 15 | 421 | 168 | 传送带（顺时针） | Conveyor Belt (Clockwise) | ConveyorBeltLeft | ConveyorBeltLeft |
| 16 | 73 | 107 | （无对应物品） | Plants2 |  | Plants2 |
| 17 | 6 | 88 | 铁矿 | Iron Ore | IronOre | Iron |
| 18 | 4 | 85 | 火把 | Torch | Torch | Torches |
| 19 | 187 | 72 | （无对应物品） | Large Piles2 |  | LargePiles2 |
| 20 | 28 | 70 | （无对应物品） | Pots |  | Pots |
| 21 | 27 | 64 | 向日葵 | Sunflower | Sunflower | Sunflower |
| 22 | 166 | 55 | 锡矿 | Tin Ore | TinOre | Tin |
| 23 | 185 | 54 | （无对应物品） | Small Piles |  | SmallPiles |
| 24 | 80 | 46 | （无对应物品） | Cactus |  | Cactus |
| 25 | 3 | 36 | 蓝花种子 | Blue Flower Seeds | FlowerPacketBlue | Plants |
| 26 | 235 | 26 | 传送机 | Teleporter | Teleporter | Teleporter |
| 27 | 616 | 24 | （无对应物品） | Vanity Tree Yellow Willow |  | VanityTreeYellowWillow |
| 28 | 137 | 16 | 飞镖机关 | Dart Trap | DartTrap | Traps |
| 29 | 422 | 16 | 传送带（逆时针） | Conveyor Belt (Counter Clockwise) | ConveyorBeltRight | ConveyorBeltRight |
| 30 | 144 | 14 | 1秒计时器 | 1 Second Timer | Timer1Second | Timers |
| 31 | 196 | 14 | 雨云 | Rain Cloud | RainCloud | RainCloud |
| 32 | 10 | 12 | 木门 | Wooden Door | WoodenDoor | ClosedDoor |
| 33 | 97 | 8 | 保险箱 | Safe | Safe | Safes |
| 34 | 136 | 5 | 开关 | Switch | Switch | Switches |
| 35 | 314 | 4 | 矿车轨道 | Minecart Track | MinecartTrack | MinecartTrack |
| 36 | 49 | 2 | 水蜡烛 | Water Candle | WaterCandle | WaterCandle |
| 37 | 646 | 2 | 暗影蜡烛 | Shadow Candle | ShadowCandle | ShadowCandle |
| 38 | 30 | 2 | 木材 | Wood | Wood | WoodBlock |
| 39 | 135 | 2 | 红压力板 | Red Pressure Plate | RedPressurePlate | PressurePlates |
| 40 | 227 | 2 | 青绿蘑菇 | Teal Mushroom | TealMushroom | DyePlants |
| 41 | 52 | 2 | （无对应物品） | Vines |  | Vines |
| 42 | 428 | 2 | 粉色测重压力板 | Pink Weighted Pressure Plate | WeightedPressurePlatePink | WeightedPressurePlate |
| 43 | 83 | 2 | （无对应物品） | Mature Herbs |  | MatureHerbs |
| 44 | 189 | 1 | 云 | Cloud | Cloud | Cloud |

## 全量对照表（按 ID 升序）

| ID | 中文名 | 英文名 | TileID 常量 | 主物品 | 物品数 | 来源 |
|---:|---|---|---|---|---:|---|
| 0 | 土块 | Dirt Block | Dirt | 2 DirtBlock | 1 | item.createTile + zh-Hans.ItemName |
| 1 | 石块 | Stone Block | Stone | 3 StoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 2 | 草种子 | Grass Seeds | Grass | 62 GrassSeeds | 3 | item.createTile + zh-Hans.ItemName |
| 3 | 蓝花种子 | Blue Flower Seeds | Plants | 4041 FlowerPacketBlue | 9 | item.createTile + zh-Hans.ItemName |
| 4 | 火把 | Torch | Torches | 8 Torch | 16 | item.createTile + zh-Hans.ItemName |
| 5 |  | Trees | Trees |  | 0 | TileID constant (no placing item) |
| 6 | 铁矿 | Iron Ore | Iron | 11 IronOre | 1 | item.createTile + zh-Hans.ItemName |
| 7 | 铜矿 | Copper Ore | Copper | 12 CopperOre | 1 | item.createTile + zh-Hans.ItemName |
| 8 | 金矿 | Gold Ore | Gold | 13 GoldOre | 1 | item.createTile + zh-Hans.ItemName |
| 9 | 银矿 | Silver Ore | Silver | 14 SilverOre | 1 | item.createTile + zh-Hans.ItemName |
| 10 | 木门 | Wooden Door | ClosedDoor | 25 WoodenDoor | 64 | item.createTile + zh-Hans.ItemName |
| 11 |  | Open Door | OpenDoor |  | 0 | TileID constant (no placing item) |
| 12 | 修复的生命水晶 | Repaired Life Crystal | Heart | 5286 RepairedLifeCrystal | 1 | item.createTile + zh-Hans.ItemName |
| 13 | 玻璃瓶 | Bottle | Bottles | 31 Bottle | 9 | item.createTile + zh-Hans.ItemName |
| 14 | 木桌 | Wooden Table | Tables | 32 WoodenTable | 29 | item.createTile + zh-Hans.ItemName |
| 15 | 木椅 | Wooden Chair | Chairs | 34 WoodenChair | 68 | item.createTile + zh-Hans.ItemName |
| 16 | 铁砧 | Iron Anvil | Anvils | 35 IronAnvil | 2 | item.createTile + zh-Hans.ItemName |
| 17 | 熔炉 | Furnace | Furnaces | 33 Furnace | 1 | item.createTile + zh-Hans.ItemName |
| 18 | 工作台 | Work Bench | WorkBenches | 36 WorkBench | 62 | item.createTile + zh-Hans.ItemName |
| 19 | 木平台 | Wood Platform | Platforms | 94 WoodPlatform | 64 | item.createTile + zh-Hans.ItemName |
| 20 | 橡实 | Acorn | Saplings | 27 Acorn | 1 | item.createTile + zh-Hans.ItemName |
| 21 | 宝箱 | Chest | Containers | 48 Chest | 43 | item.createTile + zh-Hans.ItemName |
| 22 | 魔矿 | Demonite Ore | Demonite | 56 DemoniteOre | 1 | item.createTile + zh-Hans.ItemName |
| 23 | 腐化种子 | Corrupt Seeds | CorruptGrass | 59 CorruptSeeds | 1 | item.createTile + zh-Hans.ItemName |
| 24 |  | Corrupt Plants | CorruptPlants |  | 0 | TileID constant (no placing item) |
| 25 | 黑檀石块 | Ebonstone Block | Ebonstone | 61 EbonstoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 26 | 恶魔祭坛 | Demon Altar | DemonAltar | 5532 DemonAltar | 2 | item.createTile + zh-Hans.ItemName |
| 27 | 向日葵 | Sunflower | Sunflower | 63 Sunflower | 1 | item.createTile + zh-Hans.ItemName |
| 28 |  | Pots | Pots |  | 0 | TileID constant (no placing item) |
| 29 | 猪猪存钱罐 | Piggy Bank | PiggyBank | 87 PiggyBank | 1 | item.createTile + zh-Hans.ItemName |
| 30 | 木材 | Wood | WoodBlock | 9 Wood | 1 | item.createTile + zh-Hans.ItemName |
| 31 |  | Shadow Orbs | ShadowOrbs |  | 0 | TileID constant (no placing item) |
| 32 |  | Corrupt Thorns | CorruptThorns |  | 0 | TileID constant (no placing item) |
| 33 | 蜡烛 | Candle | Candles | 105 Candle | 57 | item.createTile + zh-Hans.ItemName |
| 34 | 铜吊灯 | Copper Chandelier | Chandeliers | 106 CopperChandelier | 54 | item.createTile + zh-Hans.ItemName |
| 35 | 杰克南瓜灯 | Jack O' Lantern | Jackolanterns | 1813 JackOLantern | 1 | item.createTile + zh-Hans.ItemName |
| 36 | 礼物 | Present | Presents | 1869 Present | 1 | item.createTile + zh-Hans.ItemName |
| 37 | 陨石 | Meteorite | Meteorite | 116 Meteorite | 1 | item.createTile + zh-Hans.ItemName |
| 38 | 灰砖 | Gray Brick | GrayBrick | 129 GrayBrick | 1 | item.createTile + zh-Hans.ItemName |
| 39 | 红砖 | Red Brick | RedBrick | 131 RedBrick | 1 | item.createTile + zh-Hans.ItemName |
| 40 | 粘土块 | Clay Block | ClayBlock | 133 ClayBlock | 1 | item.createTile + zh-Hans.ItemName |
| 41 | 蓝砖 | Blue Brick | BlueDungeonBrick | 134 BlueBrick | 1 | item.createTile + zh-Hans.ItemName |
| 42 | 挂链灯笼 | Chain Lantern | HangingLanterns | 136 ChainLantern | 67 | item.createTile + zh-Hans.ItemName |
| 43 | 绿砖 | Green Brick | GreenDungeonBrick | 137 GreenBrick | 1 | item.createTile + zh-Hans.ItemName |
| 44 | 粉砖 | Pink Brick | PinkDungeonBrick | 139 PinkBrick | 1 | item.createTile + zh-Hans.ItemName |
| 45 | 金砖 | Gold Brick | GoldBrick | 141 GoldBrick | 1 | item.createTile + zh-Hans.ItemName |
| 46 | 银砖 | Silver Brick | SilverBrick | 143 SilverBrick | 1 | item.createTile + zh-Hans.ItemName |
| 47 | 铜砖 | Copper Brick | CopperBrick | 145 CopperBrick | 1 | item.createTile + zh-Hans.ItemName |
| 48 | 尖刺 | Spike | Spikes | 147 Spike | 1 | item.createTile + zh-Hans.ItemName |
| 49 | 水蜡烛 | Water Candle | WaterCandle | 148 WaterCandle | 1 | item.createTile + zh-Hans.ItemName |
| 50 | 书 | Book | Books | 149 Book | 1 | item.createTile + zh-Hans.ItemName |
| 51 | 蛛网 | Cobweb | Cobweb | 150 Cobweb | 1 | item.createTile + zh-Hans.ItemName |
| 52 |  | Vines | Vines |  | 0 | TileID constant (no placing item) |
| 53 | 沙块 | Sand Block | Sand | 169 SandBlock | 1 | item.createTile + zh-Hans.ItemName |
| 54 | 玻璃 | Glass | Glass | 170 Glass | 1 | item.createTile + zh-Hans.ItemName |
| 55 | 标牌 | Sign | Signs | 171 Sign | 1 | item.createTile + zh-Hans.ItemName |
| 56 | 黑曜石 | Obsidian | Obsidian | 173 Obsidian | 1 | item.createTile + zh-Hans.ItemName |
| 57 | 灰烬块 | Ash Block | Ash | 172 AshBlock | 1 | item.createTile + zh-Hans.ItemName |
| 58 | 狱石 | Hellstone | Hellstone | 174 Hellstone | 1 | item.createTile + zh-Hans.ItemName |
| 59 | 泥块 | Mud Block | Mud | 176 MudBlock | 1 | item.createTile + zh-Hans.ItemName |
| 60 | 丛林草种子 | Jungle Grass Seeds | JungleGrass | 195 JungleGrassSeeds | 1 | item.createTile + zh-Hans.ItemName |
| 61 |  | Jungle Plants | JunglePlants |  | 0 | TileID constant (no placing item) |
| 62 |  | Jungle Vines | JungleVines |  | 0 | TileID constant (no placing item) |
| 63 | 蓝玉石块 | Sapphire Stone Block | Sapphire | 4642 SapphireStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 64 | 红玉石块 | Ruby Stone Block | Ruby | 4644 RubyStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 65 | 翡翠石块 | Emerald Stone Block | Emerald | 4643 EmeraldStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 66 | 黄玉石块 | Topaz Stone Block | Topaz | 4641 TopazStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 67 | 紫晶石块 | Amethyst Stone Block | Amethyst | 4640 AmethystStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 68 | 钻石石块 | Diamond Stone Block | Diamond | 4645 DiamondStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 69 |  | Jungle Thorns | JungleThorns |  | 0 | TileID constant (no placing item) |
| 70 | 蘑菇草种子 | Mushroom Grass Seeds | MushroomGrass | 194 MushroomGrassSeeds | 1 | item.createTile + zh-Hans.ItemName |
| 71 |  | Mushroom Plants | MushroomPlants |  | 0 | TileID constant (no placing item) |
| 72 |  | Mushroom Trees | MushroomTrees |  | 0 | TileID constant (no placing item) |
| 73 |  | Plants2 | Plants2 |  | 0 | TileID constant (no placing item) |
| 74 |  | Jungle Plants2 | JunglePlants2 |  | 0 | TileID constant (no placing item) |
| 75 | 黑曜石砖 | Obsidian Brick | ObsidianBrick | 192 ObsidianBrick | 1 | item.createTile + zh-Hans.ItemName |
| 76 | 狱石砖 | Hellstone Brick | HellstoneBrick | 214 HellstoneBrick | 1 | item.createTile + zh-Hans.ItemName |
| 77 | 地狱熔炉 | Hellforge | Hellforge | 221 Hellforge | 1 | item.createTile + zh-Hans.ItemName |
| 78 | 粘土盆 | Clay Pot | ClayPot | 222 ClayPot | 1 | item.createTile + zh-Hans.ItemName |
| 79 | 床 | Bed | Beds | 224 Bed | 55 | item.createTile + zh-Hans.ItemName |
| 80 |  | Cactus | Cactus |  | 0 | TileID constant (no placing item) |
| 81 | 珊瑚 | Coral | Coral | 275 Coral | 1 | item.createTile + zh-Hans.ItemName |
| 82 | 太阳花种子 | Daybloom Seeds | ImmatureHerbs | 307 DaybloomSeeds | 7 | item.createTile + zh-Hans.ItemName |
| 83 |  | Mature Herbs | MatureHerbs |  | 0 | TileID constant (no placing item) |
| 84 |  | Blooming Herbs | BloomingHerbs |  | 0 | TileID constant (no placing item) |
| 85 | 墓石 | Tombstone | Tombstones | 321 Tombstone | 11 | item.createTile + zh-Hans.ItemName |
| 86 | 织布机 | Loom | Loom | 332 Loom | 1 | item.createTile + zh-Hans.ItemName |
| 87 | 钢琴 | Piano | Pianos | 333 Piano | 53 | item.createTile + zh-Hans.ItemName |
| 88 | 梳妆台 | Dresser | Dressers | 334 Dresser | 54 | item.createTile + zh-Hans.ItemName |
| 89 | 长椅 | Bench | Benches | 335 Bench | 47 | item.createTile + zh-Hans.ItemName |
| 90 | 浴缸 | Bathtub | Bathtubs | 336 Bathtub | 49 | item.createTile + zh-Hans.ItemName |
| 91 | 红旗 | Red Banner | Banners | 337 RedBanner | 250 | item.createTile + zh-Hans.ItemName |
| 92 | 灯柱 | Lamp Post | Lampposts | 341 LampPost | 1 | item.createTile + zh-Hans.ItemName |
| 93 | 提基火把 | Tiki Torch | Lamps | 342 TikiTorch | 49 | item.createTile + zh-Hans.ItemName |
| 94 | 酒桶 | Keg | Kegs | 352 Keg | 1 | item.createTile + zh-Hans.ItemName |
| 95 | 中式灯笼 | Chinese Lantern | ChineseLanterns | 344 ChineseLantern | 1 | item.createTile + zh-Hans.ItemName |
| 96 | 烹饪锅 | Cooking Pot | CookingPots | 345 CookingPot | 2 | item.createTile + zh-Hans.ItemName |
| 97 | 保险箱 | Safe | Safes | 346 Safe | 1 | item.createTile + zh-Hans.ItemName |
| 98 | 骷髅头灯笼 | Skull Lantern | SkullLanterns | 347 SkullLantern | 1 | item.createTile + zh-Hans.ItemName |
| 99 |  | Trash Can | TrashCan |  | 0 | TileID constant (no placing item) |
| 100 | 烛台 | Candelabra | Candelabras | 349 Candelabra | 44 | item.createTile + zh-Hans.ItemName |
| 101 | 书架 | Bookcase | Bookcases | 354 Bookcase | 61 | item.createTile + zh-Hans.ItemName |
| 102 | 王座 | Throne | Thrones | 355 Throne | 1 | item.createTile + zh-Hans.ItemName |
| 103 | 碗 | Bowl | Bowls | 356 Bowl | 4 | item.createTile + zh-Hans.ItemName |
| 104 | 落地大摆钟 | Grandfather Clock | GrandfatherClocks | 359 GrandfatherClock | 66 | item.createTile + zh-Hans.ItemName |
| 105 | 天使雕像 | Angel Statue | Statues | 52 AngelStatue | 70 | item.createTile + zh-Hans.ItemName |
| 106 | 锯木机 | Sawmill | Sawmill | 363 Sawmill | 1 | item.createTile + zh-Hans.ItemName |
| 107 | 钴矿 | Cobalt Ore | Cobalt | 364 CobaltOre | 1 | item.createTile + zh-Hans.ItemName |
| 108 | 秘银矿 | Mythril Ore | Mythril | 365 MythrilOre | 1 | item.createTile + zh-Hans.ItemName |
| 109 | 神圣种子 | Hallowed Seeds | HallowedGrass | 369 HallowedSeeds | 1 | item.createTile + zh-Hans.ItemName |
| 110 |  | Hallowed Plants | HallowedPlants |  | 0 | TileID constant (no placing item) |
| 111 | 精金矿 | Adamantite Ore | Adamantite | 366 AdamantiteOre | 1 | item.createTile + zh-Hans.ItemName |
| 112 | 黑檀沙块 | Ebonsand Block | Ebonsand | 370 EbonsandBlock | 1 | item.createTile + zh-Hans.ItemName |
| 113 |  | Hallowed Plants2 | HallowedPlants2 |  | 0 | TileID constant (no placing item) |
| 114 | 工匠作坊 | Tinkerer's Workshop | TinkerersWorkbench | 398 TinkerersWorkshop | 1 | item.createTile + zh-Hans.ItemName |
| 115 |  | Hallowed Vines | HallowedVines |  | 0 | TileID constant (no placing item) |
| 116 | 珍珠沙块 | Pearlsand Block | Pearlsand | 408 PearlsandBlock | 1 | item.createTile + zh-Hans.ItemName |
| 117 | 珍珠石块 | Pearlstone Block | Pearlstone | 409 PearlstoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 118 | 珍珠石砖 | Pearlstone Brick | PearlstoneBrick | 412 PearlstoneBrick | 1 | item.createTile + zh-Hans.ItemName |
| 119 | 荧光砖 | Iridescent Brick | IridescentBrick | 413 IridescentBrick | 1 | item.createTile + zh-Hans.ItemName |
| 120 | 泥石砖 | Mudstone Brick | Mudstone | 414 MudstoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 121 | 钴砖 | Cobalt Brick | CobaltBrick | 415 CobaltBrick | 1 | item.createTile + zh-Hans.ItemName |
| 122 | 秘银砖 | Mythril Brick | MythrilBrick | 416 MythrilBrick | 1 | item.createTile + zh-Hans.ItemName |
| 123 | 泥沙块 | Silt Block | Silt | 424 SiltBlock | 1 | item.createTile + zh-Hans.ItemName |
| 124 | 木梁 | Wooden Beam | WoodenBeam | 480 WoodenBeam | 1 | item.createTile + zh-Hans.ItemName |
| 125 | 水晶球 | Crystal Ball | CrystalBall | 487 CrystalBall | 1 | item.createTile + zh-Hans.ItemName |
| 126 | 迪斯科球 | Disco Ball | DiscoBall | 488 DiscoBall | 1 | item.createTile + zh-Hans.ItemName |
| 127 |  | Magical Ice Block | MagicalIceBlock |  | 0 | TileID constant (no placing item) |
| 128 | 人体模型 | Mannequin | Mannequin | 498 Mannequin | 1 | item.createTile + zh-Hans.ItemName |
| 129 | 水晶碎块 | Crystal Shard | Crystals | 502 CrystalShard | 1 | item.createTile + zh-Hans.ItemName |
| 130 | 通电石块 | Active Stone Block | ActiveStoneBlock | 511 ActiveStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 131 | 未通电石块 | Inactive Stone Block | InactiveStoneBlock | 512 InactiveStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 132 | 控制杆 | Lever | Lever | 513 Lever | 1 | item.createTile + zh-Hans.ItemName |
| 133 | 精金熔炉 | Adamantite Forge | AdamantiteForge | 524 AdamantiteForge | 2 | item.createTile + zh-Hans.ItemName |
| 134 | 秘银砧 | Mythril Anvil | MythrilAnvil | 525 MythrilAnvil | 2 | item.createTile + zh-Hans.ItemName |
| 135 | 红压力板 | Red Pressure Plate | PressurePlates | 529 RedPressurePlate | 8 | item.createTile + zh-Hans.ItemName |
| 136 | 开关 | Switch | Switches | 538 Switch | 1 | item.createTile + zh-Hans.ItemName |
| 137 | 飞镖机关 | Dart Trap | Traps | 539 DartTrap | 6 | item.createTile + zh-Hans.ItemName |
| 138 | 巨石 | Boulder | Boulder | 540 Boulder | 1 | item.createTile + zh-Hans.ItemName |
| 139 |  | Music Boxes | MusicBoxes |  | 0 | TileID constant (no placing item) |
| 140 | 魔矿砖 | Demonite Brick | DemoniteBrick | 577 DemoniteBrick | 1 | item.createTile + zh-Hans.ItemName |
| 141 | 炸药 | Explosives | Explosives | 580 Explosives | 1 | item.createTile + zh-Hans.ItemName |
| 142 | 入水泵 | Inlet Pump | InletPump | 581 InletPump | 1 | item.createTile + zh-Hans.ItemName |
| 143 | 出水泵 | Outlet Pump | OutletPump | 582 OutletPump | 1 | item.createTile + zh-Hans.ItemName |
| 144 | 1秒计时器 | 1 Second Timer | Timers | 583 Timer1Second | 5 | item.createTile + zh-Hans.ItemName |
| 145 | 糖棒块 | Candy Cane Block | CandyCaneBlock | 586 CandyCaneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 146 | 绿糖棒块 | Green Candy Cane Block | GreenCandyCaneBlock | 591 GreenCandyCaneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 147 | 雪块 | Snow Block | SnowBlock | 593 SnowBlock | 1 | item.createTile + zh-Hans.ItemName |
| 148 | 雪砖 | Snow Brick | SnowBrick | 594 SnowBrick | 1 | item.createTile + zh-Hans.ItemName |
| 149 | 蓝灯 | Blue Light | HolidayLights | 596 BlueLight | 3 | item.createTile + zh-Hans.ItemName |
| 150 | 精金梁 | Adamantite Beam | AdamantiteBeam | 604 AdamantiteBeam | 1 | item.createTile + zh-Hans.ItemName |
| 151 | 沙岩砖 | Sandstone Brick | SandstoneBrick | 607 SandstoneBrick | 1 | item.createTile + zh-Hans.ItemName |
| 152 | 黑檀石砖 | Ebonstone Brick | EbonstoneBrick | 609 EbonstoneBrick | 1 | item.createTile + zh-Hans.ItemName |
| 153 | 红泥灰 | Red Stucco | RedStucco | 611 RedStucco | 1 | item.createTile + zh-Hans.ItemName |
| 154 | 黄泥灰 | Yellow Stucco | YellowStucco | 612 YellowStucco | 1 | item.createTile + zh-Hans.ItemName |
| 155 | 绿泥灰 | Green Stucco | GreenStucco | 613 GreenStucco | 1 | item.createTile + zh-Hans.ItemName |
| 156 | 灰泥灰 | Gray Stucco | GrayStucco | 614 GrayStucco | 1 | item.createTile + zh-Hans.ItemName |
| 157 | 乌木 | Ebonwood | Ebonwood | 619 Ebonwood | 1 | item.createTile + zh-Hans.ItemName |
| 158 | 红木 | Rich Mahogany | RichMahogany | 620 RichMahogany | 1 | item.createTile + zh-Hans.ItemName |
| 159 | 珍珠木 | Pearlwood | Pearlwood | 621 Pearlwood | 1 | item.createTile + zh-Hans.ItemName |
| 160 | 彩虹砖 | Rainbow Brick | RainbowBrick | 662 RainbowBrick | 1 | item.createTile + zh-Hans.ItemName |
| 161 | 冰雪块 | Ice Block | IceBlock | 664 IceBlock | 1 | item.createTile + zh-Hans.ItemName |
| 162 | 薄冰 | Thin Ice | BreakableIce | 4391 ThinIce | 1 | item.createTile + zh-Hans.ItemName |
| 163 | 紫冰雪块 | Purple Ice Block | CorruptIce | 833 PurpleIceBlock | 1 | item.createTile + zh-Hans.ItemName |
| 164 | 粉冰雪块 | Pink Ice Block | HallowedIce | 834 PinkIceBlock | 1 | item.createTile + zh-Hans.ItemName |
| 165 |  | Stalactite | Stalactite |  | 0 | TileID constant (no placing item) |
| 166 | 锡矿 | Tin Ore | Tin | 699 TinOre | 1 | item.createTile + zh-Hans.ItemName |
| 167 | 铅矿 | Lead Ore | Lead | 700 LeadOre | 1 | item.createTile + zh-Hans.ItemName |
| 168 | 钨矿 | Tungsten Ore | Tungsten | 701 TungstenOre | 1 | item.createTile + zh-Hans.ItemName |
| 169 | 铂金矿 | Platinum Ore | Platinum | 702 PlatinumOre | 1 | item.createTile + zh-Hans.ItemName |
| 170 | 松树块 | Pine Tree Block | PineTree | 1872 PineTreeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 171 | 圣诞树 | Christmas Tree | ChristmasTree | 1873 ChristmasTree | 1 | item.createTile + zh-Hans.ItemName |
| 172 | 木水槽 | Wooden Sink | Sinks | 2827 WoodenSink | 66 | item.createTile + zh-Hans.ItemName |
| 173 | 铂金烛台 | Platinum Candelabra | PlatinumCandelabra | 714 PlatinumCandelabra | 1 | item.createTile + zh-Hans.ItemName |
| 174 | 铂金蜡烛 | Platinum Candle | PlatinumCandle | 713 PlatinumCandle | 1 | item.createTile + zh-Hans.ItemName |
| 175 | 锡砖 | Tin Brick | TinBrick | 717 TinBrick | 1 | item.createTile + zh-Hans.ItemName |
| 176 | 钨砖 | Tungsten Brick | TungstenBrick | 718 TungstenBrick | 1 | item.createTile + zh-Hans.ItemName |
| 177 | 铂金砖 | Platinum Brick | PlatinumBrick | 719 PlatinumBrick | 1 | item.createTile + zh-Hans.ItemName |
| 178 | 蓝玉 | Sapphire | ExposedGems | 177 Sapphire | 7 | item.createTile + zh-Hans.ItemName |
| 179 |  | Green Moss | GreenMoss |  | 0 | TileID constant (no placing item) |
| 180 |  | Brown Moss | BrownMoss |  | 0 | TileID constant (no placing item) |
| 181 |  | Red Moss | RedMoss |  | 0 | TileID constant (no placing item) |
| 182 |  | Blue Moss | BlueMoss |  | 0 | TileID constant (no placing item) |
| 183 |  | Purple Moss | PurpleMoss |  | 0 | TileID constant (no placing item) |
| 184 |  | Long Moss | LongMoss |  | 0 | TileID constant (no placing item) |
| 185 |  | Small Piles | SmallPiles |  | 0 | TileID constant (no placing item) |
| 186 |  | Large Piles | LargePiles |  | 0 | TileID constant (no placing item) |
| 187 |  | Large Piles2 | LargePiles2 |  | 0 | TileID constant (no placing item) |
| 188 | 仙人掌 | Cactus | CactusBlock | 276 Cactus | 1 | item.createTile + zh-Hans.ItemName |
| 189 | 云 | Cloud | Cloud | 751 Cloud | 1 | item.createTile + zh-Hans.ItemName |
| 190 | 发光蘑菇 | Glowing Mushroom | MushroomBlock | 183 GlowingMushroom | 1 | item.createTile + zh-Hans.ItemName |
| 191 | 生命木魔棒 | Living Wood Wand | LivingWood | 832 LivingWoodWand | 1 | item.createTile + zh-Hans.ItemName |
| 192 | 树叶魔棒 | Leaf Wand | LeafBlock | 933 LeafWand | 1 | item.createTile + zh-Hans.ItemName |
| 193 | 史莱姆块 | Slime Block | SlimeBlock | 762 SlimeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 194 | 骨块 | Bone Block | BoneBlock | 766 BoneBlock | 2 | item.createTile + zh-Hans.ItemName |
| 195 | 血肉块 | Flesh Block | FleshBlock | 763 FleshBlock | 1 | item.createTile + zh-Hans.ItemName |
| 196 | 雨云 | Rain Cloud | RainCloud | 765 RainCloud | 1 | item.createTile + zh-Hans.ItemName |
| 197 | 冰冻史莱姆块 | Frozen Slime Block | FrozenSlimeBlock | 767 FrozenSlimeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 198 | 沥青块 | Asphalt Block | Asphalt | 775 AsphaltBlock | 1 | item.createTile + zh-Hans.ItemName |
| 199 | 猩红种子 | Crimson Seeds | CrimsonGrass | 2171 CrimsonSeeds | 1 | item.createTile + zh-Hans.ItemName |
| 200 | 红冰雪块 | Red Ice Block | FleshIce | 835 RedIceBlock | 1 | item.createTile + zh-Hans.ItemName |
| 201 |  | Crimson Plants | CrimsonPlants |  | 0 | TileID constant (no placing item) |
| 202 | 日盘块 | Sunplate Block | Sunplate | 824 SunplateBlock | 1 | item.createTile + zh-Hans.ItemName |
| 203 | 猩红石块 | Crimstone Block | Crimstone | 836 CrimstoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 204 | 猩红矿 | Crimtane Ore | Crimtane | 880 CrimtaneOre | 1 | item.createTile + zh-Hans.ItemName |
| 205 |  | Crimson Vines | CrimsonVines |  | 0 | TileID constant (no placing item) |
| 206 | 冰雪砖 | Ice Brick | IceBrick | 883 IceBrick | 1 | item.createTile + zh-Hans.ItemName |
| 207 | 纯净喷泉 | Pure Water Fountain | WaterFountain | 909 PureWaterFountain | 10 | item.createTile + zh-Hans.ItemName |
| 208 | 暗影木 | Shadewood | Shadewood | 911 Shadewood | 1 | item.createTile + zh-Hans.ItemName |
| 209 | 大炮 | Cannon | Cannon | 928 Cannon | 4 | item.createTile + zh-Hans.ItemName |
| 210 | 地雷 | Land Mine | LandMine | 937 LandMine | 1 | item.createTile + zh-Hans.ItemName |
| 211 | 叶绿矿 | Chlorophyte Ore | Chlorophyte | 947 ChlorophyteOre | 1 | item.createTile + zh-Hans.ItemName |
| 212 | 雪球发射器 | Snowball Launcher | SnowballLauncher | 951 SnowballLauncher | 1 | item.createTile + zh-Hans.ItemName |
| 213 | 绳 | Rope | Rope | 965 Rope | 1 | item.createTile + zh-Hans.ItemName |
| 214 | 链条 | Chain | Chain | 85 Chain | 1 | item.createTile + zh-Hans.ItemName |
| 215 | 篝火 | Campfire | Campfire | 966 Campfire | 16 | item.createTile + zh-Hans.ItemName |
| 216 | 红火箭 | Red Rocket | Firework | 970 RedRocket | 4 | item.createTile + zh-Hans.ItemName |
| 217 | 搅拌机 | Blend-O-Matic | Blendomatic | 995 BlendOMatic | 1 | item.createTile + zh-Hans.ItemName |
| 218 | 绞肉机 | Meat Grinder | MeatGrinder | 996 MeatGrinder | 1 | item.createTile + zh-Hans.ItemName |
| 219 | 提炼机 | Extractinator | Extractinator | 997 Extractinator | 1 | item.createTile + zh-Hans.ItemName |
| 220 | 固化机 | Solidifier | Solidifier | 998 Solidifier | 1 | item.createTile + zh-Hans.ItemName |
| 221 | 钯金矿 | Palladium Ore | Palladium | 1104 PalladiumOre | 1 | item.createTile + zh-Hans.ItemName |
| 222 | 山铜矿 | Orichalcum Ore | Orichalcum | 1105 OrichalcumOre | 1 | item.createTile + zh-Hans.ItemName |
| 223 | 钛金矿 | Titanium Ore | Titanium | 1106 TitaniumOre | 1 | item.createTile + zh-Hans.ItemName |
| 224 | 雪泥块 | Slush Block | Slush | 1103 SlushBlock | 1 | item.createTile + zh-Hans.ItemName |
| 225 | 蜂巢魔棒 | Hive Wand | Hive | 1129 HiveWand | 1 | item.createTile + zh-Hans.ItemName |
| 226 | 丛林蜥蜴砖 | Lihzahrd Brick | LihzahrdBrick | 1101 LihzahrdBrick | 1 | item.createTile + zh-Hans.ItemName |
| 227 | 青绿蘑菇 | Teal Mushroom | DyePlants | 1107 TealMushroom | 11 | item.createTile + zh-Hans.ItemName |
| 228 | 染缸 | Dye Vat | DyeVat | 1120 DyeVat | 1 | item.createTile + zh-Hans.ItemName |
| 229 | 蜂蜜块 | Honey Block | HoneyBlock | 1125 HoneyBlock | 1 | item.createTile + zh-Hans.ItemName |
| 230 | 松脆蜂蜜块 | Crispy Honey Block | CrispyHoneyBlock | 1127 CrispyHoneyBlock | 1 | item.createTile + zh-Hans.ItemName |
| 231 |  | Larva | Larva |  | 0 | TileID constant (no placing item) |
| 232 | 木尖刺 | Wooden Spike | WoodenSpikes | 1150 WoodenSpike | 1 | item.createTile + zh-Hans.ItemName |
| 233 |  | Plant Detritus | PlantDetritus |  | 0 | TileID constant (no placing item) |
| 234 | 猩红沙块 | Crimsand Block | Crimsand | 1246 CrimsandBlock | 1 | item.createTile + zh-Hans.ItemName |
| 235 | 传送机 | Teleporter | Teleporter | 1263 Teleporter | 1 | item.createTile + zh-Hans.ItemName |
| 236 |  | Life Fruit | LifeFruit |  | 0 | TileID constant (no placing item) |
| 237 | 丛林蜥蜴祭坛 | Lihzahrd Altar | LihzahrdAltar | 1292 LihzahrdAltar | 1 | item.createTile + zh-Hans.ItemName |
| 238 |  | Plantera Bulb | PlanteraBulb |  | 0 | TileID constant (no placing item) |
| 239 | 金锭 | Gold Bar | MetalBars | 19 GoldBar | 23 | item.createTile + zh-Hans.ItemName |
| 240 | 克苏鲁之眼纪念章 | Eye of Cthulhu Trophy | Painting3X3 | 1360 EyeofCthulhuTrophy | 88 | item.createTile + zh-Hans.ItemName |
| 241 | 地下墓穴 | Catacomb | Painting4X3 | 1417 Catacomb | 1 | item.createTile + zh-Hans.ItemName |
| 242 | 最后一眼 | The Eye Sees the End | Painting6X4 | 1421 TheEyeSeestheEnd | 72 | item.createTile + zh-Hans.ItemName |
| 243 | 灌注站 | Imbuing Station | ImbuingStation | 1430 ImbuingStation | 1 | item.createTile + zh-Hans.ItemName |
| 244 | 泡泡机 | Bubble Machine | BubbleMachine | 1449 BubbleMachine | 1 | item.createTile + zh-Hans.ItemName |
| 245 | 沃尔多 | Waldo | Painting2X3 | 1474 Waldo | 29 | item.createTile + zh-Hans.ItemName |
| 246 | 冬青树 | Holly | Painting3X2 | 1908 Holly | 19 | item.createTile + zh-Hans.ItemName |
| 247 | 自动锤炼机 | Autohammer | Autohammer | 1551 Autohammer | 1 | item.createTile + zh-Hans.ItemName |
| 248 | 钯金柱 | Palladium Column | PalladiumColumn | 1589 PalladiumColumn | 1 | item.createTile + zh-Hans.ItemName |
| 249 | 泡泡糖块 | Bubblegum Block | BubblegumBlock | 1591 BubblegumBlock | 1 | item.createTile + zh-Hans.ItemName |
| 250 | 钛石块 | Titanstone Block | Titanstone | 1593 TitanstoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 251 | 南瓜 | Pumpkin | PumpkinBlock | 1725 Pumpkin | 1 | item.createTile + zh-Hans.ItemName |
| 252 | 干草 | Hay | HayBlock | 1727 Hay | 1 | item.createTile + zh-Hans.ItemName |
| 253 | 阴森木 | Spooky Wood | SpookyWood | 1729 SpookyWood | 1 | item.createTile + zh-Hans.ItemName |
| 254 | 南瓜子 | Pumpkin Seed | Pumpkins | 1828 PumpkinSeed | 1 | item.createTile + zh-Hans.ItemName |
| 255 |  | Amethyst Gemspark Off | AmethystGemsparkOff |  | 0 | TileID constant (no placing item) |
| 256 |  | Topaz Gemspark Off | TopazGemsparkOff |  | 0 | TileID constant (no placing item) |
| 257 |  | Sapphire Gemspark Off | SapphireGemsparkOff |  | 0 | TileID constant (no placing item) |
| 258 |  | Emerald Gemspark Off | EmeraldGemsparkOff |  | 0 | TileID constant (no placing item) |
| 259 |  | Ruby Gemspark Off | RubyGemsparkOff |  | 0 | TileID constant (no placing item) |
| 260 |  | Diamond Gemspark Off | DiamondGemsparkOff |  | 0 | TileID constant (no placing item) |
| 261 |  | Amber Gemspark Off | AmberGemsparkOff |  | 0 | TileID constant (no placing item) |
| 262 |  | Amethyst Gemspark | AmethystGemspark |  | 0 | TileID constant (no placing item) |
| 263 |  | Topaz Gemspark | TopazGemspark |  | 0 | TileID constant (no placing item) |
| 264 |  | Sapphire Gemspark | SapphireGemspark |  | 0 | TileID constant (no placing item) |
| 265 |  | Emerald Gemspark | EmeraldGemspark |  | 0 | TileID constant (no placing item) |
| 266 |  | Ruby Gemspark | RubyGemspark |  | 0 | TileID constant (no placing item) |
| 267 |  | Diamond Gemspark | DiamondGemspark |  | 0 | TileID constant (no placing item) |
| 268 |  | Amber Gemspark | AmberGemspark |  | 0 | TileID constant (no placing item) |
| 269 | 女性人体模型 | Womannequin | Womannequin | 1989 Womannquin | 2 | item.createTile + zh-Hans.ItemName |
| 270 | 萤火虫瓶 | Firefly in a Bottle | FireflyinaBottle | 1993 FireflyinaBottle | 1 | item.createTile + zh-Hans.ItemName |
| 271 | 荧光虫瓶 | Lightning Bug in a Bottle | LightningBuginaBottle | 2005 LightningBuginaBottle | 1 | item.createTile + zh-Hans.ItemName |
| 272 | 齿轮 | Cog | Cog | 1344 Cog | 1 | item.createTile + zh-Hans.ItemName |
| 273 | 石板 | Stone Slab | StoneSlab | 2119 StoneSlab | 1 | item.createTile + zh-Hans.ItemName |
| 274 | 沙岩板 | Sandstone Slab | SandStoneSlab | 2120 SandstoneSlab | 1 | item.createTile + zh-Hans.ItemName |
| 275 |  | Bunny Cage | BunnyCage |  | 0 | TileID constant (no placing item) |
| 276 |  | Squirrel Cage | SquirrelCage |  | 0 | TileID constant (no placing item) |
| 277 |  | Mallard Duck Cage | MallardDuckCage |  | 0 | TileID constant (no placing item) |
| 278 |  | Duck Cage | DuckCage |  | 0 | TileID constant (no placing item) |
| 279 |  | Bird Cage | BirdCage |  | 0 | TileID constant (no placing item) |
| 280 |  | Blue Jay | BlueJay |  | 0 | TileID constant (no placing item) |
| 281 |  | Cardinal Cage | CardinalCage |  | 0 | TileID constant (no placing item) |
| 282 | 鱼缸 | Fish Bowl | FishBowl | 250 FishBowl | 1 | item.createTile + zh-Hans.ItemName |
| 283 | 重型组装机 | Heavy Assembler | HeavyWorkBench | 2172 HeavyWorkBench | 1 | item.createTile + zh-Hans.ItemName |
| 284 | 铜护板 | Copper Plating | CopperPlating | 2173 CopperPlating | 1 | item.createTile + zh-Hans.ItemName |
| 285 |  | Snail Cage | SnailCage |  | 0 | TileID constant (no placing item) |
| 286 |  | Glowing Snail Cage | GlowingSnailCage |  | 0 | TileID constant (no placing item) |
| 287 | 弹药箱 | Ammo Box | AmmoBox | 2177 AmmoBox | 1 | item.createTile + zh-Hans.ItemName |
| 288 |  | Monarch Butterfly Jar | MonarchButterflyJar |  | 0 | TileID constant (no placing item) |
| 289 |  | Purple Emperor Butterfly Jar | PurpleEmperorButterflyJar |  | 0 | TileID constant (no placing item) |
| 290 |  | Red Admiral Butterfly Jar | RedAdmiralButterflyJar |  | 0 | TileID constant (no placing item) |
| 291 |  | Ulysses Butterfly Jar | UlyssesButterflyJar |  | 0 | TileID constant (no placing item) |
| 292 |  | Sulphur Butterfly Jar | SulphurButterflyJar |  | 0 | TileID constant (no placing item) |
| 293 |  | Tree Nymph Butterfly Jar | TreeNymphButterflyJar |  | 0 | TileID constant (no placing item) |
| 294 |  | Zebra Swallowtail Butterfly Jar | ZebraSwallowtailButterflyJar |  | 0 | TileID constant (no placing item) |
| 295 |  | Julia Butterfly Jar | JuliaButterflyJar |  | 0 | TileID constant (no placing item) |
| 296 |  | Scorpion Cage | ScorpionCage |  | 0 | TileID constant (no placing item) |
| 297 |  | Black Scorpion Cage | BlackScorpionCage |  | 0 | TileID constant (no placing item) |
| 298 |  | Frog Cage | FrogCage |  | 0 | TileID constant (no placing item) |
| 299 |  | Mouse Cage | MouseCage |  | 0 | TileID constant (no placing item) |
| 300 |  | Bone Welder | BoneWelder |  | 0 | TileID constant (no placing item) |
| 301 |  | Flesh Cloning Vat | FleshCloningVat |  | 0 | TileID constant (no placing item) |
| 302 |  | Glass Kiln | GlassKiln |  | 0 | TileID constant (no placing item) |
| 303 |  | Lihzahrd Furnace | LihzahrdFurnace |  | 0 | TileID constant (no placing item) |
| 304 |  | Living Loom | LivingLoom |  | 0 | TileID constant (no placing item) |
| 305 |  | Sky Mill | SkyMill |  | 0 | TileID constant (no placing item) |
| 306 |  | Ice Machine | IceMachine |  | 0 | TileID constant (no placing item) |
| 307 | 蒸汽朋克锅炉 | Steampunk Boiler | SteampunkBoiler | 2203 SteampunkBoiler | 1 | item.createTile + zh-Hans.ItemName |
| 308 | 蜂蜜分配机 | Honey Dispenser | HoneyDispenser | 2204 HoneyDispenser | 1 | item.createTile + zh-Hans.ItemName |
| 309 |  | Penguin Cage | PenguinCage |  | 0 | TileID constant (no placing item) |
| 310 |  | Worm Cage | WormCage |  | 0 | TileID constant (no placing item) |
| 311 |  | Dynasty Wood | DynastyWood |  | 0 | TileID constant (no placing item) |
| 312 |  | Red Dynasty Shingles | RedDynastyShingles |  | 0 | TileID constant (no placing item) |
| 313 |  | Blue Dynasty Shingles | BlueDynastyShingles |  | 0 | TileID constant (no placing item) |
| 314 | 矿车轨道 | Minecart Track | MinecartTrack | 2340 MinecartTrack | 3 | item.createTile + zh-Hans.ItemName |
| 315 | 珊瑚石块 | Coralstone Block | Coralstone | 2435 CoralstoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 316 |  | Blue Jellyfish Bowl | BlueJellyfishBowl |  | 0 | TileID constant (no placing item) |
| 317 |  | Green Jellyfish Bowl | GreenJellyfishBowl |  | 0 | TileID constant (no placing item) |
| 318 |  | Pink Jellyfish Bowl | PinkJellyfishBowl |  | 0 | TileID constant (no placing item) |
| 319 | 船舶瓶 | Ship in a Bottle | ShipInABottle | 2490 ShipInABottle | 1 | item.createTile + zh-Hans.ItemName |
| 320 | 海草花盆 | Seaweed Planter | SeaweedPlanter | 2496 SeaweedPlanter | 1 | item.createTile + zh-Hans.ItemName |
| 321 | 针叶木 | Boreal Wood | BorealWood | 2503 BorealWood | 1 | item.createTile + zh-Hans.ItemName |
| 322 | 棕榈木 | Palm Wood | PalmWood | 2504 PalmWood | 1 | item.createTile + zh-Hans.ItemName |
| 323 |  | Palm Tree | PalmTree |  | 0 | TileID constant (no placing item) |
| 324 |  | Beach Piles | BeachPiles |  | 0 | TileID constant (no placing item) |
| 325 | 锡护板 | Tin Plating | TinPlating | 2692 TinPlating | 1 | item.createTile + zh-Hans.ItemName |
| 326 | 瀑布块 | Waterfall Block | Waterfall | 2693 WaterfallBlock | 1 | item.createTile + zh-Hans.ItemName |
| 327 | 熔岩瀑布块 | Lavafall Block | Lavafall | 2694 LavafallBlock | 1 | item.createTile + zh-Hans.ItemName |
| 328 | 彩纸块 | Confetti Block | Confetti | 2695 ConfettiBlock | 1 | item.createTile + zh-Hans.ItemName |
| 329 | 午夜彩纸块 | Midnight Confetti Block | ConfettiBlack | 2697 ConfettiBlockBlack | 1 | item.createTile + zh-Hans.ItemName |
| 330 | 铜币 | Copper Coin | CopperCoinPile | 71 CopperCoin | 1 | item.createTile + zh-Hans.ItemName |
| 331 | 银币 | Silver Coin | SilverCoinPile | 72 SilverCoin | 1 | item.createTile + zh-Hans.ItemName |
| 332 | 金币 | Gold Coin | GoldCoinPile | 73 GoldCoin | 1 | item.createTile + zh-Hans.ItemName |
| 333 | 铂金币 | Platinum Coin | PlatinumCoinPile | 74 PlatinumCoin | 1 | item.createTile + zh-Hans.ItemName |
| 334 |  | Weapons Rack | WeaponsRack |  | 0 | TileID constant (no placing item) |
| 335 | 烟花盒 | Fireworks Box | FireworksBox | 2700 FireworksBox | 1 | item.createTile + zh-Hans.ItemName |
| 336 | 活火块 | Living Fire Block | LivingFire | 2701 LivingFireBlock | 1 | item.createTile + zh-Hans.ItemName |
| 337 | 0字雕像 | '0' Statue | AlphabetStatues | 2702 AlphabetStatue0 | 36 | item.createTile + zh-Hans.ItemName |
| 338 | 烟花喷泉 | Firework Fountain | FireworkFountain | 2738 FireworkFountain | 1 | item.createTile + zh-Hans.ItemName |
| 339 | 蚱蜢笼 | Grasshopper Cage | GrasshopperCage | 2741 GrasshopperCage | 1 | item.createTile + zh-Hans.ItemName |
| 340 |  | Living Cursed Fire | LivingCursedFire |  | 0 | TileID constant (no placing item) |
| 341 |  | Living Demon Fire | LivingDemonFire |  | 0 | TileID constant (no placing item) |
| 342 |  | Living Frost Fire | LivingFrostFire |  | 0 | TileID constant (no placing item) |
| 343 |  | Living Ichor | LivingIchor |  | 0 | TileID constant (no placing item) |
| 344 |  | Living Ultrabright Fire | LivingUltrabrightFire |  | 0 | TileID constant (no placing item) |
| 345 | 蜂蜜瀑布块 | Honeyfall Block | Honeyfall | 2787 HoneyfallBlock | 1 | item.createTile + zh-Hans.ItemName |
| 346 |  | Chlorophyte Brick | ChlorophyteBrick |  | 0 | TileID constant (no placing item) |
| 347 |  | Crimtane Brick | CrimtaneBrick |  | 0 | TileID constant (no placing item) |
| 348 |  | Shroomite Plating | ShroomitePlating |  | 0 | TileID constant (no placing item) |
| 349 | 蘑菇雕像 | Mushroom Statue | MushroomStatue | 470 MushroomStatue | 1 | item.createTile + zh-Hans.ItemName |
| 350 | 火星管道护板 | Martian Conduit Plating | MartianConduitPlating | 2860 MartianConduitPlating | 1 | item.createTile + zh-Hans.ItemName |
| 351 | 烟雾块 | Smoke Block | ChimneySmoke | 2868 SmokeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 352 |  | Crimson Thorns | CrimsonThorns |  | 0 | TileID constant (no placing item) |
| 353 | 藤蔓绳 | Vine Rope | VineRope | 2996 VineRope | 1 | item.createTile + zh-Hans.ItemName |
| 354 | 施法桌 | Bewitching Table | BewitchingTable | 2999 BewitchingTable | 1 | item.createTile + zh-Hans.ItemName |
| 355 | 炼药桌 | Alchemy Table | AlchemyTable | 3000 AlchemyTable | 1 | item.createTile + zh-Hans.ItemName |
| 356 | 附魔日晷 | Enchanted Sundial | Sundial | 3064 Sundial | 1 | item.createTile + zh-Hans.ItemName |
| 357 | 光面大理石块 | Smooth Marble Block | MarbleBlock | 3066 MarbleBlock | 1 | item.createTile + zh-Hans.ItemName |
| 358 |  | Gold Bird Cage | GoldBirdCage |  | 0 | TileID constant (no placing item) |
| 359 |  | Gold Bunny Cage | GoldBunnyCage |  | 0 | TileID constant (no placing item) |
| 360 |  | Gold Butterfly Cage | GoldButterflyCage |  | 0 | TileID constant (no placing item) |
| 361 |  | Gold Frog Cage | GoldFrogCage |  | 0 | TileID constant (no placing item) |
| 362 |  | Gold Grasshopper Cage | GoldGrasshopperCage |  | 0 | TileID constant (no placing item) |
| 363 |  | Gold Mouse Cage | GoldMouseCage |  | 0 | TileID constant (no placing item) |
| 364 |  | Gold Worm Cage | GoldWormCage |  | 0 | TileID constant (no placing item) |
| 365 | 丝绸绳 | Silk Rope | SilkRope | 3077 SilkRope | 1 | item.createTile + zh-Hans.ItemName |
| 366 | 蛛丝绳 | Web Rope | WebRope | 3078 WebRope | 1 | item.createTile + zh-Hans.ItemName |
| 367 | 大理石块 | Marble Block | Marble | 3081 Marble | 1 | item.createTile + zh-Hans.ItemName |
| 368 | 花岗岩块 | Granite Block | Granite | 3086 Granite | 1 | item.createTile + zh-Hans.ItemName |
| 369 | 光面花岗岩块 | Smooth Granite Block | GraniteBlock | 3087 GraniteBlock | 1 | item.createTile + zh-Hans.ItemName |
| 370 | 陨石砖 | Meteorite Brick | MeteoriteBrick | 3100 MeteoriteBrick | 1 | item.createTile + zh-Hans.ItemName |
| 371 | 粉史莱姆块 | Pink Slime Block | PinkSlimeBlock | 3113 PinkSlimeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 372 | 和平蜡烛 | Peace Candle | PeaceCandle | 3117 PeaceCandle | 1 | item.createTile + zh-Hans.ItemName |
| 373 | 魔法水滴管 | Magic Water Dropper | WaterDrip | 3182 MagicWaterDropper | 1 | item.createTile + zh-Hans.ItemName |
| 374 | 魔法熔岩滴管 | Magic Lava Dropper | LavaDrip | 3184 MagicLavaDropper | 1 | item.createTile + zh-Hans.ItemName |
| 375 | 魔法蜂蜜滴管 | Magic Honey Dropper | HoneyDrip | 3185 MagicHoneyDropper | 1 | item.createTile + zh-Hans.ItemName |
| 376 | 木匣 | Wooden Crate | FishingCrate | 2334 WoodenCrate | 26 | item.createTile + zh-Hans.ItemName |
| 377 | 利器站 | Sharpening Station | SharpeningStation | 3198 SharpeningStation | 1 | item.createTile + zh-Hans.ItemName |
| 378 | 训练假人 | Target Dummy | TargetDummy | 3202 TargetDummy | 1 | item.createTile + zh-Hans.ItemName |
| 379 | 泡泡 | Bubble | Bubble | 3214 Bubble | 1 | item.createTile + zh-Hans.ItemName |
| 380 | 太阳花种植盆 | Daybloom Planter Box | PlanterBox | 3215 DayBloomPlanterBox | 8 | item.createTile + zh-Hans.ItemName |
| 381 | 熔岩苔藓 | Lava Moss | LavaMoss | 4354 LavaMoss | 1 | item.createTile + zh-Hans.ItemName |
| 382 |  | Vine Flowers | VineFlowers |  | 0 | TileID constant (no placing item) |
| 383 | 生命红木魔棒 | Living Mahogany Wand | LivingMahogany | 3360 LivingMahoganyWand | 1 | item.createTile + zh-Hans.ItemName |
| 384 | 红木树叶魔棒 | Rich Mahogany Leaf Wand | LivingMahoganyLeaves | 3361 LivingMahoganyLeafWand | 1 | item.createTile + zh-Hans.ItemName |
| 385 | 水晶块 | Crystal Block | CrystalBlock | 3234 CrystalBlock | 1 | item.createTile + zh-Hans.ItemName |
| 386 |  | Trapdoor Open | TrapdoorOpen |  | 0 | TileID constant (no placing item) |
| 387 | 机关门 | Trap Door | TrapdoorClosed | 3239 Trapdoor | 1 | item.createTile + zh-Hans.ItemName |
| 388 | 高门 | Tall Gate | TallGateClosed | 3240 TallGate | 1 | item.createTile + zh-Hans.ItemName |
| 389 |  | Tall Gate Open | TallGateOpen |  | 0 | TileID constant (no placing item) |
| 390 | 熔岩灯 | Lava Lamp | LavaLamp | 3253 LavaLamp | 1 | item.createTile + zh-Hans.ItemName |
| 391 |  | Cage Enchanted Nightcrawler | CageEnchantedNightcrawler |  | 0 | TileID constant (no placing item) |
| 392 |  | Cage Buggy | CageBuggy |  | 0 | TileID constant (no placing item) |
| 393 |  | Cage Grubby | CageGrubby |  | 0 | TileID constant (no placing item) |
| 394 |  | Cage Sluggy | CageSluggy |  | 0 | TileID constant (no placing item) |
| 395 | 物品框 | Item Frame | ItemFrame | 3270 ItemFrame | 1 | item.createTile + zh-Hans.ItemName |
| 396 | 沙岩块 | Sandstone Block | Sandstone | 3271 Sandstone | 1 | item.createTile + zh-Hans.ItemName |
| 397 | 硬化沙块 | Hardened Sand Block | HardenedSand | 3272 HardenedSand | 1 | item.createTile + zh-Hans.ItemName |
| 398 | 硬化黑檀沙块 | Hardened Ebonsand Block | CorruptHardenedSand | 3274 CorruptHardenedSand | 1 | item.createTile + zh-Hans.ItemName |
| 399 | 硬化猩红沙块 | Hardened Crimsand Block | CrimsonHardenedSand | 3275 CrimsonHardenedSand | 1 | item.createTile + zh-Hans.ItemName |
| 400 | 黑檀沙岩块 | Ebonsandstone Block | CorruptSandstone | 3276 CorruptSandstone | 1 | item.createTile + zh-Hans.ItemName |
| 401 | 猩红沙岩块 | Crimsandstone Block | CrimsonSandstone | 3277 CrimsonSandstone | 1 | item.createTile + zh-Hans.ItemName |
| 402 | 硬化珍珠沙块 | Hardened Pearlsand Block | HallowHardenedSand | 3338 HallowHardenedSand | 1 | item.createTile + zh-Hans.ItemName |
| 403 | 珍珠沙岩块 | Pearlsandstone Block | HallowSandstone | 3339 HallowSandstone | 1 | item.createTile + zh-Hans.ItemName |
| 404 | 沙漠化石 | Desert Fossil | DesertFossil | 3347 DesertFossil | 1 | item.createTile + zh-Hans.ItemName |
| 405 | 壁炉 | Fireplace | Fireplace | 3364 Fireplace | 1 | item.createTile + zh-Hans.ItemName |
| 406 | 烟囱 | Chimney | Chimney | 3365 Chimney | 1 | item.createTile + zh-Hans.ItemName |
| 407 | 坚固化石 | Sturdy Fossil | FossilOre | 3380 FossilOre | 1 | item.createTile + zh-Hans.ItemName |
| 408 | 夜明矿 | Luminite | LunarOre | 3460 LunarOre | 1 | item.createTile + zh-Hans.ItemName |
| 409 | 夜明砖 | Luminite Brick | LunarBrick | 3461 LunarBrick | 1 | item.createTile + zh-Hans.ItemName |
| 410 | 星旋天塔柱 | Vortex Monolith | LunarMonolith | 3536 VortexMonolith | 4 | item.createTile + zh-Hans.ItemName |
| 411 | 引爆器 | Detonator | Detonator | 3545 Detonator | 1 | item.createTile + zh-Hans.ItemName |
| 412 | 远古操纵机 | Ancient Manipulator | LunarCraftingStation | 3549 LunarCraftingStation | 1 | item.createTile + zh-Hans.ItemName |
| 413 | 红松鼠笼 | Red Squirrel Cage | SquirrelOrangeCage | 3565 SquirrelOrangeCage | 1 | item.createTile + zh-Hans.ItemName |
| 414 | 金松鼠笼 | Gold Squirrel Cage | SquirrelGoldCage | 3566 SquirrelGoldCage | 1 | item.createTile + zh-Hans.ItemName |
| 415 |  | Lunar Block Solar | LunarBlockSolar |  | 0 | TileID constant (no placing item) |
| 416 |  | Lunar Block Vortex | LunarBlockVortex |  | 0 | TileID constant (no placing item) |
| 417 |  | Lunar Block Nebula | LunarBlockNebula |  | 0 | TileID constant (no placing item) |
| 418 |  | Lunar Block Stardust | LunarBlockStardust |  | 0 | TileID constant (no placing item) |
| 419 | 逻辑门灯（关） | Logic Gate Lamp (Off) | LogicGateLamp | 3602 LogicGateLamp_Off | 3 | item.createTile + zh-Hans.ItemName |
| 420 | 逻辑门（与） | Logic Gate (AND) | LogicGate | 3603 LogicGate_AND | 6 | item.createTile + zh-Hans.ItemName |
| 421 | 传送带（顺时针） | Conveyor Belt (Clockwise) | ConveyorBeltLeft | 3609 ConveyorBeltLeft | 1 | item.createTile + zh-Hans.ItemName |
| 422 | 传送带（逆时针） | Conveyor Belt (Counter Clockwise) | ConveyorBeltRight | 3610 ConveyorBeltRight | 1 | item.createTile + zh-Hans.ItemName |
| 423 | 逻辑感应器（昼） | Logic Sensor (Day) | LogicSensor | 3613 LogicSensor_Sun | 7 | item.createTile + zh-Hans.ItemName |
| 424 | 分线盒 | Junction Box | WirePipe | 3616 WirePipe | 1 | item.createTile + zh-Hans.ItemName |
| 425 | 广播盒 | Announcement Box | AnnouncementBox | 3617 AnnouncementBox | 1 | item.createTile + zh-Hans.ItemName |
| 426 | 红团队块 | Red Team Block | TeamBlockRed | 3621 TeamBlockRed | 1 | item.createTile + zh-Hans.ItemName |
| 427 | 红团队平台 | Red Team Platform | TeamBlockRedPlatform | 3622 TeamBlockRedPlatform | 1 | item.createTile + zh-Hans.ItemName |
| 428 | 粉色测重压力板 | Pink Weighted Pressure Plate | WeightedPressurePlate | 3626 WeightedPressurePlatePink | 4 | item.createTile + zh-Hans.ItemName |
| 429 | 彩线灯泡 | Wire Bulb | WireBulb | 3629 WireBulb | 1 | item.createTile + zh-Hans.ItemName |
| 430 |  | Team Block Green | TeamBlockGreen |  | 0 | TileID constant (no placing item) |
| 431 |  | Team Block Blue | TeamBlockBlue |  | 0 | TileID constant (no placing item) |
| 432 |  | Team Block Yellow | TeamBlockYellow |  | 0 | TileID constant (no placing item) |
| 433 |  | Team Block Pink | TeamBlockPink |  | 0 | TileID constant (no placing item) |
| 434 |  | Team Block White | TeamBlockWhite |  | 0 | TileID constant (no placing item) |
| 435 |  | Team Block Green Platform | TeamBlockGreenPlatform |  | 0 | TileID constant (no placing item) |
| 436 |  | Team Block Blue Platform | TeamBlockBluePlatform |  | 0 | TileID constant (no placing item) |
| 437 |  | Team Block Yellow Platform | TeamBlockYellowPlatform |  | 0 | TileID constant (no placing item) |
| 438 |  | Team Block Pink Platform | TeamBlockPinkPlatform |  | 0 | TileID constant (no placing item) |
| 439 |  | Team Block White Platform | TeamBlockWhitePlatform |  | 0 | TileID constant (no placing item) |
| 440 | 红玉宝石锁 | Ruby Gem Lock | GemLocks | 3644 GemLockRuby | 7 | item.createTile + zh-Hans.ItemName |
| 441 | 机关宝箱 | Trapped Chest | FakeContainers | 3665 Fake_Chest | 42 | item.createTile + zh-Hans.ItemName |
| 442 | 青绿压力垫板 | Teal Pressure Pad | ProjectilePressurePad | 3707 ProjectilePressurePad | 1 | item.createTile + zh-Hans.ItemName |
| 443 | 热喷泉 | Geyser | GeyserTrap | 3722 GeyserTrap | 1 | item.createTile + zh-Hans.ItemName |
| 444 | 蜂巢 | Bee Hive | BeeHive | 5066 BeeHive | 1 | item.createTile + zh-Hans.ItemName |
| 445 | 像素盒 | Pixel Box | PixelBox | 3725 PixelBox | 1 | item.createTile + zh-Hans.ItemName |
| 446 |  | Silly Balloon Pink | SillyBalloonPink |  | 0 | TileID constant (no placing item) |
| 447 |  | Silly Balloon Purple | SillyBalloonPurple |  | 0 | TileID constant (no placing item) |
| 448 |  | Silly Balloon Green | SillyBalloonGreen |  | 0 | TileID constant (no placing item) |
| 449 |  | Silly Streamer Blue | SillyStreamerBlue |  | 0 | TileID constant (no placing item) |
| 450 |  | Silly Streamer Green | SillyStreamerGreen |  | 0 | TileID constant (no placing item) |
| 451 |  | Silly Streamer Pink | SillyStreamerPink |  | 0 | TileID constant (no placing item) |
| 452 | 呆萌气球机 | Silly Balloon Machine | SillyBalloonMachine | 3742 SillyBalloonMachine | 1 | item.createTile + zh-Hans.ItemName |
| 453 | 呆萌丝带气球（粉） | Silly Tied Balloon (Pink) | SillyBalloonTile | 3743 SillyBalloonTiedPink | 3 | item.createTile + zh-Hans.ItemName |
| 454 | 猪龙彩罐 | Pigronata | Pigronata | 3746 Pigronata | 1 | item.createTile + zh-Hans.ItemName |
| 455 | 派对中心 | Party Center | PartyMonolith | 3747 PartyMonolith | 1 | item.createTile + zh-Hans.ItemName |
| 456 | 呆萌丝带派对气球束 | Silly Tied Bundle of Balloons | PartyBundleOfBalloonTile | 3748 PartyBundleOfBalloonTile | 1 | item.createTile + zh-Hans.ItemName |
| 457 | 派对礼物 | Party Present | PartyPresent | 3749 PartyPresent | 1 | item.createTile + zh-Hans.ItemName |
| 458 | 落沙块 | Sandfall Block | SandFallBlock | 3754 SandFallBlock | 1 | item.createTile + zh-Hans.ItemName |
| 459 | 降雪块 | Snowfall Block | SnowFallBlock | 3755 SnowFallBlock | 1 | item.createTile + zh-Hans.ItemName |
| 460 | 雪云 | Snow Cloud | SnowCloud | 3756 SnowCloudBlock | 1 | item.createTile + zh-Hans.ItemName |
| 461 | 魔法沙粒滴管 | Magic Sand Dropper | SandDrip | 3782 MagicSandDropper | 1 | item.createTile + zh-Hans.ItemName |
| 462 | 沙漠幽魂灯 | Desert Spirit Lamp | DjinnLamp | 3795 DjinnLamp | 1 | item.createTile + zh-Hans.ItemName |
| 463 | 护卫熔炉 | Defender's Forge | DefendersForge | 3813 DefendersForge | 1 | item.createTile + zh-Hans.ItemName |
| 464 | 战争桌 | War Table | WarTable | 3814 WarTable | 1 | item.createTile + zh-Hans.ItemName |
| 465 | 战争桌旗 | War Table Banner | WarTableBanner | 3815 WarTableBanner | 1 | item.createTile + zh-Hans.ItemName |
| 466 | 永恒水晶座 | Eternia Crystal Stand | ElderCrystalStand | 3816 DD2ElderCrystalStand | 1 | item.createTile + zh-Hans.ItemName |
| 467 | 水晶箱 | Crystal Chest | Containers2 | 3884 CrystalChest | 37 | item.createTile + zh-Hans.ItemName |
| 468 | 机关水晶箱 | Trapped Crystal Chest | FakeContainers2 | 3886 Fake_CrystalChest | 36 | item.createTile + zh-Hans.ItemName |
| 469 | 水晶桌 | Crystal Table | Tables2 | 3920 CrystalTable | 32 | item.createTile + zh-Hans.ItemName |
| 470 |  | Display Doll | DisplayDoll |  | 0 | TileID constant (no placing item) |
| 471 | 武器架 | Weapon Rack | WeaponsRack2 | 2699 WeaponRack | 1 | item.createTile + zh-Hans.ItemName |
| 472 | 铁砖 | Iron Brick | IronBrick | 3951 IronBrick | 1 | item.createTile + zh-Hans.ItemName |
| 473 | 铅砖 | Lead Brick | LeadBrick | 3953 LeadBrick | 1 | item.createTile + zh-Hans.ItemName |
| 474 | 病变块 | Lesion Block | LesionBlock | 3955 LesionBlock | 1 | item.createTile + zh-Hans.ItemName |
| 475 |  | Hat Rack | HatRack |  | 0 | TileID constant (no placing item) |
| 476 | 高尔夫球洞 | Golf Cup | GolfHole | 4040 GolfCup | 1 | item.createTile + zh-Hans.ItemName |
| 477 |  | Golf Grass | GolfGrass |  | 0 | TileID constant (no placing item) |
| 478 | 猩红石砖 | Crimstone Brick | CrimstoneBrick | 4050 CrimstoneBrick | 1 | item.createTile + zh-Hans.ItemName |
| 479 | 光面沙岩 | Smooth Sandstone | SmoothSandstone | 4051 SmoothSandstone | 1 | item.createTile + zh-Hans.ItemName |
| 480 | 血月天塔柱 | Blood Moon Monolith | BloodMoonMonolith | 4054 BloodMoonMonolith | 1 | item.createTile + zh-Hans.ItemName |
| 481 | 破裂蓝砖 | Cracked Blue Brick | CrackedBlueDungeonBrick | 4238 CrackedBlueBrick | 1 | item.createTile + zh-Hans.ItemName |
| 482 | 破裂绿砖 | Cracked Green Brick | CrackedGreenDungeonBrick | 4239 CrackedGreenBrick | 1 | item.createTile + zh-Hans.ItemName |
| 483 | 破裂粉砖 | Cracked Pink Brick | CrackedPinkDungeonBrick | 4240 CrackedPinkBrick | 1 | item.createTile + zh-Hans.ItemName |
| 484 | 仙人球 | Rolling Cactus | RollingCactus | 4390 RollingCactus | 1 | item.createTile + zh-Hans.ItemName |
| 485 | 蚁狮卵 | Antlion Eggs | AntlionLarva | 5067 AntlionEggs | 1 | item.createTile + zh-Hans.ItemName |
| 486 | 鼓组 | Drum Set | DrumSet | 4063 DrumSet | 1 | item.createTile + zh-Hans.ItemName |
| 487 | 野餐桌 | Picnic Table | PicnicTable | 4064 PicnicTable | 2 | item.createTile + zh-Hans.ItemName |
| 488 |  | Fallen Log | FallenLog |  | 0 | TileID constant (no placing item) |
| 489 | 风车 | Pin Wheel | PinWheel | 4074 PinWheel | 1 | item.createTile + zh-Hans.ItemName |
| 490 | 风向标 | Weather Vane | WeatherVane | 4075 WeatherVane | 1 | item.createTile + zh-Hans.ItemName |
| 491 | 虚空保险库 | Void Vault | VoidVault | 4076 VoidVault | 1 | item.createTile + zh-Hans.ItemName |
| 492 |  | Golf Grass Hallowed | GolfGrassHallowed |  | 0 | TileID constant (no placing item) |
| 493 | 白三角旗 | White Pin Flag | GolfCupFlag | 4083 GolfCupFlagWhite | 6 | item.createTile + zh-Hans.ItemName |
| 494 | 高尔夫球座 | Golf Tee | GolfTee | 4089 GolfTee | 1 | item.createTile + zh-Hans.ItemName |
| 495 | 贝壳堆 | Shell Pile | ShellPile | 4090 ShellPileBlock | 1 | item.createTile + zh-Hans.ItemName |
| 496 | 反传送门块 | Anti-Portal Block | AntiPortalBlock | 4091 AntiPortalBlock | 1 | item.createTile + zh-Hans.ItemName |
| 497 | 乌木马桶 | Ebonwood Toilet | Toilets | 4096 ToiletEbonyWood | 65 | item.createTile + zh-Hans.ItemName |
| 498 | 蜘蛛窝块 | Spider Nest Block | Spider | 4139 SpiderBlock | 1 | item.createTile + zh-Hans.ItemName |
| 499 | 腐变室 | Decay Chamber | LesionStation | 4142 LesionStation | 1 | item.createTile + zh-Hans.ItemName |
| 500 | 日耀砖 | Solar Brick | SolarBrick | 4229 SolarBrick | 1 | item.createTile + zh-Hans.ItemName |
| 501 | 星旋砖 | Vortex Brick | VortexBrick | 4230 VortexBrick | 1 | item.createTile + zh-Hans.ItemName |
| 502 | 星云砖 | Nebula Brick | NebulaBrick | 4231 NebulaBrick | 1 | item.createTile + zh-Hans.ItemName |
| 503 | 星尘砖 | Stardust Brick | StardustBrick | 4232 StardustBrick | 1 | item.createTile + zh-Hans.ItemName |
| 504 |  | Mystic Snake Rope | MysticSnakeRope |  | 0 | TileID constant (no placing item) |
| 505 | 金鱼缸 | Gold Fish Bowl | GoldGoldfishBowl | 4275 GoldGoldfishBowl | 1 | item.createTile + zh-Hans.ItemName |
| 506 | 巴斯特雕像 | Bast Statue | CatBast | 4276 CatBast | 1 | item.createTile + zh-Hans.ItemName |
| 507 | 金星块 | Gold Starry Block | GoldStarryGlassBlock | 4277 GoldStarryGlassBlock | 1 | item.createTile + zh-Hans.ItemName |
| 508 | 蓝星块 | Blue Starry Block | BlueStarryGlassBlock | 4278 BlueStarryGlassBlock | 1 | item.createTile + zh-Hans.ItemName |
| 509 | 虚空天塔柱 | Void Monolith | VoidMonolith | 4318 VoidMonolith | 1 | item.createTile + zh-Hans.ItemName |
| 510 | 箭头标牌 | Arrow Sign | ArrowSign | 4319 ArrowSign | 1 | item.createTile + zh-Hans.ItemName |
| 511 | 彩色箭头标牌 | Painted Arrow Sign | PaintedArrowSign | 4320 PaintedArrowSign | 1 | item.createTile + zh-Hans.ItemName |
| 512 |  | Green Moss Brick | GreenMossBrick |  | 0 | TileID constant (no placing item) |
| 513 |  | Brown Moss Brick | BrownMossBrick |  | 0 | TileID constant (no placing item) |
| 514 |  | Red Moss Brick | RedMossBrick |  | 0 | TileID constant (no placing item) |
| 515 |  | Blue Moss Brick | BlueMossBrick |  | 0 | TileID constant (no placing item) |
| 516 |  | Purple Moss Brick | PurpleMossBrick |  | 0 | TileID constant (no placing item) |
| 517 |  | Lava Moss Brick | LavaMossBrick |  | 0 | TileID constant (no placing item) |
| 518 |  | Lily Pad | LilyPad |  | 0 | TileID constant (no placing item) |
| 519 |  | Cattail | Cattail |  | 0 | TileID constant (no placing item) |
| 520 | 盘子 | Plate | FoodPlatter | 4326 FoodPlatter | 1 | item.createTile + zh-Hans.ItemName |
| 521 |  | Black Dragonfly Jar | BlackDragonflyJar |  | 0 | TileID constant (no placing item) |
| 522 |  | Blue Dragonfly Jar | BlueDragonflyJar |  | 0 | TileID constant (no placing item) |
| 523 |  | Green Dragonfly Jar | GreenDragonflyJar |  | 0 | TileID constant (no placing item) |
| 524 |  | Orange Dragonfly Jar | OrangeDragonflyJar |  | 0 | TileID constant (no placing item) |
| 525 |  | Red Dragonfly Jar | RedDragonflyJar |  | 0 | TileID constant (no placing item) |
| 526 |  | Yellow Dragonfly Jar | YellowDragonflyJar |  | 0 | TileID constant (no placing item) |
| 527 | 金蜻蜓罐 | Gold Dragonfly Jar | GoldDragonflyJar | 4333 GoldDragonflyJar | 1 | item.createTile + zh-Hans.ItemName |
| 528 |  | Mushroom Vines | MushroomVines |  | 0 | TileID constant (no placing item) |
| 529 |  | Sea Oats | SeaOats |  | 0 | TileID constant (no placing item) |
| 530 |  | Oasis Plants | OasisPlants |  | 0 | TileID constant (no placing item) |
| 531 | 巨石雕像 | Boulder Statue | BoulderStatue | 4355 BoulderStatue | 1 | item.createTile + zh-Hans.ItemName |
| 532 | 蝇蛆笼 | Maggot Cage | MaggotCage | 4364 MaggotCage | 1 | item.createTile + zh-Hans.ItemName |
| 533 | 大鼠笼 | Rat Cage | RatCage | 4376 RatCage | 1 | item.createTile + zh-Hans.ItemName |
| 534 | 氪苔藓 | Krypton Moss | KryptonMoss | 4377 KryptonMoss | 1 | item.createTile + zh-Hans.ItemName |
| 535 |  | Krypton Moss Brick | KryptonMossBrick |  | 0 | TileID constant (no placing item) |
| 536 | 氙苔藓 | Xenon Moss | XenonMoss | 4378 XenonMoss | 1 | item.createTile + zh-Hans.ItemName |
| 537 |  | Xenon Moss Brick | XenonMossBrick |  | 0 | TileID constant (no placing item) |
| 538 | 瓢虫笼 | Ladybug Cage | LadybugCage | 4380 LadybugCage | 1 | item.createTile + zh-Hans.ItemName |
| 539 | 氩苔藓 | Argon Moss | ArgonMoss | 4389 ArgonMoss | 1 | item.createTile + zh-Hans.ItemName |
| 540 |  | Argon Moss Brick | ArgonMossBrick |  | 0 | TileID constant (no placing item) |
| 541 | 回声块 | Echo Block | EchoBlock | 4392 EchoBlock | 1 | item.createTile + zh-Hans.ItemName |
| 542 | 猫头鹰笼 | Owl Cage | OwlCage | 4396 OwlCage | 1 | item.createTile + zh-Hans.ItemName |
| 543 | 鳉鱼缸 | Pupfish Bowl | PupfishBowl | 4398 PupfishBowl | 1 | item.createTile + zh-Hans.ItemName |
| 544 | 金瓢虫笼 | Gold Ladybug Cage | GoldLadybugCage | 4399 GoldLadybugCage | 1 | item.createTile + zh-Hans.ItemName |
| 545 | 草坪火烈鸟 | Lawn Flamingo | LawnFlamingo | 4420 LawnFlamingo | 1 | item.createTile + zh-Hans.ItemName |
| 546 | 格栅 | Grate | Grate | 4422 Grate | 1 | item.createTile + zh-Hans.ItemName |
| 547 | 盆栽森林雪松 | Potted Forest Cedar | PottedPlants1 | 4430 PottedForestCedar | 5 | item.createTile + zh-Hans.ItemName |
| 548 | 盆栽神圣树 | Potted Hallow Tree | PottedPlants2 | 4435 PottedHallowTree | 9 | item.createTile + zh-Hans.ItemName |
| 549 |  | Seaweed | Seaweed |  | 0 | TileID constant (no placing item) |
| 550 | 龟笼 | Turtle Cage | TurtleCage | 4461 TurtleCage | 1 | item.createTile + zh-Hans.ItemName |
| 551 | 丛林龟笼 | Jungle Turtle Cage | TurtleJungleCage | 4462 TurtleJungleCage | 1 | item.createTile + zh-Hans.ItemName |
| 552 | 沙堡桶 | Sandcastle Bucket | Sandcastles | 4460 SandcastleBucket | 1 | item.createTile + zh-Hans.ItemName |
| 553 | 䴙䴘笼 | Grebe Cage | GrebeCage | 4473 GrebeCage | 1 | item.createTile + zh-Hans.ItemName |
| 554 | 海鸥笼 | Seagull Cage | SeagullCage | 4474 SeagullCage | 1 | item.createTile + zh-Hans.ItemName |
| 555 | 水黾笼 | Water Strider Cage | WaterStriderCage | 4475 WaterStriderCage | 1 | item.createTile + zh-Hans.ItemName |
| 556 | 金水黾笼 | Gold Water Strider Cage | GoldWaterStriderCage | 4476 GoldWaterStriderCage | 1 | item.createTile + zh-Hans.ItemName |
| 557 |  | Grate Closed | GrateClosed |  | 0 | TileID constant (no placing item) |
| 558 | 海马笼 | Seahorse Cage | SeahorseCage | 4481 SeahorseCage | 1 | item.createTile + zh-Hans.ItemName |
| 559 | 金海马笼 | Gold Seahorse Cage | GoldSeahorseCage | 4483 GoldSeahorseCage | 1 | item.createTile + zh-Hans.ItemName |
| 560 | 高尔夫铜奖杯 | Bronze Golf Trophy | GolfTrophies | 4599 GolfTrophyBronze | 3 | item.createTile + zh-Hans.ItemName |
| 561 | 大理石柱 | Marble Column | MarbleColumn | 4554 MarbleColumn | 1 | item.createTile + zh-Hans.ItemName |
| 562 | 竹子 | Bamboo | BambooBlock | 4564 BambooBlock | 1 | item.createTile + zh-Hans.ItemName |
| 563 | 大竹子 | Large Bamboo | LargeBambooBlock | 4547 LargeBambooBlock | 1 | item.createTile + zh-Hans.ItemName |
| 564 | 等离子灯 | Plasma Lamp | PlasmaLamp | 4553 PlasmaLamp | 1 | item.createTile + zh-Hans.ItemName |
| 565 | 造雾机 | Fog Machine | FogMachine | 4552 FogMachine | 1 | item.createTile + zh-Hans.ItemName |
| 566 | 琥珀石块 | Amber Stone Block | AmberStoneBlock | 4646 AmberStoneBlock | 1 | item.createTile + zh-Hans.ItemName |
| 567 | 花园侏儒 | Garden Gnome | GardenGnome | 4609 GardenGnome | 1 | item.createTile + zh-Hans.ItemName |
| 568 | 粉仙灵罐 | Pink Fairy Jar | PinkFairyJar | 4655 PinkFairyJar | 1 | item.createTile + zh-Hans.ItemName |
| 569 | 绿仙灵罐 | Green Fairy Jar | GreenFairyJar | 4656 GreenFairyJar | 1 | item.createTile + zh-Hans.ItemName |
| 570 | 蓝仙灵罐 | Blue Fairy Jar | BlueFairyJar | 4657 BlueFairyJar | 1 | item.createTile + zh-Hans.ItemName |
| 571 |  | Bamboo | Bamboo |  | 0 | TileID constant (no placing item) |
| 572 | 光明之魂瓶 | Soul of Light in a Bottle | SoulBottles | 4695 SoulBottleLight | 6 | item.createTile + zh-Hans.ItemName |
| 573 | 破旧木标牌 | Tattered Wood Sign | TatteredWoodSign | 4710 TatteredWoodSign | 1 | item.createTile + zh-Hans.ItemName |
| 574 |  | Boreal Beam | BorealBeam |  | 0 | TileID constant (no placing item) |
| 575 |  | Rich Mahogany Beam | RichMahoganyBeam |  | 0 | TileID constant (no placing item) |
| 576 |  | Granite Column | GraniteColumn |  | 0 | TileID constant (no placing item) |
| 577 |  | Sandstone Column | SandstoneColumn |  | 0 | TileID constant (no placing item) |
| 578 |  | Mushroom Beam | MushroomBeam |  | 0 | TileID constant (no placing item) |
| 579 | 岩石巨人头 | Rock Golem Head | RockGolemHead | 4761 RockGolemHead | 1 | item.createTile + zh-Hans.ItemName |
| 580 | 地狱蝴蝶罐 | Hell Butterfly Jar | HellButterflyJar | 4846 HellButterflyJar | 1 | item.createTile + zh-Hans.ItemName |
| 581 | 熔岩萤火虫瓶 | Lavafly in a Bottle | LavaflyinaBottle | 4848 LavaflyinaBottle | 1 | item.createTile + zh-Hans.ItemName |
| 582 | 岩浆蜗牛笼 | Magma Snail Cage | MagmaSnailCage | 4850 MagmaSnailCage | 1 | item.createTile + zh-Hans.ItemName |
| 583 |  | Tree Topaz | TreeTopaz |  | 0 | TileID constant (no placing item) |
| 584 |  | Tree Amethyst | TreeAmethyst |  | 0 | TileID constant (no placing item) |
| 585 |  | Tree Sapphire | TreeSapphire |  | 0 | TileID constant (no placing item) |
| 586 |  | Tree Emerald | TreeEmerald |  | 0 | TileID constant (no placing item) |
| 587 |  | Tree Ruby | TreeRuby |  | 0 | TileID constant (no placing item) |
| 588 |  | Tree Diamond | TreeDiamond |  | 0 | TileID constant (no placing item) |
| 589 |  | Tree Amber | TreeAmber |  | 0 | TileID constant (no placing item) |
| 590 | 黄玉宝石果 | Topaz Gemcorn | GemSaplings | 4851 GemTreeTopazSeed | 7 | item.createTile + zh-Hans.ItemName |
| 591 | 吊挂盆 | Hanging Pot | PotsSuspended | 4858 PotSuspended | 9 | item.createTile + zh-Hans.ItemName |
| 592 | 吊挂火盆 | Hanging Brazier | BrazierSuspended | 4867 BrazierSuspended | 1 | item.createTile + zh-Hans.ItemName |
| 593 |  | Volcano Small | VolcanoSmall |  | 0 | TileID constant (no placing item) |
| 594 |  | Volcano Large | VolcanoLarge |  | 0 | TileID constant (no placing item) |
| 595 | 樱花树苗 | Sakura Sapling | VanityTreeSakuraSaplings | 4871 VanityTreeSakuraSeed | 1 | item.createTile + zh-Hans.ItemName |
| 596 |  | Vanity Tree Sakura | VanityTreeSakura |  | 0 | TileID constant (no placing item) |
| 597 | 丛林晶塔 | Jungle Pylon | TeleportationPylon | 4875 TeleportationPylonJungle | 11 | item.createTile + zh-Hans.ItemName |
| 598 | 熔岩蛇缸 | Lava Serpent Bowl | LavafishBowl | 4880 LavaFishbowl | 1 | item.createTile + zh-Hans.ItemName |
| 599 |  | Amethyst Bunny Cage | AmethystBunnyCage |  | 0 | TileID constant (no placing item) |
| 600 |  | Topaz Bunny Cage | TopazBunnyCage |  | 0 | TileID constant (no placing item) |
| 601 |  | Sapphire Bunny Cage | SapphireBunnyCage |  | 0 | TileID constant (no placing item) |
| 602 |  | Emerald Bunny Cage | EmeraldBunnyCage |  | 0 | TileID constant (no placing item) |
| 603 |  | Ruby Bunny Cage | RubyBunnyCage |  | 0 | TileID constant (no placing item) |
| 604 |  | Diamond Bunny Cage | DiamondBunnyCage |  | 0 | TileID constant (no placing item) |
| 605 |  | Amber Bunny Cage | AmberBunnyCage |  | 0 | TileID constant (no placing item) |
| 606 |  | Amethyst Squirrel Cage | AmethystSquirrelCage |  | 0 | TileID constant (no placing item) |
| 607 |  | Topaz Squirrel Cage | TopazSquirrelCage |  | 0 | TileID constant (no placing item) |
| 608 |  | Sapphire Squirrel Cage | SapphireSquirrelCage |  | 0 | TileID constant (no placing item) |
| 609 |  | Emerald Squirrel Cage | EmeraldSquirrelCage |  | 0 | TileID constant (no placing item) |
| 610 |  | Ruby Squirrel Cage | RubySquirrelCage |  | 0 | TileID constant (no placing item) |
| 611 |  | Diamond Squirrel Cage | DiamondSquirrelCage |  | 0 | TileID constant (no placing item) |
| 612 |  | Amber Squirrel Cage | AmberSquirrelCage |  | 0 | TileID constant (no placing item) |
| 613 | 盆栽火荆棘 | Potted Fire Brambles | PottedLavaPlants | 4904 PottedLavaPlantBramble | 2 | item.createTile + zh-Hans.ItemName |
| 614 | 盆栽余烬卷须 | Potted Ember Tendrils | PottedLavaPlantTendrils | 4906 PottedLavaPlantTendrils | 1 | item.createTile + zh-Hans.ItemName |
| 615 | 黄柳树苗 | Yellow Willow Sapling | VanityTreeWillowSaplings | 4907 VanityTreeYellowWillowSeed | 1 | item.createTile + zh-Hans.ItemName |
| 616 |  | Vanity Tree Yellow Willow | VanityTreeYellowWillow |  | 0 | TileID constant (no placing item) |
| 617 | 克苏鲁之眼圣物 | Eye of Cthulhu Relic | MasterTrophyBase | 4924 EyeofCthulhuMasterTrophy | 28 | item.createTile + zh-Hans.ItemName |
| 618 | 石材板 | Stone Accent Slab | AccentSlab | 4962 AccentSlab | 1 | item.createTile + zh-Hans.ItemName |
| 619 | 松露虫笼 | Truffle Worm Cage | TruffleWormCage | 4963 TruffleWormCage | 1 | item.createTile + zh-Hans.ItemName |
| 620 | 七彩草蛉罐 | Prismatic Lacewing Jar | EmpressButterflyJar | 4964 EmpressButterflyJar | 1 | item.createTile + zh-Hans.ItemName |
| 621 | 蛋糕块 | Slice of Cake | SliceOfCake | 3750 SliceOfCake | 1 | item.createTile + zh-Hans.ItemName |
| 622 | 茶壶 | Teapot | TeaKettle | 5008 TeaKettle | 1 | item.createTile + zh-Hans.ItemName |
| 623 | 盆栽水晶蕨 | Potted Crystal Fern | PottedCrystalPlants | 5081 PottedCrystalPlantFern | 4 | item.createTile + zh-Hans.ItemName |
| 624 |  | Abigails Flower | AbigailsFlower |  | 0 | TileID constant (no placing item) |
| 625 | 氖苔藓 | Neon Moss | VioletMoss | 5127 VioletMoss | 1 | item.createTile + zh-Hans.ItemName |
| 626 |  | Violet Moss Brick | VioletMossBrick |  | 0 | TileID constant (no placing item) |
| 627 | 氦苔藓 | Helium Moss | RainbowMoss | 5128 RainbowMoss | 1 | item.createTile + zh-Hans.ItemName |
| 628 |  | Rainbow Moss Brick | RainbowMossBrick |  | 0 | TileID constant (no placing item) |
| 629 | 臭虫笼 | Stinkbug Cage | StinkbugCage | 5133 StinkbugCage | 1 | item.createTile + zh-Hans.ItemName |
| 630 | 臭虫拦截器 | Stinkbug Blocker | StinkbugHousingBlocker | 5137 StinkbugHousingBlocker | 1 | item.createTile + zh-Hans.ItemName |
| 631 | 幽灵臭虫拦截器 | Ghostly Stinkbug Blocker | StinkbugHousingBlockerEcho | 5138 StinkbugHousingBlockerEcho | 1 | item.createTile + zh-Hans.ItemName |
| 632 | 绯红金刚鹦鹉笼 | Scarlet Macaw Cage | ScarletMacawCage | 5213 ScarletMacawCage | 1 | item.createTile + zh-Hans.ItemName |
| 633 | 灰烬草种子 | Ash Grass Seeds | AshGrass | 5214 AshGrassSeeds | 1 | item.createTile + zh-Hans.ItemName |
| 634 |  | Tree Ash | TreeAsh |  | 0 | TileID constant (no placing item) |
| 635 | 灰烬木 | Ash Wood | AshWood | 5215 AshWood | 1 | item.createTile + zh-Hans.ItemName |
| 636 |  | Corrupt Vines | CorruptVines |  | 0 | TileID constant (no placing item) |
| 637 |  | Ash Plants | AshPlants |  | 0 | TileID constant (no placing item) |
| 638 |  | Ash Vines | AshVines |  | 0 | TileID constant (no placing item) |
| 639 | 修复的魔力水晶 | Repaired Mana Crystal | ManaCrystal | 5287 RepairedManaCrystal | 1 | item.createTile + zh-Hans.ItemName |
| 640 | 蓝金刚鹦鹉笼 | Blue Macaw Cage | BlueMacawCage | 5301 BlueMacawCage | 1 | item.createTile + zh-Hans.ItemName |
| 641 | 珊瑚礁块 | Reef Block | ReefBlock | 5306 ReefBlock | 1 | item.createTile + zh-Hans.ItemName |
| 642 | 叶绿提炼机 | Chlorophyte Extractinator | ChlorophyteExtractinator | 5296 ChlorophyteExtractinator | 1 | item.createTile + zh-Hans.ItemName |
| 643 | 巨嘴鸟笼 | Toucan Cage | ToucanCage | 5314 ToucanCage | 1 | item.createTile + zh-Hans.ItemName |
| 644 | 黄玄凤鹦鹉笼 | Yellow Cockatiel Cage | YellowCockatielCage | 5315 YellowCockatielCage | 1 | item.createTile + zh-Hans.ItemName |
| 645 | 灰玄凤鹦鹉笼 | Gray Cockatiel Cage | GrayCockatielCage | 5316 GrayCockatielCage | 1 | item.createTile + zh-Hans.ItemName |
| 646 | 暗影蜡烛 | Shadow Candle | ShadowCandle | 5322 ShadowCandle | 1 | item.createTile + zh-Hans.ItemName |
| 647 | 堆石器（小） | Rubblemaker (Small) | LargePilesEcho | 5324 RubblemakerSmall | 3 | item.createTile + zh-Hans.ItemName |
| 648 |  | Large Piles2 Echo | LargePiles2Echo |  | 0 | TileID constant (no placing item) |
| 649 |  | Small Piles2x1 Echo | SmallPiles2x1Echo |  | 0 | TileID constant (no placing item) |
| 650 |  | Small Piles1x1 Echo | SmallPiles1x1Echo |  | 0 | TileID constant (no placing item) |
| 651 |  | Plant Detritus3x2 Echo | PlantDetritus3x2Echo |  | 0 | TileID constant (no placing item) |
| 652 |  | Plant Detritus2x2 Echo | PlantDetritus2x2Echo |  | 0 | TileID constant (no placing item) |
| 653 | 便携式窑 | Portable Kiln | PotsEcho | 5481 PortableKiln | 1 | item.createTile + zh-Hans.ItemName |
| 654 | TNT桶 | TNT Barrel | TNTBarrel | 5327 TNTBarrel | 1 | item.createTile + zh-Hans.ItemName |
| 655 |  | Plantera Thorns | PlanteraThorns |  | 0 | TileID constant (no placing item) |
| 656 |  | Glow Tulip | GlowTulip |  | 0 | TileID constant (no placing item) |
| 657 | 回声腔 | Echo Chamber | EchoMonolith | 5345 EchoMonolith | 1 | item.createTile + zh-Hans.ItemName |
| 658 | 以太天塔柱 | Aether Monolith | ShimmerMonolith | 5347 ShimmerMonolith | 1 | item.createTile + zh-Hans.ItemName |
| 659 | 以太块 | Aetherium Block | ShimmerBlock | 5349 ShimmerBlock | 1 | item.createTile + zh-Hans.ItemName |
| 660 | 飞灵瓶 | Faeling in a Bottle | ShimmerflyinaBottle | 5351 ShimmerflyinaBottle | 1 | item.createTile + zh-Hans.ItemName |
| 661 |  | Corrupt Jungle Grass | CorruptJungleGrass |  | 0 | TileID constant (no placing item) |
| 662 |  | Crimson Jungle Grass | CrimsonJungleGrass |  | 0 | TileID constant (no placing item) |
| 663 | 附魔月晷 | Enchanted Moondial | Moondial | 5381 Moondial | 1 | item.createTile + zh-Hans.ItemName |
| 664 | 弹力巨石 | Bouncy Boulder | BouncyBoulder | 5383 BouncyBoulder | 1 | item.createTile + zh-Hans.ItemName |
| 665 | 生命水晶巨石 | Life Crystal Boulder | LifeCrystalBoulder | 5384 LifeCrystalBoulder | 1 | item.createTile + zh-Hans.ItemName |
| 666 | 臭臭 | Poo | PoopBlock | 5395 PoopBlock | 1 | item.createTile + zh-Hans.ItemName |
| 667 | 以太砖 | Aetherium Brick | ShimmerBrick | 5398 ShimmerBrick | 1 | item.createTile + zh-Hans.ItemName |
| 668 |  | Dirtiest Block | DirtiestBlock |  | 0 | TileID constant (no placing item) |
| 669 | 月锈砖 | Lunar Rust Brick | LunarRustBrick | 5401 LunarRustBrick | 1 | item.createTile + zh-Hans.ItemName |
| 670 | 暗黑天界砖 | Dark Celestial Brick | DarkCelestialBrick | 5402 DarkCelestialBrick | 1 | item.createTile + zh-Hans.ItemName |
| 671 | 星芒砖 | Astra Brick | AstraBrick | 5403 AstraBrick | 1 | item.createTile + zh-Hans.ItemName |
| 672 | 宇宙余烬砖 | Cosmic Ember Brick | CosmicEmberBrick | 5404 CosmicEmberBrick | 1 | item.createTile + zh-Hans.ItemName |
| 673 | 冷核砖 | Cryocore Brick | CryocoreBrick | 5405 CryocoreBrick | 1 | item.createTile + zh-Hans.ItemName |
| 674 | 水星砖 | Mercury Brick | MercuryBrick | 5406 MercuryBrick | 1 | item.createTile + zh-Hans.ItemName |
| 675 | 星星皇家砖 | Star Royale Brick | StarRoyaleBrick | 5407 StarRoyaleBrick | 1 | item.createTile + zh-Hans.ItemName |
| 676 | 天熔砖 | Heavenforge Brick | HeavenforgeBrick | 5408 HeavenforgeBrick | 1 | item.createTile + zh-Hans.ItemName |
| 677 | 远古蓝砖 | Ancient Blue Brick | AncientBlueBrick | 5417 AncientBlueDungeonBrick | 1 | item.createTile + zh-Hans.ItemName |
| 678 | 远古绿砖 | Ancient Green Brick | AncientGreenBrick | 5419 AncientGreenDungeonBrick | 1 | item.createTile + zh-Hans.ItemName |
| 679 | 远古粉砖 | Ancient Pink Brick | AncientPinkBrick | 5421 AncientPinkDungeonBrick | 1 | item.createTile + zh-Hans.ItemName |
| 680 | 远古金砖 | Ancient Gold Brick | AncientGoldBrick | 5423 AncientGoldBrick | 1 | item.createTile + zh-Hans.ItemName |
| 681 | 远古银砖 | Ancient Silver Brick | AncientSilverBrick | 5425 AncientSilverBrick | 1 | item.createTile + zh-Hans.ItemName |
| 682 | 远古铜砖 | Ancient Copper Brick | AncientCopperBrick | 5427 AncientCopperBrick | 1 | item.createTile + zh-Hans.ItemName |
| 683 | 远古黑曜石砖 | Ancient Obsidian Brick | AncientObsidianBrick | 5433 AncientObsidianBrick | 1 | item.createTile + zh-Hans.ItemName |
| 684 | 远古狱石砖 | Ancient Hellstone Brick | AncientHellstoneBrick | 5435 AncientHellstoneBrick | 1 | item.createTile + zh-Hans.ItemName |
| 685 | 远古钴砖 | Ancient Cobalt Brick | AncientCobaltBrick | 5429 AncientCobaltBrick | 1 | item.createTile + zh-Hans.ItemName |
| 686 | 远古秘银砖 | Ancient Mythril Brick | AncientMythrilBrick | 5431 AncientMythrilBrick | 1 | item.createTile + zh-Hans.ItemName |
| 687 | 熔岩苔藓砖 | Lava Moss Brick | LavaMossBlock | 5439 LavaMossBlock | 1 | item.createTile + zh-Hans.ItemName |
| 688 | 氩苔藓砖 | Argon Moss Brick | ArgonMossBlock | 5440 ArgonMossBlock | 1 | item.createTile + zh-Hans.ItemName |
| 689 | 氪苔藓砖 | Krypton Moss Brick | KryptonMossBlock | 5441 KryptonMossBlock | 1 | item.createTile + zh-Hans.ItemName |
| 690 | 氙苔藓砖 | Xenon Moss Brick | XenonMossBlock | 5442 XenonMossBlock | 1 | item.createTile + zh-Hans.ItemName |
| 691 | 氖苔藓砖 | Neon Moss Brick | VioletMossBlock | 5443 VioletMossBlock | 1 | item.createTile + zh-Hans.ItemName |
| 692 | 氦苔藓砖 | Helium Moss Brick | RainbowMossBlock | 5444 RainbowMossBlock | 1 | item.createTile + zh-Hans.ItemName |
| 693 | 笋生-石出 | Mitey-Titey | Stalactite1x1Echo | 5464 MiteyTitey | 1 | item.createTile + zh-Hans.ItemName |
| 694 |  | Stalactite1x2 Echo | Stalactite1x2Echo |  | 0 | TileID constant (no placing item) |
| 695 | 复制恶魔祭坛 | Replica Demon Altar | DemonAltarReplica | 5467 DemonAltarReplica | 2 | item.createTile + zh-Hans.ItemName |
| 696 | 复制暗影珠 | Replica Shadow Orb | ShadowOrbsReplica | 5469 ShadowOrbReplica | 2 | item.createTile + zh-Hans.ItemName |
| 697 | 装饰蛛网 | Decorative Cobweb | CobwebReplica | 5471 CobwebReplica | 1 | item.createTile + zh-Hans.ItemName |
| 698 | 物品瓶 | Item Flask | DeadCellsDisplayJar | 5472 DeadCellsDisplayJar | 1 | item.createTile + zh-Hans.ItemName |
| 699 | 炼金瓶 | Alchemy Flask | DeadCellsPotionStation | 5482 DeadCellsPotionStation | 1 | item.createTile + zh-Hans.ItemName |
| 700 |  | Abigails Flower Replica | AbigailsFlowerReplica |  | 0 | TileID constant (no placing item) |
| 701 |  | Glow Tulip Replica | GlowTulipReplica |  | 0 | TileID constant (no placing item) |
| 702 |  | Life Fruit Replica | LifeFruitReplica |  | 0 | TileID constant (no placing item) |
| 703 |  | Jungle Plants Echo | JunglePlantsEcho |  | 0 | TileID constant (no placing item) |
| 704 |  | Fallen Log Echo | FallenLogEcho |  | 0 | TileID constant (no placing item) |
| 705 |  | Oasis Plants Echo | OasisPlantsEcho |  | 0 | TileID constant (no placing item) |
| 706 |  | Terragrim Shrine Echo | TerragrimShrineEcho |  | 0 | TileID constant (no placing item) |
| 707 |  | Books Echo | BooksEcho |  | 0 | TileID constant (no placing item) |
| 708 | 微光瀑布块 | Shimmerfall Block | Shimmerfall | 5493 ShimmerFallBlock | 1 | item.createTile + zh-Hans.ItemName |
| 709 | 魔法微光滴管 | Magic Shimmer Dropper | ShimmerDrip | 5492 MagicShimmerDropper | 1 | item.createTile + zh-Hans.ItemName |
| 710 | 河豚笼 | Pufferfish Cage | PufferfishCage | 5512 PufferfishCage | 1 | item.createTile + zh-Hans.ItemName |
| 711 | 彩虹巨石 | Rainbow Boulder | RainbowBoulder | 5514 RainbowBoulder | 1 | item.createTile + zh-Hans.ItemName |
| 712 | 便便巨石 | Poo Boulder | Poulder | 5516 Poulder | 1 | item.createTile + zh-Hans.ItemName |
| 713 | 熔岩巨石 | Lava Boulder | LavaBoulder | 5520 LavaBoulder | 1 | item.createTile + zh-Hans.ItemName |
| 714 | 蜘蛛巨石 | Spider Boulder | SpiderBoulder | 5521 SpiderBoulder | 1 | item.createTile + zh-Hans.ItemName |
| 715 | 奇石 | Ghoulder | Ghoulder | 5522 Ghoulder | 1 | item.createTile + zh-Hans.ItemName |
| 716 |  | Boulder That Spawns Pet | BoulderThatSpawnsPet |  | 0 | TileID constant (no placing item) |
| 717 | 熔岩云 | Lava Cloud | LavaCloud | 5569 LavaCloud | 1 | item.createTile + zh-Hans.ItemName |
| 718 | 星云 | Star Cloud | StarCloud | 5570 StarCloud | 1 | item.createTile + zh-Hans.ItemName |
| 719 | 彩虹云 | Rainbow Cloud | RainbowCloud | 5571 RainbowCloud | 1 | item.createTile + zh-Hans.ItemName |
| 720 |  | CRTMonolith | CRTMonolith |  | 0 | TileID constant (no placing item) |
| 721 |  | Retro Monolith | RetroMonolith |  | 0 | TileID constant (no placing item) |
| 722 | 坠落之星块 | Fallen Star Block | FallenStarBlock | 5622 FallenStarBlock | 1 | item.createTile + zh-Hans.ItemName |
| 723 |  | Kite Anchor | KiteAnchor |  | 0 | TileID constant (no placing item) |
| 724 |  | Critter Anchor | CritterAnchor |  | 0 | TileID constant (no placing item) |
| 725 |  | Noir Monolith | NoirMonolith |  | 0 | TileID constant (no placing item) |
| 726 | 炮弹 | Cannonball | CannonBall | 929 Cannonball | 1 | item.createTile + zh-Hans.ItemName |
| 727 |  | Team Block Red Variant | TeamBlockRedVariant |  | 0 | TileID constant (no placing item) |
| 728 |  | Team Block Green Variant | TeamBlockGreenVariant |  | 0 | TileID constant (no placing item) |
| 729 |  | Team Block Blue Variant | TeamBlockBlueVariant |  | 0 | TileID constant (no placing item) |
| 730 |  | Team Block Yellow Variant | TeamBlockYellowVariant |  | 0 | TileID constant (no placing item) |
| 731 |  | Team Block Pink Variant | TeamBlockPinkVariant |  | 0 | TileID constant (no placing item) |
| 732 |  | Team Block White Variant | TeamBlockWhiteVariant |  | 0 | TileID constant (no placing item) |
| 733 |  | Radio Thing Monolith | RadioThingMonolith |  | 0 | TileID constant (no placing item) |
| 734 | 仙林 | Feywood | Feywood | 5710 Feywood | 1 | item.createTile + zh-Hans.ItemName |
| 735 | 神圣砖 | Hallowed Brick | HallowedBrick | 5733 HallowedBrick | 1 | item.createTile + zh-Hans.ItemName |
| 736 | 复活节物块 | Easter Block | EasterBlock | 5920 EasterBlock | 1 | item.createTile + zh-Hans.ItemName |
| 737 | 哥特砖 | Gothic Brick | GothicBrick | 5922 GothicBrick | 1 | item.createTile + zh-Hans.ItemName |
| 738 | 小雪怪皮物块 | Flinx Fur Block | FlinxFurBlock | 5924 FlinxFurBlock | 1 | item.createTile + zh-Hans.ItemName |
| 739 | 水母物块 | Jellyfish Block | JellyfishBlock | 5926 JellyfishBlock | 1 | item.createTile + zh-Hans.ItemName |
| 740 | 玩具毁灭者砖 | Toybreaker Brick | ToyBreakerBlock | 5928 ToyBreakerBlock | 1 | item.createTile + zh-Hans.ItemName |
| 741 | 松木 | Pine Wood | PineWoodBlock | 5930 PineWoodBlock | 1 | item.createTile + zh-Hans.ItemName |
| 742 | 鹰身女妖块 | Harpy Block | HarpyBlock | 5953 HarpyBlock | 1 | item.createTile + zh-Hans.ItemName |
| 743 | 月盘块 | Moonplate Block | MoonplateBlock | 5996 MoonplateBlock | 1 | item.createTile + zh-Hans.ItemName |
| 744 | 图书管理员块 | Librarian Block | LibrarianBlock | 6019 LibrarianBlock | 1 | item.createTile + zh-Hans.ItemName |
| 745 | 尖刺块 | Spike Block | SpikeBlock | 6042 SpikeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 746 | 办公块 | Office Block | OfficeBlock | 6065 OfficeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 747 | 禁戒块 | Forbidden Block | ForbiddenBlock | 6088 ForbiddenBlock | 1 | item.createTile + zh-Hans.ItemName |
| 748 | 水族块 | Aquarium Block | WaterBlock | 6109 WaterBlock | 1 | item.createTile + zh-Hans.ItemName |
| 749 | 巨石块 | Boulder Block | BoulderBlock | 6132 BoulderBlock | 1 | item.createTile + zh-Hans.ItemName |
| 750 | 锋利尖刺块 | Sharp Spike Block | DamagingSpikeBlock | 6134 DamagingSpikeBlock | 1 | item.createTile + zh-Hans.ItemName |
| 751 |  | Palworld Digtoise Sleeping | PalworldDigtoiseSleeping |  | 0 | TileID constant (no placing item) |
| 752 |  | Palworld Chillet Egg | PalworldChilletEgg |  | 0 | TileID constant (no placing item) |
| 753 | 巨型提基 | Giant Tiki | GiantTiki | 6147 GiantTiki | 1 | item.createTile + zh-Hans.ItemName |
