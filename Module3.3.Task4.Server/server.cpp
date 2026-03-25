#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <array>
#include <thread>

#pragma comment(lib, "ws2_32.lib")

const int PORT = 6004;
const int BUF_SIZE = 4096;

// ---- Game logic ----

struct Game {
    std::array<char, 9> board;
    char current;

    Game() { board.fill('.'); current = 'X'; }

    std::string board_str() const {
        std::string s = "\n";
        s += " "; s += board[0]; s += " | "; s += board[1]; s += " | "; s += board[2]; s += "\n";
        s += "---+---+---\n";
        s += " "; s += board[3]; s += " | "; s += board[4]; s += " | "; s += board[5]; s += "\n";
        s += "---+---+---\n";
        s += " "; s += board[6]; s += " | "; s += board[7]; s += " | "; s += board[8]; s += "\n";
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
        return 'D';
    }
};

// ---- Networking ----

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

// ---- Session ----

void play_game(SOCKET p1, SOCKET p2) {
    send_line(p1, "You are X. Game starts! Commands: RESIGN, DRAW.");
    send_line(p2, "You are O. Game starts! Commands: RESIGN, DRAW.");

    Game game;

    while (true) {
        SOCKET cur_sock   = (game.current == 'X') ? p1 : p2;
        SOCKET other_sock = (game.current == 'X') ? p2 : p1;
        char cur_sym      = game.current;

        send_line(cur_sock,   game.board_str() + "Your turn (" + cur_sym + "), enter cell (1-9) or RESIGN/DRAW:");
        send_line(other_sock, game.board_str() + "Waiting for opponent...");

        std::string input = recv_line(cur_sock);
        if (input.empty()) {
            send_line(other_sock, "Opponent disconnected. You win!");
            break;
        }

        if (input == "RESIGN") {
            send_line(cur_sock,   "You resigned.");
            send_line(other_sock, "Opponent resigned. You win!");
            break;
        }

        if (input == "DRAW") {
            // Offer draw — immediately ask the opponent to respond
            send_line(cur_sock,   "Draw offered. Waiting for opponent...");
            send_line(other_sock, "Opponent offers a draw. Type DRAW to accept, or a move to decline.");

            std::string response = recv_line(other_sock);
            if (response.empty()) {
                send_line(cur_sock, "Opponent disconnected. You win!");
                break;
            }
            if (response == "DRAW") {
                send_line(cur_sock,   "Draw accepted!");
                send_line(other_sock, "Draw accepted!");
                break;
            }
            // Declined — treat as the opponent's move
            send_line(cur_sock, "Draw declined. Opponent makes a move.");
            input = response; // fall through to move processing below, but it's other player's move
            // Switch cur/other for the move
            std::swap(cur_sock, other_sock);
            cur_sym = (cur_sym == 'X') ? 'O' : 'X';
        }

        int cell = -1;
        try { cell = std::stoi(input) - 1; } catch (...) {}

        if (!game.make_move(cell)) {
            send_line(cur_sock, "Invalid move, try again.");
            // Re-prompt this player
            continue;
        }

        char winner = game.check_winner();
        if (winner != '\0') {
            std::string board_final = game.board_str();
            if (winner == 'D') {
                send_line(p1, board_final + "Draw!");
                send_line(p2, board_final + "Draw!");
            } else {
                SOCKET w = (winner == 'X') ? p1 : p2;
                SOCKET l = (winner == 'X') ? p2 : p1;
                send_line(w, board_final + "You win!");
                send_line(l, board_final + "You lose.");
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

    std::cout << "Task4 Server on port " << PORT << " (with RESIGN/DRAW)" << std::endl;

    while (true) {
        std::cout << "Waiting for player 1..." << std::endl;
        SOCKET p1 = accept(server_sock, nullptr, nullptr);
        send_line(p1, "Waiting for second player...");

        std::cout << "Waiting for player 2..." << std::endl;
        SOCKET p2 = accept(server_sock, nullptr, nullptr);

        std::cout << "Game started!" << std::endl;
        std::thread(play_game, p1, p2).detach();
    }

    closesocket(server_sock);
    WSACleanup();
    return 0;
}
