#!/bin/sh
docker run --name oracle -d -p 1521:1521 --privileged container-registry.oracle.com/database/standard:12.1.0.2
sleep 10
docker cp OracleSupplyCollectorTests/tests/data.sql oracle:/data.sql
docker exec -u oracle oracle sqlplus sys/Oracle@localhost @/data.sql
dotnet test
docker stop oracle
docker rm oracle
