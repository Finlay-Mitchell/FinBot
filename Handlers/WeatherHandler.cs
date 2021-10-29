using System.Xml;
using System.Net;
using Color = Discord.Color;

namespace FinBot.Handlers
{
    public class WeatherHandler
    {
        public WeatherHandler(string city)
        {
            SetCurrentURL(city);
            xmlDocument = GetXML(CurrentURL);
        }

        public bool XMLIsNull()
        {
            if(xmlDocument == null)
            {
                return true;
            }

            return false;
        }

        public float GetTemp()
        {
            XmlNode temp_node = xmlDocument.SelectSingleNode("//temperature");
            XmlAttribute temp_value = temp_node.Attributes["value"];
            string temp_string = temp_value.Value;
            return float.Parse(temp_string);
        }

        public float GetMinTemp()
        {
            XmlNode min_node = xmlDocument.SelectSingleNode("//temperature");
            XmlAttribute min_value = min_node.Attributes["min"];
            string min_string = min_value.Value;
            return float.Parse(min_string);
        }

        public float GetMaxTemp()
        {
            XmlNode max_node = xmlDocument.SelectSingleNode("//temperature");
            XmlAttribute max_value = max_node.Attributes["max"];
            string max_string = max_value.Value;
            return float.Parse(max_string);
        }

        public float GetFeelsLike()
        {
            XmlNode feels_like_node = xmlDocument.SelectSingleNode("//feels_like");
            XmlAttribute feels_like_value = feels_like_node.Attributes["value"];
            string feels_like = feels_like_value.Value;
            return float.Parse(feels_like);
        }

        public string GetWeatherType()
        {
            XmlNode weather_node = xmlDocument.SelectSingleNode("//weather");
            XmlAttribute weather_value = weather_node.Attributes["value"];
            string weather = weather_value.Value;
            return (weather.ToString());
        }

        public float GetHumidity()
        {
            XmlNode humidity_node = xmlDocument.SelectSingleNode("//humidity");
            XmlAttribute humidity_value = humidity_node.Attributes["value"];
            string humidity = humidity_value.Value;
            return float.Parse(humidity);
        }

        public float GetPressure()
        {
            XmlNode pressure_node = xmlDocument.SelectSingleNode("//pressure");
            XmlAttribute pressure_value = pressure_node.Attributes["value"];
            string pressure = pressure_value.Value;
            return float.Parse(pressure);
        }

        public int GetWeatherId()
        {
            XmlNode weatherId_node = xmlDocument.SelectSingleNode("//weather");
            XmlAttribute weatherId_value = weatherId_node.Attributes["number"];
            string weatherId = weatherId_value.Value;
            return int.Parse(weatherId);
        }

        public string GetLastUpdated()
        {
            XmlNode update_node = xmlDocument.SelectSingleNode("//lastupdate");
            XmlAttribute update_value = update_node.Attributes["value"];
            string update = update_value.Value;
            return update;
        }

        public string GetWindSpeed()
        {
            XmlNode windspeed_node = xmlDocument.SelectSingleNode("//wind//speed");
            XmlAttribute windspeed_value = windspeed_node.Attributes["name"];
            string windspeed = windspeed_value.Value;
            return windspeed;
        }

        public float GetWindSpeedValue()
        {
            XmlNode windspeed_node = xmlDocument.SelectSingleNode("//wind//speed");
            XmlAttribute windspeed_value = windspeed_node.Attributes["value"];
            string windspeed = windspeed_value.Value;
            return float.Parse(windspeed);
        }

        private string CurrentURL;
        private XmlDocument xmlDocument;

        private void SetCurrentURL(string location)
        {
            CurrentURL = $"http://api.openweathermap.org/data/2.5/weather?q={location}&mode=xml&units=metric&APPID={Global.WeatherAPIKey}";
        }

        private XmlDocument GetXML(string CurrentURL)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string xmlContent = client.DownloadString(CurrentURL);
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xmlContent);
                    return xmlDocument;
                }
            }

            catch
            {
                return null;
            }
        }
    }

    public class WeatherData
    {
        public WeatherData(string City)
        {
            city = City;
        }

        private bool XMLIsNull;
        private string city;
        private float temp;
        private float tempMax;
        private float tempMin;
        private float feelsLike;
        private string weather;
        private float humidity;
        private float pressure;
        private int weatherId;
        private string updated;
        private string windspeed;
        private float windspeedValue;

        public void CheckWeather()
        {
            WeatherHandler DataAPI = new WeatherHandler(City);
            XMLIsNull = DataAPI.XMLIsNull();
            
            if(XMLIsNull)
            {
                return;
            }

            temp = DataAPI.GetTemp();
            tempMin = DataAPI.GetMinTemp();
            tempMax = DataAPI.GetMaxTemp();
            feelsLike = DataAPI.GetFeelsLike();
            weather = DataAPI.GetWeatherType();
            pressure = DataAPI.GetPressure();
            humidity = DataAPI.GetHumidity();
            weatherId = DataAPI.GetWeatherId();
            updated = DataAPI.GetLastUpdated();
            windspeed = DataAPI.GetWindSpeed();
            windspeedValue = DataAPI.GetWindSpeedValue();
        }

        public bool XmlIsNull { get => XMLIsNull; set => XMLIsNull = value; }
        public string City { get => city; set => city = value; }
        public float Temp { get => temp; set => temp = value; }
        public float FeelsLike { get => feelsLike; set => feelsLike = value; }
        public float TempMax { get => tempMax; set => tempMax = value; }
        public float TempMin { get => tempMin; set => tempMin = value; }
        public string WeatherValue { get => weather; set => weather = value; }
        public float Humidity { get => humidity; set => humidity = value; }
        public float Pressure { get => pressure; set => pressure = value; }
        public int WeatherId { get => weatherId; set => weatherId = value; }
        public string LastUpdated { get => updated; set => updated = value; }
        public string Windspeed { get => windspeed; set => windspeed = value; }
        public float WindspeedValue { get => windspeedValue; set => windspeedValue = value; }

        /// <summary>
        /// Gets the colour to set the embed to.
        /// </summary>
        /// <param name="weather">The weather value.</param>
        /// <returns>A colour.</returns
        public Color DetermineWeatherColour(int weather)
        {
            Color SandColour = new Color(194, 178, 128);

            switch (weather)
            {
                case 800:
                    Color ClearColour = new Color(237, 237, 237);
                    return ClearColour;

                case 701:
                    return Color.LighterGrey;

                case 711:
                    return Color.DarkGrey;

                case 721:
                    return Color.LighterGrey;

                case 731:
                    return SandColour;

                case 741:
                    return Color.LighterGrey;

                case 751:
                    return SandColour;

                case 761:
                    return Color.LightGrey;

                case 762:
                    return Color.DarkerGrey;

                case 771:
                    return Color.Orange;

                case 781:
                    return Color.DarkRed;

                case int expression when weather >= 801:
                    return Color.DarkGrey;

                case int expression when weather >= 600 && weather <= 622:
                    Color SnowColour = new Color();
                    return SnowColour;

                case int expression when weather >= 500 && weather <= 531:
                    return Color.DarkBlue;

                case int expression when weather >= 300 && weather <= 321:
                    return Color.Blue;

                case int expression when weather >= 200 && weather <= 232:
                    return Color.Magenta;

                default:
                    return Color.Green;
            }
        }

        /// <summary>
        /// Converts a Celcius temperature to Farenheit.
        /// </summary>
        /// <param name="C">The celcius value.</param>
        /// <returns>A Farenheit value of the Celcius input.</returns>
        public float CelciusToFarenheit(float C)
        {
            return (C * 9 / 5) + 32;
        }
    }
}
