# 🃏 UnoGame

**一款基于 TCP 的经典 UNO 卡牌游戏，支持 2~14 人远程联机，手绘风格牌面，房主开服，成员直连。**

[![Support](https://img.shields.io/badge/Windows-支持-green/?logo=data:image/svg+xml;charset=utf-8;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz48IS0tIFVwbG9hZGVkIHRvOiBTVkcgUmVwbywgd3d3LnN2Z3JlcG8uY29tLCBHZW5lcmF0b3I6IFNWRyBSZXBvIE1peGVyIFRvb2xzIC0tPgo8c3ZnIHdpZHRoPSI4MDBweCIgaGVpZ2h0PSI4MDBweCIgdmlld0JveD0iMCAwIDE2IDE2IiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIGZpbGw9Im5vbmUiPjxwYXRoIGZpbGw9IiNGMzUzMjUiIGQ9Ik0xIDFoNi41djYuNUgxVjF6Ii8+PHBhdGggZmlsbD0iIzgxQkMwNiIgZD0iTTguNSAxSDE1djYuNUg4LjVWMXoiLz48cGF0aCBmaWxsPSIjMDVBNkYwIiBkPSJNMSA4LjVoNi41VjE1SDFWOC41eiIvPjxwYXRoIGZpbGw9IiNGRkJBMDgiIGQ9Ik04LjUgOC41SDE1VjE1SDguNVY4LjV6Ii8+PC9zdmc+)](https://github.com/CLoneLING/WindowsGarbageCleaner)
[![Buildon](https://img.shields.io/badge/Build_on-C%23-green/?logo=dotnet)](https://learn.microsoft.com/zh-cn/dotnet/csharp/)


---

## 📜 项目简介

本项目是经典的 UNO 卡牌游戏的 **Windows 桌面联机实现**。采用 C# WinForms 开发，基于 TCP Socket 构建稳定的客户端-服务器架构。**房主即主机**，其他玩家通过 IP 地址和端口直接连接，无需第三方服务器。

**特色亮点**：
- 🎨 **手绘风格牌面** – 每张牌均为手绘设计，视觉独特。
- 🌐 **远程联机** – 支持局域网或公网（需端口映射）对战。
- 👥 **2~14 人灵活对战** – 人数任意配置，人少快速，人多热闹。
- ⚡ **多种 UNO 规则** – 支持抢出、连出、+2/+4 累加、举报未喊 UNO 等。
- 🔄 **断线重连** – 网络波动后自动重连，保证对局不中断。

---

## ⚙️ 功能特性

- **房主模式**：一键创建房间，自动启动服务器，显示本机 IP。
- **成员模式**：输入房主 IP 和端口即可加入。
- **完整游戏逻辑**：
  - 数字牌、功能牌（Skip、Reverse、+2）、万能牌（Wild、+4）。
  - 出牌合法性校验、颜色匹配、数字/符号匹配。
  - **抢出机制**（非自己回合可出相同牌）。
  - **连出机制**（出牌后若手中有相同牌，可连续打出）。
  - **罚牌累加**（+2/+4 可连续叠加，必须出同类型或抽牌接受惩罚）。
  - **UNO 喊牌与举报**（剩一张时喊 UNO，被举报罚抽两张）。
- **实时状态显示**：
  - 当前回合玩家、方向、累积罚牌数。
  - 顶部牌图片及颜色指示。
  - 每位玩家的手牌数量及 UNO 状态。
- **手绘牌面**：所有牌面采用手绘设计，风格统一，视觉舒适。

---

## 🖥️ 系统要求

- **操作系统**：Windows 7 及以上
- **.NET 运行时**：.NET 8.0 Desktop Runtime 或更高版本（[下载地址](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)）
- **网络**：局域网或公网（房主需开放端口，内网穿透等）

---

## 📦 下载与运行

### 直接下载编译好的可执行文件
1. 前往 [Releases](https://github.com/CLoneLING/UnoGame/releases) 下载最新版本。
2. 下载后双击 `UnoGame.exe` 启动。