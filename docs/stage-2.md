# Этап 2: Domain + Data

Добавлены сущности Participant, Candidate, SelfNominee, PartyCandidate,
ElectionCommissionMember, ApplicationUser; enum статуса и шаблона,
справочник CandidateStatusDefinition и контракты ICandidateRepository/IUnitOfWork.

AppDbContext наследует IdentityDbContext<ApplicationUser>. Fluent API задаёт TPH,
уникальный номер кандидата, FK статуса и CHECK-ограничения. Миграция InitialCreate
создана инструментом dotnet ef; включает Identity, участников и seed трёх статусов.

CandidateRepository поддерживает фильтрацию, сортировку и tracking для редактирования.
UnitOfWork поддерживает атомарную транзакцию с несколькими сохранениями.
Порядок перестановки уникальных номеров и бизнес-валидация относятся к этапу 3.

DatabaseInitializer применяет миграции и создаёт администратора только в пустом
хранилище пользователей. UserManager использует PasswordHasher с 210 000 итераций.
Пароль генерируется RandomNumberGenerator, не хранится в миграции и не логируется.
Вывод начальных реквизитов — через callback, который WinForms направляет в консоль.
Повторный запуск не меняет пароль. Удалять рабочую БД для восстановления пароля нельзя:
процедура восстановления будет отдельной задачей.

Команды:

```powershell
git switch -c feature/domain-data
dotnet restore MayorElection.sln --force
dotnet build MayorElection.sln --no-restore -m:1 -v:minimal
dotnet ef migrations add InitialCreate --project src/MayorElection.Data --startup-project src/MayorElection.Data
dotnet test MayorElection.sln -m:1 -v:minimal
dotnet ef database update --project src/MayorElection.Data --startup-project src/MayorElection.Data --no-build
dotnet ef migrations has-pending-model-changes --project src/MayorElection.Data --startup-project src/MayorElection.Data --no-build
```

Проверки: сборка без ошибок и предупреждений, 3 интеграционных теста SQLite/Identity
прошли. Миграция применена к файловой SQLite; несохранённых изменений модели нет.
Тесты проверяют round-trip типов и фильтры, уникальность номера и rollback,
однократный bootstrap, роль и проверку пароля Identity. GUI вручную не проверялся.

Ветка feature/domain-data сливается в develop через --no-ff. Релиз и push ещё не выполняются.
