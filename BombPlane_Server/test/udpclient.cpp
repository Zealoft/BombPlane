#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <cstring>
#include <cstdio>
#include <cstdlib>
#include <unistd.h>
using namespace std;

#include "../src/BombPlane_proto.pb.h"
using namespace bombplane_proto;

#include "../src/connection.h"

short sseq, sack, rseq;
int userid;

void SendPackage(int sockfd, const char *content, short content_size)
{
    Package p;
    if (content == NULL)
        p.head.seq = htons(rseq - 1);
    else
        p.head.seq = htons(sseq++);
    p.head.len = htons(content_size + sizeof(Head));

    if (content != NULL)
        memcpy(p.content, content, content_size);
    int wlen = write(sockfd, (char *)&p, ntohs(p.head.len));
    printf("send seq = %d\n", sseq);

    printf("����len=%d��seq=%d��ʵ�ʷ���%dB\n", ntohs(p.head.len), ntohs(p.head.seq), wlen);
}

void RecvPackage(int sockfd, Package *p)
{
    char buffer[UDP_BUFFER_SIZE];
    char *cur = buffer;

    read(sockfd, buffer, sizeof(buffer));

    memcpy((char *)&(p->head), cur, sizeof(Head));
    cur += sizeof(Head);
    p->head.len = ntohs(p->head.len);
    p->head.seq = ntohs(p->head.seq);

    if (p->head.len == sizeof(Head))
        sack++;
    else
    {
        memcpy(p->content, cur, p->head.len - sizeof(Head));
        cur += p->head.len - sizeof(Head);
        rseq++;
    }
    printf("�յ�%dB��seq=%d\n", p->head.len, p->head.seq);
}

void Login(int sockfd)
{
    printf("\n\n��½����");

    int n;
    // c->s �����¼
    LoginRequest *lr = new LoginRequest; // ���ɾֲ��������ؿӣ�
    Message m;
    string s;

    lr->set_username("user");
    lr->set_password("123456");
    m.set_type(LOGIN_REQUEST);
    m.set_allocated_loginrequest(lr);
    printf("%s", m.DebugString().data());

    printf("���л�:%s", m.SerializeToString(&s) ? "suc" : "err");
    printf(" len=%d\n", s.size());

    SendPackage(sockfd, s.data(), s.size());
    printf("�����˵�¼����\n", n);

    // s->c �����¼����ACK
    Package p;
    RecvPackage(sockfd, &p);

    // s->c ��¼Ӧ��
    RecvPackage(sockfd, &p);

    // ����
    m.ParseFromArray(p.content, p.head.len - sizeof(Head));
    printf("���������\n%s", m.DebugString().data());

    SendPackage(sockfd, NULL, 0);

    LoginResponse lrp = m.loginresponse();
    userid = lrp.userid();
    printf("��¼�ɹ���userid=%d\n", userid);
}

void Invite(int sockfd)
{
    printf("\n\n�������");

    int n;
    InviteRequest *ir = new InviteRequest;
    Message m;
    string s;

    ir->set_dstuserid(userid+1);
    ir->set_srcuserid(userid);

    m.set_type(INVITE_REQUEST);
    m.set_allocated_inviterequest(ir);
    printf("%s", m.DebugString().data());

    printf("���л�:%s", m.SerializeToString(&s) ? "suc" : "err");
    printf(" len=%d\n", s.size());
        
    Package p;

    // ���ԣ�0/2/4/...���û��������� 1/3/5/...���û���������
    if (userid%2 == 0)
    {
        printf("�������:");
        printf("press enter...\n");
        getchar();

        SendPackage(sockfd, s.data(), s.size());
        printf("��������������\n");

        // s->c ACK
        RecvPackage(sockfd, &p);

        // s->c invite response
        RecvPackage(sockfd, &p);
        printf("�յ�������ظ�\n");
       
        // c->s ack
        SendPackage(sockfd, NULL, 0);

        // roomready & ack
        RecvPackage(sockfd, &p);
        printf("�յ��˿���֪ͨ\n");
        SendPackage(sockfd, NULL, 0);


    }
    else
    {
        // s->c ��¼Ӧ��
        RecvPackage(sockfd, &p);
        // ����
        m.ParseFromArray(p.content, p.head.len - sizeof(Head));
        printf("���������\n%s", m.DebugString().data());

        SendPackage(sockfd, NULL, 0);
        printf("������ɣ����Խ�������:");
        printf("press enter...\n");
        getchar();


        InviteResponse *ir = new InviteResponse;
        Message m;
        string s;

        ir->set_srcuserid(userid-1);
        ir->set_accept(true);
        m.set_type(INVITE_RESPONSE);
        m.set_allocated_inviteresponse(ir);
        printf("%s", m.DebugString().data());

        printf("���л�:%s", m.SerializeToString(&s) ? "suc" : "err");
        printf(" len=%d\n", s.size());
        SendPackage(sockfd, s.data(), s.size());

        // s->c ACK
        RecvPackage(sockfd, &p);

        // roomready & ack
        RecvPackage(sockfd, &p);
        printf("�յ��˿���֪ͨ\n");
        SendPackage(sockfd, NULL, 0);

    }
}

void Quit(int sockfd)
{
    printf("\n�����˳�����...\n");
    Message m;
    string s;
    Package p;
    m.set_type(QUIT_NOTIFICATION);
    printf("%s", m.DebugString().data());
    m.SerializeToString(&s);
    SendPackage(sockfd, s.data(), s.size());
    RecvPackage(sockfd, &p);
}

void Keepalive(int sockfd)
{
    printf("client alive\n");
    Message m;
    string s;
    Package p;
    m.set_type(KEEPALIVE_REQUEST);
    printf("%s", m.DebugString().data());
    m.SerializeToString(&s);
    SendPackage(sockfd, s.data(), s.size());
    RecvPackage(sockfd, &p);

    RecvPackage(sockfd, &p);
    printf("server alive\n");
    SendPackage(sockfd, NULL, 0);
}
// args:ip port
int main(int argc, char **argv)
{
    int sockfd;
    struct sockaddr_in servaddr;

    if (argc != 3)
        return 0;

    bzero(&servaddr, sizeof(servaddr));
    servaddr.sin_family = AF_INET;
    servaddr.sin_port = htons(atoi(argv[2]));
    inet_pton(AF_INET, argv[1], &servaddr.sin_addr);

    sockfd = socket(AF_INET, SOCK_DGRAM, 0);

    char buffer[MAX_CONTENT_SIZE];
    socklen_t salen = sizeof(servaddr); // sockaddr length
    int n;

    // c->s ����������������
    n = sendto(sockfd, "hello?", 7, 0, (struct sockaddr *)&servaddr, sizeof(servaddr));
    printf("������%dB����������.\n", n);
    printf("----------\n");

    // �����ʱ�������·������󼴿�

    // s->c ���������¶˿ڻظ�������servaddr�����Ķ˿�
    // �Ӵ˿�ʼ��ÿ��������Ҫ��һ��ACK
    n = recvfrom(sockfd, buffer, sizeof(buffer), 0, (struct sockaddr *)&servaddr, &salen);
    rseq++;
    printf("�յ���%dB������Ӧ��:%s\n", n, buffer + sizeof(Head));
    printf("��ʱ�˿�:%d\n", ntohs(servaddr.sin_port));

    // ���׽�������������ڵ�ip�Ͷ˿ڰ󶨣������Ϳ���ֱ����read/write��
    connect(sockfd, (struct sockaddr *)&servaddr, salen);

    // c->s ACK
    SendPackage(sockfd, NULL, 0);

    Login(sockfd);
    // Invite(sockfd);

    while(1){
        Keepalive(sockfd);
        sleep(3);
    }
    // Quit(sockfd);
    return 0;
}