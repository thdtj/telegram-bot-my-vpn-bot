# مرحله ۱: اجرای اپ
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# مرحله ۲: بیلد پروژه
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# کپی فایل پروژه برای restore
COPY MyTelegramBot.csproj ./
RUN dotnet restore

# کپی باقی فایل‌ها و publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# مرحله ۳: ایمیج نهایی
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyTelegramBot.dll"]
