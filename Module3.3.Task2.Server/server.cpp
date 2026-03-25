#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <array>
#include <thread>
#include <mutex>

#pragma comment(lib, "ws2_32.lib")

const int PORT = 6002;
const int BUF_SIZE = 4096;

// ---- Game logic ----

struct Game {
    std::array<char, 9> board;
    char current; // 'X' or 'O'

    Game() { board.fill('.'); current = 'X'; }

    std::string board_str() const {
        std::string s;
        s += "\n";
        s += " ";  s += board[0]; s += " | "; s += board[1]; s += " | "; s += board[2]; s += "\n";
        s += "---+---+---\n";
        s += " ";  s += board[3]; s += " | "; s += board[4]; s += " | "; s += board[5]; s += "\n";
        s += "---+---+---\n";
        s += " ";  s += board[6]; s += " | "; s += board[7]; s += " | "; s += board[8]; s += "\n";
        return s;
    }

    bool make_move(int cell) {
        if (cell < 0 || cell > 8 || board[cell] != '.') return false;
        board[cell] = current;
        current = (current == 'X') ? 'O' : 'X';
        return true;
    }

    char check_winner() const {
        const int lines[8][3] = {
            {0,1,2},{3,4,5},{6,7,8},
            {0,3,6},{1,4,7},{2,5,8},
            {0,4,8},{2,4,6}
        };
        for (auto& l : lines) {
            if (board[l[0]] != '.' && board[l[0]] == board[l[1]] && board[l[1]] == board[l[2]])
                return board[l[0]];
        }
        for (char c : board) if (c == '.') return '\0';
        return 'D'; // draw
    }
};

// ---- Networking helpers ----

bool send_line(SOCKET s, const std::string& msg) {
    std::string m = msg + "\n";
    return send(s, m.c_str(), (int)m.size(), 0) > 0;
}

std::string recv_line(SOCKET s) {
    char buf[BUF_SIZE];
    int bytes = recv(s, buf, BUF_SIZE - 1, 0);
    if (bytes <= 0) return "";
    buf[bytes] = '\0';
    std::string line(buf);
    while (!line.empty() && (line.back() == '\r' || line.back() == '\n'))
        line.pop_back();
    return line;
}

// ---- Session: two players ----

void play_game(SOCKET p1, SOCKET p2) {
    send_line(p1, "You are X. Game starts!");
    send_line(p2, "You are O. Game starts!");

    Game game;

    while (true) {
        SOCKET current_sock = (game.current == 'X') ? p1 : p2;
        SOCKET other_sock   = (game.current == 'X') ? p2 : p1;

        std::string board = game.board_str();
        send_line(current_sock, board + "Your turn (" + game.current + "), enter cell (1-9):");
        send_line(other_sock,   board + "Waiting for opponent...");

        std::string input = recv_line(current_sock);
        if (input.empty()) {
            send_line(other_sock, "Opponent disconnected. You win!");
            break;
        }

        int cell = -1;
        try { cell = std::stoi(input) - 1; } catch (...) {}

        if (!game.make_move(cell)) {
            send_line(current_sock, "Invalid move, try again.");
            continue;
        }

        char winner = game.check_winner();
        if (winner != '\0') {
            std::string board_final = game.board_str();
            if (winner == 'D') {
                send_line(p1, board_final + "Draw!");
                send_line(p2, board_final + "Draw!");
            } else {
                SOCKET winner_sock = (winner == 'X') ? p1 : p2;
                SOCKET loser_sock  = (winner == 'X') ? p2 : p1;
                send_line(winner_sock, board_final + "You win!");
                send_line(loser_sock,  board_final + "You lose.");
            }
            break;
        }
    }

    closesocket(p1);
    closesocket(p2);
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

    std::cout << "Task2 Tic-Tac-Toe Server on port " << PORT << std::endl;
    std::cout << "Waiting for pairs of players..." << std::endl;

    while (true) {
        std::cout << "Waiting for player 1..." << std::endl;
        SOCKET p1 = accept(server_sock, nullptr, nullptr);
        send_line(p1, "Waiting for second player...");

        std::cout << "Waiting for player 2..." << std::endl;
        SOCKET p2 = accept(server_sock, nullptr, nullptr);

        std::cout << "Starting game!" << std::endl;
        std::thread(play_game, p1, p2).detach();
    }

    closesocket(server_sock);
    WSACleanup();
    return 0;
}
