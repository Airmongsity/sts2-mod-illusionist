# Slay the Spire 2 游戏机制事实表

> 本文是 Illusionist MOD 的开发参考，不是面向玩家的攻略，也不是对所有单卡、单遗物和单事件文本的百科式抄录。它记录会影响 MOD 实现与平衡判断的游戏流程、生成算法、概率、结算时点和角色原生系统。

## 0. 证据范围与阅读约定

- 主要证据是工作区 `.re_current_20260723/MegaCrit/sts2/Core/` 下的反编译源码快照；本地运行日志显示该快照对应开发时实际使用的公开测试版 **v0.110.1**。
- 反编译代码不是官方稳定 API。游戏更新、RitsuLib、其他 MOD、每日挑战修正和自定义规则都可能改变本文结论。
- “概率”若由地图配额、路径选择、动态保底或候选池共同决定，本文写算法而不伪造一个静态“总体概率”。
- 下文的“标准流程”指非 Daily、非 Custom 的标准单人局；多人差异会单独说明。
- `Common / Uncommon / Rare / Ancient / Event / Token` 是卡牌的 `CardRarity`；`Attack / Skill / Power / Status / Curse / Quest` 是独立的 `CardType`。二者不能混为一谈。
- 类名和成员名用于定位证据；本文件不要求 MOD 直接调用这些内部成员。

## 1. 一整场标准游戏如何进行

### 1.1 开局

1. 玩家选择角色、进阶等级和可用的模式/修正。
2. 游戏建立 `RunState`、随机数流、角色的初始牌组、初始遗物、生命、金币和药水栏。
3. 标准角色初始金币为 99；基础能量上限通常为 3。
4. 第一幕通常是 `Overgrowth`；解锁后可出现替代第一幕 `Underdocks`。第二幕是 `Hive`，第三幕是 `Glory`。
5. 解锁 Neow Epoch 后，第一幕从 Neow 的先古之民节点开始；在首次流程尚未解锁 Neow 时，该起点会改为普通敌人节点。

### 1.2 每一幕

每幕的结构不是“连续随机房间”，而是：

1. 进入幕首的先古之民节点，回复生命并三选一获得其提供的内容；首次未解锁 Neow 的第一幕例外。
2. 在七列地图上选择一条连通路径。
3. 经过普通敌人、精英、问号、商店、宝箱、休息处等房间。
4. 最后一段固定经过休息处，再进入 Boss。
5. 战胜非最终幕 Boss 后进入下一幕；战胜第三幕最终 Boss 后进入 `TheArchitect` 结局事件。

地图中的“常规房间数”不包括幕首先古之民与 Boss，因此源码的楼层数为“常规房间数 + 2”。实际经过多少节点由玩家选择的连线路径决定，并不等于地图上生成的全部节点数。

### 1.3 战斗与奖励循环

普通战斗的基本循环是：

1. 建立战斗牌堆，把持久牌组复制为战斗中的可变卡牌实例并洗牌。
2. 敌人准备意图，玩家开始回合，补充能量并抽牌。
3. 玩家打牌、使用药水、操作角色专属资源，然后结束回合。
4. 手牌弃置/保留、临时状态清理，敌人依次执行意图。
5. 双方交替，直到所有敌人死亡、玩家死亡或其他结束条件成立。
6. 普通敌人与精英通常提供金币、药水概率和三张卡牌中的至多一张；精英额外提供遗物。非最终 Boss 提供金币、药水概率和全 Rare 的卡牌三选一。

所有卡牌奖励均可跳过；显示三张牌不代表获得三张，只能选择至多一张。

### 1.4 当前结局与进阶推进

战胜第三幕最后的 Boss 后进入 `TheArchitect`。当前实现会把本局分数换算为对 Architect 的伤害并完成结局流程。标准胜利会更新角色进度；符合条件时把该角色可选的最高进阶提高 1。Daily 和 Custom 不推进标准进阶。

## 2. 地图如何生成

### 2.1 拓扑

- 标准地图宽 7 列。
- 生成 7 条路径。路径每向上一行只能左移一列、不移或右移一列，并避免交叉；不同路径可以汇合。
- 起始行全是普通敌人。
- 最后一个常规房间行全是休息处。
- 距离末端第 7 行固定为宝箱行。
- 幕首节点是先古之民，幕末节点是 Boss。
- 多人模式每幕比单人少 1 个常规房间。

### 2.2 各幕的房间配额

下表是地图生成器尝试放置到可达图节点中的类型数量，不是玩家在一局中实际访问某类型的概率。

| 幕 | 单人/多人常规房间数 | 弱敌人序列 | 休息处配额 | 问号配额 | 商店配额 | 精英配额 |
|---|---:|---:|---:|---:|---:|---:|
| Overgrowth / Underdocks | 15 / 14 | 3 | 6–7 | 10–14 | 3 | 5；进阶 1 起为 8 |
| Hive | 14 / 13 | 2 | 6–7 | 9–13 | 3 | 5；进阶 1 起为 8 |
| Glory | 13 / 12 | 2 | 5–6 | 9–13 | 3 | 5；进阶 1 起为 8 |

其余可变节点填为普通敌人。休息处和问号数由有截断范围的随机分布产生，所以不是范围内完全等概率。

### 2.3 放置限制

- 精英和随机休息处不能出现在第 6 行以下的早期区域。
- 随机休息处不能出现在地图最后 3 行；末端固定休息处不受这条随机放置限制。
- 精英、休息处、宝箱、商店不能与同类型父子节点连续相邻。
- 生成器还限制部分兄弟节点同时成为相同的特殊类型。
- 固定的起点、首行、宝箱行、末端休息行和 Boss 不会被普通配额覆盖。
- 源码中仍有关于旧 `Warden` 进阶替换宝箱的注释，但当前 `AscensionLevel` 没有 Warden，标准 `RunManager` 也明确以 `replaceTreasureWithElites: false` 建图。不能把旧注释当成当前机制。

### 2.4 问号房的动态概率

初始权重为：

| 结果 | 初始概率/权重 | 未命中后的变化 | 命中后的变化 |
|---|---:|---:|---:|
| 普通敌人 | 10% | +10 个百分点 | 重置为 10% |
| 宝箱 | 2% | +2 个百分点 | 重置为 2% |
| 商店 | 3% | +3 个百分点 | 重置为 3% |
| 精英 | -1，禁用 | 不增长 | 默认不会命中 |
| 事件 | 剩余概率，初始 85% | 没有独立累加值 | 由其他权重的剩余量决定 |

每次进入问号房才进行一次判定。命中某个非事件类型后，该类型重置；其他允许但未命中的非事件类型按基础值增长。各幕之间重置为基础值。黑名单、Hook 或修正可以移除候选或改变增长量。

首次存档的前三个问号有教学覆盖：前两个固定为事件，第三个固定为普通敌人；之后才使用上面的动态权重。另有一段针对 `Unassigned` 点的首次事件保护，它不是普通问号算法本身。

因此不存在一个对所有玩家、所有种子都成立的“地图上敌人/事件/商店各占百分之多少”：固定配额决定图上节点，七条路径和玩家选择决定访问节点，问号内部又有动态概率。

### 2.5 敌人、事件和 Boss 的候选生成

- 每幕有自己的普通、精英和 Boss 候选池，也可包含共享内容。
- 普通战斗序列在进入地图前生成；每幕先消费固定数量的弱敌人遭遇，再从其余符合条件的遭遇中抽取。
- 事件从该幕事件与共享事件的符合条件候选中洗牌抽取；在所有唯一可用事件耗尽前通常不重复，耗尽后才允许重复。
- 每幕选一个 Boss。早期账号进度会优先安排尚未见过的 Boss，以完成发现顺序。
- 进阶 10 在最终幕安排第二个与第一个不同的 Boss。

## 3. 卡牌的分类、来源与掉落概率

### 3.1 类型与稀有度是两个维度

`CardType`：

| 类型 | 含义 |
|---|---|
| Attack | 攻击牌，通常直接造成伤害 |
| Skill | 技能牌，防御、资源、抽牌、控制等 |
| Power | 能力牌，打出后提供持续战斗效果 |
| Status | 状态牌，通常由敌人或战斗效果塞入 |
| Curse | 诅咒牌，通常是长期负面牌 |
| Quest | 任务牌，服务于特殊事件或进度 |
| None | 没有普通卡牌类型的内部内容 |

`CardRarity`：`None`、`Basic`、`Common`、`Uncommon`、`Rare`、`Ancient`、`Event`、`Token`、`Status`、`Curse`、`Quest`。

例如一张 Ancient 卡仍可以是 Attack、Skill 或 Power；“Event”不是第四种普通掉落稀有度；Status 既可作为类型也可作为稀有度名称。

### 3.2 普通战斗、精英与 Boss 的卡牌奖励

每个普通卡牌奖励界面默认生成 3 个互不重复的稳定卡牌 ID，玩家选择 0 或 1 张。下表是**每一个候选格**的基础稀有度权重：

| 来源 | Common | Uncommon | Rare |
|---|---:|---:|---:|
| 普通敌人 | 60% | 37% | 3% |
| 精英 | 50% | 40% | 10% |
| Boss | 0% | 0% | 100% |
| 普通敌人，进阶 7+ | 61.5% | 37% | 约 1.5% |
| 精英，进阶 7+ | 55% | 40% | 5% |

这不是最终独立同分布概率，因为战斗卡牌奖励共用 Rare 动态偏移：

- 初始 Rare 偏移为 **-5 个百分点**。
- 每生成一个非 Rare 候选，偏移通常增加 1 个百分点；进阶 7 起只增加 0.5 个百分点。
- 生成 Rare 后偏移重置为 -5 个百分点。
- 偏移上限为 +40 个百分点。
- 三选一中的三个格子会逐个推进这个状态，因此同一奖励界面内部也会改变后续 Rare 概率。
- Boss 候选固定 Rare，并会触发 Rare 偏移重置。

所以普通敌人的第一个候选在全新状态下实际 Rare 概率可以为 0，而不是宣传式地简单理解为恒定 3%。

### 3.3 奖励牌的升级概率

非 Rare 战斗奖励牌的基础升级概率随幕数增加：

| 幕 | 进阶 0–6 | 进阶 7+ |
|---|---:|---:|
| 第一幕 | 0% | 0% |
| 第二幕 | 25% | 12.5% |
| 第三幕 | 50% | 25% |

Rare 的这项自然升级概率为 0。遗物、修正和 Hook 仍可改变结果。

### 3.4 商店卡牌

标准商店生成：

- 5 张角色牌，卡牌类型槽固定为 `Attack, Attack, Skill, Skill, Power`；其中一张随机半价。
- 2 张无色牌，分别固定为 Uncommon 和 Rare。
- 角色槽不会生成 Basic。

角色牌商店稀有度基础权重：Common 54%、Uncommon 37%、Rare 9%；进阶 7 起为 Common 58.5%、Uncommon 37%、Rare 4.5%。商店读取当前 Rare 动态偏移，但使用“不改变未来概率”的判定，因此商店本身不会推进或重置战斗奖励的 Rare 保底。

卡牌基础价为 Common 50、Uncommon 75、Rare 150；无色牌再乘 1.15，并有约 ±5% 波动；促销牌半价。价格还可被遗物和 Hook 修改。

### 3.5 各稀有度从哪里来

| 稀有度 | 常规来源与限制 |
|---|---|
| Basic | 初始牌组和角色标志性基础牌；正常战斗奖励与普通商店稀有度生成会排除它 |
| Common | 普通/精英战斗奖励、商店及明确生成角色池卡牌的效果 |
| Uncommon | 同上，基础概率较低；也是一个商店无色槽的固定稀有度 |
| Rare | 普通/精英的动态低概率、Boss 卡牌奖励、商店及明确给 Rare 的效果 |
| Ancient | 不进入正常战斗奖励；由 Ancient 专属遗物或明确指定的特殊来源给予 |
| Event | 由事件或明确指定效果给予，没有全局“Event 掉率” |
| Token | 战斗中生成的临时功能牌，例如 Shiv、Soul、Sovereign Blade；通常不进入普通奖励和持久牌组 |
| Status | 敌人或卡牌在战斗中生成，没有普通掉率 |
| Curse | 进阶、事件、遗物或其他明确效果加入，没有普通掉率 |
| Quest | 特殊任务/事件直接创建，没有普通掉率 |

Ancient 卡的典型来源包括：

- Ancient 遗物 `DustyTome`：从角色卡池的 Ancient 候选中选一张，加入升级版。
- `ArchaicTooth`：把角色标志性 Basic 牌定向转成对应 Ancient 牌，例如 `Bash → Break`、`Neutralize → Suppress`、`Unleash → Protector`、`FallingStar → MeteorShower`、`Dualcast → Quadcast`。

源码中某些“uniform”辅助方法是从符合筛选的卡牌模型中均匀选，而不是先让 Common/Uncommon/Rare 各占三分之一。反之，某个非战斗 `RollWithBaseOdds` 的实际阈值顺序又会使结果不同于常量名称直觉：普通条件约为 Rare 3%、Uncommon 34%、Common 63%；进阶 7 起约为 Rare 1.49%、Uncommon 35.51%、Common 63%。设计 MOD 时必须确认调用的具体生成入口和候选池，不能只看方法名。

## 4. 先古之民（Ancients）

### 4.1 共同规则与出现时间

- 先古之民是每幕入口的特殊节点，不等同于 `Ancient` 稀有度卡牌。
- 进入时先回复生命。进阶 0–1 回复全部已损生命；进阶 2 起只回复已损生命的 80%。
- 通常展示 3 个选项，选择一个 Ancient 遗物或对应特殊内容。
- 第一幕：Neow；尚未解锁 Neow Epoch 的首次流程没有该入口服务。
- 第二幕：Orobas、Pael、Tezcatara 中随机一位，受 Epoch 解锁条件影响。
- 第三幕：Nonupeipe、Tanx、Vakuu 中随机一位。
- Darv 是可在第二或第三幕加入候选的共享先古之民，并非每局保证出现。
- `TheArchitect` 是第三幕 Boss 后的结局事件，不是提供三选一遗物的普通先古之民节点。

Darv 的分配先于具体先古之民选择：仅考虑一个共享先古之民时，约 1/2 被分配到第二幕；若未分配，第三幕再有 1/2 概率获得，即约 1/4 分配到第三幕，另约 1/4 本局不分配。即使分配到某幕，Darv 仍只是该幕可用先古之民候选之一，并非必然被抽中。

### 4.2 各先古之民提供的内容

以下保留源码模型名，便于核对实际资源和本地化。

#### Neow

从兼容候选中提供两个正面 Ancient 遗物和一个带明显代价的 Cursed Ancient 遗物。

- 正面池包括：`ArcaneScroll`、`BoomingConch`、`FishingRod`、`GoldenPearl`、`Kaleidoscope`、`LeadPaperweight`、`LostCoffer`、`MassiveScroll`、`NeowsTorment`、`NewLeaf`、`PhialHolster`、`PreciseScissors`、`ScrollBoxes`、`WingedBoots`，以及按条件加入的 `LavaRock`、`SmallCapsule`、`NutritiousOyster`、`StoneHumidifier`、`NeowsTalisman`、`Pomander`。
- 代价池包括：`CursedPearl`、`DowsingRod`、`HeftyTablet`、`LargeCapsule`、`LeafyPoultice`、`NeowsBones`、`NeowsSacrifice`、`PrecariousShears`、`SilkenTress`、`SilverCrucible`。

首次流程和特定修正可以替换或过滤选项。

#### Orobas

三个选项分别来自三个池：

1. `ElectricShrymp`、`GlassEye` 与一个按账号/角色条件生成的第三候选之间均匀选择；第三候选本身有 1/3 为 `PrismaticGem`，否则为另一个已解锁角色对应的 `SeaGlass`。
2. `AlchemicalCoffer`、`Driftwood`、`RadiantPearl`、`SandCastle`。
3. 合法的 `TouchOfOrobas` 或 `ArchaicTooth`；候选不足时有兜底逻辑。

#### Pael

1. `PaelsFlesh`、`PaelsHorn`、`PaelsTears`。
2. 加权池：符合条件的 `PaelsWing`、`PaelsClaw`、`PaelsTooth` 各有两份权重，`PaelsGrowth` 只有一份。
3. `PaelsEye`、`PaelsBlood`，没有事件宠物时还可出现 `PaelsLegion`。

#### Tezcatara

1. `VeryHotCocoa`、`YummyCookie`；牌组含基础 Strike 时加入 `NutritiousSoup`。
2. `BiiigHug`、`Storybook`、`ToastyMittens`。
3. `GoldenCompass`、`PumpkinCandle`、`ToyBox`、`SealOfGold`。

#### Nonupeipe

从 `BlessedAntler`、`BrilliantScarf`、`DelicateFrond`、`DiamondDiadem`、`FurCoat`、`Glitter`、`JewelryBox`、`LoomingFruit`、`SignetRing` 中洗牌取 3。若牌组中至少有 4 张可被 Swift 附魔的牌，`BeautifulBracelet` 也进入候选。

#### Tanx

从 `Claws`、`Crossbow`、`IronClub`、`MeatCleaver`、`Sai`、`SpikedGauntlets`、`TanxsWhistle`、`ThrowingAxe`、`WarHammer` 中洗牌取 3。若至少有 3 张可被 Instinct 附魔的牌，`TriBoomerang` 也进入候选。

#### Vakuu

三个选项分别来自：

1. `BloodSoakedRose`、`WhisperingEarring`、`Fiddle`。
2. `PreservedFog`、`SereTalon`、`DistinguishedCape`；其中 `SereTalon` 的选择代价包含 9 点最大生命。
3. `ChoicesParadox`、`MusicBox`、`LordsParasol`、`JeweledMask`。

#### Darv

从符合当前幕条件的 Boss 遗物集合里抽候选：`Astrolabe`、`BlackStar`、`CallingBell`、`EmptyCage`、`PandorasBox`、`RunicPyramid`、`SneckoEye`；`Ectoplasm` 与 `Sozu` 仅在对应的早期幕条件下进入，`PhilosophersStone` 与 `VelvetChoker` 要求不早于第二幕。清牌类修正会排除 `PandorasBox`。

Darv 有 50% 概率提供“两个 Boss 遗物 + `DustyTome`”，否则提供三个 Boss 遗物。

## 5. 战斗资源与回合结算

### 5.1 玩家每个普通回合基础获得什么

- 能量：回合开始时通常把当前能量**重置为最大能量**，基础为 3；并不是“在旧能量上再加 3”。若 Hook 阻止重置，才可能改为增加。
- 抽牌：基础抽 5 张，可被遗物、能力、状态和 Hook 修改。
- 格挡：在自己一侧回合开始时清除，而不是回合结束时清除；玩家的战斗第一回合不执行这次清除。
- 敌人意图：普通玩家回合开始阶段，敌人为下一次敌方行动准备意图。额外玩家回合不会重新准备敌人意图。
- 星星、充能球等专属资源按各自规则处理，并不存在一个统一的“所有资源回合结束归零”。

手牌硬上限为 10。若先天牌数量超过正常抽牌数，首回合抽牌数至少提高到先天牌数，但仍受 10 张手牌上限约束。

### 5.2 战斗开始

1. 为每位玩家创建新的 `PlayerCombatState`；能量与 Stars 等战斗资源从该战斗的初值开始。
2. 持久牌组中的牌被复制为战斗中的可变实例，放入抽牌堆并进行初始洗牌。
3. 创建玩家、敌人、宠物和充能球队列等战斗对象。
4. 执行 `BeforeCombatStart` 一类初始化 Hook、遗物和能力。
5. 进入第一个玩家回合。

持久牌组实例、战斗实例与 `ModelDb` 的规范模板不是同一个对象。战斗中的变化默认不会直接修改持久牌组，除非效果明确执行牌组级操作。

### 5.3 玩家回合开始的精确顺序

以下是当前 `CombatManager` 的主要顺序；同一阶段内部仍可能有 Hook 的 Early/Late 分层：

1. 设置当前侧与阶段，确定本回合参与者；额外回合可能只有部分玩家参与。
2. 对本侧生物执行 `BeforeTurnStart`，并把每个 Power 当前层数记录为该回合开始快照。
3. 执行 `BeforeSideTurnStart`。
4. 普通玩家回合让敌人 `PrepareForNextTurn`，即生成/展示下一意图；额外玩家回合跳过。
5. 显示回合开始阶段和必要动画。
6. 本侧生物执行 `AfterTurnStart` 并清除格挡；玩家战斗第一回合保留“无需清除”的例外。
7. 执行 `AfterBlockCleared`。
8. 为每位参与玩家执行 `SetupPlayerTurn`：
   1. 能量重置为最大能量；执行 `AfterEnergyReset`，再执行 `AfterEnergyResetLate`。
   2. 执行 `BeforeHandDraw`，再执行 `BeforeHandDrawLate`。
   3. 计算基础 5 张及全部抽牌修正。
   4. 首回合调整 Bottom/Innate 的抽牌堆位置，并把抽牌量提高到足以抽到先天牌，最多 10。
   5. 抽牌。
   6. 执行 `AfterPlayerTurnStartEarly`、`AfterPlayerTurnStart`、`AfterPlayerTurnStartLate`。
9. 执行 `AfterSideTurnStart`，再执行 `AfterSideTurnStartLate`。
10. 充能球队列执行 `AfterTurnStart`；Plasma 的被动能量在这里结算。
11. 进入自动出牌前阶段，再进入玩家可操作的 Play 阶段。

重要结论：当前版本**确实有** `BeforeHandDraw` Hook，而且它位于能量重置之后、实际抽牌之前。不能用“回合开始”四个字代替精确时点。

### 5.4 抽牌与洗牌

- 每抽一张前都会检查战斗是否结束、是否允许抽牌以及手牌是否已满。
- 抽牌堆为空而弃牌堆非空时，弃牌堆自动洗入抽牌堆，然后继续抽。
- 洗牌会调用 `ModifyShuffleOrder` 与 `AfterShuffle` 等 Hook。
- 抽到每一张牌后依次记录历史、调用 `AfterCardDrawn`，再触发卡牌自身的 drawn 行为。
- 抽牌数为小数时，正数会向上取整；最终仍受手牌 10 张上限和牌堆可用数量限制。

### 5.5 玩家回合结束：Phase One

按下结束回合并等待已排队动作完成后，先进入允许产生选择和异步动作的第一阶段：

1. `BeforeSideTurnEndVeryEarly`。
2. `BeforeSideTurnEndEarly`。
3. `BeforeSideTurnEnd`。
4. 充能球队列执行 `BeforeTurnEnd`；Lightning、Frost、Dark、Glass 等多数被动在此结算。
5. 扫描当前手牌：
   - 有 `HasTurnEndInHandEffect` 的牌保留到稍后的手牌内回合结束效果，不先走 Ethereal 分支。
   - 其他 Ethereal 牌先被消耗。
6. 逐张执行 `OnTurnEndInHand`。
7. 再次检查胜负和战斗结束状态。
8. 执行 `BeforeFlush`。

需要让玩家选牌、产生会等待的动画或执行复杂动作的效果，应放在允许选择的 Phase One，而不是清理阶段。

### 5.6 玩家回合结束：Phase Two

随后进入纯清理阶段：

1. 读取当前手牌。满足 `ShouldRetainThisTurn` 的牌留下，其他牌移到弃牌堆；若 Hook 使整个 Flush 失效，则全部留下。
2. 执行 `AfterFlush`。
3. 每张战斗牌执行 `EndOfTurnCleanup`，清除 `ExhaustOnNextPlay`、仅本回合的 Retain/Sly、只持续本回合的费用修正等临时字段。
4. 执行 `AfterSideTurnEnd`，再执行 `AfterSideTurnEndLate`。
5. 切换到敌方侧，并通知生物发生 Side Switch。

能量并不会在这一刻统一写成 0；普通情况下它在下一次玩家回合开始时被最大能量覆盖。格挡也不是这里清除，而是在该生物下一次开始自己一侧回合时清除。

### 5.7 敌人回合

1. 同样记录 Power 回合开始快照，执行 `BeforeSideTurnStart`。
2. 敌人清除自己的格挡，执行 `AfterBlockCleared`。
3. 执行 `AfterSideTurnStart` 与 Late 阶段。
4. 敌人按战斗状态中的顺序依次执行 Intent/TakeTurn；每个敌人行动后都检查胜负。
5. 执行敌方 `BeforeSideTurnEnd` 系列。
6. 战斗卡牌再次执行结束清理，以清掉在敌人回合中产生的仅本回合临时字段。
7. 执行敌方 `AfterSideTurnEnd` 与 Late，切回玩家侧。
8. 普通轮转使 Round 增加，并使玩家的 TurnNumber 前进。

### 5.8 额外玩家回合

- 可以只让部分玩家参加。
- 仍会重置/补充能量并抽牌。
- 不重新让敌人准备意图。
- 玩家 TurnNumber 会增加，但正常的全局 Round 不按普通敌我轮转方式增加。

### 5.9 Hook 分层速查

| 逻辑位置 | 顺序 |
|---|---|
| 能量重置后 | `AfterEnergyReset` → `AfterEnergyResetLate` |
| 抽牌前 | `BeforeHandDraw` → `BeforeHandDrawLate` |
| 玩家开局后 | Early → Normal → Late |
| 一侧开局后 | Normal → Late |
| 一侧结束前 | VeryEarly → Early → Normal |
| 一侧结束后 | Normal → Late |

不要依赖不同 Model 在普通 Hook 枚举中的偶然顺序；如果两个效果有严格先后关系，应选择已有的阶段、Early/Late 接口或显式排队。

## 6. 进阶 0–10

进阶效果累积：选择进阶 N 会同时启用 1 到 N 的全部效果。

| 等级 | 内部名 | 当前机制 |
|---:|---|---|
| 0 | None | 无进阶惩罚 |
| 1 | SwarmingElites | 每幕可变精英配额从 5 提高到 8 |
| 2 | WearyTraveler | 先古之民只回复已损生命的 80%；Neow 流程中等价于以约 80% 最大生命开始 |
| 3 | Poverty | 战斗金币和宝箱金币乘 0.75 |
| 4 | TightBelt | 基础药水栏从 3 降为 2 |
| 5 | AscendersBane | 第一层向牌组加入 `AscendersBane` 诅咒 |
| 6 | Inflation | 商店移除牌基础价格 75 → 100；每次已移除导致的后续涨价增量 25 → 50 |
| 7 | Scarcity | 降低 Rare 概率、Rare 保底增长和奖励牌升级率，详见卡牌章节 |
| 8 | ToughEnemies | 提升敌人的耐久；数值按具体怪物硬编码，不是统一生命百分比 |
| 9 | DeadlyEnemies | 提升敌人的攻击、力量或行动效果；按具体怪物/招式硬编码，不是统一伤害百分比 |
| 10 | DoubleBoss | 最终幕连续安排两个不同 Boss |

### 6.1 如何提高进阶

- 最高进阶为 10。
- 使用某角色在其当前最高可用进阶完成一局符合条件的标准胜利，最高可选进阶提高 1。
- 单人进阶进度按角色记录；多人有共享的进阶进度处理。
- Daily、Custom 不推进该标准进度。
- 在进阶 0 获胜可解锁进阶 1，之后逐级提升，不能一次跨多级。

## 7. 遗物如何工作与如何获取

### 7.1 运行机制

- 遗物是整局持久存在的可变 Model 实例，通过 Hook 监听战斗、房间、奖励、商店等事件。
- 获得遗物后执行 `AfterObtained`；重复、替换和禁用由具体遗物及生成器规则处理。
- 稀有度为 `Starter`、`Common`、`Uncommon`、`Rare`、`Shop`、`Event`、`Ancient`，另有 `None`。
- 普通遗物稀有度判定为 Common 50%、Uncommon 33%、Rare 17%。
- 各稀有度使用预先洗牌的 grab bag。大部分奖励从前端取，商店从后端取；已获得/已看见内容会从候选中移除，所以常规情况下不会反复出现同一遗物。
- 对应稀有度耗尽时有后备稀有度顺序；全部不可用时以 `Circlet` 兜底。

### 7.2 获取来源

- 初始遗物：角色开局自带。
- 精英：每次正常精英奖励固定包含 1 个遗物。
- 宝箱：单人固定宝箱房提供一个随机普通稀有度遗物，可跳过；同时含 42–52 金币，进阶 3 起乘 0.75。
- 商店：通常有 3 个遗物槽，其中两个为普通稀有度随机遗物，一个为 Shop 遗物；价格在模型基础价附近约 ±15%。
- 先古之民：主要提供 Ancient 遗物。
- 事件：可直接提供 Event、Ancient 或其他指定遗物。
- 具体卡牌、药水、遗物与修正也可以创建或替换遗物。

多人宝箱会为玩家生成候选并进行投票；冲突可进入石头剪刀布分配，不等同于每人无条件拿一个独立遗物。

当前普通 Boss 奖励集没有《杀戮尖塔 1》式固定 Boss 遗物选择屏；Boss 遗物主要见于 Darv 等明确来源。不要凭前作经验假定存在该流程。

## 8. 药水如何工作与如何获取

### 8.1 基础规则

- 基础药水栏为 3；进阶 4 起为 2，可被遗物等继续修改。
- 药水通常是一次性物品，使用时先从药水栏移除再结算。
- 药水可手动丢弃；是否能在战斗中生成、是否只能在战斗使用、是否需要目标由具体药水模型决定。
- 生成池由角色药水与已解锁共享药水组成。战斗内随机生成会排除明确标记为不能在战斗内生成的治疗、复活或受限药水。

### 8.2 稀有度与获取

随机药水稀有度：Common 65%、Uncommon 25%、Rare 10%。

来源包括：

- 普通敌人、精英和非最终 Boss 的战后药水概率。
- 商店固定展示 3 个互不重复的药水；基础价 Common 50、Uncommon 75、Rare 100，并有约 ±5% 波动。
- 事件、遗物、卡牌或其他明确效果。

战后药水使用动态概率：

- 当前值初始为 40%。
- 普通战斗使用当前值。
- 精英使用当前值再加 12.5 个百分点。源码注释写“25% bonus”，但实际表达式为该 bonus 的一半；应以代码为准。
- 命中药水奖励后当前值降低 10 个百分点；未命中则提高 10 个百分点。
- Hook 可以强制、阻止或修改。

## 9. 商店、休息处和房间服务

### 9.1 商店

标准商店通常包含：

- 5 张角色牌，1 张促销。
- 2 张无色牌。
- 3 个遗物，其中 1 个 Shop 遗物。
- 3 瓶药水。
- 一次移除牌服务；使用后本局后续移除价格上涨。

进阶 6 直接改变的是移除牌价格与涨价步长，不代表所有商品统一涨价。

### 9.2 休息处

基础选项：

- Heal：回复最大生命的 30%，可被 Hook/遗物修改。
- Smith：从持久牌组中选择 1 张可升级牌并升级；可取消。
- 多人还有 Mend。

遗物和任务牌可加入 `Dig`、`Lift`、`Cook`、`Clone`、`Kindle`、`Hatch` 等额外选项。休息处“能做什么”因此取决于当前持有内容，不是只有两种行为。

### 9.3 宝箱、事件和战斗房

- 宝箱：见遗物章节；固定宝箱行与问号转出的宝箱都进入宝箱房逻辑，但具体修正可改变内容。
- 事件：从幕内与共享候选中按资格抽取；事件自己决定选项、代价和奖励，没有统一的“事件奖励概率”。
- 普通敌人：金币 + 药水判定 + 卡牌三选一。
- 精英：金币 + 药水判定 + 卡牌三选一 + 遗物。
- 非最终 Boss：金币 + 药水判定 + 全 Rare 卡牌三选一。

## 10. 五名原版角色的原生机制

以下描述“原生身份”，不是引擎访问权限。无色牌、遗物、事件和 MOD 可能让其他角色接触类似机制。

### 10.1 铁甲战士（Ironclad）

- 最大生命 80。
- 初始牌组 10 张：5 Strike、4 Defend、1 Bash。
- 初始遗物 `BurningBlood`：每场战斗结束回复 6 生命。
- 没有像充能球或 Stars 那样由角色类硬编码的独立第二资源。
- 原生卡池核心包括自伤与生命交换、Strength、Vulnerable、Exhaust 和高额攻击；Burning Blood 提供跨战斗续航。

### 10.2 静默猎手（Silent）

- 最大生命 70。
- 初始牌组 12 张：5 Strike、5 Defend、1 Neutralize、1 Survivor。
- 初始遗物 `RingOfTheSnake`：战斗首回合额外抽 2，通常为 7 张。
- 原生系统包括 Poison、Shiv、弃牌与 `Sly`。
- `Sly` 的关键时序：一批牌被弃掉后，带 Sly 的牌自动打出；弃牌效果产生的替代抽牌先结算，然后才自动打出 Sly 牌。实现弃牌联动时不能颠倒此顺序。

### 10.3 亡灵契约师（Necrobinder）

- 最大生命 66。
- 初始牌组 10 张：4 Strike、4 Defend、1 Bodyguard、1 Unleash。
- 初始遗物 `BoundPhylactery`：战斗开始召唤 1 生命的 Osty；后续玩家回合能量重置时若 Osty 不存在，会再次召唤。
- `Summon` 在 Osty 不存在/死亡时创建或复活并赋予相应生命；Osty 存活时则提高其最大生命。
- `DieForYou` 可把玩家受到的未被格挡的强制攻击伤害转移给存活的 Osty。
- 原生牌可命令 Osty 攻击、牺牲或成长。
- `Soul` 是 0 费、Exhaust 的 Token，打出抽 2。
- `Doom` 达到目标当前生命时会杀死目标；敌方生物的 Doom 在敌方侧结束前结算，玩家侧生物的 Doom 在玩家侧结束后结算，二者刻意不完全对称。

### 10.4 故障机器人（Defect）

- 最大生命 75。
- 初始牌组 10 张：4 Strike、4 Defend、1 Zap、1 Dualcast。
- 初始遗物 `CrackedCore`：首回合 Channel 1 Lightning。
- 基础充能球槽为 3，队列硬上限为 10。
- Channel 时若槽已满，先 Evoke 队首充能球，再把新球放入队尾。
- 没有任何槽的非 Defect 角色首次 Channel 时，系统会先补 1 个槽。
- Lightning：回合结束被动对随机敌人造成 3，Evoke 造成 8。
- Frost：回合结束被动获得 2 格挡，Evoke 获得 5。
- Dark：从 6 起步，每次被动再增长 6，Evoke 攻击生命最低的敌人。
- Plasma：玩家回合开始的充能球阶段提供 1 能量，Evoke 提供 2 能量。
- Focus 等效果可修改充能球数值。大多数球被动在 `BeforeTurnEnd`，Plasma 在 `AfterTurnStart`，不能统一当作同一时点。

### 10.5 储君（Regent）

- 最大生命 75。
- 初始牌组 10 张：4 Strike、4 Defend、1 FallingStar、1 Venerate。
- 初始遗物 `DivineRight`：进入每个战斗房时获得 3 Stars。
- Stars 是战斗内的第二资源，不会像能量那样在普通回合结束清零；新战斗建立新的 `PlayerCombatState`，因此不会跨战斗自然保留。
- 部分牌消耗 Stars。只有明确启用相应 Hook 的效果才能用额外能量补缺少的 Stars，不能把“2 Stars = 1 能量”当成所有卡牌的通用规则。
- `Forge`：第一次在没有未消耗 Sovereign Blade 时创建一张；每次 Forge 提高所有相关 Sovereign Blade 的伤害记录，包括已进入消耗堆、仍用于追踪数值的实例。
- `SovereignBlade` 是 2 费、Retain、基础 10 伤害的 Token，升级后费用为 1。
- `FallingStar` 为 0 能量并获得 2 Stars；`Venerate` 为 1 能量并获得 2 Stars。

## 11. 玩家最多/最少可能有多少牌

“有多少牌”必须区分手牌、持久牌组、战斗临时牌和一局新增牌：

| 口径 | 下限 | 上限 |
|---|---:|---:|
| 手牌 | 0 | **10，硬上限** |
| 引擎层持久牌组 | 0；容器允许空牌组 | **没有硬编码最大值** |
| 标准角色初始牌组 | 10；Silent 为 12 | 不适用 |
| 从可跳过的普通卡牌奖励新增 | 0 | 每个奖励界面至多 1；整局总数取决于路线和来源 |

源码的持久 `CardPile` 没有牌组张数上限检查，成就/徽章还明确识别 100 张以上牌组，所以“最多 100 张”是错误结论。理论上限受可用内存、内容、事件、复制效果、修正和 MOD 限制，而不是一个引擎常量。

引擎同样支持 0 张持久牌组；但标准无修正内容能否在某个具体种子中把 10/12 张初始牌全部移除，是路线、金币、事件资格和不可移除牌共同决定的问题，不应把“容器允许 0”写成“每局都能稳定删到 0”。

一局中“最多获得多少张新牌”也没有独立全局常量：战斗奖励每次至多取 1 张，但商店、事件、遗物、任务、复制、生成 Token 和 MOD 都有不同计数口径。做平衡统计时应明确只统计持久牌组新增，还是也统计战斗生成牌。

## 12. 已在开发/维护中纠正的机制误区

这些项目经验应当作为今后实现的检查表：

1. **规范模板不是可操作实例。** `ModelDb` 中的 canonical model 是不可变模板。选牌、幻化、附魔和战斗操作必须使用玩家拥有的可变战斗/牌组实例。把模板传给选择器或变形命令可能表现为“成功选中但没有变化”。
2. **选择时可用不等于结算时仍可用。** 选牌条件和实际执行入口都要校验；异步等待期间牌堆、冻结状态、所有者或战斗状态都可能变化。
3. **冻结要在变化的总入口阻止变化。** 只在 UI 或单张卡的执行代码检查，会漏掉其他幻化来源；应在真正改变卡牌的 choke point 检查。
4. **一张卡只有一个 Affliction 槽。** `Card.Affliction` 是单数。自定义冻结/灼热等应使用 RitsuLib 的 Affliction 接口和自己的视觉资源，不应复用 Soulbound 的视觉，也不应为视觉效果自行 patch 游戏 UI。
5. **状态提供的 Retain 必须只管理自己的贡献。** 冻结给予的 Retain 在该牌打出、冻结移除时一并撤销，但不能删除卡牌自身或其他来源的 Retain。
6. **当前版本有抽牌前 Hook。** `BeforeHandDraw` 位于能量重置之后、实际抽牌之前。早期“没有抽牌前 Hook”的假设已被源码推翻。
7. **回合结束分两阶段。** Phase One 允许选择和等待；Phase Two 负责保留/弃置及临时字段清理。把需要选择的效果放入 Phase Two 容易卡住流程。
8. **致死后必须重新检查战斗状态。** Hook 或排队动作可能在 `await` 期间杀死最后一个敌人。之后继续派发、复制、幻化或等待无效动作可能造成击杀后卡死；应检查 `IsOverOrEnding` / `IsInProgress`，死亡专用直接派发 Hook 除外。
9. **地图没有一个静态总体概率。** 固定节点、配额、路径和问号动态权重必须分开分析。
10. **不要相信过时注释胜过可执行代码。** 当前没有 Warden 进阶；精英药水“25% bonus”的注释实际只加 12.5 个百分点。
11. **不要只看概率辅助方法的名称。** `RollWithBaseOdds` 的阈值顺序和 uniform 候选选择都可能与名称引发的直觉不同，要查看实际分支和输入池。
12. **稳定 ID 与可变对象身份不同。** 内容 ID 应长期保持稳定以兼容存档；同一 ID 的模板、牌组实例、战斗副本仍是不同对象。

## 13. 敌人的意图与下一意图计算

### 13.1 意图种类

`IntentType` 当前包含：

| 意图 | 表示的信息 |
|---|---|
| Attack | 单段或多段攻击；面板数值已经经过当前 Strength、Weak、Vulnerable 等伤害 Hook 预览 |
| Buff | 给自己或同伴施加正面 Power，或进入强化阶段 |
| Debuff | 给玩家施加普通负面 Power |
| DebuffStrong | 与 Debuff 使用同一图标资源，但模型把它标为强 Debuff，便于界面和内容判断 |
| Defend | 获得格挡或其他防御效果 |
| Escape | 逃离战斗 |
| Heal | 回复或复活 |
| Hidden | 不向玩家公开实际行动 |
| Summon | 召唤新的敌人或单位 |
| Sleep | 睡眠/等待，通常可由伤害或计时唤醒 |
| Stun | 眩晕，本次不执行正常行动，然后回到指定后续状态 |
| StatusCard | 向牌堆加入状态牌；图标可显示张数 |
| CardDebuff | 偷取、附魔、改变或妨碍玩家已有卡牌 |
| DeathBlow | 自杀式攻击图标；数值仍来自攻击伤害计算 |
| Unknown | 无法归入上述公开类型的未知行动 |

一个 `MoveState` 可以同时带多个意图，例如 Attack + Debuff、Attack + Defend、StatusCard + Buff。意图是对将执行动作的声明，不是动作本身；真正效果由该 Move 的 `PerformMove` 方法实现。因此只检查第一个图标会漏掉复合行动，只检查图标又可能漏掉动作方法中的条件分支。

攻击意图的单段数值会经过 `Hook.ModifyDamage(... ValueProp.Move ...)`，最低显示为 0。多段攻击的总伤害为修改后的单段伤害乘段数；Strength 对每一段分别生效，所以多段攻击从 Strength 获得的成长显著更快。

### 13.2 下一意图何时计算

- 普通玩家回合开始时，敌人在 `PrepareForNextTurn` 中调用 `Monster.RollMove`，使用 `RunRng.MonsterAi` 决定下一行动，然后刷新意图显示。
- 第一次 Roll 若初始状态本身就是 Move，会直接使用该初始 Move，不先跳到 Follow-up。
- 敌人完成行动后，状态机记录该 Move 已执行；下一次 Roll 才从 Follow-up 或分支状态继续。
- 额外玩家回合不会让敌人重新 Roll，因此看到的还是原意图。
- Stun 可以临时强制 `STUNNED` Move，并在完成后返回预先记录的后续状态。
- 生命阈值、场上同伴、已使用次数、上一次行动、冷却、Power、Boss 阶段和玩家状态都可能改变分支条件。

### 13.3 三种状态节点

1. `MoveState`：真正可执行的行动，含一个或多个意图以及 Follow-up。
2. `ConditionalBranchState`：按添加顺序检查条件，选择**第一个为 true** 的状态；它不是随机抽选。
3. `RandomBranchState`：只保留当前合法分支，按权重抽取。

随机分支的算法为：计算全部合法分支权重和，在 `[0, 总权重)` 均匀取一个浮点数，然后按分支顺序减去权重，首次减到 `<= 0` 的分支胜出。分支还可设置：

- `CanRepeatForever`：可无限连续出现。
- `CannotRepeat`：不能与上一次同动作连续。
- `CanRepeatXTimes`：最多连续 X 次。
- `UseOnlyOnce`：整场战斗只进入一次。
- `cooldown`：最近若干个实际 Move 中出现过则权重变为 0。

状态机的 `StateLog` 参与这些限制。随机意图不是每回合从所有招式等概率重抽；固定开局、循环 Follow-up、冷却和历史限制通常比原始权重更重要。

## 14. 敌人生命与每回合期望伤害

### 14.1 统计口径

敌人没有一张全局“第一幕造成 X 伤害”的表。为了得到可复现、适合 MOD 平衡的基线，以下统计采用：

- 按 Encounter，而不是把所有单体怪物直接平均。
- 同一类别的已解锁 Encounter 模型等权；随机阵容对每个 Encounter 枚举 200 个确定 Seed。
- HP 是开场所有敌人的总初始生命期望；范围也是 Encounter 总生命范围，而不是单只怪物范围。
- “伤害/回合”是每个阵容**前 4 次敌方行动**的攻击意图基础总伤害均值，非攻击行动计 0。
- 不计玩家格挡、Weak/Vulnerable、玩家人数缩放、战斗中后来获得的 Strength、Ritual、临时增伤、召唤物未来行动、反伤、状态牌价值和条件性致命效果。
- 进阶 8 列只体现 `ToughEnemies` 的生命变化；进阶 9 列体现 `DeadlyEnemies` 的基础攻击数值/段数变化。表中“进阶 9”统计运行在累积进阶环境，但进阶 10 的第二 Boss 不改变单场 Boss 模型数值。

因此它是“开战早期意图压力”的期望基线，不是玩家实际掉血，也不是完整 Boss 战的伤害模拟。越依赖成长、召唤、阶段切换或玩家行为的敌人，实际值与该基线的偏差越大。

### 14.2 各幕统计

| 幕与类别 | 进阶 0 初始总 HP：均值（Encounter 范围） | 进阶 8+：均值（范围） | 进阶 0 前四行动伤害/回合 | 进阶 9+ 前四行动伤害/回合 |
|---|---:|---:|---:|---:|
| Overgrowth 弱敌人 | 47.8（39–56） | 50.2（41–58.5） | 6.1 | 7.3 |
| Overgrowth 普通敌人 | 74.1（42–95） | 78.3（45–99.5） | 11.3 | 13.2 |
| Overgrowth 精英 | 90.7（62.5–127） | 96.3（67–132） | 9.2 | 11.0 |
| Overgrowth Boss | 244.0（173–307） | 256.3（183–324） | 12.7 | 13.9 |
| Underdocks 弱敌人 | 45.3（38–52） | 48.4（41.5–56） | 9.3 | 10.6 |
| Underdocks 普通敌人 | 66.5（48–91.5） | 70.0（52–93.5） | 10.2 | 11.8 |
| Underdocks 精英 | 109.7（75–140） | 116.0（80–150） | 13.4 | 14.9 |
| Underdocks Boss | 224.3（211–240） | 234.7（221–250） | 8.1 | 8.9 |
| Hive 弱敌人 | 79.8（75.1–87） | 83.7（77.7–92） | 14.6 | 16.4 |
| Hive 普通敌人 | 126.7（104–174） | 131.3（108–179） | 13.9 | 15.7 |
| Hive 精英 | 145.0（129–161） | 157.7（147–171） | 17.6 | 20.5 |
| Hive Boss | 369.3（321–408） | 389.3（341–428） | 14.7 | 16.5 |
| Glory 弱敌人 | 119.5（96–162） | 132.0（108–172） | 20.2 | 24.2 |
| Glory 普通敌人 | 174.8（134–261） | 186.1（144–281） | 16.1 | 18.3 |
| Glory 精英 | 270.0（234–300） | 289.3（254–320） | 21.8 | 24.7 |
| Glory Boss，开场场面 | 403.7（100–599） | 425.3（111–630） | 18.0 | 20.5 |
| Glory Boss，脚本化有效总 HP | 570.3（512–600） | 600.3（535–636） | 不适用 | 不适用 |

第三幕 Boss 的两个重要修正：

- `QueenBoss` 开场是 Queen + Rocket，所以总 HP 为 400 + 199 = 599；进阶 8 为 420 + 210 = 630。
- `TestSubject` 开场只有 100/111 HP，但死亡后依次以 200/212 和 300/313 HP 复活，完整生命预算为 600/636。只用初始血条会把它误记成全游戏最脆 Boss。

Boss 的阶段转换、随从、复活和成长使“完整战斗平均每回合伤害”依赖玩家击杀顺序和每阶段停留回合数。没有给定玩家策略与战斗长度时，不存在唯一正确值。

### 14.3 循环意图的升级效率

大多数纯固定循环本身不会自动升级，循环成长为 0。成长通常来自 Move 执行时获得 Strength/Ritual、提高内部伤害字段、升级塞入的状态牌，或切换 Boss 阶段。

对一个长度为 `L` 的循环，如果每轮获得 `S` Strength，下一轮所有攻击合计 `H` 段，则：

`下一轮总伤害增量 = S × H`

`平均每回合伤害增量 = S × H / L`

相对升级率还要除以该循环原本的平均伤害，所以它不是全游戏常量。多段攻击使 `H` 很大，是敌人循环后期膨胀的主要来源。

| 代表敌人 | 进阶 0 基础循环 | 每完成一轮后的成长 |
|---|---|---|
| Nibbit | 6 伤 + Hiss 获得 2 Strength + 12 伤；18/3 = 6 DPT | 两次单段攻击共 +4/轮，即 +1.33 DPT，约为初始 DPT 的 22.2% |
| Seapunk | 11 + 2×4 + Buff/Block；19/3 = 6.33 DPT | 获得 1 Strength，下一轮 5 个命中共 +5，即 +1.67 DPT，约 26.3% |
| TurretOperator | 3×5 + 3×5 + Reload；30/3 = 10 DPT | Reload 获得 1 Strength，下一轮 10 个命中共 +10，即 +3.33 DPT，约 33.3% |
| GlobeHead | 13 + 6×3 + 16；47/3 = 15.67 DPT | 每轮获得 2 Strength，完整下一轮 5 个命中共 +10，即 +3.33 DPT，约 21.3% |
| Aeonglass | 22 + 11×2 + 强化；44/3 = 14.67 DPT | 第一次强化获得 3 Strength，之后每次强化量再 +1；DPT 依次额外 +3、+4、+5……，不是固定百分比 |
| DevotedSculptor | 先施加 9 Ritual，之后反复 12 伤 | Ritual 跳过刚施加的那个敌方回合结束，之后每个敌方回合结束 +9 Strength；攻击近似 12、21、30…… |
| WaterfallGiant | 多阶段压力循环 | `CurrentPressureGunDamage` 每次相关行动后固定 +5，另有 Pressurize、治疗和爆炸阶段，不能压成单一三招循环 |

进阶 9 还会同时改变基础伤害、Buff 数量或多段次数。例如 Seapunk 的单段踢 11→13、每轮 Strength 1→2；此时成长速度不只是基础伤害同比增加，而是多段攻击的循环增量也翻倍。

要获得某个 Boss 的真正“长期 DPT”，必须先指定：观察多少回合、玩家何时触发阶段、是否击杀随从、是否计算状态牌未来伤害、是否计算 Vulnerable，以及是否在玩家本应已经死亡后继续模拟。

## 15. Seed 与随机数流

### 15.1 可读 Seed 如何变成数值

- 默认随机 Seed 长 12，字符表为 `0123456789ABCDEFGHJKLMNPQRSTUVWXYZ`，刻意不含 I 和 O。
- 玩家输入会转大写、去除首尾空白，并把 O 替换为 0、I 替换为 1。
- 新版 Seed 字符串以 UTF-8 输入 `XxHash64`，得到 64 位 `RunRngSet.Seed`。
- 以 `old` 开头的兼容 Seed 使用旧确定性哈希，并保留旧版行为。
- 底层 PRNG 为 `xoshiro256**`，用 `SplitMix64` 从 64 位 Seed 初始化四个内部状态。

Seed 不是每次判定直接做一次哈希。每个随机流都维护自己的 xoshiro 状态与调用计数；存档保存完整内部状态和 counter，读档后从保存位置继续。

### 15.2 Run 级独立随机流

每个流以 `runSeed + XxHash64(snake_case 流名)` 独立初始化：

| 流 | 用途 |
|---|---|
| `UpFront` | 开局预生成的幕、地图内容、遭遇顺序、事件顺序、遗物候选等 |
| `Shuffle` | 初始洗牌与弃牌堆洗回抽牌堆 |
| `UnknownMapPoint` | 进入问号时的房间类型 |
| `CombatCardGeneration` | 战斗中生成“新卡牌”的效果 |
| `CombatPotionGeneration` | 战斗中生成药水 |
| `CombatCardSelection` | 从战斗中已存在卡牌里随机选牌 |
| `CombatEnergyCosts` | Confusion、Snecko Eye 等随机费用 |
| `CombatTargets` | 卡牌和攻击的随机目标 |
| `MonsterAi` | 敌人随机意图与部分召唤选择 |
| `Niche` | 不希望干扰主要序列的零散判定，也用于怪物 HP 等 |
| `CombatOrbs` | 随机生成充能球 |
| `TreasureRoomRelics` | 多人宝箱冲突分配 |

所以使用同一局 Seed 不代表所有随机行为消费同一个序列。多调用一次 `CombatCardSelection` 不会改变下一张奖励牌或敌人意图，只会改变该随机选牌流的后续结果。反过来，在错误的流中加入一次 MOD 随机调用，会让该流之后的结果全部错位。

### 15.3 玩家级随机流

每名玩家的基础数值为 `XxHash64(seed字符串) + 玩家槽位索引`，再按名字派生：

- `Rewards`：卡牌奖励、药水奖励内容、是否出现药水等玩家专属奖励。
- `Shops`：商店库存。
- `Transformations`：随机转化的结果。

多人同一局因此共享地图和敌人 AI，但每个槽位的卡牌奖励/商店/转化序列不同。这里使用玩家槽位而非网络 ID，以保证 Daily 等模式的稳定性。

### 15.4 内容专用 Seed

- Encounter 随机阵容：`runSeed + TotalFloor + XxHash64(encounterId)`。
- Event：`runSeed +（共享事件为 0，否则玩家槽位）+ XxHash64(eventId)`。
- `new Rng(player, contentId, mixin)`：`runSeed + 玩家槽位 + XxHash64(contentId) + mixin`。同一内容 ID 若可能在一局重复且希望不同结果，必须提供 mixin。
- 单个怪物还有以 Run Seed、地图坐标、幕索引与 CombatId 派生的内容 RNG；但 Move 状态机的随机分支明确使用共享的 `MonsterAi` 流。
- `Rng.Chaotic` 以当前 Unix 时间初始化，只适用于不影响玩法的视效/音效随机；它不保存，不能用于需要联机同步或读档复现的机制。

同 Seed 复现还要求相同游戏版本、内容注册表、解锁状态、角色/玩家槽位、路径和随机流调用顺序。MOD 增删候选或提前消费随机数后，同一个字符串不保证得到原版相同结果。

## 16. 随机意图、目标、卡牌与奖励如何生成

### 16.1 随机意图

敌人使用 `MonsterAi`。随机分支只在满足重复次数、冷却和条件限制的候选之间按权重抽取；固定 Follow-up 和 Conditional Branch 不消费同样意义上的权重抽选。场上多个敌人依次 Roll，共用 `MonsterAi` 流，因此前一个敌人是否需要随机判定会影响后一个敌人的下一次随机结果。

### 16.2 随机目标

- 自动打出且没有显式目标的 `AnyEnemy` 牌，从当前 `HittableEnemies` 中用 `CombatTargets.NextItem` 均匀选一个。
- `AnyAlly` 在其他存活玩家同伴中均匀选择；没有合法目标则该牌不执行正常效果并进入结果牌堆。
- `AttackCommand.TargetingRandomOpponents` 每一击从当前合法目标均匀选择。
- 默认允许多段随机攻击重复命中同一目标；`allowDuplicates: false` 时已命中的目标会从后续候选移除，若命中数超过合法目标数则属于调用错误。
- “随机敌人”只在调用时的合法集合中均匀，不保证每个稳定 ID 长期轮流命中，也不按生命值加权。

### 16.3 战斗中生成新卡牌

`CardFactory.FilterForCombat` 的默认过滤：

- 必须 `CanBeGeneratedInCombat`。
- 排除 Basic、Ancient、Event。
- 排除不符合当前单人/多人约束的牌。
- 对输入候选去重。

`GetDistinctForCombat` 使用 `CombatCardGeneration` 从过滤后集合无放回取指定数量，所以同一次三选一不会出现重复稳定 ID；`GetForCombat` 则每次有放回抽取，允许生成多张同牌。生成结果是当前 `CombatState` 中归玩家所有的可变实例，不是 `ModelDb` 规范模板。

效果仍可主动传入 Token、Status 或更窄的候选池；“默认过滤”不表示所有生成牌都来自角色 Common/Uncommon/Rare 池。

### 16.4 随机指定已有卡牌

- 从手牌、抽牌堆、弃牌堆等先建立符合效果条件的实例列表，再用 `CombatCardSelection.NextItem` 均匀选择。
- 需要多张不重复目标时，通常先稳定/非稳定洗牌再 `Take(N)`，或使用无放回 `TakeRandom`。
- 候选是当前战斗实例；同稳定 ID 的两张实体牌是两个候选。
- 空列表的 `NextItem` 返回默认值/null。效果必须在选择前和结算前处理空集合与状态变化。
- 随机转化通常使用玩家的 `Transformations` 流，而在战斗内由特定 Power 主动要求的随机变形也可能显式传入 `CombatCardSelection`；应以调用点传入的 RNG 为准。

### 16.5 卡牌奖励

卡牌奖励的完整顺序为：

1. 从角色/无色/指定 CardPool 和解锁状态建立候选。
2. 应用来源、单人/多人约束、Hook 与显式 Flags。
3. 若不是 Uniform，先用玩家 `Rewards` 流结合 `CardRarityOdds` 判定稀有度；战斗奖励会推进 Rare 动态偏移。
4. 若所抽稀有度没有候选，按 `GetNextHighestRarityWithWrapping` 寻找下一种合法稀有度；全部循环后仍无候选才失败。
5. 在最终稀有度候选中用 `Rewards.NextItem` 均匀选稳定 ID。
6. 把已选规范牌加入该奖励界面的 blacklist，生成下一格，因此默认三格互不重复。
7. 为每格单独进行升级判定；升级 Roll 也消费 `Rewards` 流。
8. 最后执行可修改整组奖励选项的 Hook。

奖励的稀有度状态、候选选择和升级共享玩家 `Rewards` 流，但与 `CombatCardGeneration`、`Shuffle`、`MonsterAi` 隔离。查看一张奖励牌本身不会重掷；真正改变生成调用次数、候选池或 Hook 才会改变后续序列。

## 17. 敌人的主要 Buff 与 Debuff

### 17.1 通用高频 Power

| Power | 类型 | 主要效果 |
|---|---|---|
| Strength | Buff；可为负 | 每层给每一段 Powered Attack 加 1 伤害；多段攻击收益按段数放大 |
| Ritual | Buff | 敌方刚施加的当回合结束不触发；之后每次所有者所在一侧结束时获得等于层数的 Strength |
| Artifact | Buff | 抵消一次可见 Debuff 并消耗 1 层；多人基础层数可缩放 |
| Plating | Buff | 开战及敌方回合结束前获得对应格挡，之后可按自身规则衰减 |
| Thorns | Buff | 受到 Powered Attack 前向攻击者造成对应非攻击伤害 |
| Minion | Buff | 主体死亡时该随从通常不阻止战斗结束，并按随从规则清理 |
| Intangible | Buff | 对单次伤害设置极低伤害上限，持续时间由层数/具体来源管理 |
| Burrowed | Buff | 通常保留格挡；格挡被打破后触发钻出等后续行为 |
| Asleep / Slumber | Buff | 等待、倒计时或被未格挡伤害唤醒，常与 Plating 和强制后续 Move 联动 |
| HardToKill | Buff | 给单次伤害设置上限 |
| Illusion | Buff | 管理不可正常命中、死亡后恢复/转阶段或召唤物身份等特殊规则 |
| Stock / Reattach / SteamEruption | Buff | 保留备用个体、重接分节怪物或阻止战斗过早结束，属于具体 Encounter 的生命管理机制 |

敌人源码中直接施加最频繁的是 Strength，其次是玩家身上的 Frail、Weak、Vulnerable，再之后是 Artifact、Plating、Minion 和各种 Boss 专属 Power。

### 17.2 玩家常见 Debuff

| Power/效果 | 主要效果与时点 |
|---|---|
| Weak | 所有者的 Powered Attack ×0.75；通常在敌方侧结束时减少持续时间 |
| Vulnerable | 所有者受到的 Powered Attack ×1.5；通常在敌方侧结束时减少持续时间 |
| Frail | 来自卡牌/怪物 Move 的 Powered Block ×0.75；通常在敌方侧结束时减少持续时间 |
| 负 Strength / Dexterity | 分别降低每段攻击或每次受其影响的格挡；可被 Artifact 阻挡，是否恢复取决于配套临时 Power |
| Poison | 在中毒者所在一侧的 `AfterSideTurnStart` 造成不可格挡、Unpowered 伤害，然后每次触发降低 1；Accelerant 可增加同一开局的触发次数 |
| Constrict | 在中招者所在一侧结束后造成等于层数的 Unpowered 伤害，不自然递减 |
| Shrink | 使所有者的 Powered Attack 伤害乘 0.7；有限层数会在所有者一侧结束后递减，负层数表示无限 |
| Tangled | 给当前无 Affliction 的 Attack 附加 `Entangled`，提高/限制其使用成本，并在所有者一侧结束后移除该 Power |
| Hex | 让符合条件的牌获得 Ethereal/`Hexed` Affliction；影响的是实际卡牌实例 |
| Slow | 玩家每打出一张牌使该敌人本回合受到的 Powered Attack 多 10%，回合切换后计数归零 |
| Surrounded | 根据玩家面向，使来自背后的 Powered Attack ×1.5；面向可被特定操作改变 |
| CardDebuff / StatusCard | 不是单一 Power：可能塞入 Wound、Burn、Wither 等，偷牌，或对手牌施加 Affliction；必须查看具体 Move |

Weak、Vulnerable、Frail 都按敌方侧结束衰减，而不是在玩家回合结束立刻衰减。这使“持续 1”通常仍覆盖玩家接下来的行动或敌人接下来的一轮攻击，具体取决于施加时点。

### 17.3 Buff/Debuff 与意图的关系

- `BuffIntent` / `DebuffIntent` 只说明行动类别，不公开具体 Power 名称和层数。
- AttackIntent 面板会实时读取当前伤害 Hook，所以敌人刚获得 Strength 后，下一次刷新意图数值会变；已经显示的意图也可由 UI 刷新反映 Power 变化。
- Artifact 判断的是目标收到的 Power 是否为可见 Debuff；StatusCard、直接卡牌 Affliction、失去牌或纯 HP 操作不一定会被 Artifact 阻止。
- `DebuffStrong` 目前主要是表达“更危险的 Debuff”意图，不自动给实际效果乘倍；实际数值仍在 Move 方法中。
- 评估一张操纵意图的 MOD 卡时，必须分别确认它读取的是意图图标、`Monster.NextMove`、实际 AttackIntent 伤害，还是最终执行的 Move；这四者不是同一层。

## 18. 维护时的证据索引

关键反编译文件：

| 主题 | 主要源码位置 |
|---|---|
| 整局与跨幕流程 | `Core/Runs/RunManager.cs`、`Core/Runs/RunState.cs` |
| 地图拓扑与配额 | `Core/Map/StandardActMap.cs`、`Core/Map/MapPointTypeCounts.cs`、`Core/Models/ActModel.cs`、`Core/Models/Acts/*.cs` |
| 问号动态概率 | `Core/Odds/UnknownMapPointOdds.cs` |
| 卡牌类型与稀有度 | `Core/Entities/Cards/CardType.cs`、`CardRarity.cs` |
| 卡牌奖励概率 | `Core/Odds/CardRarityOdds.cs`、`Core/Rewards/CardReward.cs`、`Core/Rewards/RewardsSet.cs` |
| 抽牌与牌堆 | `Core/Commands/CardPileCmd.cs`、`Core/Entities/Cards/CardPile.cs` |
| 回合时序 | `Core/Combat/CombatManager.cs`、`Core/Combat/PlayerCombatState.cs`、`Core/Hooks/Hook.cs` |
| 进阶 | `Core/Entities/Ascension/AscensionLevel.cs`、`Core/Entities/Ascension/AscensionManager.cs` 及各模型的 `AscensionManager.HasLevel` 分支 |
| 遗物 | `Core/Factories/RelicFactory.cs`、`Core/Runs/RelicGrabBag.cs`、`Core/Models/Relics/*.cs` |
| 药水 | `Core/Factories/PotionFactory.cs`、`Core/Odds/PotionRewardOdds.cs`、`Core/Models/Potions/*.cs` |
| 商店与休息处 | `Core/Entities/Merchant/*.cs`、`Core/Rooms/MerchantRoom.cs`、`Core/Rooms/RestSiteRoom.cs`、`Core/Entities/RestSite/*.cs` |
| 先古之民 | `Core/Models/Events/Neow.cs`、`Orobas.cs`、`Pael.cs`、`Tezcatara.cs`、`Nonupeipe.cs`、`Tanx.cs`、`Vakuu.cs`、`Darv.cs` |
| 角色基础数据 | `Core/Models/Characters/*.cs`、`Core/Models/Relics/Starter/*.cs` |
| 充能球 | `Core/Commands/OrbCmd.cs`、`Core/Entities/Orbs/*.cs`、`Core/Models/Orbs/*.cs` |
| 敌人意图与状态机 | `Core/MonsterMoves/Intents/*.cs`、`Core/MonsterMoves/MonsterMoveStateMachine/*.cs`、`Core/Models/MonsterModel.cs` |
| 敌人生命、行动与成长 | `Core/Models/Monsters/*.cs`、`Core/Models/Encounters/*.cs`、各幕 `GenerateAllEncounters()` |
| Seed 与随机流 | `Core/Helpers/SeedHelper.cs`、`StringHelper.cs`、`Core/Random/Rng.cs`、`MegaRandom.cs`、`Core/Runs/RunRngSet.cs`、`Core/Random/PlayerRngSet.cs` |
| 随机卡牌和奖励 | `Core/Factories/CardFactory.cs`、`Core/Runs/CardCreationOptions.cs`、`Core/Rewards/CardReward.cs` |
| 敌人 Buff/Debuff | `Core/Models/Powers/*.cs` 及各 Monster 的 Move 实现 |

更新游戏版本后，优先重新核对：`AscensionLevel`、`CombatManager` 回合阶段、奖励概率常量、地图房间配额、角色初始牌组/遗物、问号权重和 Ancient 候选池。若这些发生变化，应先更新本文的证据版本，再据此评估 Illusionist。
