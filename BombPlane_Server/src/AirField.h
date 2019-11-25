#ifndef AIRFIELD_H
#define AIRFIELD_H

#include "common.h"

class AirField
{
public:
	AirField();

	~AirField();

	// 按顺序给出机头，两翼尖的坐标
	GenResult GenPlane(const PlaneLoc pl);

	// 其实有点奇怪，是被别人炸的，调用的却是自己的函数
	BombResult Bomb(const Coord cd);

	bool RevealPlane(const PlaneLoc pl);

	// 是否已初始化完毕（是否都活着）
	bool Ready();

	// 是否输掉（是否都死了）
	bool Lose();

	void DebugOutput();

private:
	Board board;

	PlanePos ppos[PLANENUM];

	PlaneState ps[PLANENUM];

};

#endif