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
using OpenQA.Selenium.DevTools.V108.Browser;
using System.Threading.Tasks;

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
            CheckScrapeTillToday().GetAwaiter().GetResult();
            Console.WriteLine("Done");
            // setup daily scraping service
            // SetupScrapingService();
        }

        // Needs to return a task so we can await it and the main program doesn't start shutting down before we finish saving records
        async static Task<bool> CheckScrapeTillToday()
        {
            EconomicEventsDB db = new EconomicEventsDB();
            DateTime? mostRecentDayThatHasEvents = db.MostRecentDayThatHasEvents().Result;

            if (mostRecentDayThatHasEvents == null)
            {
                // default start date is 1/1/2011
                mostRecentDayThatHasEvents = new DateTime(2023, 3, 5);
                mostRecentDayThatHasEvents = TimeZoneInfo.ConvertTimeToUtc(mostRecentDayThatHasEvents.Value);
                mostRecentDayThatHasEvents = DateTime.SpecifyKind(mostRecentDayThatHasEvents.Value, DateTimeKind.Utc);
            }

            while (mostRecentDayThatHasEvents.Value <= DateTime.Now)
            {
                DateTime eventDate = mostRecentDayThatHasEvents.Value;
                List<EconomicEvent> eventsToAdd = EconomicCalendarWebScraper.ScrapeDate(eventDate);

                await db.AddEventsForDay(eventDate, eventsToAdd);
                if (db.ExceededReads || db.ExceededWrites)
                {
                    Console.WriteLine("Rached limit at: ", eventDate.ToString());
                    break;
                }
                
                mostRecentDayThatHasEvents = mostRecentDayThatHasEvents.Value.AddDays(1);
            }

            return true;
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
