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

// TODO:对查询语句进行优化，以及return、释放的逻辑

SQL_RESULT Login(Player &player, string password)
{
    MYSQL *mysql;
    MYSQL_RES *result;
    MYSQL_ROW row;
    char buffer[MAX_MYSQL_QUERY_LENGTH];

    // 初始化 mysql 变量，失败返回NULL
    if ((mysql = mysql_init(NULL))==NULL)
    {
        cout << "mysql_init failed" << endl;
        return ERR_IN;
    }

    // 连接数据库，失败返回NULL
    // 1、mysqld没运行
    // 2、没有指定名称的数据库存在
    if (mysql_real_connect(mysql, "localhost", MYSQL_USERNAME, MYSQL_PASSWORD, MYSQL_DATABASE, 0, NULL, 0) == NULL)
    {
        cout << "mysql_real_connect failed(" << mysql_error(mysql) << ")" << endl;
        return ERR_IN;
    }

    //mysql_set_character_set(mysql, "gbk"); 

    // 进行查询，成功返回0，不成功非0
    // 1、查询字符串存在语法错误
    // 2、查询不存在的数据表
    sprintf(buffer, "select id, password from player where name = '%s'", player.name.data());
    if (mysql_query(mysql, buffer))
    {
        cout << "mysql_query failed(" << mysql_error(mysql) << ")" << endl;
        return ERR_IN;
    }

    // 将查询结果存储起来，出现错误则返回NULL
    // 注意：查询结果为NULL，不会返回NULL
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
