#ifndef GAME_H
#define GAME_H

#include "AirField.h"

// 先手的序号为0，后手的序号为1
class Game
{
public:
    // 记录p的初始化，返回是否双方都初始化完毕
    bool InitPos(PlaneLoc *pl, int p);

    BombResult Bomb(const Coord cd, int p)
    {
        return af[p].Bomb(cd);
    }

    bool Reveal(const PlaneLoc pl, int p)
    {
        return af[p].RevealPlane(pl);
    }

    // 未产生赢家：返回-1，否则返回胜者
    int Win();

private:
    AirField af[2];
};

#endif