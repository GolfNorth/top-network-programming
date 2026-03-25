#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <array>
#include <thread>
#include <cstdlib>
#include <ctime>
#include <chrono>

#pragma comment(lib, "ws2_32.lib")

const int PORT = 6003;
const int BUF_SIZE = 4096;

// ---- Game logic ----

struct Game {
    std::array<char, 9> board;
    char current;

    Game() { board.fill('.'); current = 'X'; srand((unsigned)time(nullptr)); }

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

    // Simple AI: win if possible, block if needed, else random
    int ai_move() const {
        const int lines[8][3] = {
            {0,1,2},{3,4,5},{6,7,8},
            {0,3,6},{1,4,7},{2,5,8},
            {0,4,8},{2,4,6}
        };
        // Try to win
        for (auto& l : lines) {
            int empty = -1, count = 0;
            for (int i : l) {
                if (board[i] == current) count++;
                else if (board[i] == '.') empty = i;
            }
            if (count == 2 && empty != -1) return empty;
        }
        // Try to block
        char opp = (current == 'X') ? 'O' : 'X';
        for (auto& l : lines) {
            int empty = -1, count = 0;
            for (int i : l) {
                if (board[i] == opp) count++;
                else if (board[i] == '.') empty = i;
            }
            if (count == 2 && empty != -1) return empty;
        }
        // Center
        if (board[4] == '.') return 4;
        // Random
        std::array<int,9> free; int n = 0;
        for (int i = 0; i < 9; i++) if (board[i] == '.') free[n++] = i;
        return free[rand() % n];
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
    if (s == INVALID_SOCKET) return true; // computer player
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

// ---- Play session ----

enum class PlayerType { Human, Computer };

void play_session(SOCKET p1, SOCKET p2, PlayerType t1, PlayerType t2) {
    Game game;

    auto get_socket = [&](char sym) { return (sym == 'X') ? p1 : p2; };
    auto get_type   = [&](char sym) { return (sym == 'X') ? t1 : t2; };

    send_line(p1, "You are X");
    if (t2 == PlayerType::Human) send_line(p2, "You are O");

    while (true) {
        char cur = game.current;
        SOCKET cur_sock  = get_socket(cur);
        SOCKET other_sock = get_socket((cur == 'X') ? 'O' : 'X');
        PlayerType cur_type = get_type(cur);

        int cell = -1;

        if (cur_type == PlayerType::Computer) {
            // AI pause for realism
            std::this_thread::sleep_for(std::chrono::milliseconds(500));
            cell = game.ai_move();
            send_line(p1, game.board_str() + "Computer (" + cur + ") plays cell " + std::to_string(cell + 1));
            if (t2 == PlayerType::Human)
                send_line(p2, game.board_str() + "Computer (" + cur + ") plays cell " + std::to_string(cell + 1));
        } else {
            // Human turn
            send_line(cur_sock, game.board_str() + "Your turn (" + cur + "), enter cell (1-9):");
            if (other_sock != INVALID_SOCKET && get_type((cur == 'X') ? 'O' : 'X') == PlayerType::Human)
                send_line(other_sock, game.board_str() + "Waiting for opponent...");

            while (true) {
                std::string input = recv_line(cur_sock);
                if (input.empty()) {
                    send_line(other_sock, "Opponent disconnected.");
                    closesocket(p1); if (p2 != INVALID_SOCKET) closesocket(p2);
                    return;
                }
                try { cell = std::stoi(input) - 1; } catch (...) { cell = -1; }
                if (cell >= 0 && cell <= 8 && game.board[cell] == '.') break;
                send_line(cur_sock, "Invalid move, try again:");
            }
        }

        game.make_move(cell);

        char winner = game.check_winner();
        if (winner != '\0') {
            std::string board_final = game.board_str();
            if (winner == 'D') {
                send_line(p1, board_final + "Draw!");
                if (t2 == PlayerType::Human) send_line(p2, board_final + "Draw!");
            } else {
                if (t1 == PlayerType::Human && t2 == PlayerType::Human) {
                    SOCKET w = (winner == 'X') ? p1 : p2;
                    SOCKET l = (winner == 'X') ? p2 : p1;
                    send_line(w, board_final + "You win!");
                    send_line(l, board_final + "You lose.");
                } else if (t1 == PlayerType::Human) {
                    PlayerType human_type = (winner == 'X') ? t1 : t2; (void)human_type;
                    bool human_won = (winner == 'X' && t1 == PlayerType::Human) ||
                                    (winner == 'O' && t2 == PlayerType::Human);
                    send_line(p1, board_final + (human_won ? "You win!" : "Computer wins."));
                } else {
                    send_line(p1, board_final + "Computer " + winner + " wins!");
                }
            }
            break;
        }
    }

    closesocket(p1);
    if (p2 != INVALID_SOCKET) closesocket(p2);
}

// ---- Main ----

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

    std::cout << "Task3 Server on port " << PORT << " (game modes)" << std::endl;

    while (true) {
        std::cout << "Waiting for player 1..." << std::endl;
        SOCKET p1 = accept(server_sock, nullptr, nullptr);

        send_line(p1, "Select mode:\n1 - Human vs Human\n2 - Human vs Computer\n3 - Computer vs Computer\nEnter 1/2/3:");

        std::string mode_str = recv_line(p1);
        int mode = 0;
        try { mode = std::stoi(mode_str); } catch (...) {}

        if (mode == 1) {
            send_line(p1, "Waiting for second player...");
            std::cout << "Mode HH: waiting for player 2..." << std::endl;
            SOCKET p2 = accept(server_sock, nullptr, nullptr);
            std::thread(play_session, p1, p2, PlayerType::Human, PlayerType::Human).detach();
        } else if (mode == 2) {
            std::cout << "Mode HC: starting..." << std::endl;
            std::thread(play_session, p1, INVALID_SOCKET, PlayerType::Human, PlayerType::Computer).detach();
        } else if (mode == 3) {
            std::cout << "Mode CC: starting..." << std::endl;
            std::thread(play_session, p1, INVALID_SOCKET, PlayerType::Computer, PlayerType::Computer).detach();
        } else {
            send_line(p1, "Invalid mode.");
            closesocket(p1);
        }
    }

    closesocket(server_sock);
    WSACleanup();
    return 0;
}
