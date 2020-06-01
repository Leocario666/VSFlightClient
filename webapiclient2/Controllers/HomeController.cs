﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using webapiclient2.Factory;
using webapiclient2.Models;
using webapiclient2.Utility;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace webapiclient2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IOptions<MySettingsModel> appSettings;

        public HomeController(ILogger<HomeController> logger, IOptions<MySettingsModel> app)
        {
            appSettings = app;
            ApplicationSettings.WebApiUrl = appSettings.Value.WebApiBaseUrl;
            _logger = logger;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var data = await ApiClientFactory.Instance.GetFlights();

            var allFlights = from avion in data
                    where avion.Date > DateTime.Now && avion.RemainingSeats != 0
                    select avion;
            return View(allFlights);
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Login(Passengers p)
        {
            var data = await ApiClientFactory.Instance.GetPassengers();

            var GetID = from passenger in data
                    where passenger.Surname.Equals(p.Surname)
                    select passenger.PersonId;

            var PassengerID = GetID.ToArray();
            HttpContext.Session.SetInt32("idpassenger", PassengerID[0]);
            if (GetID != null)
            {
                return RedirectToAction("Bookings", "Home", new { id = PassengerID[0] });
            } else
            {
                return View();
            }

        }

        public async Task<IActionResult> OneFlight(int id)
        {
            var data = await ApiClientFactory.Instance.GetFlight(id);
            HttpContext.Session.SetInt32("FlightNumber", data.FlightNo);
            HttpContext.Session.SetInt32("FlightPrice", data.Price);
            return View(data);
        }

        public async Task<IActionResult> Bookings()
        {
            
            var data = await ApiClientFactory.Instance.GetBookings();
            
            var avions = await ApiClientFactory.Instance.GetFlights();
            
            List<Bookings> list = new List<Bookings>();

            int passengerID = HttpContext.Session.GetInt32("idpassenger").Value;
            var myBookings = from f in data
                       where f.PassengerId.Equals(passengerID)
                       select f;
                
            ViewData["Flights"] = avions;
            return View(myBookings);
        }

        public IActionResult Problems()
        {
            return View();
        }

        public async Task<IActionResult> BuyTickets()
        {
            //création dans la db d'un nouveau booking avec le prix  le prix la session pour l'Id du mec et l'id du vol
            Bookings Booked = new Bookings();
            Booked.FlightNo = (int)HttpContext.Session.GetInt32("FlightNumber"); ;
            Booked.PassengerId = (int)HttpContext.Session.GetInt32("idpassenger");
            Booked.Price = (int)HttpContext.Session.GetInt32("FlightPrice"); ;

            await ApiClientFactory.Instance.BuyOneTicket(Booked);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Statistics()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}
