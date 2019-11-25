#include <iostream>
#include <list>
#include <cstdlib>
#include <time.h>
using namespace std;

#include "../src/sql.h"

int main()
{
    Player p;
    SQL_RESULT r;

    p.name = "p1";
    r = Login(p, "96e79218965eb72c92a549dd5a330112");
    cout << r << ' ' << p.id << ' ' << p.name << endl;

    p.name = "p1";
    r = Login(p, "9");
    cout << r << ' ' << p.id << ' ' << p.name << endl;

    p.name = "p";
    r = Login(p, "9");
    cout << r << ' ' << p.id << ' ' << p.name << endl;

    p.name = "p2";
    r = IsRegistered(p);
    cout << r << ' ' << p.id << ' ' << p.name << endl;

    p.name = "nnn";
    r = IsRegistered(p);
    cout << r << ' ' << p.id << ' ' << p.name << endl;

    // iterator²âÊÔ
    list<int> l;
    list<int>::iterator iter;

    l.push_back(1);
    //l.push_back(2);
    iter = l.begin();
    iter = l.erase(iter);
    cout<<(iter==l.end())<<' '<<(iter==l.begin())<<' '<<(l.begin()==l.end())<<endl;
    iter--;
    cout<<(iter==l.end())<<' '<<(iter==l.begin())<<' '<<(l.begin()==l.end())<<endl;
    cout<<"end"<<endl;

    // rand²âÊÔ
    srand(time(0));
    int a, b;
    a = rand() % 2;
    b = !a;
    cout<<a<<' '<<b<<endl;
    a = rand() % 2;
    b = !a;
    cout<<a<<' '<<b<<endl;

    int *www = new int;
    *www = 1;
    cout<<*www;
}