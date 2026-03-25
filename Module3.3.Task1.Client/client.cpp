#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>

#pragma comment(lib, "ws2_32.lib")

const int PORT = 6001;
const int BUF_SIZE = 4096;

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
    std::cout << "Type messages (BYE to quit):" << std::endl;

    char buf[BUF_SIZE];
    std::string line;
    while (std::getline(std::cin, line)) {
        std::string msg = line + "\n";
        send(sock, msg.c_str(), (int)msg.size(), 0);

        int bytes = recv(sock, buf, BUF_SIZE - 1, 0);
        if (bytes <= 0) break;
        buf[bytes] = '\0';

        std::string reply(buf);
        while (!reply.empty() && (reply.back() == '\r' || reply.back() == '\n'))
            reply.pop_back();

        std::cout << "Server: " << reply << std::endl;

        if (line == "BYE") break;
    }

    closesocket(sock);
    WSACleanup();
    return 0;
}
