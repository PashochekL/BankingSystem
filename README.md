# BankingSystem

Backend pet-проект банковской системы на ASP.NET Core и .NET 8. Проект состоит из отдельных Web API сервисов для пользователей, счетов и кредитов, использует PostgreSQL для хранения данных, JWT для authentication, Refresh Token для продления сессий и Hangfire для фонового начисления процентов.

## Возможности

- Создание пользователей сотрудниками.
- Login по телефону и паролю.
- JWT access token и Refresh Token.
- Refresh Token rotation и revoke при logout.
- Роли `Client` и `Employee`.
- Блокировка пользователей.
- Доступ клиента только к собственному профилю.
- Создание, просмотр и закрытие счетов.
- Deposit и withdraw по счетам.
- История операций по счету.
- Проверка владельца счета для клиентских операций.
- Защита операций с балансом через optimistic concurrency.
- Создание, изменение и просмотр кредитных тарифов.
- Создание кредитов клиентами по активным тарифам.
- Просмотр кредитов и история кредитных операций.
- Repayment по кредитам.
- Фоновое начисление процентов по активным кредитам.
- Защита начисления процентов от повторной обработки одной даты.
- Validation входных данных на уровне service logic.
- Global exception handling для доменных ошибок.
- Structured logging через `ILogger`.
- Health checks для сервисов и подключений к базам.
- Docker Compose для локального запуска backend.
- Unit tests для основной бизнес-логики сервисов.

## Сервисы

| Сервис | Локальный порт | Назначение |
| --- | --- | --- |
| `UsersService` | `5257` | Пользователи, login, JWT, Refresh Token, роли и блокировка |
| `AccountsService` | `5066` | Счета, баланс, операции и история операций |
| `CreditsService` | `5239` | Кредитные тарифы, кредиты, repayment и начисление процентов |

Каждый сервис имеет собственный `DbContext`, свои EF Core migrations и отдельную PostgreSQL базу.

### UsersService

`UsersService` отвечает за пользователей и authentication.

Основные endpoint:

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/users`
- `GET /api/users/me`
- `GET /api/users/{id}`
- `GET /api/users`
- `PATCH /api/users/{id}/block`

`Employee` может создавать пользователей, получать список пользователей, смотреть любого пользователя и блокировать других пользователей. `Client` может получить только собственный профиль. Заблокированный пользователь не может выполнить login или refresh сессии.

### AccountsService

`AccountsService` отвечает за банковские счета и операции с балансом.

Основные endpoint:

- `POST /api/accounts`
- `GET /api/accounts`
- `GET /api/accounts/{id}`
- `POST /api/accounts/{id}/close`
- `POST /api/accounts/{id}/deposit`
- `POST /api/accounts/{id}/withdraw`
- `GET /api/accounts/{id}/operations`

`Client` работает только со своими счетами. `Employee` может смотреть счета разных пользователей. Счет нельзя закрыть, если `Balance` не равен нулю. Закрытый счет не принимает операции deposit и withdraw.

### CreditsService

`CreditsService` отвечает за кредитные тарифы, кредиты и начисление процентов.

Основные endpoint:

- `POST /api/credit-tariffs`
- `GET /api/credit-tariffs`
- `GET /api/credit-tariffs/{id}`
- `PATCH /api/credit-tariffs/{id}`
- `POST /api/credits`
- `GET /api/credits`
- `GET /api/credits/{id}`
- `POST /api/credits/{id}/repay`
- `GET /api/credits/{id}/operations`

`Employee` управляет `CreditTariff`: создает тарифы, меняет ставку, название и активность. `Client` может создать `Credit` только по активному тарифу. Repayment уменьшает `RemainingAmount`, а при полном погашении кредит получает статус `Paid`.

## Authentication и Authorization

Authentication реализована в `UsersService`.

При успешном login сервис возвращает:

- `accessToken`;
- `refreshToken`;
- данные пользователя.

Refresh Token хранится в базе как hash. При refresh старый Refresh Token отзывается, после чего создается новый Refresh Token. Logout отзывает переданный Refresh Token.

Authorization использует стандартный ASP.NET Core role-based authorization и проверки на уровне services:

- `Client` получает доступ только к своим данным;
- `Employee` получает доступ к управленческим операциям;
- создание Credit через обычный endpoint разрешено только `Client`;
- `Employee` не может заблокировать самого себя.

## Операции со счетами

Счет создается с нулевым балансом. Для денежных операций используется `decimal`.

Deposit:

- требует положительную сумму;
- запрещен для закрытого счета;
- создает запись в истории операций.

Withdraw:

- требует положительную сумму;
- запрещен для закрытого счета;
- проверяет достаточность `Balance`;
- создает запись в истории операций.

Close:

- проверяет существование счета;
- проверяет доступ текущего пользователя;
- запрещает закрытие счета с ненулевым `Balance`;
- устанавливает `IsClosed` и `ClosedAt`.

История операций доступна через `GET /api/accounts/{id}/operations` с pagination.

## Кредитование

Кредитные тарифы описывают название, процентную ставку и активность. Неактивный тариф нельзя использовать для создания нового кредита.

Credit создается с:

- `InitialAmount`;
- `RemainingAmount`;
- `InterestRate`, скопированным из выбранного тарифа;
- `CreatedAt`;
- `LastInterestAccrualAt`;
- статусом `Active`.

Repayment уменьшает `RemainingAmount`. Если долг погашен полностью, статус меняется на `Paid`. Все основные изменения по кредиту записываются в `CreditOperation`.

## Фоновые задачи

`CreditsService` использует Hangfire и отдельную PostgreSQL базу `hangfire-db`.

Ежедневная recurring job начисляет проценты по активным кредитам. Проценты рассчитываются по количеству дней с последнего начисления. После обработки обновляется `LastInterestAccrualAt`, поэтому одна и та же дата не обрабатывается повторно.

## Конкурентность и надёжность

В проекте реализованы:

- optimistic concurrency для обновлений счетов и кредитов;
- защита операций с `Balance` от конфликтующих записей;
- идемпотентность начисления процентов через `LastInterestAccrualAt`;
- validation входных DTO на уровне services;
- global exception handling middleware;
- structured logging через `ILogger`;
- health checks для backend services;
- поддержка `CancellationToken` в controllers, services, repositories и EF Core calls.

## Технологии

- `.NET 8`
- `ASP.NET Core Web API`
- `Entity Framework Core`
- `PostgreSQL`
- `Npgsql`
- `JWT Bearer authentication`
- `ASP.NET Core Identity PasswordHasher`
- `Hangfire`
- `Docker Compose`
- `xUnit`
- `Moq`

## Запуск

Для локального запуска backend через Docker:

```powershell
docker compose up --build
```

Docker Compose поднимает:

- `users-service`
- `users-db`
- `accounts-service`
- `accounts-db`
- `credits-service`
- `credits-db`
- `hangfire-db`

Адреса сервисов:

- `UsersService`: `http://localhost:5257`
- `AccountsService`: `http://localhost:5066`
- `CreditsService`: `http://localhost:5239`

Health checks:

- `http://localhost:5257/health`
- `http://localhost:5066/health`
- `http://localhost:5239/health`

Локальные параметры подключения к базам и JWT secret заданы в `docker-compose.yml` как development configuration.

## API

Swagger включен для окружения `Development`:

- `http://localhost:5257/swagger`
- `http://localhost:5066/swagger`
- `http://localhost:5239/swagger`

## Тесты

Запуск всех tests:

```powershell
dotnet test KrutoBank.sln
```

Запуск tests по отдельным сервисам:

```powershell
dotnet test tests\UsersService.Tests\UsersService.Tests.csproj
dotnet test tests\AccountsService.Tests\AccountsService.Tests.csproj
dotnet test tests\CreditsService.Tests\CreditsService.Tests.csproj
```

В solution есть unit tests для:

- `UsersService`: authentication, Refresh Token flow и user management;
- `AccountsService`: создание счетов, deposit, withdraw, close, ownership checks и concurrency scenarios;
- `CreditsService`: credit tariffs, credit creation, repayment, access checks и interest accrual job.
