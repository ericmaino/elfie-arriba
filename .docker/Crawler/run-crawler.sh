#!/bin/sh
while [ -z ${CRAWLER_DISABLED} ]
do
   echo dotnet Arriba.WorkItemCrawler.dll $@
   dotnet Arriba.WorkItemCrawler.dll $@
   sleep 300
done