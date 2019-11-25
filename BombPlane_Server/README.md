# �������ͻ���ͨ��

## �ṹ�嶨��

ÿ��UDP�����Ϊ`sizeof(Head) + MAX_CONTENT_SIZE`  
��`len == sizeof(Head)`��ʾ����һ��ACK��

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

## ͨ�Ź���

*��СΪ1�Ļ�������Э��*

����{"foo"}��ʾһ��UDP��������  
{len, seq, "bar"}��ʾһ��UDP������ŵ�Package�ṹ�������

�ͻ����������ӣ�  
c->s:{"hello!"}  
��c�����������ӣ���һ��������ذ��������ʱ���ط����ɣ�

�����ʹ����ʱ�˿ڻظ���  
s->c:{11, 0, "hello!"}

�ͻ���ACK��  
c->s:{4, 0, NULL}

�ͻ��˷��͵�¼����  
c->s:{20, 0, "..."}

�����ACK��  
s->c:{4, 0, NULL}

����˷��������û�״̬��  
s->c:{100, 1, "..."}

�ͻ���ACK:  
c->s:{4, 1, "..."}

......

## ����볬ʱ�ش�����

������A��B������A����ǰ������š�������š���������ACK��ŷֱ�Ϊrseq, sseq, sack  

A�������ݰ�p��

```c++
p.head.seq = sseq++;
SendPackage(B, p);
```

A�յ����Ϊseq�����ݰ�p��

```c++
if(seq == rseq)
{
    ResolvePackage(p);
    // ����ACK
    p.head.len = htons(sizeof(Head));
    p.head.seq = htons(rseq);
    SendPackage(B, p);
    rseq++;
}
else if(seq < rseq)
{
    // Bδ�յ�A����ACK����ʱ���ش���
    // �����Ҫ���·���ACK
    // ����1λ�������ڣ�ֻ���� seq==rseq-1
    p.head.len = htons(sizeof(Head));
    p.head.seq = htons(seq);
    SendPackage(B, p);
}
else
{
    // ��Ӧ���ֵ����
}
```

A�յ���һ�����Ϊseq��ACK��

```c++
if(seq == sack)
{
    sack++;
    // ��ʱӦ��sack==sseq
}
else if(seq < sack)
{
    // �ط������ݰ����Է����յ����ظ�ACK�ˣ����Լ���
}
else
{
    // ��Ӧ���ֵ����
}
```

## У���

��ʱδʵ��
