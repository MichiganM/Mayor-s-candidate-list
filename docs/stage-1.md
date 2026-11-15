# Этап 1: каркас решения

Созданы solution, три библиотеки net8.0, WinForms net8.0-windows и проект xUnit.
Версии PackageReference закреплены после восстановления NuGet; EF Core и Identity согласованы в ветке 8.0.
Локальный инструмент dotnet-ef: 8.0.34. SDK: 8.0.428 (локально, исключён из Git).

Основные выполненные команды (повторены для соответствующих проектов):

```powershell
git init -b main
git commit -m "chore: initialize repository and architecture outline"
git switch -c develop
git switch -c feature/solution-scaffold
dotnet new sln -n MayorElection
dotnet new classlib -n MayorElection.Domain -o src/MayorElection.Domain -f net8.0 --no-restore
dotnet new classlib -n MayorElection.Data -o src/MayorElection.Data -f net8.0 --no-restore
dotnet new classlib -n MayorElection.Services -o src/MayorElection.Services -f net8.0 --no-restore
dotnet new winforms -n MayorElection.WinForms -o src/MayorElection.WinForms -f net8.0 --no-restore
dotnet new xunit -n MayorElection.Tests -o tests/MayorElection.Tests -f net8.0 --no-restore
dotnet sln MayorElection.sln add <project.csproj>
dotnet add <project> reference <dependency>
dotnet add <project> package <package> --version <version>
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 8.0.*
dotnet build MayorElection.sln -m:1 -v:minimal
dotnet test MayorElection.sln --no-build -m:1 -v:minimal
git diff --check
git commit -m "feat: scaffold layered solution and dependency injection"
git switch develop
git merge --no-ff feature/solution-scaffold -m "chore: merge solution scaffold into develop"
```

Сборка: успешно, ошибок 0. При сборке получены предупреждения NU1900 из-за
недоступности индекса проверки уязвимостей NuGet в ограниченной среде.
Модульные тесты будут добавлены на этапе 5; пустой проект не подтверждает корректность сервисов.
GUI вручную на этом этапе не проверялся.

main сохраняет исходный коммит, develop содержит каркас после merge --no-ff.
Релиз, тег и публикация в GitHub относятся к последующим этапам.
