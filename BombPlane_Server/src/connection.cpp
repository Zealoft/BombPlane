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

// ���ߺ���

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
        printf("����־�ļ�ʧ��:%d\n", errno);
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

// ��ǰ����

// ��ֹ��������Rǰ׺
enum EndReason
{
	RKICK,	// ���¾�ͬ����¼
	RQUIT,	// �û������˳�
	RERROR	// �û������쳣��������ʱ
};
void EndConnection(list<Connection>::iterator &iter, EndReason r);

/*******************************************/
/*               ���ݰ������               */
/*******************************************/

// ����������ݰ��������β
// content = NULL��ʾ��ack����content_size��Ϊ0��ʾ�Ƕ����ش�����ack
// ���͵�ʱ��Ϊ�����зǿ� && sack==sseq && selectд����
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
			p.head.seq = iter->rseq - 1; // �ԶԷ��ش������ݰ�ACK��������1λ�������ڣ��ش���seq==rseq-1
	}
	else
	{
		if (iter->wqueue.empty())
			p.head.seq = iter->sseq;
		else
			p.head.seq = iter->tseq + 1;	// д�������һ�����ݰ���seq+1
		memcpy(p.content, content, content_size);
	}

	printf("<MAKE>fd%d������ͷ��len=%d, seq=%d\n", iter->sockfd, p.head.len, p.head.seq);
	iter->wqueue.push(p);
	// ��Ϊд�������һ�����ݰ�����Ҫ��¼��seq
	if (p.head.len > sizeof(Head))
		iter->tseq = p.head.seq;
	/***** �����ڵ��ԣ�ʹ��LITE��PROTOBUFʱ����ɾ�� *****/
	/*
	if (content != NULL)
	{
		Message m;
		m.ParseFromArray(content, content_size);
		printf("��������\n%s", m.DebugString().data());
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

		// �����ݿ������жϵ�¼���
		LoginResponse *lrp = new LoginResponse;
		iter->player.name = m.loginrequest().username();
		SQL_RESULT r = Login(iter->player, m.loginrequest().password());
		if (r == SUC)
		{
			// ������ظ���¼���Ѿɵ�¼������
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
		printf("�û�%s��½�Ľ��Ϊ��%d\n", m.loginrequest().username().data(), r);
		if (r != SUC)
			break;
		// ��¼�ɹ��ſɵ���˴�

		// ���¼�û����������б��������ѵ�¼�û���������֪ͨ
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
				// ���ѵ�¼���û���ӵ������б���
				ou = oln->add_onlinelist();
				ou->set_username(it->player.name);
				ou->set_userid(it->player.id);
				ou->set_inroom(it->state == SGame);

				// �������ѵ�¼�û���������֪ͨ
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

		// �Լ���userid�����
		if (src != iter->player.id)
		{
			src = iter->player.id;
			m.mutable_inviterequest()->set_srcuserid(src);
		}

		list<Connection>::iterator diter;
		for (diter = conl.begin(); diter != conl.end(); ++diter)
		{
			// TODO:�����ʹ�ʱ���Է�Ҳ���������״̬
			// �ڿͻ��˴������������Ҳ���ִ���������������ظ�InviteResponse��userid=0
			if (diter != iter && diter->player.id == dst)
				break;
		}

		if (diter == conl.end())
		{
			// TODO:û�ҵ����շ����Է�δע���������
			// �ڿͻ��˴������������Ҳ���ִ���������������ظ�InviteResponse��userid=-1
			printf("δ�ҵ��������� id=%d\n", dst);
		}
		else
		{
			string s;
			printf("�ɹ��ҵ��������� id=%d,��ʼת��\n", dst);
			m.SerializeToString(&s);
			MakePackage(diter, s.data(), s.size());

			// �������û�����UPDATEROOM
			UpdateroomBroadcast *urb = new UpdateroomBroadcast;
			Message nm;
			urb->set_userid1(src);
			urb->set_userid2(dst);
			urb->set_inout(true);

			nm.set_type(UPDATEROOM_BROADCAST);
			nm.set_allocated_updateroombroadcast(urb);

			nm.SerializeToString(&s);

			// �����AB����ĵ�¼�ͻ��˹㲥
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
			// û�ҵ������ߣ�˵���Ѿ�����
			printf("δ�ҵ�����ظ��Ľ��շ� id=%d\n", src);
			// TODO:�����ΪA������������̵��ߣ���Ҫ֪ͨB
		}
		else
		{
			printf("�ɹ��ҵ������� id=%d���Է�ѡ����%s����ʼת��\n", src, m.inviteresponse().accept() ? "����" : "�ܾ�");
			string s;
			m.SerializeToString(&s);
			MakePackage(siter, s.data(), s.size());

			if (m.inviteresponse().accept() == true)
			{
				// �������룬׼����ʼ��Ϸ������Ⱥ���
				srand(time(0));
				siter->od = rand() % 2;
				iter->od = !siter->od;
				iter->oiter = siter;
				siter->oiter = iter;
				iter->game = siter->game = new Game();

				iter->state = SGame;
				iter->oiter->state = SGame;
				Log("��ʼ��Ϸ��" + iter->player.name + " vs " + siter->player.name +
					"�������ߣ�" + (iter->od == 0 ? iter->player.name : iter->player.name) + "��");
			}
			else
			{
				// �ܾ����룬�����������û�����UPDATEROOM
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
		// ���ΪNULL���������ǵ�һ����Ϸ
		if (iter->game == NULL)
		{
			srand(time(0));
			iter->od = rand() % 2;
			iter->oiter->od = !iter->od;
			iter->game = iter->oiter->game = new Game;
			Log("���¿�ʼ��Ϸ��" + iter->player.name + " vs " + iter->oiter->player.name +
				"�������ߣ�" + (iter->od == 0 ? iter->player.name : iter->oiter->player.name) +"��");
		}

		// ��ʼ���ɻ�����
		PlaneLoc pl[3];
		InitPosTrans(m.initposnotification(), pl);
		bool ready = iter->game->InitPos(pl, iter->od);
		// �����־
		char buffer[LOG_BUFFER_SIZE];
		sprintf(buffer, "Plane1:(%d, %d), (%d, %d), (%d, %d)\nPlane2:(%d, %d), (%d, %d), (%d, %d)\nPlane3:(%d, %d), (%d, %d), (%d, %d)",
			pl[0][0][0], pl[0][0][1], pl[0][1][0], pl[0][1][1], pl[0][2][0], pl[0][2][1],
			pl[1][0][0], pl[1][0][1], pl[1][1][0], pl[1][1][1], pl[1][2][0], pl[1][2][1],
			pl[2][0][0], pl[2][0][1], pl[2][1][0], pl[2][1][1], pl[2][2][0], pl[2][2][1]);
		Log("���" + iter->player.name + "�ѷ�����ϣ�\n" + buffer);
		// ˫�����������
		if (ready)
		{
			GamestartNotification *gsn = new GamestartNotification;
			gsn->set_userid(iter->od == 0 ? iter->player.id : iter->oiter->player.id);
			nm.set_type(GAMESTART_NOTIFICATION);
			nm.set_allocated_gamestartnotification(gsn);
			nm.SerializeToString(&s);
			MakePackage(iter, s.data(), s.size());
			MakePackage(iter->oiter, s.data(), s.size());
			Log("˫����Ҷ��ѷ�����ϣ�");
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

		// �����־
		char buffer[LOG_BUFFER_SIZE];
		sprintf(buffer, "(%d, %d)�������%d", cd[0], cd[1], r);
		Log("���" + iter->player.name + "��ը" + buffer);
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

		// �����־
		char buffer[LOG_BUFFER_SIZE];
		sprintf(buffer, "(%d, %d), (%d, %d), (%d, %d)�������%d", pl[0][0], pl[0][1], pl[1][0], pl[1][1], pl[2][0], pl[2][1], r);
		Log("���" + iter->player.name + "����²⣺" + buffer);

		// �ж���Ϸ�Ƿ��Ѿ��������
		int w = iter->game->Win();
		if (w == -1)
			break;

		GameoverNotification *gon = new GameoverNotification;
		// ֻ�б�������ʤ��
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
		Log("���" + iter->player.name + "Ӯ��ʤ����");
		break;
	}
	case EXITGAME_NOTIFICATION:
		delete iter->game;
		iter->state = SRoom;
		iter->oiter->state = SRoom;

		// ֪ͨ����������
		GamecrushNotification *gcn = new GamecrushNotification;
		gcn->set_reason(GamecrushNotification::OPPONENT_OFF);
		nm.set_type(GAMECRUSH_NOTIFICATION);
		nm.set_allocated_gamecrushnotification(gcn);
		nm.SerializeToString(&s);
		MakePackage(iter->oiter, s.data(), s.size());
		Log("���" + iter->player.name + "�˳���Ϸ��");

		// �뿪���䣬�����������û�����UPDATEROOM
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

// �����յ������ݰ�
void ResolvePackage(list<Connection>::iterator &iter, Package p)
{
	Message m;
	m.ParseFromArray(p.content, p.head.len - sizeof(Head));

	// ע��switch�ж��������Ҫ��case��Ӵ�����
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
		printf("fd%d�����Ͽ�����\n", iter->sockfd);
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
/*               UDP���Ӵ����              */
/*******************************************/

// ���������ò���һ�����׽���
// port:ָ���Ķ˿ںţ����������Զ�ѡ��Ķ˿ں�
// use_port:�Ƿ�ʹ��ָ���Ķ˿ڣ�����ʹ���Զ�ѡ��Ķ˿�
int NewSocket(short *port, bool use_port)
{
	const static short port_start = 21000; 	// �󶨶˿ڵĿ�ʼλ��
	static short port_cur = port_start;		// ��ǰ׼�����ԵĶ˿�
	const int try_times = 50;				// �������Զ˿ڵĴ���

	// �����������׽���
	int sockfd;
	int nonblock = 1;

	if ((sockfd = socket(AF_INET, SOCK_DGRAM, 0)) == -1)
		return -1;
	if (ioctl(sockfd, FIONBIO, &nonblock) == -1)
	{
		close(sockfd);
		return -1;
	}

	// ���÷���˵�ַ�ṹ��
	struct sockaddr_in my_addr;

	my_addr.sin_family = AF_INET;
	if (use_port)
		my_addr.sin_port = htons(*port);
	else
		my_addr.sin_port = htons(port_cur);
	my_addr.sin_addr.s_addr = htonl(INADDR_ANY);
	bzero(&(my_addr.sin_zero), sizeof(my_addr.sin_zero));

	// �󶨵�ַ�ṹ���socket
	if (bind(sockfd, (struct sockaddr *)&my_addr, sizeof(struct sockaddr)) < 0)
	{
		if (use_port)
		{
			printf("�˿�%u��ʧ��\n", *port);
			return -1;
		}

		printf("�˿�%u��ʧ��ֱ��...\n", port_cur);
		int i;
		for (i = 0; i < try_times; i++)
		{
			my_addr.sin_port = htons(++port_cur);
			if (bind(sockfd, (struct sockaddr *)&my_addr, sizeof(struct sockaddr)) == 0)
				break;
		}

		if (i == try_times)
		{
			printf("����%d�ζ�û�гɹ���\n", try_times);
			close(sockfd);
			return -1;
		}
	}

	// ֻ�а󶨳ɹ����ܵ���˴�

	if (!use_port)
		*port = port_cur++;

	printf("�˿�%u�󶨳ɹ�\n", *port);

	return sockfd;
}

// ͨ�������׽������磬��һ����ʱ�׽��֣��൱������
// ��ʱ�׽������ں��������ݴ���
// ע��������֪�Ķ˿������������ݵ��ͻ���
void ShakeHands(int connfd)
{
	struct sockaddr_in cliaddr;
	socklen_t salen = sizeof(cliaddr); // sockaddr length �����ʼ��������EINVAL
	char buffer[MAX_CONTENT_SIZE];
	int len;
	char ipstr[16];

	len = recvfrom(connfd, buffer, sizeof(buffer), 0, (struct sockaddr *)&cliaddr, &salen);
	if (len < 0)
	{
		printf("���ְ���ȡʧ�ܣ�%d\n", errno);
		return;
	}

	inet_ntop(AF_INET, &cliaddr.sin_addr, ipstr, 16);
	printf("����%s:%u�����ӣ������Ϊ%dB��\n", ipstr, ntohs(cliaddr.sin_port), len);
	if (len > 0)
		printf("��������Ϊ��%s\n", buffer);

	// �ж��ظ�������
	list<Connection>::iterator iter;
	for (iter = conl.begin();iter != conl.end();iter++)
	{
		// ֱ��sockaddr_in��ȵ��жϻ���뱨��
		if (iter->sa.sin_addr.s_addr == cliaddr.sin_addr.s_addr && iter->sa.sin_port == cliaddr.sin_port)
		{
			printf("��fd%d��ip�Ͷ˿��ظ����ܾ�����\n", iter->sockfd);
			return;
		}
	}

	// ��������
	short port;
	int sockfd = NewSocket(&port, false);
	if (sockfd < 0)
		return;
	printf("����޳�ͻ���������ӣ�fd%d\n", sockfd);

	if (connect(sockfd, (struct sockaddr *)&cliaddr, salen) < 0)
	{
		printf("��%s:%u����ʧ�ܣ�%d", ipstr, ntohs(cliaddr.sin_port), errno);
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

	// ����һ���ʺ�������ڸ�֪�˿ں�
	// �Ӵ��Ժ�ÿ��������ҪACK
	char s[7] = "Hello!";
	MakePackage(--conl.end(), s, sizeof(s));
}

// ȡָ�����ӵ�д���ж��׷��ͳ�ȥ
// ����ʱ����
// 1��selectд����(again==false)
// 2����ʱ�ش�(again==true)
// 3���ر�����ǰ(force==true)
void SendPackage(const list<Connection>::iterator &iter, bool again, bool force = false)
{
	Package p = iter->wqueue.front();
	int len = p.head.len, wlen;

	printf("<SEND>fd%d��len=%d, seq=%d�������", iter->sockfd, len, p.head.seq);
	if (0)
	{
		// ���ڲ��Գ�ʱ�ش���50%���ʲ���ACK
		if(p.head.len == sizeof(Head))
		{
			srand(time(0));
			int x = rand() % 2;
			if (x)
			{
				printf("�˴�ACK����(seq=%d)��\n", p.head.seq);
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
		printf("����%d\n", errno);
		return;
	}
	else if (wlen < len)
		printf("ʵ��д��%dB\n", wlen);
	else
		printf("�ɹ���\n");

	if (force)
		iter->wqueue.pop();
	else
	{
		// ACK���˾ͷ��ˣ������ط�
		// ���ݰ�Ҫ���յ���ACK��pop
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

// ��ĳ�������յ����ݰ�
// select������ʱ�Զ�����
// TODO:����һ�����ݰ����ضϵ��������β���ָ���
// TODO:�ж϶�ȡ���Ķ�����ʵ�����٣���β���ָ���
void RecvPackage(list<Connection>::iterator &iter)
{
	char buffer[UDP_BUFFER_SIZE];
	char *cur;
	int rlen;
	Package p;

	// ����һ�ζ��꣬Ҫ��Ȼ�ᱻ���������µڶ��ζ�ȡEAGAIN
	rlen = read(iter->sockfd, buffer, UDP_BUFFER_SIZE);
	DumpArray(buffer, rlen);
	printf("<RECEIVE>fd%d��", iter->sockfd);
	if (rlen < 0)
	{
		printf("����%d\n", iter->sockfd, errno);
		return;
	}
	else
		printf("%dB�ı���\n", rlen);
	cur = buffer;

	// ��������ʱ��
	iter->beattime = time(0);

	// ���ܶ��UDP���ı�ƴ�����ˣ�������ʱû������
	// ѭ������ÿ�����ģ�����ÿ�����Ķ��������ģ���ͷ��д�ĳ���Ҳ����ȷ��
	// ������һλ��������Э�飬ֻ����ͬʱ���������������ǳ�ʱ�ش�ʱ��
	while (cur < buffer + rlen - 1)
	{
		if (cur != buffer)
			printf("���ֶ��UDP���ı�ƴ��һ����\n");

		// ��������ͷ
		memcpy((char *)&p.head, cur, sizeof(Head));
		cur += sizeof(Head);
		p.head.len = ntohs(p.head.len);
		p.head.seq = ntohs(p.head.seq);
		if (p.head.len < sizeof(Head))
		{
			printf("��ͷ��д�ĳ��ȱȱ�ͷ���Ȼ�С:%d\n", p.head.len);
			EndConnection(iter, RERROR);
			return;
		}
		printf("����ͷ��len=%d, seq=%d\n", p.head.len, p.head.seq);

		if (p.head.len == sizeof(Head))
		{
			printf("�յ�����ACK��");
			if (p.head.seq < iter->sack)
			{
				printf("�ط������ݰ����Է����յ����ظ���ACK\n");
			}
			else if (p.head.seq > iter->sack)
			{
				printf("����쳣��\n");
				EndConnection(iter, RERROR);
			}
			else
			{
				printf("������\n");
				iter->wqueue.pop();
				iter->sack++;
			}
			return;
		}

		memcpy(p.content, cur, p.head.len - sizeof(Head));
		cur += p.head.len - sizeof(Head);
		printf("�յ��������ݰ���");

		if (p.head.seq < iter->rseq)
		{
			// ACK���ˣ��Է��ش�����������ACK
			printf("��ǰrseq=%d������ACK\n", iter->rseq);
			/******************/
			Package p;
			p.head.len = htons(sizeof(Head));
			p.head.seq = htons(iter->rseq);
			int wlen = write(iter->sockfd, (char *)&p, ntohs(p.head.len));
			printf("�����%dB\n", wlen);
			/******************/
			//MakePackage(iter, NULL, 1);
		}
		else if (p.head.seq > iter->rseq)
		{
			printf("�ͻ��˷�������쳣��\n");
			EndConnection(iter, RERROR);
		}
		else
		{
			printf("������\n");
			/***** �����ڵ��ԣ�ʹ��LITE��PROTOBUFʱ����ɾ�� *****/
			/*
			Message m;
			m.ParseFromArray(p.content, p.head.len - sizeof(Head));
			printf("��������\n%s", m.DebugString().data());
			/*************************************************/
			MakePackage(iter, NULL, 0);
			ResolvePackage(iter, p);

		}
	}
}

// ���еĿͻ����˳����뾭���˴�
// �˴��ĵ���������Ҫʹ������
void EndConnection(list<Connection>::iterator &iter, EndReason r)
{
	printf("�ر�fd%d������\n", iter->sockfd);

	Message m;
	string s;

	// �û������Լ��˳��ŷ���β����
	if (r != RQUIT)
	{
		// ɾ������ǰ����Ҫ��������֪ͨ���ͻ�������ظ�ACK������Ҳû��
		if (r == RKICK)
			m.set_type(KICK_NOTIFICATION);
		else if (r == RERROR)
			m.set_type(ERROR_NOTIFICATION);
		m.SerializeToString(&s);
		MakePackage(iter, s.data(), s.size());

		// д�������ж���ȫ������ȥ
		while(!iter->wqueue.empty())
			SendPackage(iter, false, true);
	}

	// �ѵ�¼�û������ߣ�Ҫ֪ͨ�������е�¼�û�
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

		// ��Ϸ�е��û�����
		if (iter->state == SGame)
		{
			delete iter->game;
			iter->oiter->state = SRoom;

			// ֪ͨ����������
			GamecrushNotification *gcn = new GamecrushNotification;
			gcn->set_reason(GamecrushNotification::OPPONENT_OFF);
			m.set_type(GAMECRUSH_NOTIFICATION);
			m.set_allocated_gamecrushnotification(gcn);
			m.SerializeToString(&s);
			MakePackage(iter->oiter, s.data(), s.size());
		}
	}

	// ��ʽ�رա�ɾ������
	close(iter->sockfd);
	iter = conl.erase(iter);
	iter--; // �������Ӻ������һ��ѭ������++iter������Ҫ��--������
			// ������Ϊ��ʱ��iter--���ᷢ���仯
}

// ���ڳ�ʱ�ش�����
void SetTimer(int sig)
{
	alarm(1);
}

// ��ʱ�ش��Լ�������
void RetransmissionAlive()
{
	time_t t = time(0);
	list<Connection>::iterator iter;
	for (iter = conl.begin(); iter != conl.end(); iter++)
	{
		if (iter->sseq > iter->sack && t - iter->sendtime > RETRANSMISSION_DELAY)
		{
			SendPackage(iter, true);

			printf("�˴��Ƕ�fd%d���ط�\n", iter->sockfd);
		}
		if (t - iter->beattime > ALIVE_TIME_OUT)
		{
			printf("fd%d������ʱ\n", iter->sockfd);
			EndConnection(iter, RERROR);
		}
	}
}

// select�����������ӵĺ��ĺ���
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
