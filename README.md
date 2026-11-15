# MayorElection

Курсовой проект: программа формирования списка кандидатов на выборах мэра.

## Состояние

Этап 1: каркас решения. Функции кандидатов и аутентификации будут реализованы на следующих этапах.

## Требования и запуск

Windows, .NET 8 SDK, доступ к NuGet для восстановления зависимостей.
Выбран .NET 8 согласно требованиям задания; поддержка этой версии завершилась 10 ноября 2026 года.
Для промышленного использования требуется переход на поддерживаемую версию.

```powershell
dotnet restore MayorElection.sln
dotnet build MayorElection.sln --no-restore
dotnet test MayorElection.sln --no-build
dotnet run --project src/MayorElection.WinForms
```

Если SDK установлен локально, используйте `.tools/dotnet/dotnet.exe` вместо `dotnet`.
На этапе каркаса открывается информационная форма; вход появится на этапе 4.

## Архитектура

- Domain: сущности, enum и контракты репозиториев. Зависимость от Identity необходима для требуемого ApplicationUser : IdentityUser.
- Data → Domain: EF Core, SQLite, Identity store, репозитории и миграции.
- Services → Domain: бизнес-сервисы, DTO, валидация. Слой не зависит от EF Core или UI.
- WinForms → Services, Data: формы и composition root с DI.
- Tests → Services, Data: модульные и интеграционные проверки.

Маппинг DTO будет ручным: небольшой набор сущностей позволяет явно контролировать поля без дополнительной библиотеки.
Строка подключения задаётся в `appsettings.json`. Реальная БД и логи исключены из Git.

## GitFlow

`main` — релизы, `develop` — интеграция. Функции: `feature/*` от develop с merge `--no-ff`.
Релизы: `release/*` от develop, merge в main и develop, тег версии.
Исправления релиза: `hotfix/*` от main с merge в main и develop.
На этапе 1 релизный тег не создаётся. GitHub remote будет добавлен после получения URL.

## План скриншотов

После реализации добавить изображения: вход, регистрация, настройка 2FA, список с фильтрами, редактирование кандидата.
