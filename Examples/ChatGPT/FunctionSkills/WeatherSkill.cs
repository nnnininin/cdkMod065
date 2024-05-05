using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

using Newtonsoft.Json;
using ChatdollKit.Network;
using ChatdollKit.Dialog.Processor;

namespace ChatdollKit.Examples.ChatGPT
{
    public class WeatherSkill : ChatGPTFunctionSkillBase
    {
        //インスペクタから設定できないようにprivateにしておく
        private string FunctionName = "get_weather";
        private string FunctionDescription = "Get current weather in the location.";
        private ChatdollHttp client = new ChatdollHttp(timeout: 20000); // HTTPクライアントのインスタンスを作成

        public override ChatGPTFunction GetFunctionSpec()
        {
            // Make function spec for ChatGPT Function Calling
            var func = new ChatGPTFunction(FunctionName, FunctionDescription, ExecuteFunction);
            func.AddProperty("location", new Dictionary<string, object>() { { "type", "string" } });
            return func;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        protected override async UniTask<FunctionResponse> ExecuteFunction(
            string argumentsJsonString,
            CancellationToken token
        )
        {
            Debug.Log($"argumentsJSONString: {argumentsJsonString}");
            var arguments = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                argumentsJsonString
            );
            var locationName = arguments.GetValueOrDefault("location");

            Debug.Log($"Selected location: {locationName}");

            // ここでGetWeatherAsyncメソッドを呼び出して天気情報を取得
            var weatherInfo = await GetWeatherAsync(argumentsJsonString, token);

            // APIから取得したデータをrespに格納
            var resp = new Dictionary<string, object>() { { "weatherInfo", weatherInfo } };

            return new FunctionResponse(JsonConvert.SerializeObject(resp));
        }

        public async UniTask<string> GetWeatherAsync(string jsonString, CancellationToken token)
        {
            var funcArgs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            var locationName = funcArgs.GetValueOrDefault("location");

            var locationCode = string.Empty;
            if (locationName == "札幌")
            {
                locationCode = "016010";
            }
            else if (locationName == "仙台")
            {
                locationCode = "040010";
            }
            else if (locationName == "東京")
            {
                locationCode = "130010";
            }
            else if (locationName == "名古屋")
            {
                locationCode = "230010";
            }
            else if (locationName == "大阪")
            {
                locationCode = "270000";
            }
            else if (locationName == "広島")
            {
                locationCode = "340010";
            }
            else if (locationName == "福岡")
            {
                locationCode = "400010";
            }
            else if (locationName == "那覇")
            {
                locationCode = "471010";
            }

            if (string.IsNullOrEmpty(locationName) || string.IsNullOrEmpty(locationCode))
            {
                return "ロケーションが指定されていないか、不明なロケーションです。ユーザーに質問してください。";
            }

            // Get weather
            var weatherResponse = await client.GetJsonAsync<WeatherResponse>(
                $"https://weather.tsukumijima.net/api/forecast/city/{locationCode}",
                cancellationToken: token
            );

            var ret =
                $"- 都市: {weatherResponse.location.city}\n- 天気: {weatherResponse.forecasts[0].telop}\n";

            if (weatherResponse.forecasts[0].temperature.max.celsius != null)
            {
                ret += $"- 最高気温: {weatherResponse.forecasts[0].temperature.max.celsius}度";
            }
            else if (weatherResponse.forecasts[0].temperature.min.celsius != null)
            {
                ret += $"- 最低気温: {weatherResponse.forecasts[0].temperature.min.celsius}度";
            }
            else
            {
                ret += "- 気温: 情報なし";
            }

            return $"以下は天気予報の確認結果です。あなたの言葉でユーザーに伝えてください。\n\n\"\"\"{ret}\"\"\"";
        }

        private class WeatherResponse
        {
            public Location location;
            public List<Forecast> forecasts;
        }

        private class Location
        {
            public string city;
        }

        private class Forecast
        {
            public string telop;
            public Temperature temperature;
        }

        private class Temperature
        {
            public TemperatureItem max;
            public TemperatureItem min;
        }

        private class TemperatureItem
        {
            public string celsius;
        }
    }
}
