using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace RedditUITests
{
    public abstract class WebTestCore
    {
        protected IWebDriver Driver;
        private const string BaseUrl = "https://www.reddit.com/";

        [OneTimeSetUp]
        public void TestSuiteSetup()
        {
            var options = new ChromeOptions();
            options.AddArgument("--disable-notifications");
            Driver = new ChromeDriver(options);
            Driver.Manage().Window.Maximize();
            Driver.Navigate().GoToUrl(BaseUrl);
        }

        [OneTimeTearDown]
        public void TestSuiteCleanup()
        {
            Driver.Quit();
        }

        protected void WaitForElement(By locator, int timeout = 15)
        {
            new WebDriverWait(Driver, TimeSpan.FromSeconds(timeout))
                .Until(d => d.FindElement(locator));
        }
    }
}