#ifndef COMMON_H
#define COMMON_H

// string���ȻҪ��iostream
#include <iostream>
using namespace std;

// ��ҽṹ�壬�����ݿ⡢���ӹ���

struct Player
{
    int id;
    string name;
};

// ����Ϸ�߼������ӹ���

#define ROWNUM 10
#define COLNUM 10
#define PLANENUM 3

#define PLANEBLOCKNUM 10
#define LOCATORBLOCKNUM 3

// OB:Out of Bound
enum  GenResult
{
	SUCCESS,
	FULL,
	LOCATOROB,	// ��ͷ�����Խ��
	TAILOB,		// ��βԽ�磬Ŀǰ���᷵�ش�ֵ���������ΪWRONGSHAPE
	WRONGSHAPE,
	INTERSECTED
};

// δ��ʼ���򱻲��в���DEAD
// ���л�ͷ����DEAD
enum PlaneState
{
	DEAD,
	LIVE
};

enum BlockState
{
	NONE,		// �հ�����
	HIDDEN,		// ���صķɻ�
	DAMAGED,	// �����еķɻ�
	REVEALED	// �ѱ�¶
};

enum BombResult
{
	MISS,
	HIT,
	DESTROYED
};

// ������protobuf��Coordinate����
typedef int Coord[2];

// ����ͷ��������������β��˳��洢
typedef Coord PlanePos[PLANEBLOCKNUM];

// ������protobuf��PlaneLocator����
typedef Coord PlaneLoc[LOCATORBLOCKNUM];

typedef BlockState Board[ROWNUM][COLNUM];


#endif