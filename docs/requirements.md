# Соответствие заданию

| Требование | Реализация | Проверка |
| --- | --- | --- |
| Два шаблона кандидата | SelfNominee, PartyCandidate, CandidateEditForm | GUI-добавление обоих типов, SQLite round-trip |
| Валидация | DataAnnotations, CandidateValidator, Identity validators | UnitTests, ServiceTests |
| Иерархия участников | Participant → Candidate → два шаблона; ElectionCommissionMember | DataIntegrationTests |
| Номер, дата, статус | Candidate, CandidateStatus; CHECK/FK/unique в SQLite | DataIntegrationTests, ServiceTests |
| Список и фильтры | MainForm DataGridView, CandidateService.ListAsync | GUI списка, тесты фильтров |
| CRUD и смена статуса | CandidateService, CandidateEditForm, MainForm | UnitTests/ServiceTests, GUI-добавление |
| Выбор фильтров и статуса | ComboBox в MainForm/CandidateEditForm | Визуальная проверка форм |
| Сохранение и загрузка | ExportService, JSON v1, Save/OpenFileDialog | Round-trip, некорректный JSON, rollback при конфликте |
| Состав и порядок | ListOrderService, CandidateService.DeleteAsync/AddAsync | UnitTests и SQLite-тесты порядка/удаления |
| Регистрация, хеш и TOTP | AuthService, UserManager, PasswordHasher, AuthenticatorTokenProvider | UnitTests, ServiceTests, bootstrap-тест |

Слои: Domain, Data, Services, WinForms, Tests. Services не ссылается на EF Core или UI.
Миграция создана dotnet ef; справочник статусов заполнен HasData; администратор создаётся
в runtime с криптографически случайным паролем. Секретов в миграции нет.

Политика Identity: минимум 12 символов пароля с разными регистрами, цифрой и спецсимволом;
210 000 итераций стандартного PasswordHasher; 5 ошибок входа — блокировка на 15 минут.
Сессия появляется только после всех факторов и хранится в DI scope.

Границы проекта: нет привязки к законодательству конкретной страны (возраст, гражданство,
подписи и юридическая проверка партий не моделируются), нет разграничения CRUD по ролям.
Все зарегистрированные пользователи работают с общим списком; роль Admin создаётся seed-процедурой.
