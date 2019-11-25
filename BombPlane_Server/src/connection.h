#ifndef CONNECTION_H
#define CONNECTION_H

#include <queue>
#include <time.h>
#include <list>
#include <netinet/in.h>

#include "common.h"
#include "Game.h"

#define MAX_CONTENT_SIZE 1024
#define RETRANSMISSION_DELAY 2
#define UDP_BUFFER_SIZE 65536
#define ALIVE_TIME_OUT 10
#define LOG_BUFFER_SIZE 1024

struct Head
{
    short len;
    short seq;
};

// TODO:加校验和
struct Package
{
    Head head;
    char content[MAX_CONTENT_SIZE];
};

enum State
{
    SLogin, // 建立UDP连接，登录中
    SRoom,  // 等待邀请以及邀请中
    SGame   // 邀请成功，进入游戏
};

struct Connection
{
    State state;

    sockaddr_in sa;     // 用于判断重复的握手，网络序
    int sockfd;
    queue<Package> wqueue;
    short tseq;         // tail seq:写队列最后一个数据包的seq（如果非空）
    short sseq;         // send seq:准备发送的下个数据包的序列号
    short sack;         // send ack:下个收到的ack包的序列号
    short rseq;         // receive seq:下个收到的数据包序列号

    time_t beattime;    // 上次心跳/收到对方发包的时间
    time_t sendtime;    // 上次发消息的时间

    Game *game;
    list<Connection>::iterator oiter;   // opponent iterator
    int od;     // 先手offensive = 0，后手defensive = 1
    Player player;
};

int NewSocket(short *port, bool use_port);

void ProcessConnections(int connfd);

#endif