#!/bin/sh
echo Checking for container...

echo Starting Oracle database...
docker run --name oracle -d -p 1521:1521 --privileged docker.pkg.github.com/s2shape/docker-images/oracle-xe:11.2.0

echo Waiting for oracle to startup...
sleep 30

echo Inserting test data...
docker cp OracleSupplyCollectorLoader/tests/data.sql oracle:/data.sql
docker exec -u oracle oracle sqlplus system/oracle@localhost @/data.sql

export ORACLE_USER=system
export ORACLE_PASSWORD=oracle
export ORACLE_SID=XE
export ORACLE_HOST=localhost

dotnet build
dotnet test
docker stop oracle
docker rm oracle
