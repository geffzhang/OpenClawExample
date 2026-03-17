# OpenClawExample

基于 `Alibaba.OpenSandbox` 的最小示例，用于在本地 `OpenSandbox` 服务上创建并启动 `OpenClaw` 沙箱实例。

## 项目说明

当前示例会：

1. 连接本地 `OpenSandbox` 服务：`http://localhost:8090`
2. 使用镜像：`ghcr.io/openclaw/openclaw:latest`
3. 启动 `OpenClaw` gateway
4. 轮询沙箱公开端点，等待服务可用
5. 输出可访问地址

当前实现使用的启动命令：
node dist/index.js gateway --port 18789 --allow-unconfigured --verbose

````````markdown
> 说明：
>
> - 未使用 `--bind=lan`，避免触发 `Control UI allowedOrigins` 安全配置错误
> - 未启用 `NetworkPolicy`，避免当前环境下的 `egress sidecar` 启动失败

---

## 运行环境

- `.NET 10`
- C# `14.0`
- 本地可访问的 `OpenSandbox` 服务
- Docker / OpenSandbox 服务端运行环境已正确安装并可拉取镜像

---

## 依赖

项目依赖：

- `Alibaba.OpenSandbox` `0.1.0-alpha.3`

项目文件：
<Project Sdk="Microsoft.NET.Sdk"> <PropertyGroup> <OutputType>Exe</OutputType> <TargetFramework>net10.0</TargetFramework> <ImplicitUsings>enable</ImplicitUsings> <Nullable>enable</Nullable> </PropertyGroup> <ItemGroup> <PackageReference Include="Alibaba.OpenSandbox" Version="0.1.0-alpha.3" /> </ItemGroup> </Project>

---

## 配置

### 1. OpenSandbox 服务地址

代码中默认使用：const string server = "http://localhost:8090";

如果服务不在本机，请修改 `Program.cs`。

### 2. Gateway Token

程序会读取环境变量 `OPENCLAW_GATEWAY_TOKEN`：
set OPENCLAW_GATEWAY_TOKEN=your-token

如果未设置，将使用默认值：dummy-token-for-sandbox

---

## 核心流程

`Program.cs` 的流程如下：

1. 构造 `SandboxCreateOptions`
2. 创建沙箱
3. 使用自定义 `HealthCheck` 轮询 HTTP 端点
4. 沙箱就绪后读取公开地址

---

## 关键参数说明

### `SkipHealthCheck = true`

创建沙箱时跳过平台默认健康检查，改用示例中的自定义 HTTP 检查逻辑：
HealthCheck = CheckOpenClawAsync'

### `Entrypoint`

当前使用：Entrypoint = [ "node dist/index.js gateway --port 18789 --allow-unconfigured --verbose" ],

### `gatewayPort`

示例默认端口：const int gatewayPort = 18789;

---

## 已知问题

### 1. `Egress sidecar container failed to start`

出现原因通常是启用了 `NetworkPolicy`，而当前 `OpenSandbox` 服务端环境无法正常启动 `egress sidecar`。

示例中已注释掉以下配置：
//NetworkPolicy = new NetworkPolicy //{ //    DefaultAction = NetworkRuleAction.Deny, //    Egress = [ new NetworkRule { Action = NetworkRuleAction.Allow, Target = "pypi.org" } ] //}

如果重新启用该配置，可能再次触发该错误。

### 2. `non-loopback Control UI requires gateway.controlUi.allowedOrigins`

出现原因通常是启动参数使用了：--bind=lan

这会要求额外配置 `gateway.controlUi.allowedOrigins`。当前示例已移除 `--bind=lan`。

如果确实需要 LAN 暴露，请在 `OpenClaw` 配置中显式设置允许的 origins。

---

## 故障排查

### 沙箱创建失败

检查：

- `OpenSandbox` 服务是否已启动
- Docker 是否正常运行
- 镜像 `ghcr.io/openclaw/openclaw:latest` 是否可拉取
- `server` 地址是否正确

### 一直等待 Ready

检查：

- 容器内 `OpenClaw` 是否成功启动
- 端口 `18789` 是否被正确暴露
- 沙箱 endpoint 是否可访问

### Token 相关问题

检查环境变量：
echo %OPENCLAW_GATEWAY_TOKEN%

---

## 示例代码位置

主逻辑位于：

- `Program.cs`

---

## 后续可扩展项

可进一步补充：

- 支持从命令行传入 `server` / `image` / `port`
- 将 `allowedOrigins` 改为显式配置
- 增加更详细的启动日志输出
- 为 `Sandbox.CreateAsync` 和 `WaitUntilReadyAsync` 增加异常分类处理