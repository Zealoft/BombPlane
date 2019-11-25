#ifndef SQL_H
#define SQL_H

#include "common.h"

#define MAX_MYSQL_QUERY_LENGTH 1024

enum SQL_RESULT
{
    SUC,    // �ɹ�
    ERR_NE, // �û�������
    ERR_WP, // �������
    ERR_IN, // �ڲ�����
};

// ��user.name��user.password�������ݿ�ƥ�䣬��ȡ��id
// �ɹ�������SUC
// �޴��û�������ERR_NE
// ������󣺷���ERR_WP
SQL_RESULT Login(Player &player, string password);

// ����user.name�ж��Ƿ���ע�ᣬ��ȥ��id
// ��ע�᣺����SUC
// δע�᣺����ERR_NE
SQL_RESULT IsRegistered(Player &player);

#endif