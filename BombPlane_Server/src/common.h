#ifndef COMMON_H
#define COMMON_H

// string类居然要加iostream
#include <iostream>
using namespace std;

// 玩家结构体，与数据库、连接共享

struct Player
{
    int id;
    string name;
};

// 与游戏逻辑、连接共享

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
	LOCATOROB,	// 机头或翼尖越界
	TAILOB,		// 机尾越界，目前不会返回此值，此情况归为WRONGSHAPE
	WRONGSHAPE,
	INTERSECTED
};

// 未初始化或被猜中才是DEAD
// 击中机头不算DEAD
enum PlaneState
{
	DEAD,
	LIVE
};

enum BlockState
{
	NONE,		// 空白区域
	HIDDEN,		// 隐藏的飞机
	DAMAGED,	// 被击中的飞机
	REVEALED	// 已暴露
};

enum BombResult
{
	MISS,
	HIT,
	DESTROYED
};

// 避免与protobuf的Coordinate重名
typedef int Coord[2];

// 按机头、机翼、机腰、机尾的顺序存储
typedef Coord PlanePos[PLANEBLOCKNUM];

// 避免与protobuf的PlaneLocator重名
typedef Coord PlaneLoc[LOCATORBLOCKNUM];

typedef BlockState Board[ROWNUM][COLNUM];


#endif