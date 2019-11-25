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

// TODO:��У���
struct Package
{
    Head head;
    char content[MAX_CONTENT_SIZE];
};

enum State
{
    SLogin, // ����UDP���ӣ���¼��
    SRoom,  // �ȴ������Լ�������
    SGame   // ����ɹ���������Ϸ
};

struct Connection
{
    State state;

    sockaddr_in sa;     // �����ж��ظ������֣�������
    int sockfd;
    queue<Package> wqueue;
    short tseq;         // tail seq:д�������һ�����ݰ���seq������ǿգ�
    short sseq;         // send seq:׼�����͵��¸����ݰ������к�
    short sack;         // send ack:�¸��յ���ack�������к�
    short rseq;         // receive seq:�¸��յ������ݰ����к�

    time_t beattime;    // �ϴ�����/�յ��Է�������ʱ��
    time_t sendtime;    // �ϴη���Ϣ��ʱ��

    Game *game;
    list<Connection>::iterator oiter;   // opponent iterator
    int od;     // ����offensive = 0������defensive = 1
    Player player;
};

int NewSocket(short *port, bool use_port);

void ProcessConnections(int connfd);

#endif