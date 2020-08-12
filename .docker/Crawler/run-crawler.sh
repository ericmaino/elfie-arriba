#!/bin/sh
RUN_CRAWLER=1

if [ ! -z ${CRAWLER_DISABLED} ];
then
   RUN_CRAWLER=0

   if [ "${CRAWLER_DISABLED}" = "0" ] || [ "${CRAWLER_DISABLED}" = "false" ];
   then
      RUN_CRAWLER=1
   fi
fi

if [ "${RUN_CRAWLER}" = "1" ]
then
   while :;
   do
      echo dotnet Arriba.WorkItemCrawler.dll $@
      dotnet Arriba.WorkItemCrawler.dll $@
      sleep 300
   done   
else
   echo "CRAWLER DISABLED"
fi

