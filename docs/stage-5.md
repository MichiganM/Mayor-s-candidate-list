# Этап 5: тесты, документация и релиз

В UnitTests добавлены 6 модульных проверок без SQLite: валидация до записи,
смена статуса, порядок списка, стандартный PasswordHasher с солью,
стандартный AuthenticatorTokenProvider и датированная валидация через TimeProvider.
Вместе с существующими интеграционными проверками — 22 теста.

Тестовые зависимости обновлены и закреплены: xUnit 2.9.3, runner 2.8.2,
Microsoft.NET.Test.Sdk 17.14.2, coverlet.collector 6.0.4. Это устранило найденные
уязвимости транзитивных System.Net.Http 4.3.0 и System.Text.RegularExpressions 4.3.0.
Повторный аудит NuGet всех пяти проектов не обнаружил известных уязвимых пакетов.
Результат относится к использованному индексу advisory на дату проверки и не заменяет будущие обновления.

Добавлены GitHub Actions для Windows с Release build, тестами, аудитом, проверкой миграций
и автономным publish win-x64; actions закреплены SHA официальных версий.
Подготовлены CHANGELOG, правила AGENTS, таблица требований и сценарий приёмки.

Проверки, выполненные локально:

- Release-тесты: 22/22.
- Покрытие строк Services: 97,58%; Domain: 100%; Data: 62,19%.
  Отчёт Cobertura в artifacts/TestResults, WinForms этим запуском не инструментировался.
- Автономный publish win-x64 успешно собран и запущен с отдельной БД в .tools:
  показалась настоящая форма входа, bootstrap прошёл без ошибки. Рабочая БД не изменялась.
- На отдельной artifacts/migration-check.db выполнены InitialCreate → 0 → InitialCreate.
- Полный ручной сценарий с внешним аутентификатором и файловыми диалогами не выполнялся;
  TOTP и файловые операции проверены автоматическими тестами сервисов.

GitFlow: feature/release-readiness → develop (--no-ff), затем release/1.0.0
с номером версии → main и develop (--no-ff), annotated tag v1.0.0 на main.
Публикуемые refs: main, develop, v1.0.0. История feature- и release-коммитов сохраняется merge-коммитами.

Remote: https://github.com/MichiganM/Mayor-s-candidate-list.git
