# FunPayBot

## Описание проекта
FunPayBot — это бот для работы с платформой FunPay, который позволяет автоматизировать взаимодействие с пользователями, отправлять ответы на команды и отслеживать новые сообщения в чате.

## Основные функции
- **Подключение к FunPay** с использованием Golden Key.
- **Обработка пользовательских команд** и отправка заданных ответов.
- **Мониторинг чатов** для обнаружения новых сообщений.

## Установка и настройка
### Шаг 1: Клонирование репозитория
Склонируйте проект на локальный компьютер:
```bash
git clone https://github.com/bogdan1us/FunPay-client-.NET.git
```

### Шаг 2: Компиляция проекта
Убедитесь, что у вас установлен .NET SDK 8. Скомпилируйте проект с помощью команды:
```bash
dotnet build
```

### Шаг 3: Настройка конфигурации
При первом запуске программа запросит у вас Golden Key и команды для обработки. Эти данные будут сохранены в файл `config.json`:
- Введите Golden Key (ключ для авторизации на FunPay).
- Добавьте пользовательские команды в формате:
  - **Команда**: текст команды, например, `!помощь`.
  - **Ответ**: текст ответа, который бот будет отправлять на эту команду.

Пример файла `config.json`:
```json
{
  "GoldenKey": "ваш-golden-key",
  "Commands": {
    "!помощь": "Привет! Чем я могу помочь?",
    "!цена": "Цена: 500 рублей."
  }
}
```

### Шаг 4: Запуск программы
Запустите бота командой:
```bash
dotnet run
```

## Использование
1. После запуска бот подключается к вашему аккаунту FunPay с использованием указанного Golden Key.
2. Бот проверяет новые сообщения в чате каждые 1 секунду.
3. Если получено сообщение, соответствующее одной из заданных команд, бот отправляет ответ в этот чат.

## Зависимости
- **.NET 6.0 и выше**
- **HtmlAgilityPack** для работы с HTML-страницами.
- **System.Text.Json** для работы с конфигурацией.
- **HttpClient** для взаимодействия с FunPay API.

## Возможные ошибки
- **Ошибка при чтении конфигурации**: Проверьте формат файла `config.json`.
- **Ошибка аутентификации**: Убедитесь, что ваш Golden Key действителен.
- **Ошибка при отправке сообщения**: Возможно, истёк ваш CSRF-токен.

## Разработчик
Этот проект создан для автоматизации работы с FunPay.

Если у вас есть предложения или вопросы, пишите в разделе Issues на GitHub или свяжитесь со мной в Telegram @bogdanius

Так же можете вступить в мой телеграм чат , там мы будем с вами общаться)

вот ссылка -> https://t.me/+RzzeDaJ5V8VlNDQy

## Лицензия
Этот проект распространяется под лицензией MIT. Подробнее в файле [LICENSE](LICENSE).

