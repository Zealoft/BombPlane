#include <iostream>
using namespace std;

#include "AirField.h"

AirField::AirField()
{
	int i, j;
	for (i = 0; i < ROWNUM; i++)
		for (j = 0; j < COLNUM; j++)
			board[i][j] = NONE;
	for (i = 0; i < PLANENUM; i++)
		ps[i] = DEAD;
}

AirField::~AirField()
{
}

int pattern[4][5] = {	{ 0,0,1,0,0 },
						{ 1,1,1,1,1 },
						{ 0,0,1,0,0 },
						{ 0,1,1,1,0 } };
enum Direction
{
	UP,
	DOWN,
	LEFT,
	RIGHT,
	ERROR
};

//形状判断和机尾越界判断
//形状确定正确，则确定机头、机翼不会越界
bool IsCorrect(const PlaneLoc pl, const Direction dir)
{
	switch (dir)
	{
	case UP:
		if ((pl[0][0] + 1 == pl[1][0] && pl[0][1] - 2 == pl[1][1] && pl[0][0] + 1 == pl[2][0] && pl[0][1] + 2 == pl[2][1] ||
			pl[0][0] + 1 == pl[2][0] && pl[0][1] - 2 == pl[2][1] && pl[0][0] + 1 == pl[1][0] && pl[0][1] + 2 == pl[1][1]) &&
			ROWNUM - pl[0][0] >= 4)
			return true;
		break;
	case DOWN:
		if ((pl[0][0] - 1 == pl[1][0] && pl[0][1] - 2 == pl[1][1] && pl[0][0] - 1 == pl[2][0] && pl[0][1] + 2 == pl[2][1] ||
			pl[0][0] - 1 == pl[2][0] && pl[0][1] - 2 == pl[2][1] && pl[0][0] - 1 == pl[1][0] && pl[0][1] + 2 == pl[1][1]) &&
			pl[0][0] >= 3)
			return true;
		break;
	case LEFT:
		if ((pl[0][0] - 2 == pl[1][0] && pl[0][1] + 1 == pl[1][1] && pl[0][0] + 2 == pl[2][0] && pl[0][1] + 1 == pl[2][1] ||
			pl[0][0] - 2 == pl[2][0] && pl[0][1] + 1 == pl[2][1] && pl[0][0] + 2 == pl[1][0] && pl[0][1] + 1 == pl[1][1]) &&
			COLNUM - pl[0][1] >= 4)
			return true;
		break;
	case RIGHT:
		if ((pl[0][0] - 2 == pl[1][0] && pl[0][1] - 1 == pl[1][1] && pl[0][0] + 2 == pl[2][0] && pl[0][1] - 1 == pl[2][1] ||
			pl[0][0] - 2 == pl[2][0] && pl[0][1] - 1 == pl[2][1] && pl[0][0] + 2 == pl[1][0] && pl[0][1] - 1 == pl[1][1]) &&
			pl[0][1] >= 3)
			return true;
		break;
	}
	return false;
}

Direction ReadDirection(const PlaneLoc pl)
{
	//1号翼尖在机头上边
	if (pl[1][0] < pl[0][0])
	{
		//2号翼尖在机头上边
		if (pl[2][0] < pl[0][0])
		{
			if (IsCorrect(pl, DOWN))
				return DOWN;
			else
				return ERROR;
		}
		//2号翼尖在机头下边
		else
		{
			//2号翼尖在机头左下
			if (pl[2][1] < pl[0][1])
			{
				if (IsCorrect(pl, RIGHT))
					return RIGHT;
				else
					return ERROR;
			}
			//2号翼尖在机头右下
			else
			{
				if (IsCorrect(pl, LEFT))
					return LEFT;
				else
					return ERROR;
			}
		}
	}
	//1号翼尖在机头下边
	else
	{
		//2号翼尖在机头下边
		if (pl[2][0] > pl[0][0])
		{
			if (IsCorrect(pl, UP))
				return UP;
			else
				return ERROR;
		}
		//2号翼尖在机头上边
		else
		{
			//2号翼尖在机头左上
			if (pl[2][1] < pl[0][1])
			{
				if (IsCorrect(pl, RIGHT))
					return RIGHT;
				else
					return ERROR;
			}
			//2号翼尖在机头右上
			else
			{
				if (IsCorrect(pl, LEFT))
					return LEFT;
				else
					return ERROR;
			}
		}
	}
	return ERROR;
}

GenResult AirField::GenPlane(const PlaneLoc pl)
{
	int i, j, k;
	int num;
	//判断现在是第几架
	for (num = 0; num < PLANENUM; num++)
		if (ps[num] == DEAD)
			break;
	if (num == PLANENUM)
		return FULL;

	//定位点（机头或翼尖）过界
	for (i = 0; i < LOCATORBLOCKNUM; i++)
		if (pl[i][0] < 0 || pl[i][0] >= ROWNUM ||
			pl[i][1] < 0 || pl[i][1] >= COLNUM)
			return LOCATOROB;
	
	/*已经有了形状判断，此处的判断无用
	//机头判断过界
	if (pl[0][0] < 3 && pl[0][1] < 3 ||
		pl[0][0] < 3 && COLNUM - pl[0][1] <= 3 ||
		ROWNUM - pl[0][0] <= 3 && pl[0][1] < 3 ||
		ROWNUM - pl[0][0] <= 3 && COLNUM - pl[0][1] <= 3)
		return OTHEROB;

	//翼尖判断过界
	//等价于不允许在四个角
	if (pl[1][0] < 1 && pl[1][1] < 1 ||
		pl[1][0] < 1 && COLNUM - pl[1][1] <= 1 ||
		ROWNUM - pl[1][0] <= 1 && pl[1][1] < 1 ||
		ROWNUM - pl[1][0] <= 1 && COLNUM - pl[1][1] <= 1)
		return OTHEROB;
	if (pl[2][0] < 1 && pl[2][1] < 1 ||
		pl[2][0] < 1 && COLNUM - pl[2][1] <= 1 ||
		ROWNUM - pl[2][0] <= 1 && pl[2][1] < 1 ||
		ROWNUM - pl[2][0] <= 1 && COLNUM - pl[2][1] <= 1)
		return OTHEROB;
	*/

	Direction dir = ReadDirection(pl);

	k = 0;
	switch (dir)
	{
	case UP:
		for (i = 0; i < 4; i++)
			for (j = 0; j < 5; j++)
				if (pattern[i][j])
				{
					ppos[num][k][0] = pl[0][0] + i;
					ppos[num][k][1] = pl[0][1] - 2 + j;
					k++;
				}
		break;
	case DOWN:
		for (i = 0; i < 4; i++)
			for (j = 0; j < 5; j++)
				if (pattern[i][j])
				{
					ppos[num][k][0] = pl[0][0] - i;
					ppos[num][k][1] = pl[0][1] - 2 + j;
					k++;
				}
		break;
	case LEFT:
		for (i = 0; i < 4; i++)
			for (j = 0; j < 5; j++)
				if (pattern[i][j])
				{
					ppos[num][k][0] = pl[0][0] - 2 + j;
					ppos[num][k][1] = pl[0][1] + i;
					k++;
				}
		break;
	case RIGHT:
		for (i = 0; i < 4; i++)
			for (j = 0; j < 5; j++)
				if (pattern[i][j])
				{
					ppos[num][k][0] = pl[0][0] - 2 + j;
					ppos[num][k][1] = pl[0][1] - i;
					k++;
				}
		break;
	case ERROR:
		return WRONGSHAPE;
	}

	bool intersected = false;
	for (i = 0; i < PLANEBLOCKNUM; i++)
		if (board[ppos[num][i][0]][ppos[num][i][1]] != NONE)
			intersected = true;

	if (intersected)
		return INTERSECTED;
	else
	{
		for (i = 0; i < PLANEBLOCKNUM; i++)
			board[ppos[num][i][0]][ppos[num][i][1]] = HIDDEN;
		ps[num] = LIVE;
		return SUCCESS;
	}
}

BombResult AirField::Bomb(const Coord cd)
{
	int i, j;
	for (i = 0; i < PLANENUM; i++)
	{
		//击中机头
		if (ppos[i][0][0] == cd[0] && ppos[i][0][1] == cd[1])
		{
			if (board[cd[0]][cd[1]] == HIDDEN)
			{
				board[cd[0]][cd[1]] = DAMAGED;
				// ps[i] = DEAD;
				return DESTROYED;
			}
			else
				return MISS;
		}
		for (j = 1; j < PLANEBLOCKNUM; j++)
		{
			//击中机身
			if (ppos[i][j][0] == cd[0] && ppos[i][j][1] == cd[1])
			{
				if (board[cd[0]][cd[1]] == HIDDEN)
				{
					board[cd[0]][cd[1]] = DAMAGED;
					return HIT;
				}
				else
					return MISS;
				
				/*不应在此判断
				//判断击中部分是否构成一架完整的飞机
				int k;
				bool whole = true;
				for (k = 0; k < PLANEBLOCKNUM; k++)
					if (board[ppos[i][k][0]][ppos[i][k][1]] == NONE)
					{
						//ERROR!
						break;
					}
					else if (board[ppos[i][k][0]][ppos[i][k][1]] == HIDDEN)
					{
						whole = false;
						break;
					}

				if (whole)
				{
					ps[i] = DEAD;
					return DESTROYED;
				}
				else
					return HIT;
				*/
			}
		}
	}
	return MISS;
}

bool AirField::RevealPlane(const PlaneLoc pl)
{
	int i, j;
	for (i = 0; i < PLANENUM; i++)
	{
		//机头
		if (ppos[i][0][0] != pl[0][0] || ppos[i][0][1] != pl[0][1])
			continue;
		//机翼
		if ((ppos[i][1][0] != pl[1][0] || ppos[i][1][1] != pl[1][1]) &&
			(ppos[i][1][0] != pl[2][0] || ppos[i][1][1] != pl[2][1]))
			continue;
		if ((ppos[i][5][0] != pl[1][0] || ppos[i][5][1] != pl[1][1]) &&
			(ppos[i][5][0] != pl[2][0] || ppos[i][5][1] != pl[2][1]))
			continue;

		//猜测正确
		ps[i] = DEAD;
		for (j = 0; j < PLANEBLOCKNUM; j++)
			board[ppos[i][j][0]][ppos[i][j][1]] = REVEALED;
		return true;
	}
	return false;
}

bool AirField::Ready()
{
	int i;
	for (i = 0;i < PLANENUM;i++)
		if(ps[i] == DEAD)
			break;
	if (i == PLANENUM)
		return true;
	else
		return false;
}

bool AirField::Lose()
{
	int i;
	for (i = 0;i < PLANENUM;i++)
		if(ps[i] == LIVE)
			break;
	if (i == PLANENUM)
		return true;
	else
		return false;
}

void AirField::DebugOutput()
{
	int i, j;
	
	cout << "存活情况：";
	for (i = 0; i < PLANENUM; i++)
		cout << ps[i] << ' ';
	cout << endl;

	cout << "  | ";
	for (i = 0; i < COLNUM; i++)
		cout << i << ' ';
	cout << endl;
	cout << "----";
	for (i = 0; i < COLNUM; i++)
		cout << "--";
	cout << endl;

	for (i = 0; i < ROWNUM; i++)
	{
		cout << i << " |" << ' ';
		for (j = 0; j < COLNUM; j++)
			cout << board[i][j] << ' ';
		cout << endl;
	}
	cout << endl;
}
