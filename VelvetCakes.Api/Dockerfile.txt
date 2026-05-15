# 1. Базовый образ для сборки
# Мы говорим Docker: "Возьми официальный образ .NET 8 для сборки приложений"
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 2. Устанавливаем рабочую директорию внутри контейнера
# Это как "зайти в папку /src"
WORKDIR /src

# 3. Копируем файл проекта (чтобы Docker знал, какие зависимости нужны)
# Сначала копируем только .csproj файл — это ускоряет сборку
COPY ["VelvetCakes.Api.csproj", "."]

# 4. Восстанавливаем зависимости (NuGet пакеты)
# Это аналог "dotnet restore" на вашем компьютере
RUN dotnet restore

# 5. Копируем ВСЕ файлы проекта в контейнер
COPY . .

# 6. Публикуем приложение (создаём готовые файлы)
# Это аналог "dotnet publish -c Release"
RUN dotnet publish -c Release -o /app/publish

# 7. Финальный образ (тот, который будет работать)
# Берём лёгкий образ ASP.NET (только для запуска, без инструментов сборки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# 8. Рабочая директория для запущенного приложения
WORKDIR /app

# 9. Устанавливаем необходимые системные библиотеки
# ICU нужна для корректной работы с датами и языками
# tzdata нужна для часовых поясов
RUN apt-get update && apt-get install -y icu-devtools tzdata && rm -rf /var/lib/apt/lists/*

# 10. Копируем собранное приложение из предыдущего этапа
COPY --from=build /app/publish .

# 11. Создаём папку для загруженных пользователем изображений
# Эта папка будет хранить фото десертов
RUN mkdir -p /app/wwwroot/uploads

# 12. Настройки окружения
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false  # Включаем поддержку разных языков
ENV ASPNETCORE_URLS=http://+:8080                 # Приложение слушает порт 8080
ENV TZ=Europe/Moscow                              # Устанавливаем московское время

# 13. Открываем порт 8080 для внешних подключений
EXPOSE 8080

# 14. Команда запуска приложения
ENTRYPOINT ["dotnet", "VelvetCakes.Api.dll"]