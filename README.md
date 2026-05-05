## Описание
Демонстрационное приложение на .NET 8, разработанное в рамках практики SRE. 
Реализует API управления заметками с распределенным кэшированием.

### Технологический стек:
- **App:** .NET 8 Web API
- **Cache:** Redis (Bitnami Helm Chart)
- **Logging:** Serilog (Compact JSON format)
- **Deployment:** Helm + ArgoCD (GitOps)
- **Monitoring:** Grafana + ElastAlert

## Архитектурные решения и обоснования
1. **Несколько экземпляров (Replicas: 3):** Обеспечивает отказоустойчивость. Если один под упадет, трафик распределится на остальные.
2. **Redis как внешний кэш:** Так как экземпляров несколько, локальный кэш (In-Memory) приведет к рассинхронизации данных. Redis обеспечивает единое состояние (Shared State) для всех подов.
3. **JSON логи:** Позволяют ElasticSearch автоматически индексировать поля (`level`, `method`, `NoteId`) без использования сложных регулярных выражений. Это критично для быстрой настройки алертов.
4. **GitOps (ArgoCD):** Весь жизненный цикл приложения описан в коде. Изменение реплик или версии образа происходит через Pull Request, а не руками в консоли.

## Эндпоинты и тест-кейсы
Приложение доступно на порту `8080` (внутри кластера) или через `Port-forward`.

### 1. Создание заметки (POST)
- **Позитивный:** `curl -X POST http://localhost:8080/api/notes/1 -H "Content-Type: application/json" -d '{"content": "Hello"}'`
- **Негативный (генерирует Error лог):** `curl -X POST http://localhost:8080/api/notes/2 -H "Content-Type: application/json" -d '{"content": ""}'`

### 2. Получение заметки (GET)
- **Позитивный (из Redis):** `curl http://localhost:8080/api/notes/1`
- **Негативный:** `curl http://localhost:8080/api/notes/999` (возвращает 404 и Warning)

## Как запустить
1. Склонировать репозиторий.
2. Применить ArgoCD Application: `kubectl apply -f argocd/application.yaml`.
3. Дождаться статуса `Synced` в панели ArgoCD.

## Мониторинг
- Модель дашборда Grafana: `observability/grafana-dashboard.json`
- Правило ElastAlert: `observability/elastalert-rule.yaml` (настроено на отлов логов с уровнем `Error`).
