FROM ericmaino/elfie-arriba:latest as build

# # #  --- Dotnet Core Runtime Image --- # # #
FROM mcr.microsoft.com/dotnet/core/runtime

COPY --from=build /src/Arriba/Tools/bin/Debug/netcoreapp3.1 /Arriba/Tools/app
ADD ./service-config/ /Databases
ADD ./run-crawler.sh /Arriba/Tools/app

WORKDIR /Arriba/Tools/app
RUN chmod +x ./run-crawler.sh

ENTRYPOINT [ "./run-crawler.sh" ]
CMD ["configName=default", "mode=-i"]