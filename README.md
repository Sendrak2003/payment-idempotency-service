# wallet-api

Сервис для вступительного задания «Платёж прошёл? Докажи»: принимает платёжные операции,
надёжно отправляет их во внешний `provider-simulator` и считает операцию завершённой только
после callback-квитанции от провайдера.

## Стек

- **.NET 10**, ASP.NET Core Minimal API
- **Clean Architecture** (без лишнего): `WalletApi.Domain` → `WalletApi.Application` →
  `WalletApi.Infrastructure` → `WalletApi.Api`
- **PostgreSQL** (EF Core) — единственный источник истины, переживает пересоздание контейнера
  благодаря volume `candidate-data`
- Асинхронная отправка платежа выполняется **вне** HTTP-запроса `/submit` через `Channel<string>`
  в том же процессе + фоновый `BackgroundService`, без отдельного брокера — задание прямо
  говорит, что большая архитектура не нужна
- Ручной retry с идемпотентным `Idempotency-Key`/`X-Correlation-ID` для вызовов провайдера
- **Swagger UI** на `/swagger`

Всё собирается и работает только через Docker (локального .NET SDK на машине нет — сборка идёт
внутри `mcr.microsoft.com/dotnet/sdk:10.0`).

## Запуск

```bash
docker compose up --build
```

Поднимаются три сервиса: `candidate-service` (порт 8080), `postgres`, `provider-simulator`
(порт 8081, публичный образ `ghcr.io/fintech-dev-lab/internship-provider-simulator:v0.2.0`).
При первом старте кандидат сам создаёт схему БД (`EnsureCreated`, с ретраями, пока Postgres не
готов).

```bash
curl http://localhost:8080/health
# {"status":"OK"}
```

Swagger UI: http://localhost:8080/swagger

## Архитектура и как выполнен главный инвариант

- **Операция** (`Operation`, `WalletApi.Domain`) — агрегат со статусами `CREATED → PROCESSING →
  {COMPLETED | REJECTED}` и списком неизменяемых событий (`OperationEvent`, с `fromStatus`/
  `toStatus`), не переходит из финального состояния обратно.
- **`POST /operations/{id}/submit`** внутри одной транзакции: блокирует строку операции
  (`SELECT ... FOR UPDATE`) и переводит `CREATED → PROCESSING`. Сам HTTP-вызов к провайдеру в
  этот момент ещё не выполняется, поэтому блокировка строки не удерживается на время внешнего
  запроса — сразу после коммита операция лишь кладётся в in-process очередь (best-effort хинт;
  durability обеспечивает уже сохранённый статус `PROCESSING` в Postgres, а не очередь).
  Повторный/конкурентный `submit` для операции не в статусе `CREATED` просто возвращает уже
  сохранённое состояние (200), а не создаёт новый переход (202 получает только тот запрос,
  который реально выполнил переход).
- **`PaymentSubmissionWorker`** читает очередь и вызывает провайдера (`POST {PROVIDER_URL}/payments`)
  с `Idempotency-Key`/`X-Correlation-ID`, равными `operationId`. При сетевой ошибке/таймауте
  ничего не меняет — операция остаётся `PROCESSING`, т.к. провайдер мог уже принять платёж. При
  успехе фиксируется только `providerPaymentId`, статус не меняется — финальный статус
  выставляет только `/receipts`.
- **`PendingOperationsRecoveryService`** стартует сразу при запуске и затем каждые 10 секунд
  находит операции `PROCESSING` и заново кладёт их в очередь с тем же `operationId` (тем же
  ключом идемпотентности): операции без `providerPaymentId` — уже через 5 секунд (провайдер мог
  и не получить запрос), операции с уже известным `providerPaymentId` — через 30 секунд
  (callback может идти естественным образом дольше, но если он всё же потерян, повторный
  `POST /payments` с тем же ключом — задокументированный путь восстановления и не создаёт
  второй платёж).
- **`POST /receipts`**: тоже одна транзакция с блокировкой строки. Идемпотентный повтор той же
  квитанции — `204` без нового перехода; **поздняя квитанция с противоположным результатом для
  уже финальной операции — тоже `204`, игнорируется, финальный статус не меняется** (это
  логируется, но не 409); `409` — только когда `providerPaymentId` в квитанции не совпадает с
  уже привязанным к операции.

## API

| Метод | Маршрут | Успешный статус |
|---|---|---|
| GET | `/health` | 200 |
| POST | `/operations` | 201 |
| POST | `/operations/{id}/submit` | 202 (новая отправка) / 200 (уже отправлена) |
| POST | `/receipts` | 204 |
| GET | `/operations/{id}` | 200 |
| GET | `/operations/{id}/events` | 200 |

Тело события (`GET /operations/{id}/events`):

```json
{
  "eventId": 1,
  "type": "CREATED",
  "fromStatus": null,
  "toStatus": "CREATED",
  "message": "Operation created",
  "occurredAt": "2026-07-15T12:00:00Z"
}
```

Тело квитанции (`POST /receipts`, шлёт `provider-simulator`):

```json
{
  "providerPaymentId": "aa5b7856-e9f2-4fd5-955b-38b1f28d9c57",
  "operationId": "operation-123",
  "result": "COMPLETED",
  "message": "Payment completed",
  "occurredAt": "2026-07-15T12:00:00Z"
}
```

## Полный сквозной сценарий

```bash
# 1. health
curl http://localhost:8080/health

# 2. создать операцию
curl -X POST http://localhost:8080/operations \
  -H "Content-Type: application/json" \
  -d '{"operationId":"operation-123","amount":"1000.00","currency":"RUB","description":"Оплата заказа"}'
# -> 201, status=CREATED

# 2a. повторное создание того же operationId -> 409
curl -i -X POST http://localhost:8080/operations \
  -H "Content-Type: application/json" \
  -d '{"operationId":"operation-123","amount":"1000.00","currency":"RUB"}'

# 3. отправить в провайдера (202 — новая отправка; повтор/конкурентный вызов вернёт 200)
curl -i -X POST http://localhost:8080/operations/operation-123/submit
curl -i -X POST http://localhost:8080/operations/operation-123/submit

# 4. посмотреть текущее состояние (provider-simulator сам присылает callback на /receipts;
#    статус станет COMPLETED/REJECTED обычно в течение секунды)
curl http://localhost:8080/operations/operation-123

# 5. история переходов
curl http://localhost:8080/operations/operation-123/events

# 6. неизвестная операция -> 404
curl -i http://localhost:8080/operations/does-not-exist
```

## Что проверено вручную (через `docker compose up` с реальным `provider-simulator`)

- создание операции и `409` на дубликат `operationId`;
- полный цикл `submit -> provider -> callback -> COMPLETED` на настоящем provider-simulator;
- 5 параллельных `submit` одной операции — ровно один `202`, остальные `200`, ровно один переход
  `PROCESSING` в истории;
- повтор той же квитанции — `204` без нового события;
- поздняя квитанция с противоположным результатом (та же `providerPaymentId`) — `204`, финальный
  статус не изменился;
- квитанция с чужим `providerPaymentId` для уже привязанной операции — `409`;
- `404` для неизвестной операции;
- данные (включая `providerPaymentId` и историю событий) переживают `docker restart` /
  `docker kill` + `docker start` контейнера `candidate-service`;
- `submit`, прерванный рестартом контейнера до вызова провайдера, продолжается после старта тем
  же `Idempotency-Key` и доходит до `COMPLETED`.

## Переменные окружения

| Переменная | Назначение |
|---|---|
| `PROVIDER_URL` | базовый адрес провайдера, вызывается `{PROVIDER_URL}/payments` |
| `ConnectionStrings__Postgres` | строка подключения к Postgres |

Обе уже выставлены в `docker-compose.yml`.
