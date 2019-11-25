# 服务器客户端通信

## 结构体定义

每个UDP包最大为`sizeof(Head) + MAX_CONTENT_SIZE`  
当`len == sizeof(Head)`表示这是一个ACK包

```c++
struct Head
{
    short len;
    short seq;
};

struct Package
{
    Head head;
    char content[MAX_CONTENT_SIZE];
};
```

## 通信过程

*大小为1的滑动窗口协议*

下面{"foo"}表示一个UDP包的内容  
{len, seq, "bar"}表示一个UDP包里面放的Package结构体的内容

客户端请求连接：  
c->s:{"hello!"}  
（c主动发起连接，第一个包无需回包。如果超时，重发即可）

服务端使用临时端口回复：  
s->c:{11, 0, "hello!"}

客户端ACK：  
c->s:{4, 0, NULL}

客户端发送登录请求：  
c->s:{20, 0, "..."}

服务端ACK：  
s->c:{4, 0, NULL}

服务端发送其他用户状态：  
s->c:{100, 1, "..."}

客户端ACK:  
c->s:{4, 1, "..."}

......

## 序号与超时重传机制

假设有A、B两方，A方当前接收序号、发送序号、发送且已ACK序号分别为rseq, sseq, sack  

A发送数据包p：

```c++
p.head.seq = sseq++;
SendPackage(B, p);
```

A收到序号为seq的数据包p：

```c++
if(seq == rseq)
{
    ResolvePackage(p);
    // 发送ACK
    p.head.len = htons(sizeof(Head));
    p.head.seq = htons(rseq);
    SendPackage(B, p);
    rseq++;
}
else if(seq < rseq)
{
    // B未收到A发的ACK，超时后重传了
    // 因此需要重新发送ACK
    // 对于1位滑动窗口，只可能 seq==rseq-1
    p.head.len = htons(sizeof(Head));
    p.head.seq = htons(seq);
    SendPackage(B, p);
}
else
{
    // 不应出现的情况
}
```

A收到了一个序号为seq的ACK：

```c++
if(seq == sack)
{
    sack++;
    // 此时应有sack==sseq
}
else if(seq < sack)
{
    // 重发了数据包，对方都收到并回复ACK了，忽略即可
}
else
{
    // 不应出现的情况
}
```

## 校验和

暂时未实现
