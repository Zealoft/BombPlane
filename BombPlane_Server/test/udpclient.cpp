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

    printf("发送len=%d，seq=%d，实际发送%dB\n", ntohs(p.head.len), ntohs(p.head.seq), wlen);
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
    printf("收到%dB，seq=%d\n", p->head.len, p->head.seq);
}

void Login(int sockfd)
{
    printf("\n\n登陆测试");

    int n;
    // c->s 请求登录
    LoginRequest *lr = new LoginRequest; // 不可局部变量！重坑！
    Message m;
    string s;

    lr->set_username("user");
    lr->set_password("123456");
    m.set_type(LOGIN_REQUEST);
    m.set_allocated_loginrequest(lr);
    printf("%s", m.DebugString().data());

    printf("序列化:%s", m.SerializeToString(&s) ? "suc" : "err");
    printf(" len=%d\n", s.size());

    SendPackage(sockfd, s.data(), s.size());
    printf("发送了登录请求\n", n);

    // s->c 请求登录包的ACK
    Package p;
    RecvPackage(sockfd, &p);

    // s->c 登录应答
    RecvPackage(sockfd, &p);

    // 解析
    m.ParseFromArray(p.content, p.head.len - sizeof(Head));
    printf("解析结果：\n%s", m.DebugString().data());

    SendPackage(sockfd, NULL, 0);

    LoginResponse lrp = m.loginresponse();
    userid = lrp.userid();
    printf("登录成功！userid=%d\n", userid);
}

void Invite(int sockfd)
{
    printf("\n\n邀请测试");

    int n;
    InviteRequest *ir = new InviteRequest;
    Message m;
    string s;

    ir->set_dstuserid(userid+1);
    ir->set_srcuserid(userid);

    m.set_type(INVITE_REQUEST);
    m.set_allocated_inviterequest(ir);
    printf("%s", m.DebugString().data());

    printf("序列化:%s", m.SerializeToString(&s) ? "suc" : "err");
    printf(" len=%d\n", s.size());
        
    Package p;

    // 测试：0/2/4/...号用户发送邀请 1/3/5/...号用户接收邀请
    if (userid%2 == 0)
    {
        printf("邀请测试:");
        printf("press enter...\n");
        getchar();

        SendPackage(sockfd, s.data(), s.size());
        printf("发送了邀请请求\n");

        // s->c ACK
        RecvPackage(sockfd, &p);

        // s->c invite response
        RecvPackage(sockfd, &p);
        printf("收到了邀请回复\n");
       
        // c->s ack
        SendPackage(sockfd, NULL, 0);

        // roomready & ack
        RecvPackage(sockfd, &p);
        printf("收到了开房通知\n");
        SendPackage(sockfd, NULL, 0);


    }
    else
    {
        // s->c 登录应答
        RecvPackage(sockfd, &p);
        // 解析
        m.ParseFromArray(p.content, p.head.len - sizeof(Head));
        printf("解析结果：\n%s", m.DebugString().data());

        SendPackage(sockfd, NULL, 0);
        printf("邀请完成！测试接受邀请:");
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

        printf("序列化:%s", m.SerializeToString(&s) ? "suc" : "err");
        printf(" len=%d\n", s.size());
        SendPackage(sockfd, s.data(), s.size());

        // s->c ACK
        RecvPackage(sockfd, &p);

        // roomready & ack
        RecvPackage(sockfd, &p);
        printf("收到了开房通知\n");
        SendPackage(sockfd, NULL, 0);

    }
}

void Quit(int sockfd)
{
    printf("\n正在退出连接...\n");
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

    // c->s 主动发起连接请求
    n = sendto(sockfd, "hello?", 7, 0, (struct sockaddr *)&servaddr, sizeof(servaddr));
    printf("发送了%dB的连接请求.\n", n);
    printf("----------\n");

    // 如果超时，则重新发起请求即可

    // s->c 服务器从新端口回复，覆盖servaddr变量的端口
    // 从此开始，每个包都需要有一个ACK
    n = recvfrom(sockfd, buffer, sizeof(buffer), 0, (struct sockaddr *)&servaddr, &salen);
    rseq++;
    printf("收到了%dB的连接应答:%s\n", n, buffer + sizeof(Head));
    printf("临时端口:%d\n", ntohs(servaddr.sin_port));

    // 将套接字与服务器现在的ip和端口绑定，这样就可以直接用read/write了
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