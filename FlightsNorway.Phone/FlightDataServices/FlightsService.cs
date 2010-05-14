﻿using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using FlightsNorway.Phone.Model;

namespace FlightsNorway.Phone.FlightDataServices
{
    public class FlightsService
    {
        private readonly string _serviceUrl;
        private readonly string _direction;
        private readonly string _lastUpdate;

        private readonly AirlineDictionary _airlines;
        private readonly AirportDictionary _airports;
        private readonly StatusDictionary _statuses;

        public FlightsService(AirlineDictionary airlines, AirportDictionary airports, StatusDictionary statuses)
        {
            _airlines = airlines;
            _airports = airports;
            _statuses = statuses;

            _serviceUrl = "http://flydata.avinor.no/XmlFeed.asp?TimeFrom={0}&TimeTo={1}&airport={2}";
            _lastUpdate = "&lastUpdate=2009-03-10T15:03:00";
            _direction = "&direction={0}";
        }     

        public IObservable<IEnumerable<Flight>> GetFlightsFrom(Airport fromAirport)
        {            
            string url = string.Format(_serviceUrl, 1, 7, fromAirport.Code);
            return GetFlightsFrom(url);
        }

        private IObservable<IEnumerable<Flight>> GetFlightsFrom(string url)
        {
            return WebRequestFactory.GetData(new Uri(url), ParseFlightsXml);
        }
        
        private IEnumerable<Flight> ParseFlightsXml(XmlReader reader)
        {
            var xml = XDocument.Load(reader);

            return from airport in xml.Elements("airport")
                   from flights in airport.Elements("flights")
                   from flight in flights.Elements("flight")
                   select new Flight
                              {
                                  Airline = _airlines[flight.ElementValueOrEmpty("airline")],
                                  Airport = _airports[flight.ElementValueOrEmpty("airport")],
                                  Status = ReadStatus(flight.Element("status")),
                                  UniqueId = flight.AttributeValueOrEmpty("uniqueID"),
                                  Gate = flight.ElementValueOrEmpty("gate"),
                                  Direction = ConvertToDirection(flight.Element("arr_dep")),
                                  LastLeg = ConvertToFlightArea(flight.Element("dom_int")),
                                  ScheduledTime = flight.ElementAsDateTime("schedule_time")
                              };
        }

        private FlightStatus ReadStatus(XElement element)
        {
            if(element == null) return FlightStatus.Empty;

            var flightStatus = new FlightStatus();
            flightStatus.Status = _statuses[element.AttributeValueOrEmpty("code")];
            flightStatus.StatusTime = element.AttributeAsDateTime("time");            
            return flightStatus;
        }

        private static Direction ConvertToDirection(XElement element)
        {
            if(element == null) return Direction.Depature;
            return element.Value == "D" ? Direction.Depature : Direction.Arrival;
        }

        private static FlightArea ConvertToFlightArea(XElement element)
        {
            if(element == null) return FlightArea.Domestic;
            if (element.Value == "S") return FlightArea.Schengen;
            if (element.Value == "I") return FlightArea.International;            
            return FlightArea.Domestic;            
        }
    }
}