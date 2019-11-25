#ifndef GAME_H
#define GAME_H

#include "AirField.h"

// ���ֵ����Ϊ0�����ֵ����Ϊ1
class Game
{
public:
    // ��¼p�ĳ�ʼ���������Ƿ�˫������ʼ�����
    bool InitPos(PlaneLoc *pl, int p);

    BombResult Bomb(const Coord cd, int p)
    {
        return af[p].Bomb(cd);
    }

    bool Reveal(const PlaneLoc pl, int p)
    {
        return af[p].RevealPlane(pl);
    }

    // δ����Ӯ�ң�����-1�����򷵻�ʤ��
    int Win();

private:
    AirField af[2];
};

#endif