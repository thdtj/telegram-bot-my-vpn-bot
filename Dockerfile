# ========================
# مرحله ۱: ایمیج بیس برای اجرا
# ========================
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# ========================
# مرحله ۲: بیلد پروژه
# ========================
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# کپی کل سورس پروژه
COPY . .

# بازیابی پکیج‌ها و بیلد
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ========================
# مرحله ۳: اجرای نهایی اپلیکیشن
# ========================
FROM base AS final
WORKDIR /app

# کپی فایل‌های بیلد شده از مرحله قبلی
COPY --from=build /app/publish .

# اجرای فایل اصلی اپلیکیشن
ENTRYPOINT ["dotnet", "MyTelegramBot.dll"]
