using Drision.Framework.Common;
using ServerResourceMonitor;
using System;
using Drision.Framework.Entity.HighTechZone;
using System.Linq;
using CommonBll.Models;
using LinkService;

namespace SimpleCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServerResource.Start();
                WeatherService.Start();
                InformationService.Start();
                LinkServiceValidate.Start();
                Console.WriteLine("完成");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }
    }
}


