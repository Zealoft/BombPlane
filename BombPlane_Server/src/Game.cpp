#include <cstdio>

#include "Game.h"

bool Game::InitPos(PlaneLoc *pl, int p)
{
    int i;
    GenResult r;
    printf("%s�߳�ʼ�������", p ? "����" : "����");
    for (i = 0;i < PLANENUM;i++)
    {
        r = af[p].GenPlane(pl[i]);
        printf("%d ", r);
    }
    printf("\n");
    if (af[0].Ready() && af[1].Ready())
        return true;
    else
        return false;
}

int Game::Win()
{
    if(af[0].Lose())
        return 1;
    if(af[1].Lose())
        return 0;
    return -1;
}