#include <Windows.h>
#include <cstdio>
#include <iostream>
#include <string>
#include <vector>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

struct N2NUserData
{
    std::string user_id;
    std::string supernode_ip;
    std::string community_name;
    std::string device_name;
    std::string password;
    std::string community_key;

    int supernode_port = 0;
    int encrypt_algorithm = 0;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(
    N2NUserData,
    user_id,
    supernode_ip,
    supernode_port,
    community_name,
    device_name,
    password,
    community_key,
    encrypt_algorithm
);

// n2n edge 入口
extern "C" int edge(int argc, char* argv[]);


// ============================================================
// 向 Saenai 主程序发送 JSON 日志
// ============================================================
static void SendLog(
    HANDLE pipe,
    const char* level,
    const std::string& message)
{
    if (pipe == INVALID_HANDLE_VALUE)
        return;

    try
    {
        json event =
        {
            {"type", "log"},
            {"level", level},
            {"message", message}
        };

        // replace：遇到非法 UTF-8 字节（例如被编译器按本地代码页编码的中文）
        // 替换为 '?'，而不是抛出 json::type_error 直接终止进程。
        const std::string line =
            event.dump(-1, ' ', false, json::error_handler_t::replace) + "\n";

        DWORD written = 0;

        WriteFile(
            pipe,
            line.data(),
            static_cast<DWORD>(line.size()),
            &written,
            nullptr
        );
    }
    catch (const json::exception&)
    {
        // 日志发送失败绝不能拖垮 n2n 本体。
    }
}


// ============================================================
// 从管道读取配置
//
// 协议：
// {
//     "type": "configure",
//     "data": { ... }
// }\n
// ============================================================
static bool ReadConfiguration(
    HANDLE pipe,
    N2NUserData& data)
{
    std::string buffer;

    char chunk[1024];
    DWORD bytesRead = 0;

    while (true)
    {
        const BOOL success = ReadFile(
            pipe,
            chunk,
            sizeof(chunk),
            &bytesRead,
            nullptr
        );

        if (!success)
        {
            return false;
        }

        if (bytesRead == 0)
        {
            return false;
        }

        buffer.append(chunk, bytesRead);

        const std::size_t newline = buffer.find('\n');

        if (newline == std::string::npos)
        {
            continue;
        }

        const std::string jsonText =
            buffer.substr(0, newline);

        try
        {
            const json request =
                json::parse(jsonText);

            if (request.value("type", "") != "configure")
            {
                return false;
            }

            if (!request.contains("data"))
            {
                return false;
            }

            data =
                request.at("data").get<N2NUserData>();

            return true;
        }
        catch (const json::exception&)
        {
            return false;
        }
    }
}


// ============================================================
// 使用 Saenai 配置启动 edge
// ============================================================
static int RunConfiguredEdge(
    const std::string& pipeName)
{
    // --------------------------------------------------------
    // 1. 打开命名管道
    // --------------------------------------------------------

    const std::wstring name(
        pipeName.begin(),
        pipeName.end()
    );

    const std::wstring fullName =
        L"\\\\.\\pipe\\" + name;

    HANDLE pipe = CreateFileW(
        fullName.c_str(),
        GENERIC_READ | GENERIC_WRITE,
        0,
        nullptr,
        OPEN_EXISTING,
        0,
        nullptr
    );

    if (pipe == INVALID_HANDLE_VALUE)
    {
        std::cerr
            << "Unable to open Saenai control pipe: "
            << GetLastError()
            << std::endl;

        return 2;
    }


    // --------------------------------------------------------
    // 2. 读取配置
    // --------------------------------------------------------

    N2NUserData data;

    if (!ReadConfiguration(pipe, data))
    {
        SendLog(
            pipe,
            "error",
            "无法读取应用连接配置。"
        );

        CloseHandle(pipe);
        return 3;
    }

    SendLog(
        pipe,
        "system",
        "已收到连接配置，正在启动 n2n edge。"
    );


    // --------------------------------------------------------
    // 3. 构造 edge 参数
    // --------------------------------------------------------

    std::vector<std::string> args;

    args.emplace_back("edge");

    // -l supernode
    args.emplace_back("-l");
    args.emplace_back(
        data.supernode_ip +
        ":" +
        std::to_string(data.supernode_port)
    );

    // -c community
    args.emplace_back("-c");
    args.emplace_back(data.community_name);

    // -I device name
    args.emplace_back("-I");
    args.emplace_back(data.device_name);

    // -J user password
    args.emplace_back("-J");
    args.emplace_back(data.password);

    // -k community key
    args.emplace_back("-k");
    args.emplace_back(data.community_key);

    // 日志策略：保持 n2n 默认 TRACE_NORMAL 级别，只输出关键事件
    //（注册/应答/网卡创建等）。应用侧从这些关键行中提取连接状态。

    // -A1 ~ -A5
    //
    // 0 表示不主动指定算法，让 n2n 使用默认行为。
    //
    if (data.encrypt_algorithm >= 1 &&
        data.encrypt_algorithm <= 5)
    {
        args.emplace_back(
            "-A" +
            std::to_string(data.encrypt_algorithm)
        );
    }


    // --------------------------------------------------------
    // 4. 构造 char* argv
    //
    // 注意：
    // 这里必须使用 std::string::data()
    // 而不是 const_cast<char*>(c_str())
    //
    // C++17 中 data() 对非 const string 返回 char*
    // --------------------------------------------------------

    std::vector<char*> argv;

    argv.reserve(args.size() + 1);

    for (auto& arg : args)
    {
        argv.push_back(const_cast<char*>(arg.c_str()));
    }

    argv.push_back(nullptr);


    SendLog(
        pipe,
        "system",
        "n2n edge 参数准备完成。"
    );


    // --------------------------------------------------------
    // 6. 调用 edge
    // --------------------------------------------------------

    SendLog(
        pipe,
        "system",
        "正在进入 n2n edge。"
    );

    const int argc =
        static_cast<int>(args.size());

    const int result =
        edge(argc, argv.data());


    // --------------------------------------------------------
    // 7. edge 返回
    // --------------------------------------------------------

    SendLog(
        pipe,
        result == 0 ? "system" : "error",
        "n2n edge 已停止，退出代码：" +
        std::to_string(result)
    );


    // --------------------------------------------------------
    // 8. 关闭管道
    // --------------------------------------------------------

    CloseHandle(pipe);

    return result;
}


// ============================================================
// 程序入口
// ============================================================
int main(
    int argc,
    char* argv[])
{
    // Saenai 模式：
    //
    // edge.exe --saenai-pipe <pipe-name>
    //
    if (argc == 3 &&
        std::string(argv[1]) == "--saenai-pipe")
    {
        return RunConfiguredEdge(argv[2]);
    }


    // --------------------------------------------------------
    // 普通命令行模式
    //
    // 保留原来的 edge CLI 行为
    // --------------------------------------------------------

    return edge(argc, argv);
}
