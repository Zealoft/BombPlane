#include <unistd.h>
#include <sys/socket.h>
#include <sys/ioctl.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <cstdio>
#include <cstring>
#include <errno.h>
#include <list>
#include <iostream>
#include <signal.h>
using namespace std;

#include "BombPlane_proto.pb.h"
using namespace bombplane_proto;

#include "connection.h"
#include "sql.h"

list<Connection> conl;

// 工具函数

void DumpArray(const char *buffer, int size)
{
	int i;
	for (i = 0; i < size; i++)
	{
		printf("%02x ", (unsigned char)buffer[i]);
		if (i % 16 == 0 && i != 0)
			printf("\n");
	}
	printf("\n");
}

void InitPosTrans(InitposNotification ipr, PlaneLoc *pl)
{
	pl[0][0][0] = ipr.loc1().pos1().y();
	pl[0][0][1] = ipr.loc1().pos1().x();
	pl[0][1][0] = ipr.loc1().pos2().y();
	pl[0][1][1] = ipr.loc1().pos2().x();
	pl[0][2][0] = ipr.loc1().pos3().y();
	pl[0][2][1] = ipr.loc1().pos3().x();

    
	pl[1][0][0] = ipr.loc2().pos1().y();
	pl[1][0][1] = ipr.loc2().pos1().x();
	pl[1][1][0] = ipr.loc2().pos2().y();
	pl[1][1][1] = ipr.loc2().pos2().x();
	pl[1][2][0] = ipr.loc2().pos3().y();
	pl[1][2][1] = ipr.loc2().pos3().x();

	
	pl[2][0][0] = ipr.loc3().pos1().y();
	pl[2][0][1] = ipr.loc3().pos1().x();
	pl[2][1][0] = ipr.loc3().pos2().y();
	pl[2][1][1] = ipr.loc3().pos2().x();
	pl[2][2][0] = ipr.loc3().pos3().y();
	pl[2][2][1] = ipr.loc3().pos3().x();
}

void PlaneLocatorTrans(PlaneLocator plr, PlaneLoc *pl)
{
	pl[0][0][0] = plr.pos1().y();
	pl[0][0][1] = plr.pos1().x();
	pl[0][1][0] = plr.pos2().y();
	pl[0][1][1] = plr.pos2().x();
	pl[0][2][0] = plr.pos3().y();
	pl[0][2][1] = plr.pos3().x();
}

void Log(string s)
{
	FILE *fp=fopen("log", "a");
    if(fp == NULL)
    {
        printf("打开日志文件失败:%d\n", errno);
        return;
    }

    time_t t = time(0);
    tm *ptm = localtime(&t);
    char gettime[21];
    sprintf(gettime, "%d-%02d-%02d %02d:%02d:%02d ", ptm->tm_year+1900, ptm->tm_mon+1, ptm->tm_mday, ptm->tm_hour, ptm->tm_min, ptm->tm_sec);

    fwrite(gettime, 1, strlen(gettime), fp);
	s += '\n';
    fwrite(s.data(), 1, s.size(), fp);
    fclose(fp);
    printf(s.data());
}

// 提前声明

// 防止重名加上R前缀
enum EndReason
{
	RKICK,	// 踢下旧同名登录
	RQUIT,	// 用户主动退出
	RERROR	// 用户报文异常或心跳超时
};
void EndConnection(list<Connection>::iterator &iter, EndReason r);

/*******************************************/
/*               数据包处理层               */
/*******************************************/

// 造包并将数据包加入队列尾
// content = NULL表示是ack包，content_size不为0表示是对于重传包的ack
// 发送的时机为：队列非空 && sack==sseq && select写返回
void MakePackage(const list<Connection>::iterator &iter, const char *content, int content_size)
{
	Package p;
	if (content == NULL)
		p.head.len = 4;
	else
		p.head.len = content_size > MAX_CONTENT_SIZE ? sizeof(Head) + MAX_CONTENT_SIZE : sizeof(Head) + content_size;

	if (content == NULL)
	{
		if (content_size == 0)
			p.head.seq = iter->rseq++;
		else
			p.head.seq = iter->rseq - 1; // 对对方重传的数据包ACK，由于是1位滑动窗口，重传包seq==rseq-1
	}
	else
	{
		if (iter->wqueue.empty())
			p.head.seq = iter->sseq;
		else
			p.head.seq = iter->tseq + 1;	// 写队列最后一个数据包的seq+1
		memcpy(p.content, content, content_size);
	}

	printf("<MAKE>fd%d：报文头：len=%d, seq=%d\n", iter->sockfd, p.head.len, p.head.seq);
	iter->wqueue.push(p);
	// 作为写队列最后一个数据包，需要记录其seq
	if (p.head.len > sizeof(Head))
		iter->tseq = p.head.seq;
	/***** 仅用于调试，使用LITE版PROTOBUF时必须删除 *****/
	/*
	if (content != NULL)
	{
		Message m;
		m.ParseFromArray(content, content_size);
		printf("解析包：\n%s", m.DebugString().data());
	}
	/*************************************************/
}

void PLogin(list<Connection>::iterator &iter, Message m)
{
	switch (m.type())
	{
	case LOGIN_REQUEST:
	{
		Message nm;	// new messsage
		string s;

		// 与数据库连接判断登录结果
		LoginResponse *lrp = new LoginResponse;
		iter->player.name = m.loginrequest().username();
		SQL_RESULT r = Login(iter->player, m.loginrequest().password());
		if (r == SUC)
		{
			// 如果是重复登录，把旧登录踢下线
			list<Connection>::iterator it;
			bool kick = false;
			for (it = conl.begin();it != conl.end();it++)
			{
				if (it->player.id == iter->player.id && it != iter)
				{
					EndConnection(it, RKICK);
					kick = true;
				}
			}
			if (kick)
				lrp->set_state(LoginResponse::SUCCESS_KICK);
			else
				lrp->set_state(LoginResponse::SUCCESS);
			lrp->set_userid(iter->player.id);
		}
		else if (r == ERR_NE)
			lrp->set_state(LoginResponse::NOT_EXIST);
		else if (r == ERR_WP)
			lrp->set_state(LoginResponse::WRONG_PASSWORD);
		else if (r == ERR_IN)
			lrp->set_state(LoginResponse::SERVER_ERROR);

		nm.set_type(LOGIN_RESPONSE);
		nm.set_allocated_loginresponse(lrp);
		nm.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());
		printf("用户%s登陆的结果为：%d\n", m.loginrequest().username().data(), r);
		if (r != SUC)
			break;
		// 登录成功才可到达此处

		// 向登录用户发送在线列表，向其他已登录用户发送上线通知
		list<Connection>::iterator it;
		OnlineUser *ou;
		OnlinelistNotification *oln = new OnlinelistNotification;
		UpdateonlineBroadcast *uob = new UpdateonlineBroadcast;

		nm.Clear();
		nm.set_type(UPDATEONLINE_BROADCAST);
		uob->set_username(iter->player.name);
		uob->set_userid(iter->player.id);
		uob->set_online(true);
		nm.set_allocated_updateonlinebroadcast(uob);
		nm.SerializeToString(&s);
		for (it = conl.begin();it != conl.end();it++)
		{
			if(it->state != SLogin)
			{
				// 将已登录的用户添加到在线列表中
				ou = oln->add_onlinelist();
				ou->set_username(it->player.name);
				ou->set_userid(it->player.id);
				ou->set_inroom(it->state == SGame);

				// 向其他已登录用户发送上线通知
				MakePackage(it, s.data(), s.size());
			}
		}
		nm.Clear();
		nm.set_type(ONLINELIST_NOTIFICATION);
		nm.set_allocated_onlinelistnotification(oln);
		nm.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());

		iter->state = SRoom;
		break;
	}
	}
}

void PRoom(const list<Connection>::iterator &iter, Message m)
{
	switch(m.type())
	{
	case INVITE_REQUEST:
	{
		int src = m.inviterequest().srcuserid();
		int dst = m.inviterequest().dstuserid();

		// 自己的userid填错了
		if (src != iter->player.id)
		{
			src = iter->player.id;
			m.mutable_inviterequest()->set_srcuserid(src);
		}

		list<Connection>::iterator diter;
		for (diter = conl.begin(); diter != conl.end(); ++diter)
		{
			// TODO:邀请送达时，对方也处于邀请的状态
			// 在客户端处理，如果服务器也出现此情况，可以立即回复InviteResponse，userid=0
			if (diter != iter && diter->player.id == dst)
				break;
		}

		if (diter == conl.end())
		{
			// TODO:没找到接收方，对方未注册或已下线
			// 在客户端处理，如果服务器也出现此情况，可以立即回复InviteResponse，userid=-1
			printf("未找到被邀请者 id=%d\n", dst);
		}
		else
		{
			string s;
			printf("成功找到被邀请者 id=%d,开始转发\n", dst);
			m.SerializeToString(&s);
			MakePackage(diter, s.data(), s.size());

			// 向所有用户发送UPDATEROOM
			UpdateroomBroadcast *urb = new UpdateroomBroadcast;
			Message nm;
			urb->set_userid1(src);
			urb->set_userid2(dst);
			urb->set_inout(true);

			nm.set_type(UPDATEROOM_BROADCAST);
			nm.set_allocated_updateroombroadcast(urb);

			nm.SerializeToString(&s);

			// 向除了AB以外的登录客户端广播
			list<Connection>::iterator it;
			for (it = conl.begin(); it != conl.end(); ++it)
			{
				if (it != iter && it != diter && it->state != SLogin)
					MakePackage(it, s.data(), s.size());
			}
		}
		break;
	}
	case INVITE_RESPONSE:
	{
		int src = m.inviteresponse().srcuserid();
		int dst = iter->player.id;

		list<Connection>::iterator siter;
		for (siter = conl.begin(); siter != conl.end(); ++siter)
		{
			if (siter != iter && siter->player.id == src)
				break;
		}

		if (siter == conl.end())
		{
			// 没找到邀请者，说明已经下线
			printf("未找到邀请回复的接收方 id=%d\n", src);
			// TODO:此情况为A发完邀请后立刻掉线，需要通知B
		}
		else
		{
			printf("成功找到邀请者 id=%d，对方选择了%s，开始转发\n", src, m.inviteresponse().accept() ? "接受" : "拒绝");
			string s;
			m.SerializeToString(&s);
			MakePackage(siter, s.data(), s.size());

			if (m.inviteresponse().accept() == true)
			{
				// 接收邀请，准备开始游戏，随机先后手
				srand(time(0));
				siter->od = rand() % 2;
				iter->od = !siter->od;
				iter->oiter = siter;
				siter->oiter = iter;
				iter->game = siter->game = new Game();

				iter->state = SGame;
				iter->oiter->state = SGame;
				Log("开始游戏：" + iter->player.name + " vs " + siter->player.name +
					"（先手者：" + (iter->od == 0 ? iter->player.name : iter->player.name) + "）");
			}
			else
			{
				// 拒绝邀请，向其他在线用户发送UPDATEROOM
				UpdateroomBroadcast *urb = new UpdateroomBroadcast;
				Message nm;
				urb->set_userid1(src);
				urb->set_userid2(dst);
				urb->set_inout(false);

				nm.set_type(UPDATEROOM_BROADCAST);
				nm.set_allocated_updateroombroadcast(urb);

				nm.SerializeToString(&s);

				list<Connection>::iterator it;
				for (it = conl.begin(); it != conl.end(); ++it)
				{
					if (it != iter && it != siter && it->state != SLogin)
						MakePackage(it, s.data(), s.size());
				}
			}
		}
		break;
	}
	}
}

void PGame(const list<Connection>::iterator &iter, Message m)
{
	Message nm;
	string s;

	switch(m.type())
	{
	case INITPOS_NOTIFICATION:
	{
		// 如果为NULL，表明不是第一局游戏
		if (iter->game == NULL)
		{
			srand(time(0));
			iter->od = rand() % 2;
			iter->oiter->od = !iter->od;
			iter->game = iter->oiter->game = new Game;
			Log("重新开始游戏：" + iter->player.name + " vs " + iter->oiter->player.name +
				"（先手者：" + (iter->od == 0 ? iter->player.name : iter->oiter->player.name) +"）");
		}

		// 初始化飞机坐标
		PlaneLoc pl[3];
		InitPosTrans(m.initposnotification(), pl);
		bool ready = iter->game->InitPos(pl, iter->od);
		// 输出日志
		char buffer[LOG_BUFFER_SIZE];
		sprintf(buffer, "Plane1:(%d, %d), (%d, %d), (%d, %d)\nPlane2:(%d, %d), (%d, %d), (%d, %d)\nPlane3:(%d, %d), (%d, %d), (%d, %d)",
			pl[0][0][0], pl[0][0][1], pl[0][1][0], pl[0][1][1], pl[0][2][0], pl[0][2][1],
			pl[1][0][0], pl[1][0][1], pl[1][1][0], pl[1][1][1], pl[1][2][0], pl[1][2][1],
			pl[2][0][0], pl[2][0][1], pl[2][1][0], pl[2][1][1], pl[2][2][0], pl[2][2][1]);
		Log("玩家" + iter->player.name + "已放置完毕：\n" + buffer);
		// 双方都放置完毕
		if (ready)
		{
			GamestartNotification *gsn = new GamestartNotification;
			gsn->set_userid(iter->od == 0 ? iter->player.id : iter->oiter->player.id);
			nm.set_type(GAMESTART_NOTIFICATION);
			nm.set_allocated_gamestartnotification(gsn);
			nm.SerializeToString(&s);
			MakePackage(iter, s.data(), s.size());
			MakePackage(iter->oiter, s.data(), s.size());
			Log("双方玩家都已放置完毕！");
		}
		break;
	}
	case BOMB_REQUEST:
	{
		Coord cd;
		cd[0] = m.bombrequest().pos().y();
		cd[1] = m.bombrequest().pos().x();
		BombResult r = iter->oiter->game->Bomb(cd, !iter->od);

		BombResponse *brp = new BombResponse;
		Coordinate *pcd = new Coordinate;
		pcd->set_y(cd[0]);
		pcd->set_x(cd[1]);
		if (r == MISS)
			brp->set_res(BombResponse::MISS);
		else if (r == HIT)
			brp->set_res(BombResponse::HIT);
		else if (r == DESTROYED)
			brp->set_res(BombResponse::DESTORYED);
		brp->set_allocated_pos(pcd);
		nm.set_type(BOMB_RESPONSE);
		nm.set_allocated_bombresponse(brp);
		nm.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());
		MakePackage(iter->oiter, s.data(), s.size());

		// 输出日志
		char buffer[LOG_BUFFER_SIZE];
		sprintf(buffer, "(%d, %d)，结果：%d", cd[0], cd[1], r);
		Log("玩家" + iter->player.name + "轰炸" + buffer);
		break;
	}
	case GUESS_REQUEST:
	{
		PlaneLoc pl;
		PlaneLocatorTrans(m.guessrequest().loc(), &pl);
		bool r = iter->game->Reveal(pl, !iter->od);

		PlaneLocator *plr = new PlaneLocator(m.guessrequest().loc());
		GuessResponse *grp = new GuessResponse;
		grp->set_destroyed(r);
		grp->set_allocated_loc(plr);
		nm.set_type(GUESS_RESPONSE);
		nm.set_allocated_guessresponse(grp);
		nm.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());
		MakePackage(iter->oiter, s.data(), s.size());

		// 输出日志
		char buffer[LOG_BUFFER_SIZE];
		sprintf(buffer, "(%d, %d), (%d, %d), (%d, %d)，结果：%d", pl[0][0], pl[0][1], pl[1][0], pl[1][1], pl[2][0], pl[2][1], r);
		Log("玩家" + iter->player.name + "发起猜测：" + buffer);

		// 判断游戏是否已经产生结果
		int w = iter->game->Win();
		if (w == -1)
			break;

		GameoverNotification *gon = new GameoverNotification;
		// 只有本方可能胜利
		gon->set_winnerid(iter->player.id);
		//if (iter->od == 0)
		//	gon->set_winnerid(w == 0 ? iter->player.id : iter->oiter->player.id);
		//else
		//	gon->set_winnerid(w == 1 ? iter->player.id : iter->oiter->player.id);
		nm.set_type(GAMEOVER_NOTIFICATION);
		nm.set_allocated_gameovernotification(gon);
		nm.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());
		MakePackage(iter->oiter, s.data(), s.size());

		delete iter->game;
		iter->game = NULL;
		iter->oiter->game = NULL;
		Log("玩家" + iter->player.name + "赢得胜利！");
		break;
	}
	case EXITGAME_NOTIFICATION:
		delete iter->game;
		iter->state = SRoom;
		iter->oiter->state = SRoom;

		// 通知对手已下线
		GamecrushNotification *gcn = new GamecrushNotification;
		gcn->set_reason(GamecrushNotification::OPPONENT_OFF);
		nm.set_type(GAMECRUSH_NOTIFICATION);
		nm.set_allocated_gamecrushnotification(gcn);
		nm.SerializeToString(&s);
		MakePackage(iter->oiter, s.data(), s.size());
		Log("玩家" + iter->player.name + "退出游戏！");

		// 离开房间，向其他在线用户发送UPDATEROOM
		UpdateroomBroadcast *urb = new UpdateroomBroadcast;
		Message nm;
		urb->set_userid1(iter->player.id);
		urb->set_userid2(iter->oiter->player.id);
		urb->set_inout(false);

		nm.set_type(UPDATEROOM_BROADCAST);
		nm.set_allocated_updateroombroadcast(urb);
		nm.SerializeToString(&s);
		list<Connection>::iterator it;
		for (it = conl.begin(); it != conl.end(); ++it)
		{
			if (it != iter && it != iter->oiter && it->state != SLogin)
				MakePackage(it, s.data(), s.size());
		}
		break;
	}
}

// 解析收到的数据包
void ResolvePackage(list<Connection>::iterator &iter, Package p)
{
	Message m;
	m.ParseFromArray(p.content, p.head.len - sizeof(Head));

	// 注：switch中定义变量需要在case后加大括号
	switch (m.type())
	{
	case KEEPALIVE_REQUEST:
	{
		Message nm;
		string s;
		nm.set_type(KEEPALIVE_RESPONSE);
		nm.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());
		iter->beattime = time(0);
		break;
	}
	case QUIT_NOTIFICATION:
		printf("fd%d主动断开连接\n", iter->sockfd);
		EndConnection(iter, RQUIT);
		break;
	default:
		switch(iter->state)
		{
		case SLogin:
			PLogin(iter, m);
			break;
		case SRoom:
			PRoom(iter, m);
			break;
		case SGame:
			PGame(iter, m);
			break;
		}
	}
}

/*******************************************/
/*               UDP连接处理层              */
/*******************************************/

// 创建，设置并绑定一个新套接字
// port:指定的端口号，或者填入自动选择的端口号
// use_port:是否使用指定的端口，若否，使用自动选择的端口
int NewSocket(short *port, bool use_port)
{
	const static short port_start = 21000; 	// 绑定端口的开始位置
	static short port_cur = port_start;		// 当前准备尝试的端口
	const int try_times = 50;				// 连续尝试端口的次数

	// 创建和设置套接字
	int sockfd;
	int nonblock = 1;

	if ((sockfd = socket(AF_INET, SOCK_DGRAM, 0)) == -1)
		return -1;
	if (ioctl(sockfd, FIONBIO, &nonblock) == -1)
	{
		close(sockfd);
		return -1;
	}

	// 设置服务端地址结构体
	struct sockaddr_in my_addr;

	my_addr.sin_family = AF_INET;
	if (use_port)
		my_addr.sin_port = htons(*port);
	else
		my_addr.sin_port = htons(port_cur);
	my_addr.sin_addr.s_addr = htonl(INADDR_ANY);
	bzero(&(my_addr.sin_zero), sizeof(my_addr.sin_zero));

	// 绑定地址结构体和socket
	if (bind(sockfd, (struct sockaddr *)&my_addr, sizeof(struct sockaddr)) < 0)
	{
		if (use_port)
		{
			printf("端口%u绑定失败\n", *port);
			return -1;
		}

		printf("端口%u绑定失败直到...\n", port_cur);
		int i;
		for (i = 0; i < try_times; i++)
		{
			my_addr.sin_port = htons(++port_cur);
			if (bind(sockfd, (struct sockaddr *)&my_addr, sizeof(struct sockaddr)) == 0)
				break;
		}

		if (i == try_times)
		{
			printf("尝试%d次都没有成功！\n", try_times);
			close(sockfd);
			return -1;
		}
	}

	// 只有绑定成功可能到达此处

	if (!use_port)
		*port = port_cur++;

	printf("端口%u绑定成功\n", *port);

	return sockfd;
}

// 通过连接套接字联络，绑定一个临时套接字，相当于握手
// 临时套接字用于后来的数据传输
// 注：众所周知的端口永不发送数据到客户端
void ShakeHands(int connfd)
{
	struct sockaddr_in cliaddr;
	socklen_t salen = sizeof(cliaddr); // sockaddr length 必须初始化，否则EINVAL
	char buffer[MAX_CONTENT_SIZE];
	int len;
	char ipstr[16];

	len = recvfrom(connfd, buffer, sizeof(buffer), 0, (struct sockaddr *)&cliaddr, &salen);
	if (len < 0)
	{
		printf("握手包读取失败：%d\n", errno);
		return;
	}

	inet_ntop(AF_INET, &cliaddr.sin_addr, ipstr, 16);
	printf("来自%s:%u的连接（请求包为%dB）\n", ipstr, ntohs(cliaddr.sin_port), len);
	if (len > 0)
		printf("请求内容为：%s\n", buffer);

	// 判断重复的握手
	list<Connection>::iterator iter;
	for (iter = conl.begin();iter != conl.end();iter++)
	{
		// 直接sockaddr_in相等的判断会编译报错
		if (iter->sa.sin_addr.s_addr == cliaddr.sin_addr.s_addr && iter->sa.sin_port == cliaddr.sin_port)
		{
			printf("与fd%d的ip和端口重复，拒绝连接\n", iter->sockfd);
			return;
		}
	}

	// 接受握手
	short port;
	int sockfd = NewSocket(&port, false);
	if (sockfd < 0)
		return;
	printf("检查无冲突，接受连接：fd%d\n", sockfd);

	if (connect(sockfd, (struct sockaddr *)&cliaddr, salen) < 0)
	{
		printf("与%s:%u连接失败：%d", ipstr, ntohs(cliaddr.sin_port), errno);
		return;
	}

	static int client_id = 0;

	Connection con;
	con.sa = cliaddr;
	con.sockfd = sockfd;
	con.state = SLogin;
	con.beattime = time(0);
	con.sendtime = 0;
	con.sseq = 0;
	con.sack = 0;
	con.rseq = 0;
	conl.push_back(con);

	// 发送一个问候包，用于告知端口号
	// 从此以后，每个包都需要ACK
	char s[7] = "Hello!";
	MakePackage(--conl.end(), s, sizeof(s));
}

// 取指定连接的写队列队首发送出去
// 调用时机：
// 1、select写返回(again==false)
// 2、超时重传(again==true)
// 3、关闭连接前(force==true)
void SendPackage(const list<Connection>::iterator &iter, bool again, bool force = false)
{
	Package p = iter->wqueue.front();
	int len = p.head.len, wlen;

	printf("<SEND>fd%d：len=%d, seq=%d，结果：", iter->sockfd, len, p.head.seq);
	if (0)
	{
		// 用于测试超时重传，50%概率不发ACK
		if(p.head.len == sizeof(Head))
		{
			srand(time(0));
			int x = rand() % 2;
			if (x)
			{
				printf("此次ACK不发(seq=%d)！\n", p.head.seq);
				iter->wqueue.pop();
				return;
			}
		}
	}
	p.head.len = htons(p.head.len);
	p.head.seq = htons(p.head.seq);

	wlen = write(iter->sockfd, (char *)&p, len);
	if (wlen == -1)
	{
		printf("错误：%d\n", errno);
		return;
	}
	else if (wlen < len)
		printf("实际写入%dB\n", wlen);
	else
		printf("成功！\n");

	if (force)
		iter->wqueue.pop();
	else
	{
		// ACK发了就发了，不会重发
		// 数据包要等收到了ACK再pop
		if (len == sizeof(Head))
			iter->wqueue.pop();
		else
		{
			if (!again)
				iter->sseq++;
			iter->sendtime = time(0);
		}
	}
}

// 从某个连接收到数据包
// select读返回时自动调用
// TODO:考虑一个数据包被截断的情况：加尾部分隔符
// TODO:判断读取到的东西比实际上少：加尾部分隔符
void RecvPackage(list<Connection>::iterator &iter)
{
	char buffer[UDP_BUFFER_SIZE];
	char *cur;
	int rlen;
	Package p;

	// 必须一次读完，要不然会被丢弃，导致第二次读取EAGAIN
	rlen = read(iter->sockfd, buffer, UDP_BUFFER_SIZE);
	DumpArray(buffer, rlen);
	printf("<RECEIVE>fd%d：", iter->sockfd);
	if (rlen < 0)
	{
		printf("错误：%d\n", iter->sockfd, errno);
		return;
	}
	else
		printf("%dB的报文\n", rlen);
	cur = buffer;

	// 更新心跳时间
	iter->beattime = time(0);

	// 可能多个UDP报文被拼起来了，不过暂时没有遇到
	// 循环解析每个报文，假设每个报文都是完整的，报头填写的长度也是正确的
	// 由于是一位滑动窗口协议，只可能同时有两个包（不考虑超时重传时）
	while (cur < buffer + rlen - 1)
	{
		if (cur != buffer)
			printf("出现多个UDP报文被拼成一个！\n");

		// 解析报文头
		memcpy((char *)&p.head, cur, sizeof(Head));
		cur += sizeof(Head);
		p.head.len = ntohs(p.head.len);
		p.head.seq = ntohs(p.head.seq);
		if (p.head.len < sizeof(Head))
		{
			printf("报头填写的长度比报头长度还小:%d\n", p.head.len);
			EndConnection(iter, RERROR);
			return;
		}
		printf("报文头：len=%d, seq=%d\n", p.head.len, p.head.seq);

		if (p.head.len == sizeof(Head))
		{
			printf("收到的是ACK：");
			if (p.head.seq < iter->sack)
			{
				printf("重发了数据包，对方都收到并回复了ACK\n");
			}
			else if (p.head.seq > iter->sack)
			{
				printf("序号异常！\n");
				EndConnection(iter, RERROR);
			}
			else
			{
				printf("正常！\n");
				iter->wqueue.pop();
				iter->sack++;
			}
			return;
		}

		memcpy(p.content, cur, p.head.len - sizeof(Head));
		cur += p.head.len - sizeof(Head);
		printf("收到的是数据包，");

		if (p.head.seq < iter->rseq)
		{
			// ACK丢了，对方重传，本方重新ACK
			printf("当前rseq=%d，重新ACK\n", iter->rseq);
			/******************/
			Package p;
			p.head.len = htons(sizeof(Head));
			p.head.seq = htons(iter->rseq);
			int wlen = write(iter->sockfd, (char *)&p, ntohs(p.head.len));
			printf("结果：%dB\n", wlen);
			/******************/
			//MakePackage(iter, NULL, 1);
		}
		else if (p.head.seq > iter->rseq)
		{
			printf("客户端发送序号异常！\n");
			EndConnection(iter, RERROR);
		}
		else
		{
			printf("正常！\n");
			/***** 仅用于调试，使用LITE版PROTOBUF时必须删除 *****/
			/*
			Message m;
			m.ParseFromArray(p.content, p.head.len - sizeof(Head));
			printf("解析包：\n%s", m.DebugString().data());
			/*************************************************/
			MakePackage(iter, NULL, 0);
			ResolvePackage(iter, p);

		}
	}
}

// 所有的客户端退出必须经过此处
// 此处的迭代器必须要使用引用
void EndConnection(list<Connection>::iterator &iter, EndReason r)
{
	printf("关闭fd%d的连接\n", iter->sockfd);

	Message m;
	string s;

	// 用户不是自己退出才发收尾报文
	if (r != RQUIT)
	{
		// 删除连接前，需要发送下线通知，客户端无需回复ACK，回了也没用
		if (r == RKICK)
			m.set_type(KICK_NOTIFICATION);
		else if (r == RERROR)
			m.set_type(ERROR_NOTIFICATION);
		m.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());

		// 写队列所有东西全部发出去
		while(!iter->wqueue.empty())
			SendPackage(iter, false, true);
	}

	// 已登录用户的下线，要通知其他所有登录用户
	if (iter->state != SLogin)
	{
		m.Clear();
		list<Connection>::iterator it;
		UpdateonlineBroadcast *uob = new UpdateonlineBroadcast;
		uob->set_username(iter->player.name);
		uob->set_userid(iter->player.id);
		uob->set_online(false);
		m.set_type(UPDATEONLINE_BROADCAST);
		m.set_allocated_updateonlinebroadcast(uob);
		m.SerializeToString(&s);
		for (it = conl.begin(); it != conl.end(); ++it)
		{
			if (it->state != SLogin && it != iter)
				MakePackage(it, s.data(), s.size());
		}

		// 游戏中的用户掉线
		if (iter->state == SGame)
		{
			delete iter->game;
			iter->oiter->state = SRoom;

			// 通知对手已下线
			GamecrushNotification *gcn = new GamecrushNotification;
			gcn->set_reason(GamecrushNotification::OPPONENT_OFF);
			m.set_type(GAMECRUSH_NOTIFICATION);
			m.set_allocated_gamecrushnotification(gcn);
			m.SerializeToString(&s);
			MakePackage(iter->oiter, s.data(), s.size());
		}
	}

	// 正式关闭、删除连接
	close(iter->sockfd);
	iter = conl.erase(iter);
	iter--; // 结束连接后进入下一次循环，会++iter，所以要先--抵消掉
			// 当链表为空时，iter--不会发生变化
}

// 用于超时重传机制
void SetTimer(int sig)
{
	alarm(1);
}

// 超时重传以及心跳包
void RetransmissionAlive()
{
	time_t t = time(0);
	list<Connection>::iterator iter;
	for (iter = conl.begin(); iter != conl.end(); iter++)
	{
		if (iter->sseq > iter->sack && t - iter->sendtime > RETRANSMISSION_DELAY)
		{
			SendPackage(iter, true);

			printf("此次是对fd%d的重发\n", iter->sockfd);
		}
		if (t - iter->beattime > ALIVE_TIME_OUT)
		{
			printf("fd%d心跳超时\n", iter->sockfd);
			EndConnection(iter, RERROR);
		}
	}
}

// select并发处理连接的核心函数
void ProcessConnections(int connfd)
{
	signal(SIGALRM, SetTimer);
	alarm(1);

	list<Connection>::iterator iter;
	fd_set rfds, wfds;
	int maxfd;
	int result;

	while (1)
	{
		FD_ZERO(&rfds);
		FD_ZERO(&wfds);
		FD_SET(connfd, &rfds);
		maxfd = connfd;
		for (iter = conl.begin(); iter != conl.end();iter++)
		{
			FD_SET(iter->sockfd, &rfds);
			if (!iter->wqueue.empty() && iter->sack == iter->sseq)
				FD_SET(iter->sockfd, &wfds);
			if (iter->sockfd > maxfd)
				maxfd = iter->sockfd;
		}

		if (1)
		{
			printf("select...\n");
			if (conl.size() > 0)
			{	
				printf("    fd state   wqs  sseq  sack  rseq    id  name\n");
				for (iter = conl.begin(); iter != conl.end(); iter++)
				{
					printf("%6d%6d%6d%6d%6d%6d", iter->sockfd, iter->state, iter->wqueue.size(), iter->sseq, iter->sack, iter->rseq);
					if (iter->state != SLogin)
						printf("%6d %5s", iter->player.id, iter->player.name.data());
					printf("\n");
				}
			}
		}
		fflush(stdout);

		result = select(maxfd + 1, &rfds, &wfds, NULL, NULL);
		if (result > 0)
		{
			if (FD_ISSET(connfd, &rfds))
				ShakeHands(connfd);

			for (iter = conl.begin(); iter != conl.end(); ++iter)
			{
				if (FD_ISSET(iter->sockfd, &rfds))
					RecvPackage(iter);

				if (FD_ISSET(iter->sockfd, &wfds))
					SendPackage(iter, false);
			}
		}
		else if (result < 0)
		{
			RetransmissionAlive();
		}
		printf("\n");
	}
}
