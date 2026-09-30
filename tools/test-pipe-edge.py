# -*- coding: utf-8 -*-
# 模拟 Saenai 应用(Pipes.cs)的 edge 启动流程:
#   NamedPipeServerStream -> edge.exe --saenai-pipe <name> -> 发送 configure JSON
# 使用已知正常的配置值,检验 "应用 -> 管道 -> n2n" 传输路径本身是否正常。
import threading
import time
import subprocess
import sys
import uuid
import os

import win32pipe
import win32file
import pywintypes

EDGE = r"D:\myfile\Ceshi\N2N_TOOLS_WINUI\external\n2n\build\Release\edge.exe"
STDOUT_LOG = r"D:\myfile\Ceshi\N2N_TOOLS_WINUI\tools\edge-stdout.log"
STDERR_LOG = r"D:\myfile\Ceshi\N2N_TOOLS_WINUI\tools\edge-stderr.log"

# 与 Pipes.cs 实际输出一致的 JSON(显式 JsonPropertyName 的 snake_case 字段)
JSON_PAYLOAD = ('{"type":"configure","data":{"user_id":"test","supernode_ip":"121.196.236.194",'
                '"supernode_port":7654,"community_name":"HOI4S","device_name":"kaiser",'
                '"password":"7878","community_key":"5Zf5HcQGZpp4pdmxjzKE","encrypt_algorithm":4}}')

WAIT_SECONDS = 45

pipe_name = "SaenaiTest-" + uuid.uuid4().hex
full_name = "\\\\.\\pipe\\" + pipe_name
print("== pipe :", full_name)
print("== payload:", JSON_PAYLOAD)

# 服务端管道(字节模式,阻塞 IO,与 C# 服务端字节模式对应)
handle = win32pipe.CreateNamedPipe(
    full_name,
    win32pipe.PIPE_ACCESS_DUPLEX,
    win32pipe.PIPE_TYPE_BYTE | win32pipe.PIPE_READMODE_BYTE | win32pipe.PIPE_WAIT,
    1, 64 * 1024, 64 * 1024, 0, None)

connected = threading.Event()

def accept_thread():
    try:
        win32pipe.ConnectNamedPipe(handle, None)
    except pywintypes.error as e:
        print("!! ConnectNamedPipe error:", e)
        return
    connected.set()

acceptor = threading.Thread(target=accept_thread, daemon=True)

stdout_f = open(STDOUT_LOG, "wb")
stderr_f = open(STDERR_LOG, "wb")

proc = subprocess.Popen(
    [EDGE, "--saenai-pipe", pipe_name],
    stdout=stdout_f, stderr=stderr_f,
    creationflags=subprocess.CREATE_NO_WINDOW)
print("== edge.exe started, pid=%d" % proc.pid)

acceptor.start()
if not connected.wait(15):
    print("!! edge did not connect to the pipe within 15s")
    if proc.poll() is None:
        proc.kill()
    sys.exit(2)
print("== pipe connected")

payload = (JSON_PAYLOAD + "\n").encode("utf-8")
win32file.WriteFile(handle, payload)
print("== configure json sent")

pipe_lines = []

def read_thread():
    while True:
        try:
            hr, data = win32file.ReadFile(handle, 64 * 1024)
            text = data.decode("utf-8", errors="replace")
            for line in text.splitlines():
                if line:
                    pipe_lines.append(line)
                    print("PIPE>> " + line)
        except pywintypes.error:
            break  # 管道已断开(edge 退出)

reader = threading.Thread(target=read_thread, daemon=True)
reader.start()

start = time.time()
while time.time() - start < WAIT_SECONDS:
    code = proc.poll()
    if code is not None:
        print("== edge EXITED after %.1fs, code=%d (0x%08X)" % (time.time() - start, code, code & 0xFFFFFFFF))
        break
    time.sleep(0.5)
else:
    print("== edge still running after %ds (no crash) -> transmission path OK" % WAIT_SECONDS)
    proc.kill()
    proc.wait()

try:
    win32file.CloseHandle(handle)
except Exception:
    pass

time.sleep(1)
print("== edge stdout (%s) ===" % STDOUT_LOG)
print(open(STDOUT_LOG, "rb").read().decode("utf-8", errors="replace") or "(empty)")
print("== edge stderr ===")
print(open(STDERR_LOG, "rb").read().decode("utf-8", errors="replace") or "(empty)")
