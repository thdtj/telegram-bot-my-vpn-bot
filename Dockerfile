# مرحله اول: آماده‌سازی محیط اجرایی
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

# مرحله دوم: بیلد پروژه
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# مرحله سوم: اجرای اپلیکیشن نهایی
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "telegram-bot-my-vpn-bot.dll"]
