#ifndef AIRFIELD_H
#define AIRFIELD_H

#include "common.h"

class AirField
{
public:
	AirField();

	~AirField();

	// ��˳�������ͷ������������
	GenResult GenPlane(const PlaneLoc pl);

	// ��ʵ�е���֣��Ǳ�����ը�ģ����õ�ȴ���Լ��ĺ���
	BombResult Bomb(const Coord cd);

	bool RevealPlane(const PlaneLoc pl);

	// �Ƿ��ѳ�ʼ����ϣ��Ƿ񶼻��ţ�
	bool Ready();

	// �Ƿ�������Ƿ����ˣ�
	bool Lose();

	void DebugOutput();

private:
	Board board;

	PlanePos ppos[PLANENUM];

	PlaneState ps[PLANENUM];

};

#endif