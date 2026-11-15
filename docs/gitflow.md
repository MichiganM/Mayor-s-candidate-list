# GitFlow

История разработки сохранена настоящими feature-ветками и merge-коммитами `--no-ff`.
main содержит production-релиз, develop — интеграционную историю. Версия: v1.0.0.

## Новая функция

```powershell
git switch develop
git switch -c feature/short-name
# Реализация и проверки
git add <paths>
git commit -m "feat: describe behavior"
git switch develop
git merge --no-ff feature/short-name
```

## Релиз

```powershell
git switch develop
git switch -c release/1.0.0
# Версия, changelog и проверка сборки/тестов
git switch main
git merge --no-ff release/1.0.0
git tag -a v1.0.0 -m "Release 1.0.0"
git switch develop
git merge --no-ff release/1.0.0
git push --atomic origin main develop v1.0.0
```

## Хотфикс (шаблон для будущего исправления)

```powershell
git switch main
git switch -c hotfix/1.0.1
# Исправление и тесты; обновить Version и changelog
git add <paths>
git commit -m "fix: describe correction"
git switch main
git merge --no-ff hotfix/1.0.1
git tag -a v1.0.1 -m "Release 1.0.1"
git switch develop
git merge --no-ff hotfix/1.0.1
git push --atomic origin main develop v1.0.1
```

Хотфикс-ветка не создаётся без реального дефекта выпущенной версии.
Для просмотра графа: `git log --oneline --graph --decorate --all`.
Feature/release-ветки сохранены локально; production/integration refs и тег публикуются в GitHub.
