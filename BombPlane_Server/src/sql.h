#ifndef SQL_H
#define SQL_H

#include "common.h"

#define MAX_MYSQL_QUERY_LENGTH 1024

enum SQL_RESULT
{
    SUC,    // 成功
    ERR_NE, // 用户不存在
    ERR_WP, // 密码错误
    ERR_IN, // 内部错误
};

// 用user.name和user.password进行数据库匹配，并取出id
// 成功：返回SUC
// 无此用户：返回ERR_NE
// 密码错误：返回ERR_WP
SQL_RESULT Login(Player &player, string password);

// 根据user.name判断是否已注册，并去除id
// 已注册：返回SUC
// 未注册：返回ERR_NE
SQL_RESULT IsRegistered(Player &player);

#endif