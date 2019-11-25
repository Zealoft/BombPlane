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
		printf("���������Ŀ���㣡\n");
		return -1;
	}

	pid_t pid;

	// ��һ��fork
	if((pid = fork()) < 0)
	{
		perror("fork");
		exit(1);
	}
	else if(pid > 0)
		exit(0);
	
	// �ڶ���fork
	if ((pid = fork()) > 0)
		exit(0);

	//chdir("/");
	umask(0);

	short port = atoi(argv[1]);
	int connfd = NewSocket(&port, true);
	if(connfd < 0)
	{
		printf("�����׽��ֳ�ʼ��δ�ɹ���%d\n", errno);
		return -1;
	}
	printf("���ڵȴ�����...\n", port);

	ProcessConnections(connfd);

	printf("����������ֹ.\n");
	close(connfd);
	return 0;
}