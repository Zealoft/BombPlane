drop database if exists db1651965;
create database db1651965;
use db1651965;

create table player(
    id int not null auto_increment primary key,
    name varchar(8) not null,
    password char(32) not null
)default charset = gbk;

drop procedure if exists add_player;
delimiter //
create procedure add_player()
begin
    declare i int;
    set i = 0;
    while i < 1000 do
        insert into player(name, password) values(convert(i, char), md5(convert(i, char)));
        set i = i + 1;
    end while;
end//
delimiter ;

insert into player(name, password) values('ricozero', md5('ricozero'));
insert into player(name, password) values('simon', md5('simon'));
insert into player(name, password) values('zealoft', md5('zealoft'));
call add_player();

drop procedure if exists add_player;