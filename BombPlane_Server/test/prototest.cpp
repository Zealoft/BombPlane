#include <iostream>
#include <strstream>
#include "../src/BombPlane_proto.pb.h"
using namespace std;
using namespace bombplane_proto;

int main()
{
	OnlineUser user;
	cout << user.ByteSize() << ' ' << sizeof(user) << endl;
	user.set_userid(222);
	cout << user.DebugString();

	Message m;
	cout << m.DebugString() << endl;
	LoginRequest *lr = new LoginRequest;	//������ֲ�����
	lr->set_password("1");
	m.set_allocated_loginrequest(lr);
	cout << m.DebugString() << endl;

	//���л�����������
	Message r;
	string s;
	char buffer[1024];
	strstream ss;

	//array
	//���л�ʱ��Ҫ�趨������󳤶ȣ����ܻᵼ�����л�ʧ��
	cout << "���л������" << m.SerializeToString(&s) << endl;
	cout << "���������" << m.ParseFromArray(s.data(), s.size()) << endl;
	cout << m.DebugString() << endl;

	//string
	//���Ը���size()�ж������Ƿ�̫�����Ӷ����ŵش���
	cout << "���л������" << m.SerializeToString(&s) << endl;
	memcpy(buffer, s.data(), s.size());	//s.length()Ч��һ��
	memcpy(buffer + s.size(), s.data(), s.size());	//��������һ�ݣ��Դ�
	cout << "���������" << r.ParseFromString(string(buffer, s.size())) << endl;
	cout << r.DebugString() << endl;

	//strstream
	//���Ƽ�
	cout << "���л������" << m.SerializeToOstream(&ss) << endl;
	cout << "���������" << r.ParseFromIstream(&ss) << endl;
	cout << r.DebugString() << endl;

	//repeated����
	OnlineUser *ou;
	OnlinelistNotification *oln = new OnlinelistNotification;
	ou = oln->add_onlinelist();
	ou->set_userid(1);
	ou = oln->add_onlinelist();
	ou->set_userid(2);
	m.set_allocated_onlinelistnotification(oln);
	cout << m.DebugString();

	//delete����
	delete ou;
	delete oln;
	cout<<"��ô��?"<<endl;
}