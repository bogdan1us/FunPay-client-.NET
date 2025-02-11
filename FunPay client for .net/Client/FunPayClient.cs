using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace fpapi
{
    /// <summary>
    /// Клиент для работы с FunPay API, позволяющий отправлять сообщения и мониторить чат.
    /// </summary>
    class FunPayClient
    {

        private readonly HttpClient _http;
        private readonly string _key;
        private string _token;
        private readonly Dictionary<string, string> _chatNames = new(); // Словарь: ChatId -> Username
        private string _lastMessageId = "";

        private const string ApiUrl = "https://funpay.com";
        private const string USERNAME_TO_IGNORE = "Brissal";



        /// <summary>
        /// Инициализирует новый экземпляр <see cref="FunPayClient"/> с заданным ключом.
        /// </summary>
        /// <param name="key">Golden Key для аутентификации.</param>
        public FunPayClient(string key)
        {
            _key = key;
            _http = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                         System.Net.DecompressionMethods.Deflate |
                                         System.Net.DecompressionMethods.Brotli
            });
            SetupClient();
        }

        /// <summary>
        /// Настраивает заголовки HTTP‑клиента.
        /// </summary>
        private void SetupClient()
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
            _http.DefaultRequestHeaders.Add("Cookie", $"golden_key={_key};");
            _http.DefaultRequestHeaders.ExpectContinue = false;
        }



        /// <summary>
        /// Иницилизирует аккаунт FunPay.
        /// </summary>
        /// <returns>CSRF‑token.</returns>
        /// <exception cref="Exception">Выбрасывается, если не удалось найти необходимые данные.</exception>
        public async Task<string> Init()
        {
            var res = await _http.GetStringAsync(ApiUrl);
            var tokenStart = res.IndexOf("data-app-data=\"", StringComparison.Ordinal);
            if (tokenStart == -1)
                throw new Exception("Ошибка: не найден data-app-data");

            tokenStart += "data-app-data=\"".Length;
            var tokenEnd = res.IndexOf("\"", tokenStart, StringComparison.Ordinal);
            if (tokenEnd == -1)
                throw new Exception("Ошибка входа: data-app-data некорректен");

            var jsonData = res.Substring(tokenStart, tokenEnd - tokenStart);
            var decodedJson = System.Web.HttpUtility.HtmlDecode(jsonData);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(decodedJson);

            if (!jsonDoc.RootElement.TryGetProperty("csrf-token", out var tokenEl))
                throw new Exception("Ошибка: csrf-token не найден");

            _token = tokenEl.GetString();
            return _token;
        }

        /// <summary>
        /// Отправляет сообщение в указанный чат.
        /// </summary>
        /// <param name="chatId">Идентификатор чата.</param>
        /// <param name="message">Сообщение для отправки.</param>
        /// <returns>Ответ сервера.</returns>
        /// <exception cref="InvalidOperationException">Если CSRF‑token не инициализирован.</exception>
        public async Task<string> SendMsg(string chatId, string message)
        {
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException("Ошибка: csrf-token не инициализирован");

            var payload = new StringContent(
                $"request={{\"action\":\"chat_message\",\"data\":{{\"node\":\"{chatId}\",\"content\":\"{message}\"}}}}&csrf_token={_token}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            var response = await _http.PostAsync(ApiUrl + "/runner/", payload);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Проверяет наличие новых сообщений в чате и возвращает данные о последнем сообщении.
        /// Если имя автора отсутствует, оно берётся из ранее сохранённого словаря.
        /// </summary>
        /// <returns>
        /// Кортеж с именем автора, текстом сообщения и идентификатором чата, если найдено новое сообщение; 
        /// иначе <c>null</c>.
        /// </returns>
        public async Task<(string User, string MsgText, string ChatId)?> CheckChats()
        {
            try
            {
                HtmlDocument chatListDoc = await GetHtmlDocumentAsync(ApiUrl + "/chat/");
                string chatId = GetFirstChatId(chatListDoc);
                if (string.IsNullOrEmpty(chatId))
                {
                    return null;
                }

                string storedAuthor = _chatNames.ContainsKey(chatId) ? _chatNames[chatId] : "";

                HtmlDocument chatDetailsDoc = await GetHtmlDocumentAsync($"{ApiUrl}/chat/?node={chatId}");
                HtmlNode latestMessageNode = GetLatestMessageNode(chatDetailsDoc);
                if (latestMessageNode == null)
                {
                    return null;
                }

                (string messageId, string author, string messageText) = ParseMessageNode(latestMessageNode);

                if (messageId == _lastMessageId)
                {
                    return null;
                }
                _lastMessageId = messageId;

                if (!string.IsNullOrEmpty(author))
                {
                    _chatNames[chatId] = author;
                }
                else if (!string.IsNullOrEmpty(storedAuthor))
                {
                    author = storedAuthor;
                }

                if (string.IsNullOrEmpty(author))
                {
                    return null;
                }

                if (author == USERNAME_TO_IGNORE)
                {
                    return null;
                }

                return (author, messageText, chatId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при мониторинге чата: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Возвращает имя чата (имя пользователя) по его идентификатору.
        /// </summary>
        /// <param name="chatId">Идентификатор чата.</param>
        /// <returns>Имя пользователя, если оно найдено; иначе "Unknown".</returns>
        public string GetChatName(string chatId)
        {
            return _chatNames.TryGetValue(chatId, out var name) ? name : "Unknown";
        }


        /// <summary>
        /// Загружает HTML-документ по указанному URL.
        /// </summary>
        /// <param name="url">URL для загрузки HTML.</param>
        /// <returns>Экземпляр <see cref="HtmlDocument"/> с загруженным HTML.</returns>
        private async Task<HtmlDocument> GetHtmlDocumentAsync(string url)
        {
            string html = await _http.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

        /// <summary>
        /// Извлекает идентификатор первого чата из HTML-документа списка чатов.
        /// </summary>
        /// <param name="doc">HTML-документ со списком чатов.</param>
        /// <returns>Идентификатор чата в виде строки, или пустую строку если не найден.</returns>
        private string GetFirstChatId(HtmlDocument doc)
        {
            HtmlNode list = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'contact-list custom-scroll')]");
            if (list != null)
            {
                HtmlNode firstItem = list.SelectSingleNode(".//a[1]");
                if (firstItem != null)
                {
                    return firstItem.GetAttributeValue("data-id", "");
                }
            }
            return "";
        }

        /// <summary>
        /// Извлекает узел, содержащий последнее сообщение, из HTML-документа чата.
        /// </summary>
        /// <param name="doc">HTML-документ чата.</param>
        /// <returns>Узел последнего сообщения, или <c>null</c> если не найден.</returns>
        private HtmlNode GetLatestMessageNode(HtmlDocument doc)
        {
            return doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'chat-message-list')]//div[contains(@class, 'chat-msg-item')][last()]");
        }

        /// <summary>
        /// Разбирает узел сообщения и извлекает его идентификатор, имя автора и текст.
        /// </summary>
        /// <param name="messageNode">Узел HTML, представляющий сообщение.</param>
        /// <returns>
        /// Кортеж, содержащий: <c>messageId</c>, <c>author</c> и <c>messageText</c>.
        /// </returns>
        private (string messageId, string author, string messageText) ParseMessageNode(HtmlNode messageNode)
        {
            string messageId = messageNode.GetAttributeValue("id", "");
            HtmlNode authorNode = messageNode.SelectSingleNode(".//div[@class='media-user-name']//a[@class='chat-msg-author-link']");
            string author = authorNode?.InnerText.Trim() ?? "";
            HtmlNode textNode = messageNode.SelectSingleNode(".//div[@class='chat-msg-body']//div[@class='chat-msg-text']");
            string messageText = textNode?.InnerText.Trim() ?? "";
            return (messageId, author, messageText);
        }

    }
}
