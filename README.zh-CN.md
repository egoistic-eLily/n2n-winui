# Saenai Network

[English](README.md) | [简体中文](README.zh-CN.md)

WinUI 3 桌面客户端，以图形方式管理 [n2n](https://github.com/ntop/n2n) 虚拟局域网（VPN）的 edge 连接。
登录后自动拉起并配置 `edge.exe`，通过命名管道下发会话参数，实时回传连接状态与日志——完全不需要命令行知识。

> [!IMPORTANT]
> **本项目完完全全是一个套壳（GUI 外壳）。** 所有隧道通信均由上游 n2n 引擎完成；用户管理、身份验证与会话分发全部由外部服务端项目承担（见[项目依赖](#项目依赖)）。本仓库自身不包含任何服务端逻辑。

## 项目依赖

本项目无法独立运行，依赖以下项目：

| 项目 | 角色 |
| --- | --- |
| [ntop/n2n](https://github.com/ntop/n2n)（3.0 稳定版，内嵌于 `external/n2n/`） | VPN 引擎本体——加密、报文转发、TAP 处理 |
| [ChingCdesu/supernode-frontend](https://github.com/ChingCdesu/supernode-frontend) | **一切的基础**：本客户端所连接的 n2n 超级节点服务器及其管理工具 |
| [egoistic-eLily/n2n-user-server](https://github.com/egoistic-eLily/n2n-user-server) | 专为本项目编写的用户管理/验证服务端，负责校验登录并返回 n2n 会话参数 |

## 项目结构

```
N2N_TOOLS_WINUI/
├── N2N-Saenai.slnx            # 解决方案
├── N2N-Saenai.csproj          # WinUI 3 应用工程
├── App.xaml / MainWindow.xaml # 应用入口与主窗口
├── Views/                     # 页面（登录页、主页/连接页）
├── Core/                      # edge 进程会话管理（命名管道宿主 Pipes.cs）
├── Service/                   # 登录 API 客户端
├── Initialization/            # 应用初始化（配置、TAP 驱动检测/安装）
├── Serialization/             # JSON 源生成上下文（支持裁剪发布）
├── Assets/                    # 应用图标等资源
├── drivers/tap0901/           # 随应用分发的 TAP-Windows V9 驱动包
├── external/n2n/              # 内嵌 n2n 3.0 源码
│   ├── saenai/MAIN.cpp        # Saenai 定制入口：管道接收配置 → 调用 edge()
│   ├── third_party/nlohmann/  # 内嵌 nlohmann/json 单头文件
│   └── build/                 # CMake 构建输出（不入库，见 .gitignore）
└── tools/test-pipe-edge.py    # 管道链路独立测试脚本（模拟应用完整启动流程）
```

## 环境要求

- Windows 10 1809+ / Windows 11，x64
- .NET 10 SDK（WinUI 3 / Windows App SDK 2.2）
- Visual Studio 2026 或更高（含 MSVC 工具集与"C++ CMake tools for Windows"）
- CMake 3.20+
- TAP-Windows V9 虚拟网卡（可在应用内一键安装 `drivers/tap0901`）

## 编译

### 1. 编译 n2n edge（含 Saenai 管道宿主）

```powershell
cmake -S external/n2n -B external/n2n/build
cmake --build external/n2n/build --config Release --target edge
```

产物：`external/n2n/build/Release/edge.exe`。

CMake 工程会把上游 edge 与 `external/n2n/saenai/MAIN.cpp` 一起编译（全局启用 `/utf-8`：源码中的中文日志字面量必须以 UTF-8 编码，否则 `nlohmann::json::dump()` 会直接终止进程）。生成的可执行文件完全兼容 n2n 原生命令行；仅当以 `--saenai-pipe <名称>` 启动时才进入 Saenai 管道模式。

### 2. 编译应用

用 Visual Studio 打开 `N2N-Saenai.slnx` 直接生成，或：

```powershell
dotnet build N2N-Saenai.csproj -c Release
```

构建时会自动把 `drivers/tap0901` 与 `edge.exe` 拷贝到输出目录（`drivers/`、`n2n/`）。

### 3.（可选）验证管道链路

```powershell
python tools/test-pipe-edge.py
```

脚本会创建命名管道、以管道模式启动 edge、下发测试配置并打印全部交互，可用于独立验证"应用 → 管道 → n2n"链路。

## 连接流程

1. 用户登录，服务端返回 n2n 会话参数（仅驻留内存）。
2. 应用检测 TAP 驱动，缺失时请求管理员授权安装。
3. 应用启动 `n2n/edge.exe --saenai-pipe <随机名>`，并重定向其 stdout/stderr。
4. edge 内的宿主代码（`external/n2n/saenai/MAIN.cpp`）连接命名管道，应用下发一条 JSON 配置。
5. 宿主将配置转换为 edge 原生参数（`-l`、`-c`、`-I`、`-J`、`-k`、`-A<n>`）后调用上游 `edge()`；生命周期事件以 JSON 行经管道回传，n2n 日志经 stdout 回传。
6. 应用从输出中识别"虚拟网卡创建 + 超级节点应答"，切换为"连接成功"并显示分配到的虚拟 IP。
7. 断开即结束 edge 进程树。

## 登录接口约定

**本应用不内置任何默认服务器地址**（不含默认域名、证书或凭据）。使用前你需要部署自己的 [n2n-user-server](https://github.com/egoistic-eLily/n2n-user-server)：

1. 在登录页的"服务器地址"一栏填入你自己服务端的登录接口（例如 `https://your-server/login`）——地址会先校验格式，登录成功后保存在本机，下次启动自动回填。也可以在编译前直接把地址写进 `Initialization/Config.cs`（`LauncherConfig.ApiUrl`）。
2. 客户端向配置的接口发起 POST 请求，`Content-Type: application/json`：

```json
{"userid":"alice","password":"user-password"}
```

期望的响应结构（多余字段忽略）：

```json
{
  "success": true,
  "data": {
    "user_id":"alice", "supernode_ip":"203.0.113.10", "supernode_port":7654,
    "community_name":"example", "device_name":"alice-pc", "password":"edge-password",
    "community_key":"community-secret", "encrypt_algorithm":4
  }
}
```

`success: false`、非 2xx、响应不完整或超时均视为登录失败。凭据与 n2n 密钥不会写入日志面板或命令行。

## 许可说明

本仓库以 **GNU General Public License v3.0（GPLv3）** 分发（见 [LICENSE](LICENSE)）。`external/n2n` 内嵌并修改了 [n2n](https://github.com/ntop/n2n) 3.0 稳定版，其代码以 GPLv3 授权并编译进随应用分发的 `edge.exe`，因此整个作品必须同样采用 GPLv3。
