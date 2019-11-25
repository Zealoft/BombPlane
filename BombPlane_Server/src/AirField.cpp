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

//��״�жϺͻ�βԽ���ж�
//��״ȷ����ȷ����ȷ����ͷ��������Խ��
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
	//1������ڻ�ͷ�ϱ�
	if (pl[1][0] < pl[0][0])
	{
		//2������ڻ�ͷ�ϱ�
		if (pl[2][0] < pl[0][0])
		{
			if (IsCorrect(pl, DOWN))
				return DOWN;
			else
				return ERROR;
		}
		//2������ڻ�ͷ�±�
		else
		{
			//2������ڻ�ͷ����
			if (pl[2][1] < pl[0][1])
			{
				if (IsCorrect(pl, RIGHT))
					return RIGHT;
				else
					return ERROR;
			}
			//2������ڻ�ͷ����
			else
			{
				if (IsCorrect(pl, LEFT))
					return LEFT;
				else
					return ERROR;
			}
		}
	}
	//1������ڻ�ͷ�±�
	else
	{
		//2������ڻ�ͷ�±�
		if (pl[2][0] > pl[0][0])
		{
			if (IsCorrect(pl, UP))
				return UP;
			else
				return ERROR;
		}
		//2������ڻ�ͷ�ϱ�
		else
		{
			//2������ڻ�ͷ����
			if (pl[2][1] < pl[0][1])
			{
				if (IsCorrect(pl, RIGHT))
					return RIGHT;
				else
					return ERROR;
			}
			//2������ڻ�ͷ����
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
	//�ж������ǵڼ���
	for (num = 0; num < PLANENUM; num++)
		if (ps[num] == DEAD)
			break;
	if (num == PLANENUM)
		return FULL;

	//��λ�㣨��ͷ����⣩����
	for (i = 0; i < LOCATORBLOCKNUM; i++)
		if (pl[i][0] < 0 || pl[i][0] >= ROWNUM ||
			pl[i][1] < 0 || pl[i][1] >= COLNUM)
			return LOCATOROB;
	
	/*�Ѿ�������״�жϣ��˴����ж�����
	//��ͷ�жϹ���
	if (pl[0][0] < 3 && pl[0][1] < 3 ||
		pl[0][0] < 3 && COLNUM - pl[0][1] <= 3 ||
		ROWNUM - pl[0][0] <= 3 && pl[0][1] < 3 ||
		ROWNUM - pl[0][0] <= 3 && COLNUM - pl[0][1] <= 3)
		return OTHEROB;

	//����жϹ���
	//�ȼ��ڲ��������ĸ���
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
		//���л�ͷ
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
			//���л���
			if (ppos[i][j][0] == cd[0] && ppos[i][j][1] == cd[1])
			{
				if (board[cd[0]][cd[1]] == HIDDEN)
				{
					board[cd[0]][cd[1]] = DAMAGED;
					return HIT;
				}
				else
					return MISS;
				
				/*��Ӧ�ڴ��ж�
				//�жϻ��в����Ƿ񹹳�һ�������ķɻ�
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
		//��ͷ
		if (ppos[i][0][0] != pl[0][0] || ppos[i][0][1] != pl[0][1])
			continue;
		//����
		if ((ppos[i][1][0] != pl[1][0] || ppos[i][1][1] != pl[1][1]) &&
			(ppos[i][1][0] != pl[2][0] || ppos[i][1][1] != pl[2][1]))
			continue;
		if ((ppos[i][5][0] != pl[1][0] || ppos[i][5][1] != pl[1][1]) &&
			(ppos[i][5][0] != pl[2][0] || ppos[i][5][1] != pl[2][1]))
			continue;

		//�²���ȷ
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
	
	cout << "��������";
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
