#include <iostream>
#include <iomanip>
#include <mysql.h>
#include <stdlib.h>
#include <stdio.h>
#include <cstring>
using namespace std;

#include "sql.h"

#define MYSQL_USERNAME "u1651965"
#define MYSQL_PASSWORD "u1651965"
#define MYSQL_DATABASE "db1651965"

// TODO:�Բ�ѯ�������Ż����Լ�return���ͷŵ��߼�

SQL_RESULT Login(Player &player, string password)
{
    MYSQL *mysql;
    MYSQL_RES *result;
    MYSQL_ROW row;
    char buffer[MAX_MYSQL_QUERY_LENGTH];

    // ��ʼ�� mysql ������ʧ�ܷ���NULL
    if ((mysql = mysql_init(NULL))==NULL)
    {
        cout << "mysql_init failed" << endl;
        return ERR_IN;
    }

    // �������ݿ⣬ʧ�ܷ���NULL
    // 1��mysqldû����
    // 2��û��ָ�����Ƶ����ݿ����
    if (mysql_real_connect(mysql, "localhost", MYSQL_USERNAME, MYSQL_PASSWORD, MYSQL_DATABASE, 0, NULL, 0) == NULL)
    {
        cout << "mysql_real_connect failed(" << mysql_error(mysql) << ")" << endl;
        return ERR_IN;
    }

    //mysql_set_character_set(mysql, "gbk"); 

    // ���в�ѯ���ɹ�����0�����ɹ���0
    // 1����ѯ�ַ��������﷨����
    // 2����ѯ�����ڵ����ݱ�
    sprintf(buffer, "select id, password from player where name = '%s'", player.name.data());
    if (mysql_query(mysql, buffer))
    {
        cout << "mysql_query failed(" << mysql_error(mysql) << ")" << endl;
        return ERR_IN;
    }

    // ����ѯ����洢���������ִ����򷵻�NULL
    // ע�⣺��ѯ���ΪNULL�����᷵��NULL
    if ((result = mysql_store_result(mysql)) == NULL)
    {
        cout << "mysql_store_result failed" << endl;
        return ERR_IN;
    }

    SQL_RESULT r;
    if((row = mysql_fetch_row(result)) != NULL)
    {
        if(strcmp(row[1], password.data())==0)
        {
            player.id = atoi(row[0]);
            r = SUC;
        }
        else
            r = ERR_WP;
    }
    else
        r = ERR_NE;

    mysql_free_result(result);
    mysql_close(mysql);
    return r;
}

SQL_RESULT IsRegistered(Player &player)
{
    MYSQL *mysql;
    MYSQL_RES *result;
    MYSQL_ROW row;
    char buffer[MAX_MYSQL_QUERY_LENGTH];

    if ((mysql = mysql_init(NULL)) == NULL)
    {
        cout << "mysql_init failed" << endl;
        return ERR_IN;
    }

    if (mysql_real_connect(mysql, "localhost", MYSQL_USERNAME, MYSQL_PASSWORD, MYSQL_DATABASE, 0, NULL, 0) == NULL)
    {
        cout << "mysql_real_connect failed(" << mysql_error(mysql) << ")" << endl;
        return ERR_IN;
    }

    sprintf(buffer, "select id from player where name = '%s'", player.name.data());

    if (mysql_query(mysql, buffer))
    {
        cout << "mysql_query failed(" << mysql_error(mysql) << ")" << endl;
        return ERR_IN;
    }

    if ((result = mysql_store_result(mysql)) == NULL)
    {
        cout << "mysql_store_result failed" << endl;
        return ERR_IN;
    }

    SQL_RESULT r;
    if((row = mysql_fetch_row(result)) != NULL)
    {
        player.id = atoi(row[0]);
        r = SUC;
    }
    else
        r = ERR_NE;

    mysql_free_result(result);
    mysql_close(mysql);
    return r;
}
