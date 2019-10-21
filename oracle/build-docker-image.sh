#!/bin/bash

ORACLE_DISTR=oracle-xe-11.2.0-1.0.x86_64.rpm
if [[ ! -f "$ORACLE_DISTR" ]]; then
    echo "Missing $ORACLE_DISTR !"
    echo "Download it from http://download.oracle.com/otn/linux/oracle11g/xe/oracle-xe-11.2.0-1.0.x86_64.rpm.zip and unpack"
    exit 1
fi
git clone https://github.com/madhead/docker-oracle-xe.git
mkdir docker-oracle-xe/rpm
tar czvf docker-oracle-xe/rpm/$ORACLE_DISTR.tar.gz $ORACLE_DISTR
pushd docker-oracle-xe
docker build -t docker.pkg.github.com/s2shape/docker-images/oracle-xe:11.2.0 .
popd
