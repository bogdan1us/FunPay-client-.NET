using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Collections.Generic;
using fpapi;
using FunPay_for_.net;

namespace FunPayBot
{

    class Program
    {
        static async Task Main(string[] args)
        {
            Config config;
            string configPath = "config.json";

            if (File.Exists(configPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    config = JsonSerializer.Deserialize<Config>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении конфигурации: {ex.Message}");
                    return;
                }
            }
            else
            {
                config = new Config();

                Console.Write("Введите Голден Кей: ");
                config.GoldenKey = Console.ReadLine()?.Trim();

                Console.WriteLine("Добавьте команды. Для завершения ввода оставьте строку пустой.");
                while (true)
                {
                    Console.Write("Введите команду (например, !помощь): ");
                    string command = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(command))
                        break;

                    Console.Write("Введите ответ команды: ");
                    string response = Console.ReadLine();
                    config.Commands[command] = response;
                }

                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                    };
                    var configJson = JsonSerializer.Serialize(config, options);
                    await File.WriteAllTextAsync(configPath, configJson);
                    Console.WriteLine("Конфигурация сохранена в файле config.json");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при сохранении конфигурации: {ex.Message}");
                    return;
                }
            }

            try
            {
                var client = new FunPayClient(config.GoldenKey);
                await client.Init();
                Console.WriteLine("Клиент активирован. Просматриваем сообщения.");

                while (true)
                {
                    var message = await client.CheckChats();
                    if (message != null)
                    {
                        if (config.Commands.TryGetValue(message.Value.MsgText, out var response))
                        {
                            await client.SendMsg($"{message.Value.ChatId}", response);
                            Console.WriteLine($"Ответ на команду '{message.Value.MsgText}' отправлен в чат {message.Value.ChatId}");
                        }
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}
