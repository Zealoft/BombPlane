OBJS = main.o proto.o connection.o sql.o game.o airfield.o

# 当make server时，如果目录下有server.sh，会自动执行：
# cat server.sh >server 
# chmod a+x server

s:build/server

t:build/client build/prototest build/sqltest

# 服务器

build/server:$(addprefix build/, ${OBJS})
	g++ -g -o $@ $^ -L./lib -lprotobuf -Wl,-rpath=./lib $(shell mysql_config --libs)

build/main.o:src/main.cpp
	g++ -g -o $@ -c $<

build/proto.o:src/BombPlane_proto.pb.cc src/BombPlane_proto.pb.h
	g++ -g -o $@ -c $< -I./include

build/connection.o:src/connection.cpp src/connection.h src/common.h
	g++ -g -o $@ -c $< -I./include

build/sql.o:src/sql.cpp src/sql.h src/common.h
	g++ -g -o $@ -c $< $(shell mysql_config --cflags)

build/game.o:src/Game.cpp src/Game.h
	g++ -g -o $@ -c $<

build/airfield.o:src/AirField.cpp src/AirField.h src/common.h
	g++ -g -o $@ -c $<

# 测试
# QUESTION:为什么张子岳进行链接时不加include目录（-L）会报错？

build/client:test/udpclient.cpp build/proto.o
	g++ -g -o $@.o -c $< -I./include
	g++ -g -o $@ $@.o build/proto.o -L./lib -lprotobuf -Wl,-rpath=./lib

build/prototest:test/prototest.cpp build/proto.o
	g++ -g -o $@.o -c $< -Wno-deprecated -I./include
	g++ -g -o $@ $@.o build/proto.o -L./lib -lprotobuf -Wl,-rpath=./lib

build/sqltest:test/sqltest.cpp build/sql.o
	g++ -g -o $@.o -c $<
	g++ -g -o $@ $@.o build/sql.o $(shell mysql_config --libs)

#其它

clean:
	rm -rf build/*.o
	rm -f $(EXES)