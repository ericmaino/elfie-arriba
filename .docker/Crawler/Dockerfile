FROM arriba-base as build

# # #  --- Functions Runtime Image --- # # #
FROM mcr.microsoft.com/dotnet/core/runtime

COPY --from=build /src/Arriba/Tools/bin/Debug/netcoreapp3.1 /Arriba/Tools/app
ADD ./service-config/ /Databases
WORKDIR /Arriba/Tools/app

RUN ls -l
RUN find /Arriba
CMD ["dotnet", "Arriba.WorkItemCrawler.dll", "configName=cseng", "mode=-i"] 