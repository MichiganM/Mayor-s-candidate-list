# MayorElection

Курсовой проект: программа формирования списка кандидатов на выборах мэра.

## Состояние

Этап 2: Domain и Data реализованы, миграция InitialCreate создана. Бизнес-сервисы и рабочие формы будут добавлены на следующих этапах.

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
При запуске применяются миграции. В пустой БД создаётся пользователь `admin` с ролью `Admin`.
Случайный пароль выводится в отдельную консоль только при создании; сохраните его.
Пароль не записывается в файлы или логи. Identity PasswordHasher настроен на 210 000 итераций,
блокировка — 5 неудачных попыток на 15 минут (учёт попыток будет реализован AuthService на этапе 3).
До этапа 4 экран каркаса не выполняет вход и не предоставляет доступ к операциям с кандидатами.

## Миграции

Выполнять из корня репозитория:

```powershell
dotnet tool restore
dotnet ef database update --project src/MayorElection.Data --startup-project src/MayorElection.Data
dotnet ef migrations add <Name> --project src/MayorElection.Data --startup-project src/MayorElection.Data
```

Design-time factory ищет `src/MayorElection.WinForms/appsettings.json` вверх от текущего каталога.
Другой файл можно передать после `-- --settings <absolute-path>`.
Относительный путь SQLite разрешается относительно текущего рабочего каталога процесса;
для постоянного расположения задайте абсолютный `Data Source` в своей конфигурации.
Команда `database update` создаёт схему и справочник статусов; администратор создаётся при запуске приложения.

Иерархия Participant хранится по TPH. Номер кандидата уникален и положителен;
ссылочная целостность статусов и обязательность партийных данных защищены ограничениями SQLite.
Изменяемые сущности загружаются GetByIdAsync с tracking, списки — без tracking.
Repository не выполняет SaveChanges: сохранением и транзакциями управляет UnitOfWork.

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
