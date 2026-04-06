# Шаблон микросервисной системы асинхронной обработки B2B-лидов

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Kafka](https://img.shields.io/badge/Kafka-7.5.0-black)](https://kafka.apache.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)](https://www.postgresql.org/)

## О проекте

Данный проект представляет собой референсный шаблон для построения отказоустойчивой, масштабируемой и событийно-ориентированной системы обработки B2B-лидов.

**Ключевая идея:** декомпозиция процесса обработки лида (захват → обогащение → скоринг → распределение → уведомление) на независимые слабосвязанные микросервисы, взаимодействующие через Apache Kafka.

## Цели и задачи шаблона

- **Образовательная:** Демонстрация современных архитектурных паттернов (EDA, Saga, Outbox, CQRS) на практическом примере.
- **Практическая:** Предоставление разработчикам и малому/среднему бизнесу готовой основы для ускоренного создания промышленных B2B-систем.
- **Инженерная:** Реализация надежной обработки данных с гарантиями `at-least-once`, идемпотентностью и сквозной наблюдаемостью.

## Архитектура

Система построена на **микросервисной архитектуре** с **асинхронным событийно-ориентированным взаимодействием (EDA)**.

### Контейнерная диаграмма (C4)

<img width="2321" height="1121" alt="C4-диаграмма" src="https://github.com/user-attachments/assets/d819da81-32da-4cf4-beb4-6a62868eba6b" />

### Ключевые компоненты

| Компонент | Ответственность | База данных |
|:---|:---|:---|
| **API Gateway (YARP)** | Единая точка входа, маршрутизация запросов к Lead Service | — |
| **Lead Service** | Центральный агрегат. Управляет жизненным циклом лида (конечный автомат), идемпотентностью HTTP API | `lead_db` |
| **Enrichment Service** | Асинхронное обогащение данных из внешних источников (API эмулятор) с механизмом повторных попыток | `enrichment_db` |
| **Scoring Service** | Квалификация лида на основе динамических правил (хранятся в БД). Поддержка версионности правил | `scoring_db` |
| **Distribution Service** | Распределение лидов по менеджерам/системам на основе стратегий (RoundRobin, Territory, ScoreBased) | `distribution_db` |
| **Notification Service** | Отправка уведомлений о ключевых событиях (лог/email) | `notification_db` |

### Диаграмма состояний агрегата Lead

<img width="996" height="368" alt="stateMachine (1)" src="https://github.com/user-attachments/assets/e291b421-fdfc-4984-b3e7-a29efe573f3a" />

## Технологический стек

| Категория | Технологии |
|:---|:---|
| **Платформа** | .NET 10, ASP.NET Core |
| **Брокер сообщений** | Apache Kafka (Confluent.Kafka), Schema Registry (Avro) |
| **База данных** | PostgreSQL 16, Entity Framework Core 10 |
| **Наблюдаемость** | OpenTelemetry, .NET Aspire Dashboard |
| **Контейнеризация** | Docker, Docker Compose |
| **API Gateway** | YARP |
| **Тестирование** | NUnit, Moq, AutoFixture, NBomber |

## Старт

### Предварительные требования

- .NET 10 SDK
- Docker Desktop
- Git

### Запуск системы

1. **Клонируйте репозиторий:**

```bash
git clone https://github.com/Kostornoj-Dmitrij/lead-processing-platform.git
cd lead-processing-platform
```

2. **Запустите инфраструктуру и микросервисы:**

```bash
docker-compose up -d
```

Для остановки всех контейнеров:
```bash
docker-compose down -v
```

3. **Дождитесь запуска всех контейнеров.**
Все сервисы должны быть в статусе healthy или running (кроме init-kafka).

### Проверка работоспособности
1. **Создание нового лида (через API Gateway)**

Используйте Postman (или любой другой клиент) для отправки запроса:

| Параметр | Значение |
|:---|:---|
| **Method** | `POST` |
| **URL** | `http://localhost:8080/api/leads` |
| **Header** | `Content-Type: application/json` |
| **Header** | `Idempotency-Key: <уникальный-идентификатор>` |

**Тело запроса (JSON):**

```json
{
  "source": "web_form",
  "companyName": "Success Corp",
  "contactPerson": "John Doe",
  "email": "john@success.com",
  "phone": "+1-555-123-4567",
  "customFields": {
    "industry": "Technology",
    "companySize": "50-100"
  }
}
```

**Ответ:** `202 Accepted` с DTO созданного лида.

2. **Мониторинг через Aspire Dashboard**

Откройте в браузере: http://localhost:18888

Здесь можно увидеть трассировки, метрики и централизованные логи всех сервисов.

## Ключевые технические решения

### 1. Паттерн Transactional Outbox

Гарантирует атомарность сохранения бизнес-данных и публикации события.

**Как работает:** Событие сохраняется в таблицу `outbox_messages` в той же транзакции, что и бизнес-данные. Фоновый сервис публикует события из Outbox в Kafka с механизмом повторных попыток и Dead Letter Queue.

### 2. Идемпотентность

| Уровень | Механизм | Описание |
|:---|:---|:---|
| **HTTP (Lead Service)** | `Idempotency-Key` заголовок | Ключ и хэш запроса сохраняются в БД. При повторном запросе возвращается кэшированный ответ |
| **События (Transactional Inbox)** | Таблица `inbox_messages` | Каждый потребитель сохраняет `message_id`. Уникальный индекс предотвращает повторную обработку |
| **Dead Letter Queue (DLQ)** | Специальный топик Kafka | Сообщения, не прошедшие обработку после N попыток, перемещаются в DLQ для анализа |

### 3. Наблюдаемость (OpenTelemetry)

**Распространение контекста трассировки:**
- W3C Trace Context (`traceparent` header) автоматически пробрасывается через HTTP заголовки и Kafka
- При публикации события в Kafka добавляется заголовок `traceparent`
- При получении события контекст восстанавливается перед обработкой

## Метрики

| Сервис | Доступные метрики |
|:---|:---|
| **API Gateway** | `gateway.proxy.duration`, `gateway.requests.total` |
| **Lead Service** | `leads.created/qualified/rejected/distributed/closed.total`, `lead.processing.duration` |
| **Enrichment Service** | `enrichment.requests/success/failure/retry.total`, `enrichment.duration` |
| **Scoring Service** | `scoring.requests/success/failure.total`, `scoring.rules.evaluated` |
| **Distribution Service** | `distribution.attempts/success/failure/retry.total`, `distribution.duration`, `distribution.http.status_codes` |
| **Notification Service** | `notifications.sent/failed.total` |

## Структура проекта

```text
LeadProcessingPlatform/
├── _infrastructure/                 # Docker и скрипты инициализации
├── services/                        # Микросервисы (5 шт.)
│   ├── ApiGateway/                  # API Gateway (YARP)
│   ├── LeadService/                 # Сервис управления лидами
│   ├── EnrichmentService/           # Сервис обогащения
│   ├── ScoringService/              # Сервис скоринга
│   ├── DistributionService/         # Сервис распределения
│   └── NotificationService/         # Сервис уведомлений
├── shared/                          # Общие библиотеки
│   ├── SharedKernel/                # BaseEntity, ValueObject, интерфейсы
│   ├── SharedInfrastructure/        # Outbox, Inbox, Kafka, OpenTelemetry
│   ├── SharedHosting/               # Настройка хоста, middleware
│   └── AvroSchemas/                 # Avro-схемы для событий
└── tests/                           # Модульные и нагрузочные тесты
```

## Тестирование

### Модульные тесты

| Сервис | Количество тестов | Покрытие |
|:---|:---:|:---:|
| Lead Service | 159 | ~87% |
| Enrichment Service | 44 | ~90% |
| Scoring Service | 42 | ~94% |
| Distribution Service | 55 | ~86% |
| Notification Service | 43 | ~96% |

### Нагрузочное тестирование
