FROM microsoft/dotnet:2.1-sdk
WORKDIR /app

# environmental behaviours
ENV serverdns=server
# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# copy and build everything else
COPY . ./
RUN dotnet publish -c Release -o out
ENTRYPOINT ["dotnet", "out/VSCode.dll"]