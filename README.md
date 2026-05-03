# VPet.Plugin.VpetAPI - 智能倍率调整版

<div align="center">

![VPet](https://img.shields.io/badge/VPet-1.1.0.50-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-Apache%202.0-green)
![Version](https://img.shields.io/badge/version-10030-orange)

**让高等级桌宠发挥真正实力的智能插件**

[功能介绍](#-功能介绍) • [安装方法](#-安装方法) • [API文档](#-完整-api-文档) • [使用示例](#-使用示例)

</div>

---

## 📖 功能介绍

这是一个增强版的VpetAPI插件，在原有HTTP API控制功能的基础上，新增了**智能倍率自动调整**功能。

### 核心功能

#### 1. HTTP API控制（原有功能）
- 在本地启动HTTP服务（127.0.0.1:52814）
- 通过POST接口控制桌宠移动、说话、工作、学习、玩耍、睡觉等
- 支持获取商店物品列表（收藏、正餐、零食、饮料、功能性、药品）

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
```

---

## 📡 完整 API 文档

### 基础信息

- **服务地址**: `http://127.0.0.1:52814`
- **请求方式**: `POST`
- **Content-Type**: `application/json`

---

### 1. 桌宠移动

**接口**: `POST /move_to`

**请求参数**:
```json
{
  "x": 100,
  "y": 200,
  "isCreeping": false
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| x | int | 是 | 目标X坐标 |
| y | int | 是 | 目标Y坐标 |
| isCreeping | bool | 否 | 是否爬行模式（默认false） |

**返回示例**:
```json
{}
```

---

### 2. 桌宠说话

**接口**: `POST /say`

**请求参数**:
```json
{
  "text": "你好呀！"
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| text | string | 是 | 说话内容（最多500字符） |

**返回示例**:
```json
{}
```

---

### 3. 桌宠随机说话

**接口**: `POST /say_rnd`

**请求参数**:
```json
{
  "text": "你好呀！"
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| text | string | 是 | 说话内容（最多500字符） |

**说明**: 与 `/say` 的区别是会随机选择说话动画。

**返回示例**:
```json
{}
```

---

### 4. 设置睡眠状态

**接口**: `POST /set_sleep`

**请求参数**:
```json
{
  "isSleeping": true
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| isSleeping | bool | 是 | true=睡觉，false=唤醒 |

**返回示例**:
```json
{}
```

---

### 5. 开始工作

**接口**: `POST /set_work`

**请求参数**:
```json
{
  "id": "清屏"
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | string | 否 | 工作名称或索引，为空则随机 |

**说明**: 插件会自动应用最佳倍率。

**返回示例**:
```json
{}
```

---

### 6. 开始学习

**接口**: `POST /set_study`

**请求参数**:
```json
{
  "id": "学画画"
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | string | 否 | 学习项目名称或索引，为空则随机 |

**说明**: 插件会自动应用最佳倍率。

**返回示例**:
```json
{}
```

---

### 7. 开始玩耍

**接口**: `POST /set_play`

**请求参数**:
```json
{
  "id": "跳绳"
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | string | 否 | 玩耍项目名称或索引，为空则随机 |

**说明**: 插件会自动应用最佳倍率。

**返回示例**:
```json
{}
```

---

### 8. 获取工作列表

**接口**: `POST /get_work_list`

**请求参数**:
```json
{}
```

**返回示例**:
```json
{
  "data": ["文案", "清屏", "直播", "研究", "删错误"]
}
```

---

### 9. 获取学习列表

**接口**: `POST /get_study_list`

**请求参数**:
```json
{}
```

**返回示例**:
```json
{
  "data": ["学书法", "学画画", "学编程"]
}
```

---

### 10. 获取玩耍列表

**接口**: `POST /get_play_list`

**请求参数**:
```json
{}
```

**返回示例**:
```json
{
  "data": ["跳绳", "打球", "玩游戏"]
}
```

---

### 11. 获取收藏物品列表

**接口**: `POST /get_favorite_food_list`

**请求参数**:
```json
{}
```

**返回示例**:
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
      "description": "物品介绍文本"
    }
  ]
}
```

---

### 12. 获取正餐列表

**接口**: `POST /get_meal_list`

**请求参数**: `{}`

**返回格式**: 同上

---

### 13. 获取零食列表

**接口**: `POST /get_snack_list`

**请求参数**: `{}`

**返回格式**: 同上

---

### 14. 获取饮料列表

**接口**: `POST /get_drink_list`

**请求参数**: `{}`

**返回格式**: 同上

---

### 15. 获取功能性物品列表

**接口**: `POST /get_functional_list`

**请求参数**: `{}`

**返回格式**: 同上

---

### 16. 获取药品列表

**接口**: `POST /get_drug_list`

**请求参数**: `{}`

**返回格式**: 同上

---

### 17. 获取礼品列表

**接口**: `POST /get_gift_list`

**请求参数**: `{}`

**返回格式**: 同上

---

### 18. 购买物品

**接口**: `POST /buy_item`

**请求参数**:
```json
{
  "id": "GTC5090",
  "count": 1
}
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | string | 否 | 物品名称或ID，为空则随机购买 |
| count | int | 否 | 购买数量，默认1 |

**说明**: 自动扣除金钱、使用物品，并播放食用/礼品动画。

**返回示例**:
```json
{
  "message": "购买成功",
  "item": "GTC5090",
  "count": 1,
  "totalPrice": 5999.0,
  "remainingMoney": 60352.38
}
```

---

### 19. 重置状态

**接口**: `POST /reset_status`

**请求参数**:
```json
{}
```

**说明**: 停止当前工作/学习/玩耍，恢复正常状态。

**返回示例**:
```json
{}
```

---

### 20. 设置自定义菜单

**接口**: `POST /set_menu`

**请求参数**:
```json
{
  "自定义菜单": {
    "menu_item_1": {
      "name": "菜单项1",
      "callbackUrl": "http://example.com/callback1"
    },
    "menu_item_2": {
      "name": "菜单项2",
      "callbackUrl": "http://example.com/callback2"
    }
  }
}
```

**说明**: 在桌宠右键菜单中添加自定义项，点击时会调用 callbackUrl。

**返回示例**:
```json
{}
```

---

### 21. 获取桌宠信息

**接口**: `POST /get_pet_info`

**请求参数**:
```json
{}
```

**返回示例**:
```json
{
  "data": {
    "name": "桌宠名字",
    "level": 256,
    "money": 66352.38,
    "exp": 651800.45,
    "levelUpNeed": 655360,
    "strength": 356.0,
    "strengthMax": 356.0,
    "feeling": 220.84,
    "strengthFood": 272.96,
    "strengthDrink": 272.03,
    "likability": 2650.0,
    "health": 100.0
  }
}
```

**说明**: 读取桌宠当前等级、金钱、经验、体力、心情、饱腹感、口渴度、好感度、健康度。**始终返回真实数据，不受 UI 篡改影响**。主程序未提供困意值字段，因此未返回困意值。

---

### 22. 篡改桌宠等级（仅修改UI显示）

**接口**: `POST /set_fake_level`

**请求参数**:
```json
{
  "level": 999
}
```

**说明**: 使用 Harmony Hook 接管面板 UI 刷新函数，使**游戏面板显示假等级**；不修改真实存档，不影响工作/购买等游戏逻辑，**API 返回的仍是真实数据**。

---

### 23. 篡改桌宠金钱（仅修改UI显示）

**接口**: `POST /set_fake_money`

**请求参数**:
```json
{
  "money": 999999.99
}
```

**说明**: 使用 Harmony Hook 接管面板 UI 刷新函数，使**游戏面板显示假金钱**；不修改真实存档，不影响购买/扣钱等游戏逻辑，**API 返回的仍是真实数据**。

---

### 24. 恢复UI真实数值

**接口**: `POST /reset_fake_data`

**请求参数**:
```json
{}
```

**说明**: 清除等级与金钱篡改值，恢复 UI 与 API 返回真实数据。

---

### 物品字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| name | string | 翻译后的物品名称 |
| id | string | 物品原始ID/名称 |
| price | double | 价格 |
| exp | int | 经验值 |
| strengthFood | double | 饱腹感 |
| strengthDrink | double | 口渴度 |
| strength | double | 体力 |
| feeling | double | 心情 |
| health | double | 健康 |
| likability | double | 好感度数值 |
| likabilityPercent | string | 喜好程度百分比 |
| description | string | 物品介绍文本（已翻译） |

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

# 获取正餐列表
curl -X POST http://127.0.0.1:52814/get_meal_list \
  -H "Content-Type: application/json" \
  -d '{}'
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
│   ├── VPet.Plugin.VpetAPI.dll
│   └── 0Harmony.dll
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
- Lib.Harmony (2.3.3)
- VPet-Simulator.Core (1.1.0.50)
- VPet-Simulator.Windows.Interface (1.1.0.50)

---

## 📜 许可证

遵循VPet项目的Apache License 2.0许可证。

---

## 🎉 更新日志

### v10030 (当前版本)
- ✨ 新增智能倍率自动调整功能
- ✨ 新增商店物品列表API（6个接口）
- ✨ 自动计算并应用最大可用倍率
- ✨ 让高等级桌宠获得最高收益
- 🔧 优化代码结构
- 📝 添加详细文档

### v10020 (原版本)
- 基础HTTP API控制功能

---

## 📚 在线文档

完整的 API 文档和在线调试工具：

🔗 **[Apifox 在线文档](http://vpet-api.apifox.cn/)**

---

## 💬 支持

如有问题或建议，请联系作者或在VPet社区反馈。

---

<div align="center">

**让你的高等级桌宠发挥真正的实力！** 🚀

Made with ❤️ by 小铃铛·MadeSpark

</div>
