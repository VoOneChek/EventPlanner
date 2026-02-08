# Event Planner

Веб-приложение для планирования событий с фиксированным или гибким временем и возможностью получения оптимального времени встречи на основе доступности участников.

Проект разработан с использованием ASP.NET Core MVC и Entity Framework Core.

---

## Возможности

- Регистрация и авторизация пользователей
- Подтверждение email через код
- Создание событий:
  - с фиксированной датой и временем
  - с гибкими интервалами времени
- Присоединение участников к событию
- Указание доступных временных интервалов
- Автоматический расчёт оптимального времени события
- Согласие или отказ от фиксированной даты (участником)
- Просмотр:
  - активных событий
  - завершённых событий
- Удаление завершённых событий
- Управление профилем пользователя (смена имени, пароля, email с подтверждением)

---

## Архитектура проекта

Проект реализован по паттерну **MVC (Model-View-Controller)**:

### Models
- `User` — пользователь системы
- `Event` — событие
- `Participant` — участник события
- `AvailabilityInterval` — интервалы доступности
- `EventStatus` — статус события (Created, Calculated, Closed)

### Controllers
- `HomeController` — главное меню
- `AccountController` — регистрация, вход, профиль
- `EventController` — создание, участие, расчёт, завершение событий

### Views
- Представления для:
  - Главной страницы (Index)
  - Меню авторизованного пользователя (Menu)
  - Создания события (Create)
  - Ввод кода события (Join)
  - Участия в событии (JoinEvent)
  - Просмотра результатов (Result)
  - Списка событий авторизованного пользователя (MyEvents)
  - Завершённых событий (ClosedEvents)
  - Подтверждения кода (ConfirmCode)

---

## Конфигурация

Проект использует переменные окружения для хранения конфиденциальных данных.

Перед запуском необходимо указать следующие переменные среды:

| Переменная | Описание | Пример |
|-----------|----------|----------|
| `EVENT_DB_CONNECTION` | Строка подключения к PostgreSQL | Host=localhost;Port=5432;Database=EventPlannerDb;Username=postgres;Password=postgres |
| `SmtpClientHost` | SMTP сервер | smtp.gmail.com |
| `SmtpClientPort` | Порт SMTP | 587 |
| `MailLogin` | Почта отправителя | example@gmail.com |
| `MailPassword` | Пароль от почты | ptck rksv kxdk wlyq |

Пример комманд для PowerShell:
```powershell
setx EVENT_DB_CONNECTION "Host=localhost;Port=5432;Database=EventPlannerDb;Username=postgres;Password=postgres"
```
```powershell
setx SmtpClientHost "smtp.gmail.com"
```
```powershell
setx SmtpClientPort "587"
```
```powershell
setx MailLogin "example@gmail.com"
```
```powershell
setx MailPassword "ptck rksv kxdk wlyq"
```

---

##  База данных

Используется **Entity Framework Core** и PostgreSQL (LocalDB).

Основные таблицы:
- Users
- Events
- Participants
- AvailabilityIntervals

Связи:
- Один пользователь → много событий
- Одно событие → много участников
- Один участник → много интервалов

---

## Установка и запуск

### NuGet пакеты

- Microsoft.EntityFrameworkCore (8.0.23)
- Microsoft.EntityFrameworkCore.Tools (8.0.23)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.11)
- BCrypt.Net-Next (4.0.3) — хэширование паролей

### Требования:
- .NET 8 SDK
- PostgreSQL
- Visual Studio / Rider

### Шаги запуска

1. Клонировать репозиторий:
```bash
git clone https://github.com/your-username/EventPlannerWebApplication.git
```
2. Установить переменные окружения (см. раздел Конфигурация)
3. Применить миграции (в консоли диспетчера пакетов):
```
Update-Database
```
4. Запустить проект
