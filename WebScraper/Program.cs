﻿using Topshelf;
using log4net.Config;
using Autofac;
using Autofac.Extras.Quartz;
using System.Collections.Specialized;
using System.Configuration;
using WebScraper.Scraping;
using System.Collections.Generic;
using System;
using WebScraper.Firestore;
using System.Threading.Tasks;
using System.Linq;
using WebScraper.Types;
using WebScraper.Database;

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
            //if (args.Contains("catchup"))
            //{
            //    CheckScrapeTillToday().GetAwaiter().GetResult();
            //}

            //setup daily scraping service
            //if (args.Contains("service"))
            //{
            //    SetupScrapingService();
            //}

            MoveEventsFromFirestoreToExcelDB().GetAwaiter().GetResult();
        }

        // Needs to return a task so we can await it and the main program doesn't start shutting down before we finish saving records
        async static Task<bool> CheckScrapeTillToday()
        {
            EconomicEventsDB db = new EconomicEventsDB();
            DateTime? currentScrapingDate = db.MostRecentDayThatHasEvents().Result;

            if (currentScrapingDate == null)
            {
                currentScrapingDate = new DateTime(2011, 1, 1, 0, 0, 0);
            }
            else
            {
                // remove hour/minute/seonds from this date as this is the date that gets used for events marked as 'All Day'
                // I want those events to always have a time of midnight for whatever day they are on
                currentScrapingDate = new DateTime(currentScrapingDate.Value.Year, currentScrapingDate.Value.Month, currentScrapingDate.Value.Day,
                    0, 0, 0);
            }

            DateTime endOfToday = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            EconomicCalendarWebScraper webScraper = new EconomicCalendarWebScraper();

            while (currentScrapingDate.Value <= endOfToday)
            {
                List<EconomicEvent> eventsToAdd = new List<EconomicEvent>();

                // keep trying if we fail. There is internal tracking to throw an exception so that this doesn't become an infinite loop
                while (!webScraper.ScrapeDate(currentScrapingDate.Value, out eventsToAdd))
                {
                    continue;
                }

                if (eventsToAdd.Any())
                {
                    await db.AddEvents(eventsToAdd);
                    if (db.ExceededReads || db.ExceededWrites)
                    {
                        Console.WriteLine("Reached limit at: ", currentScrapingDate.ToString());
                        return true;
                    }
                }

                // only increment the day if the driver didn't fail. Else we will set a differnt driver and try again
                currentScrapingDate = currentScrapingDate.Value.AddDays(1);
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

        static async Task<bool> MoveEventsFromFirestoreToExcelDB()
        {
            DateTime startTime = new DateTime(2011, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime endTime = new DateTime(2023, 3, 12, 0, 0, 0, DateTimeKind.Utc);

            EconomicEventsDB firestoreDB = new EconomicEventsDB();
            EconomicCalendarDB excelDB = new EconomicCalendarDB();

            DateTime dateLow = startTime;
            DateTime dateHigh = startTime.AddDays(1);

            while (dateLow < endTime)
            {
                List<EconomicEvent> events = await firestoreDB.GetEventsBetween(dateLow, dateHigh);
                if (events.Any())
                {
                    excelDB.Add(dateLow, events);
                }

                dateLow = dateLow.AddDays(1);
                dateHigh = dateHigh.AddDays(1);
            }

            return true;
        }
    }
}
