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
using System.Linq;

namespace WebScraper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // setup validation for firestore
            string pathToFirestoreKey = AppDomain.CurrentDomain.BaseDirectory + "firestoreKey.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToFirestoreKey);
            
            // get up to date on economic events, await the result even though its really nothing so that the main thread doesn't shutdown
            // while we are still gathering / storing data
            CheckScrapeTillToday().GetAwaiter().GetResult();

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
                // default start date is 1/1/2011. Set at hour 6 so when I convert to utc it stays the same day
                // Central time is -6 utc 
                mostRecentDayThatHasEvents = new DateTime(2011, 1, 1, 6, 0, 0);
            }
            else
            {
                // make sure we dont' re scrape our most recent days events
                mostRecentDayThatHasEvents = mostRecentDayThatHasEvents.Value.AddDays(1);
            }

            DateTime endOfToday = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            while (mostRecentDayThatHasEvents.Value <= endOfToday)
            {
                DateTime eventDate = mostRecentDayThatHasEvents.Value;
                List<EconomicEvent> eventsToAdd = new List<EconomicEvent>();
                EconomicCalendarWebScraper webScraper = new EconomicCalendarWebScraper();

                if (webScraper.ScrapeDate(eventDate, out eventsToAdd))
                {
                    if (eventsToAdd.Any())
                    {
                        eventDate = TimeZoneInfo.ConvertTimeToUtc(mostRecentDayThatHasEvents.Value);
                        eventDate = DateTime.SpecifyKind(mostRecentDayThatHasEvents.Value, DateTimeKind.Utc);

                        await db.AddEventsForDay(eventDate, eventsToAdd);
                        if (db.ExceededReads || db.ExceededWrites)
                        {
                            Console.WriteLine("Reached limit at: ", eventDate.ToString());
                            break;
                        }
                    }

                    // only increment the day if the driver didn't fail. Else we will set a differnt driver and try again
                    mostRecentDayThatHasEvents = mostRecentDayThatHasEvents.Value.AddDays(1);
                }
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
