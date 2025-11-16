using UnityEngine;

public class WeatherInfo
{
    public WeatherType currentWeather;
    public float weatherDuration;
    public float weatherTimer;
    
    public float currentPollution;
    public float currentThirst;
    public float currentTemp;
    
    public WeatherInfo()
    {
        weatherTimer = 0f;
        weatherDuration = 0f;
    }
}
