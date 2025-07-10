# مرحله اول: آماده‌سازی محیط اجرایی
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

# مرحله دوم: بیلد پروژه
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# فقط فایل csproj را کپی کن و restore بزن
COPY MyTelegramBot.csproj ./
RUN dotnet restore

# حالا بقیه فایل‌ها را کپی و publish کن
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# مرحله سوم: اجرای اپلیکیشن نهایی
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MyTelegramBot.dll"]
