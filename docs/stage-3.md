# Этап 3: бизнес-сервисы

Добавлены:

- DTO: CandidateDto, RegisterRequest, AuthResult, TwoFactorSetup, SessionUser.
- CandidateValidator, DtoValidation, ручной CandidateMapping.
- AuthService, UserSession, CandidateService, ListOrderService, ExportService.
- Регистрация сервисов AddElectionServices и подключение в WinForms composition root.
- ServiceTests: 13 тестов сервисов с настоящими SQLite и ASP.NET Core Identity.

AuthService использует UserManager и стандартный authenticator token provider Identity.
TOTP не реализован вручную в приложении. Независимый генератор RFC 6238 присутствует только
в тестах для получения кода и проверки настоящего Identity provider. Пароли хеширует
PasswordHasher, самописного PBKDF2 нет. Сессия и незавершённый вход существуют только в DI scope.
Подготовка 2FA возвращает otpauth URI; отрисовка QR через QRCoder относится к этапу 4.

Бизнес-сервисы не зависят от EF Core или WinForms. Репозитории и UnitOfWork предоставляют
работу с данными. Запись и перенумерация атомарны. JSON импортирует только DTO кандидатов,
не содержит пользователей и секретов аутентификации. Смена шаблона существующей записи
отклоняется явно, а номер изменяется только через ListOrderService.

Проверки:

- dotnet build MayorElection.sln --no-restore -m:1 -v:minimal — без ошибок/предупреждений.
- dotnet test MayorElection.sln --no-restore -m:1 -v:minimal — 16/16 (3 теста Data + 13 тестов Services).
- Валидация ФИО, дат, партийной принадлежности, подтверждения пароля.
- Смена статуса, редактирование, перемещения на границе и внутри списка, удаление/добавление.
- JSON round-trip и повторный импорт с теми же Id; неверный JSON и позиции сохраняют исходный список.
- Конфликт первичного ключа при импорте вызывает rollback; текущий scope остаётся пригодным к работе.
- Пароль проверяется стандартным Identity hasher, исходный пароль в PasswordHash отсутствует.
- TOTP setup, успешная проверка и отказ без первого фактора; истечение пятиминутного запроса.
- Блокировка после пяти ошибок пароля или второго фактора; полный вход сбрасывает счётчик.
- Logout закрывает операции с кандидатами.

GitFlow: feature/business-services от develop, commit feat, merge --no-ff в develop.
main не изменяется до релизного этапа. GUI на этом этапе не проверялся: пока открывается форма каркаса.
