using Newtonsoft.Json;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace RedditUITests
{
    public class RedditFunctionalTests : WebTestCore
    {
        [Test]
        [Order(1)]
        public void PlatformTitleValidation()
        {
            Assert.That(Driver.Title,
                Is.EqualTo("Reddit - The heart of the internet"),
                "Некорректный заголовок страницы");
        }

        [Test]
        [Order(2)]
        public void PrimaryInterfaceComponentsCheck()
        {
            Assert.Multiple(() =>
            {
                VerifyElementPresence(By.CssSelector("body > shreddit-app > reddit-header-large"),
                    "Основной хедер");

                VerifyElementState(By.Name("q"),
                    "Поисковая строка",
                    expectedEnabled: true);

                VerifyElementText(By.XPath("//a[contains(@href, '/login')]"),
                    "Log In",
                    "Кнопка авторизации");
            });
        }

        [Test]
        [Order(3)]
        public void VerifyPostsVisibility()
        {
            try
            {
                // 1. Ожидаем загрузки контейнера с постами
                new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                    .Until(d => d.FindElement(By.CssSelector("shreddit-feed")));

                // 2. Получаем все посты через кастомный тег
                var posts = new WebDriverWait(Driver, TimeSpan.FromSeconds(10))
                    .Until(d => d.FindElements(By.CssSelector("shreddit-post")));

                // 3. Проверяем базовые характеристики
                Assert.Multiple(() =>
                {
                    Assert.That(posts.Count, Is.GreaterThan(3), "Менее 4 постов на странице");
                    Assert.That(posts[0].Displayed, "Первый пост не отображается");
                    Assert.That(posts[0].GetAttribute("post-title"), Is.Not.Empty, "Заголовок отсутствует");
                });

                // 4. Проверка структуры поста
                var firstPost = posts[0];
                Assert.That(firstPost.FindElement(By.CssSelector("[slot='full-post-link']")).Displayed,
                    "Ссылка на пост не найдена");
            }
            catch (Exception ex)
            {
                TakeScreenshot("posts_error");
                throw new Exception($"Ошибка проверки постов: {ex.Message}");
            }
        }

        // Вспомогательные методы
        private void DismissNewUserPrompts()
        {
            try
            {
                // Закрытие новых промптов 2024
                var overlays = Driver.FindElements(By.CssSelector("div[data-testid='onboarding']"));
                foreach (var overlay in overlays)
                {
                    if (overlay.Displayed)
                    {
                        overlay.FindElement(By.CssSelector("button[aria-label='Close']")).Click();
                        Thread.Sleep(300);
                    }
                }
            }
            catch { /* Игнорируем если элементы отсутствуют */ }
        }

        // Вспомогательные методы
        private void CloseCookieBanner()
        {
            try
            {
                var cookieBanner = Driver.FindElement(
                    By.CssSelector("button[data-testid='cookie-banner-accept']"));
                if (cookieBanner.Displayed) cookieBanner.Click();
            }
            catch { /* Баннер отсутствует */ }
        }

        private void TakeScreenshot(string name)
        {
            var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
            screenshot.SaveAsFile($"{DateTime.Now:yyyyMMdd_HHmmss}_{name}.png"); // Убрали параметр формата
        }

        [Test]
        [Order(4)]
        public void LoginFormValidation()
        {
            try
            {
                // 1. Явный переход с принудительной загрузкой
                Driver.Navigate().GoToUrl("https://www.reddit.com/login/");
                new WebDriverWait(Driver, TimeSpan.FromSeconds(20))
                    .Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                // 2. Обработка возможной CAPTCHA
                if (IsElementPresent(By.CssSelector("div[data-testid='captcha-container']")))
                {
                    throw new Exception("CAPTCHA блокирует тест");
                }

                // 3. Новые селекторы 2024
                var usernameField = new WebDriverWait(Driver, TimeSpan.FromSeconds(20))
                    .Until(d =>
                    {
                        var element = d.FindElement(By.CssSelector("#login-username")); // #login-username
                        return element.Displayed && element.Enabled ? element : null;
                    });

                // 4. Улучшенная проверка парольного поля
                var passwordField = Driver.FindElement(By.CssSelector("#login-password"));
                var submitButton = Driver.FindElement(By.XPath("//button[.//span[text()='Log In']]"));
            }
            catch (Exception ex)
            {
                TakeScreenshot("login_error");
                LogPageStructure();
                throw new Exception($"Ошибка: {ex.Message}\nUser-Agent: {GetUserAgent()}");
            }
        }

        private void LogPageStructure()
        {
            try
            {
                var body = Driver.FindElement(By.TagName("body"));
                File.WriteAllText($"page_structure_{DateTime.Now:HHmmss}.txt",
                    $"URL: {Driver.Url}\n" +
                    $"Title: {Driver.Title}\n" +
                    $"Body content:\n{body.Text}\n" +
                    $"HTML:\n{Driver.PageSource}");
            }
            catch { /* Игнорируем ошибки логирования */ }
        }

        private string GetUserAgent()
        {
            return (string)((IJavaScriptExecutor)Driver)
                .ExecuteScript("return navigator.userAgent;");
        }

        protected bool IsElementPresent(By locator)
        {
            try
            {
                Driver.FindElement(locator);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки элемента: {ex.Message}");
                return false;
            }
        }

        private void VerifyElementPresence(By locator, string elementName)
        {
            var element = Driver.FindElement(locator);
            Assert.That(element.Displayed,
                $"{elementName} не отображается");
        }

        private void VerifyElementState(By locator, string elementName, bool expectedEnabled)
        {
            var element = Driver.FindElement(locator);
            Assert.That(element.Enabled == expectedEnabled,
                $"{elementName} имеет неверное состояние активности");
        }

        private void VerifyElementText(By locator, string expectedText, string elementName)
        {
            var element = Driver.FindElement(locator);
            Assert.That(element.Text.Trim(),
                Is.EqualTo(expectedText),
                $"{elementName} содержит некорректный текст");
        }
    }
}