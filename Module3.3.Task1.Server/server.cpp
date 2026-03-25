#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <thread>

#pragma comment(lib, "ws2_32.lib")

const int PORT = 6001;
const int BUF_SIZE = 4096;

void handle_client(SOCKET client_sock, std::string client_addr) {
    std::cout << "[+] Client connected: " << client_addr << std::endl;

    char buf[BUF_SIZE];
    while (true) {
        int bytes = recv(client_sock, buf, BUF_SIZE - 1, 0);
        if (bytes <= 0) break;
        buf[bytes] = '\0';

        std::string msg(buf);
        // trim trailing \r\n
        while (!msg.empty() && (msg.back() == '\r' || msg.back() == '\n'))
            msg.pop_back();

        if (msg == "BYE") {
            std::cout << "[" << client_addr << "] disconnected" << std::endl;
            std::string reply = "BYE\n";
            send(client_sock, reply.c_str(), (int)reply.size(), 0);
            break;
        }

        std::cout << "[" << client_addr << "] " << msg << std::endl;

        std::string reply = "ECHO: " + msg + "\n";
        send(client_sock, reply.c_str(), (int)reply.size(), 0);
    }

    closesocket(client_sock);
}

int main() {
    WSADATA wsa;
    WSAStartup(MAKEWORD(2, 2), &wsa);

    SOCKET server_sock = socket(AF_INET, SOCK_STREAM, 0);

    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(PORT);
    addr.sin_addr.s_addr = INADDR_ANY;

    bind(server_sock, (sockaddr*)&addr, sizeof(addr));
    listen(server_sock, SOMAXCONN);

    std::cout << "Task1 Server listening on port " << PORT << std::endl;

    while (true) {
        sockaddr_in client_addr{};
        int addr_len = sizeof(client_addr);
        SOCKET client_sock = accept(server_sock, (sockaddr*)&client_addr, &addr_len);
        if (client_sock == INVALID_SOCKET) continue;

        char ip[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, &client_addr.sin_addr, ip, sizeof(ip));
        std::string client_str = std::string(ip) + ":" + std::to_string(ntohs(client_addr.sin_port));

        std::thread(handle_client, client_sock, client_str).detach();
    }

    closesocket(server_sock);
    WSACleanup();
    return 0;
}
