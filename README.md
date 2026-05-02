# VPet.Plugin.VpetAPI - 智能倍率调整版

<div align="center">

![VPet](https://img.shields.io/badge/VPet-1.1.0.50-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-Apache%202.0-green)
![Version](https://img.shields.io/badge/version-10030-orange)

**让高等级桌宠发挥真正实力的智能插件**

[功能介绍](#-功能介绍) • [安装方法](#-安装方法) • [工作原理](#-工作原理) • [使用示例](#-使用示例)

</div>

---

## 📖 功能介绍

这是一个增强版的VpetAPI插件，在原有HTTP API控制功能的基础上，新增了**智能倍率自动调整**功能。

### 核心功能

#### 1. HTTP API控制（原有功能）
- 在本地启动HTTP服务（127.0.0.1:52814）
- 通过POST接口控制桌宠移动、说话、工作、学习、玩耍、睡觉等

#### 2. 智能倍率调整（新增功能）✨
- 🎯 **自动计算最佳倍率**：根据桌宠等级自动选择最高可用倍率
- 💰 **最大化收益**：让高等级桌宠做任何工作都能获得最高收益
- 🤖 **完全自动化**：无需手动调整，插件自动处理

---

## 🎮 工作原理

### VPet的倍率系统

VPet中每个工作都有一个**基础档位**（LevelLimit），通过倍率来调整实际等级上限：

```
实际等级上限 = (LevelLimit + 10) × 倍率
最大倍率 = Min(4000, 桌宠等级) ÷ (LevelLimit + 10)
```

### 示例说明

以**清屏**工作为例（LevelLimit = 10）：

| 桌宠等级 | 最大倍率计算 | 最大倍率 | 实际等级上限 |
|---------|------------|---------|------------|
| 260级 | 260 ÷ 20 | **13** | 20 × 13 = **260** |
| 100级 | 100 ÷ 20 | **5** | 20 × 5 = **100** |
| 50级 | 50 ÷ 20 | **2** | 20 × 2 = **40** |
| 15级 | 15 ÷ 20 | **1** | 20 × 1 = **20** |

### 插件的作用

**原来**：你需要手动在游戏界面调整倍率滑块  
**现在**：插件自动计算并应用最大倍率，让你获得最高收益！

---

## 📊 效果对比

### 场景：260级桌宠做"清屏"工作

#### 不使用插件
- 倍率：x1（默认）
- 等级上限：20
- 收益：💰（低）

#### 使用插件
- 倍率：**x13**（自动）
- 等级上限：**260**
- 收益：💰💰💰💰💰（高）

**收益提升：13倍！**

---

## 🚀 安装方法

### 步骤1：下载
找到 `1230_VpetAPIHttp` 文件夹

### 步骤2：复制
将整个文件夹复制到VPet的mod目录：
```
VPet安装目录/mod/1230_VpetAPIHttp/
```

### 步骤3：重启
重启VPet，看到以下提示即表示成功：
```
✅ VpetAPI HTTP 服务已启动：127.0.0.1:52814
✅ 等级限制自动调整功能已启用
```

---

## 💡 使用示例

### 自动模式（推荐）

**完全自动**，无需任何设置！

1. 右键桌宠 → 选择工作/学习/玩耍
2. 插件自动计算最佳倍率
3. 享受最高收益！

### HTTP API模式

通过程序控制桌宠：

```bash
# 开始工作（自动应用最佳倍率）
curl -X POST http://127.0.0.1:52814/set_work \
  -H "Content-Type: application/json" \
  -d '{"id":"清屏"}'

# 开始学习（自动应用最佳倍率）
curl -X POST http://127.0.0.1:52814/set_study \
  -H "Content-Type: application/json" \
  -d '{"id":"学画画"}'

# 开始玩耍（自动应用最佳倍率）
curl -X POST http://127.0.0.1:52814/set_play \
  -H "Content-Type: application/json" \
  -d '{"id":"跳绳"}'
```

---

## 🛒 商店物品列表 API

插件复用了官方“更好买”窗口的数据源 `mw.Foods` 与 `Food.FoodType` 分类逻辑，支持读取收藏、正餐、零食、饮料、功能性、药品列表。

### 接口列表

| 接口 | 说明 |
|------|------|
| `POST /get_favorite_food_list` | 取收藏列表 |
| `POST /get_meal_list` | 取正餐列表 |
| `POST /get_snack_list` | 取零食列表 |
| `POST /get_drink_list` | 取饮料列表 |
| `POST /get_functional_list` | 取功能性列表 |
| `POST /get_drug_list` | 取药品列表 |

### 返回格式

```json
{
  "data": [
    {
      "name": "纸包鸡",
      "id": "纸包鸡",
      "price": 35.5,
      "exp": 0,
      "strengthFood": 10.0,
      "strengthDrink": 0.0,
      "strength": 0.0,
      "feeling": 1.0,
      "health": 0.0,
      "likability": 0.0,
      "description": "喜好度:\t100%\n物品介绍"
    }
  ]
}
```

字段说明：

| 字段 | 说明 |
|------|------|
| `name` | 翻译后的物品名称 |
| `id` | 物品原始ID/名称 |
| `price` | 价格 |
| `exp` | 经验值 |
| `strengthFood` | 饱腹感 |
| `strengthDrink` | 口渴度 |
| `strength` | 体力 |
| `feeling` | 心情 |
| `health` | 健康 |
| `likability` | 喜好程度/好感度 |
| `description` | 介绍 |

### 调用示例

```bash
curl -X POST http://127.0.0.1:52814/get_meal_list -H "Content-Type: application/json" -d '{}'
```

---

## 📈 详细数据

### 不同工作的倍率对比

假设桌宠等级为260：

| 工作名称 | 基础档位 | 最大倍率 | 实际上限 | 收益提升 |
|---------|---------|---------|---------|---------|
| 清屏 | 10 | 13x | 260 | ⭐⭐⭐⭐⭐ |
| 直播 | 20 | 8x | 240 | ⭐⭐⭐⭐ |
| 研究 | 15 | 10x | 250 | ⭐⭐⭐⭐⭐ |
| 删错误 | 6 | 16x | 256 | ⭐⭐⭐⭐⭐ |
| 跳绳 | 12 | 11x | 242 | ⭐⭐⭐⭐⭐ |
| 学书法 | 8 | 14x | 252 | ⭐⭐⭐⭐⭐ |
| 学画画 | 25 | 7x | 245 | ⭐⭐⭐⭐ |

---

## ❓ 常见问题

<details>
<summary><b>Q1: 会修改我的存档吗？</b></summary>

**A:** 不会！插件只在运行时调整，不修改任何配置文件或存档。
</details>

<details>
<summary><b>Q2: 低等级桌宠能用吗？</b></summary>

**A:** 可以！插件会根据桌宠等级自动计算合适的倍率。
- 260级桌宠：获得最高倍率
- 100级桌宠：获得中等倍率
- 50级桌宠：获得较低倍率
</details>

<details>
<summary><b>Q3: 如何确认插件在工作？</b></summary>

**A:** 在调试模式下可以看到日志：
```
[自动倍率] 清屏: 档位10 x13 = 上限260 (桌宠260级)
[自动倍率] 直播: 档位20 x8 = 上限240 (桌宠260级)
```
</details>

<details>
<summary><b>Q4: 可以关闭这个功能吗？</b></summary>

**A:** 可以！删除mod文件夹即可恢复原版。
</details>

<details>
<summary><b>Q5: 为什么有些工作倍率不同？</b></summary>

**A:** 因为每个工作的基础档位（LevelLimit）不同：
- 档位越低，最大倍率越高（如删错误档位6，倍率16x）
- 档位越高，最大倍率越低（如学画画档位25，倍率7x）
</details>

---

## 🔧 技术细节

### 核心代码

```csharp
// 计算最大倍率
int maxMultiplier = Math.Min(4000, 桌宠等级) / (LevelLimit + 10);

// 应用倍率
var adjustedWork = work.Double(maxMultiplier);

// 实际等级上限
int actualLimit = (LevelLimit + 10) * maxMultiplier;
```

### 文件结构

```
1230_VpetAPIHttp/
├── plugin/
│   └── VPet.Plugin.VpetAPI.dll
├── icon.png
└── info.lps
```

---

## 📝 版本信息

- **版本号**: 10030
- **游戏版本**: 11049
- **作者**: 小铃铛·MadeSpark
- **作者ID**: 1427082406

---

## 🛠️ 开发说明

### 编译项目

```bash
cd D:\DevelopmentFolder\C#\Vpet.Plugin.VpetAPI
dotnet build -c Release
```

### 依赖项

- .NET 8.0 Windows
- VPet-Simulator.Core (1.1.0.50)
- VPet-Simulator.Windows.Interface (1.1.0.50)

---

## 📜 许可证

遵循VPet项目的Apache License 2.0许可证。

---

## 🎉 更新日志

### v10030 (当前版本)
- ✨ 新增智能倍率自动调整功能
- ✨ 自动计算并应用最大可用倍率
- ✨ 让高等级桌宠获得最高收益
- 🔧 优化代码结构
- 📝 添加详细文档

### v10020 (原版本)
- 基础HTTP API控制功能

---

## 💬 支持

如有问题或建议，请联系作者或在VPet社区反馈。

---

<div align="center">

**让你的高等级桌宠发挥真正的实力！** 🚀

Made with ❤️ by 小铃铛·MadeSpark

</div>
