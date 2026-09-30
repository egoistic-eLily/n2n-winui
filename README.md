# Saenai Network

[English](README.md) | [简体中文](README.zh-CN.md)

A WinUI 3 desktop client that manages an [n2n](https://github.com/ntop/n2n) edge connection as a virtual LAN (VPN).
After signing in it launches and configures `edge.exe` automatically, hands the session parameters over a named pipe, and reports connection status and logs in real time — no command line knowledge required.

> [!IMPORTANT]
> **This project is purely a GUI wrapper (a "shell").** All tunneling is done by the upstream n2n engine; user management, authentication and session distribution are entirely handled by external server projects (see [Dependencies](#dependencies)). This repository contains no server-side logic of its own.

## Dependencies

This project cannot run on its own. It depends on:

| Project | Role |
| --- | --- |
| [ntop/n2n](https://github.com/ntop/n2n) (3.0 stable, vendored in `external/n2n/`) | The VPN engine itself — encryption, packet forwarding, TAP handling |
| [ChingCdesu/supernode-frontend](https://github.com/ChingCdesu/supernode-frontend) | **The foundation of everything**: the n2n supernode server and its management tooling that this client connects to |
| [egoistic-eLily/N2N_TOOLS_Server](https://github.com/egoistic-eLily/N2N_TOOLS_Server) | A custom user-management/auth server created specifically for this project (repository not public yet). It validates sign-ins and returns the n2n session parameters |

## Project structure

```
N2N_TOOLS_WINUI/
├── N2N-Saenai.slnx            # Solution
├── N2N-Saenai.csproj          # WinUI 3 application project
├── App.xaml / MainWindow.xaml # Entry point and main window
├── Views/                     # Pages (login page, home/connection page)
├── Core/                      # edge process session management (named-pipe host, Pipes.cs)
├── Service/                   # Login API client
├── Initialization/            # App initialization (config, TAP driver detection/install)
├── Serialization/             # JSON source-generation context (trim-safe publishing)
├── Assets/                    # App icons and resources
├── drivers/tap0901/           # Bundled TAP-Windows V9 driver package
├── external/n2n/              # Vendored n2n 3.0 source tree
│   ├── saenai/MAIN.cpp        # Custom entry point: receives config over a named pipe, then calls edge()
│   ├── third_party/nlohmann/  # Vendored nlohmann/json single header
│   └── build/                 # CMake build output (not committed, see .gitignore)
└── tools/test-pipe-edge.py    # Standalone pipe-path test script (simulates the full app launch flow)
```

## Prerequisites

- Windows 10 1809+ / Windows 11, x64
- .NET 10 SDK (WinUI 3 / Windows App SDK 2.2)
- Visual Studio 2026 or newer with the MSVC toolset and *C++ CMake tools for Windows*
- CMake 3.20+
- A TAP-Windows V9 adapter (the app can install the bundled `drivers/tap0901` package for you)

## Building

### 1. Build the n2n edge binary (with the Saenai pipe host)

```powershell
cmake -S external/n2n -B external/n2n/build
cmake --build external/n2n/build --config Release --target edge
```

Output: `external/n2n/build/Release/edge.exe`.

The CMake project compiles the upstream edge together with `external/n2n/saenai/MAIN.cpp` (`/utf-8` is enforced project-wide; the Chinese log literals must be UTF-8 encoded or `nlohmann::json::dump()` will abort the process). The resulting executable stays fully compatible with the native n2n command line — it only enters Saenai pipe mode when started with `--saenai-pipe <name>`.

### 2. Build the app

Open `N2N-Saenai.slnx` in Visual Studio and build, or:

```powershell
dotnet build N2N-Saenai.csproj -c Release
```

The build automatically copies `drivers/tap0901` and `edge.exe` into the output directory (`drivers/`, `n2n/`).

### 3. (Optional) Verify the pipe path

```powershell
python tools/test-pipe-edge.py
```

The script creates a named pipe, starts edge in pipe mode, sends a test configuration, and prints the full interaction — useful for verifying the "app → pipe → n2n" chain in isolation.

## Connection flow

1. The user signs in; the server returns the n2n session parameters (kept in memory only).
2. The app checks for the TAP driver and offers an elevated install if missing.
3. The app starts `n2n/edge.exe --saenai-pipe <random-name>` with redirected stdout/stderr.
4. The host code inside edge (`external/n2n/saenai/MAIN.cpp`) connects to the named pipe; the app sends one JSON configuration message.
5. The host translates the configuration into native edge options (`-l`, `-c`, `-I`, `-J`, `-k`, `-A<n>`) and calls upstream `edge()`. Lifecycle events come back as JSON lines over the pipe; n2n logs come back over stdout.
6. The app watches the output for "TAP device created" plus "supernode acknowledgement", then switches to **Connected** and displays the assigned virtual IP.
7. Disconnecting terminates the edge process tree.

## Login API contract

`POST https://saenai.asia:8443/login` with `Content-Type: application/json`:

```json
{"userid":"alice","password":"user-password"}
```

Expected response shape (extra fields are ignored):

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

`success: false`, a non-2xx HTTP result, an incomplete response or a timeout is shown as a failed sign-in. Credentials and n2n keys are never written to the log panel or the command line.

## License notes

The vendored `external/n2n` is based on [n2n](https://github.com/ntop/n2n) 3.0 stable and is licensed under **GPLv3**; use and redistribute this repository accordingly.
