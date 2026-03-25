// Same client as Task2 — works for any text-based game session
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <thread>
#include <atomic>

#pragma comment(lib, "ws2_32.lib")

const int PORT = 6003;
const int BUF_SIZE = 4096;

std::atomic<bool> running{true};

void recv_loop(SOCKET sock) {
    char buf[BUF_SIZE];
    std::string leftover;
    while (running) {
        int bytes = recv(sock, buf, BUF_SIZE - 1, 0);
        if (bytes <= 0) { running = false; break; }
        buf[bytes] = '\0';
        leftover += buf;

        size_t pos;
        while ((pos = leftover.find('\n')) != std::string::npos) {
            std::string line = leftover.substr(0, pos);
            leftover = leftover.substr(pos + 1);
            if (!line.empty() && line.back() == '\r') line.pop_back();
            std::cout << line << std::endl;
        }
    }
}

int main(int argc, char* argv[]) {
    const char* host = (argc > 1) ? argv[1] : "127.0.0.1";

    WSADATA wsa;
    WSAStartup(MAKEWORD(2, 2), &wsa);

    SOCKET sock = socket(AF_INET, SOCK_STREAM, 0);

    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(PORT);
    inet_pton(AF_INET, host, &addr.sin_addr);

    if (connect(sock, (sockaddr*)&addr, sizeof(addr)) != 0) {
        std::cerr << "Connection failed" << std::endl;
        return 1;
    }

    std::cout << "Connected to " << host << ":" << PORT << std::endl;

    std::thread receiver(recv_loop, sock);

    std::string line;
    while (running && std::getline(std::cin, line)) {
        std::string msg = line + "\n";
        send(sock, msg.c_str(), (int)msg.size(), 0);
    }

    running = false;
    closesocket(sock);
    receiver.join();
    WSACleanup();
    return 0;
}
