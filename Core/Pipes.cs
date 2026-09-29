using N2N_Saenai.Initialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace N2N_Saenai.Core
{
    public class Pipe {
        public static Process _edgeProcess;
        // 异步启动管道服务
        public static async Task StartAsync(N2NUserData data)
        {
                using (var pipeServer = new NamedPipeServerStream(
                    "SaenaiN2NPipe",
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous)) // 关键：开启异步选项
                {
                    Console.WriteLine("正在异步等待连接...");

                    // 1. 异步等待客户端连接，不会阻塞主线程
                    await pipeServer.WaitForConnectionAsync();

                    try
                    {
                        // 3. 序列化并添加换行符（配合 C++ 端的粘包处理）
                        string jsonString = JsonSerializer.Serialize(data) + "\n";
                        byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

                        // 4. 异步写入数据
                        await pipeServer.WriteAsync(buffer, 0, buffer.Length);
                        await pipeServer.FlushAsync();

                        Console.WriteLine("异步发送成功！");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"连接处理出错: {ex.Message}");
                    }
                }
        }

        public static async Task LaunchEdgeClient(string exePath, N2NUserData data)
        {
            try
            {
                string arguments = $"-l {data.supernode_ip}:{data.supernode_port} " +
                           $"-c \"{data.community_name}\" " +
                           $"-I \"{data.device_name}\" " +
                           $"-J \"{data.password}\" " +
                           $"-k \"{data.community_key}\" " +
                           $"-A{data.encrypt_algorithm}";
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,// 你的 edge.exe 完整路径
                    Arguments = arguments,      
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = false,           // 如果你想让 edge 运行在后台，可以设为 true
                    WindowStyle = ProcessWindowStyle.Normal
                };
                // 启动进程
                _edgeProcess = Process.Start(startInfo);

            }
            catch (Exception ex)
            {
                
            }
        }
        public async static Task StopEdgeClient()
        {
            try
            {
                // 检查进程是否存在且尚未退出
                if (_edgeProcess != null && !_edgeProcess.HasExited)
                {
                    _edgeProcess.Kill(); // 强行杀掉进程
                    _edgeProcess.Dispose(); // 释放资源
                    _edgeProcess = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止进程时出错: {ex.Message}");
            }
        }
    }
}
