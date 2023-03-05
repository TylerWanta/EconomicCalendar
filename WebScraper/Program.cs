using Topshelf;
using log4net.Config;
using Autofac;
using Autofac.Extras.Quartz;
using System.Collections.Specialized;
using System.Configuration;
using WebScraper.Scraping;
using System.Collections.Generic;
using System;
using WebScraper.Firestore;
using System.Web.SessionState;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace WebScraper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // setup validation for firestore
            string pathToFirestoreKey = AppDomain.CurrentDomain.BaseDirectory + "firestoreKey.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToFirestoreKey);

            // get up to date on economic events 
            CheckScrapeTillToday();

            // setup daily scraping service
            // SetupScrapingService();
        }

        static void CheckScrapeTillToday()
        {
            EconomicEventsDB db = new EconomicEventsDB();
            DateTime? mostRecentDayThatHasEvents = db.MostRecentDayThatHasEvents().Result;

            if (mostRecentDayThatHasEvents == null)
            {
                // default start date is 1/1/2011
                mostRecentDayThatHasEvents = new DateTime(2011, 1, 1);
            }

            int count = 0;
            while (mostRecentDayThatHasEvents.Value <= DateTime.Now)
            {
                // sleep every few requests so that we don't overload their server
                if (count >= 50)
                {
                    System.Threading.Thread.Sleep(3000);
                    count = 0;
                }

                DateTime eventDate = mostRecentDayThatHasEvents.Value;
                List<EconomicEvent> eventsToAdd = EconomicCalendarWebScraper.ScrapeDate(eventDate);

                db.AddEventsForDay(eventDate, eventsToAdd);
                if (db.ExceededReads || db.ExceededWrites)
                {
                    Console.WriteLine("Rached limit at: ", eventDate.ToString());
                    return;
                }

                count += 1;
                mostRecentDayThatHasEvents.Value.AddDays(1);
            }
        }

        static void SetupScrapingService()
        {
            XmlConfigurator.Configure();
            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<DailyWebScraperService>().AsSelf().InstancePerLifetimeScope();
            containerBuilder.RegisterModule(new QuartzAutofacFactoryModule { ConfigurationProvider = context => (NameValueCollection)ConfigurationManager.GetSection("quartz") });
            containerBuilder.RegisterModule(new QuartzAutofacJobsModule());

            IContainer container = containerBuilder.Build();
            HostFactory.Run(hostConfigurator =>
            {
                hostConfigurator.SetServiceName("Daily Economic Calendar Scraper");
                hostConfigurator.SetDisplayName("Economic Calendar Web Scraper");
                hostConfigurator.SetDescription("Scrapes Economic Calendar data once per day");

                hostConfigurator.RunAsLocalSystem();
                hostConfigurator.UseLog4Net();

                hostConfigurator.Service<DailyWebScraperService>(serviceConfigurator =>
                {
                    serviceConfigurator.ConstructUsing(hostSettings => container.Resolve<DailyWebScraperService>());

                    serviceConfigurator.WhenStarted(service => service.OnStart());
                    serviceConfigurator.WhenStopped(service => service.OnStop());
                });
            });
        }
    }
}
