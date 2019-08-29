#!/bin/sh
echo Checking for container...

result=$( docker images -q madhead/docker-oracle-xe )

if [[ -n "$result" ]]; then
  echo "Container exists"
else
  echo "Container madhead/docker-oracle-xe not found, please build it at oracle/build-docker-image.sh"
  exit 1
fi

echo Starting Oracle database...
docker run --name oracle -d -p 1521:1521 --privileged madhead/docker-oracle-xe

echo Waiting for oracle to startup...
sleep 30

echo Inserting test data...
docker cp OracleSupplyCollectorTests/tests/data.sql oracle:/data.sql
docker exec -u oracle oracle sqlplus system/oracle@localhost @/data.sql

echo { > OracleSupplyCollectorTests/Properties/launchSettings.json
echo   \"profiles\": { >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo     \"OracleSupplyCollectorTests\": { >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo       \"commandName\": \"Project\", >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo       \"environmentVariables\": { >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo         \"ORACLE_USER\": \"system\", >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo         \"ORACLE_PASSWORD\": \"oracle\", >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo         \"ORACLE_SID\": \"XE\", >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo         \"ORACLE_HOST\": \"localhost\" >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo       } >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo     } >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo   } >> OracleSupplyCollectorTests/Properties/launchSettings.json
echo } >> OracleSupplyCollectorTests/Properties/launchSettings.json

dotnet build
dotnet test
docker stop oracle
docker rm oracle
