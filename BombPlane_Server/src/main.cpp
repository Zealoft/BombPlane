#include <unistd.h>		// fork, close
#include <sys/stat.h>	// umask
#include <cstdlib>		// exit, atoi
#include <errno.h>
#include <cstdio>
using namespace std;

#include "connection.h"

int main(int argc, char *argv[])
{
	if(argc < 2)
	{
		printf("输入参数数目不足！\n");
		return -1;
	}

	pid_t pid;

	// 第一次fork
	if((pid = fork()) < 0)
	{
		perror("fork");
		exit(1);
	}
	else if(pid > 0)
		exit(0);
	
	// 第二次fork
	if ((pid = fork()) > 0)
		exit(0);

	//chdir("/");
	umask(0);

	short port = atoi(argv[1]);
	int connfd = NewSocket(&port, true);
	if(connfd < 0)
	{
		printf("连接套接字初始化未成功：%d\n", errno);
		return -1;
	}
	printf("正在等待连接...\n", port);

	ProcessConnections(connfd);

	printf("服务器已终止.\n");
	close(connfd);
	return 0;
}