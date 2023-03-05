using Topshelf;
using log4net.Config;
using Autofac;
using Autofac.Extras.Quartz;
using System.Collections.Specialized;
using System.Configuration;
using WebScraper.Scraping;
using System.Collections.Generic;
using System;

namespace WebScraper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CheckScrapeTillToday();

            XmlConfigurator.Configure();
            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<DailyWebScraperService>().AsSelf().InstancePerLifetimeScope();
            containerBuilder.RegisterModule(new QuartzAutofacFactoryModule{ ConfigurationProvider = context => (NameValueCollection)ConfigurationManager.GetSection("quartz") });
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

        static void CheckScrapeTillToday()
        {
            // going to have to ping firebase and see what our most recent record is and scrape from that day on 
            List<EconomicEvent> eventsToAdd = EconomicCalendarWebScraper.ScrapeFromDate(DateTime.Now);

            // add previous days events to firebase
        }
    }
}
