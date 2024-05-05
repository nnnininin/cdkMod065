using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using ChatdollKit.Network;
using ChatdollKit.Dialog.Processor;

namespace ChatdollKit.Examples.ChatGPT
{
    public class HoroscopeSkill : ChatGPTFunctionSkillBase
    {
        //変更ポイント
        private string FunctionName = "get_horoscope";
        private string FunctionDescription = "Get horoscope of the date. Provide the zodiac sign in Japanese and the date in YYYY/MM/DD format. " +
                                             "The function will utilize the provided date and day of the week to generate accurate horoscope predictions.";
        //ここまで変更ポイント
        private ChatdollHttp client = new ChatdollHttp(timeout: 20000);

        public override ChatGPTFunction GetFunctionSpec()
        {
            //funcはchatGPTに渡すプロパティの仕様を定義する
            var func = new ChatGPTFunction(FunctionName, FunctionDescription, ExecuteFunction);
            func.AddProperty(
                "zodiac_sign",
                new Dictionary<string, object>() { { "type", "string" } }
            );
            func.AddProperty(
                "date",
                new Dictionary<string, object>() { { "type", "string" } }
            );
            
            Debug.Log("func"+JsonConvert.SerializeObject(func));
            
            return func;
        }
        
        protected override async UniTask<FunctionResponse> ExecuteFunction(
            string argumentsJsonString,
            CancellationToken token
        )
        {
            Debug.Log($"Received arguments: {argumentsJsonString}");

            var horoscopeInfo = await GetHoroscopeAsync(argumentsJsonString, token);
            Debug.Log($"Received horoscope info: {horoscopeInfo}");
            var resp = new Dictionary<string, object>() { { "horoscopeInfo", horoscopeInfo } };

            Debug.Log($"Final response: {JsonConvert.SerializeObject(resp)}");

            return new FunctionResponse(JsonConvert.SerializeObject(resp));
        }

        //ここのメソッド名とその中身も目的に合わせて変更する
        public async UniTask<string> GetHoroscopeAsync(string jsonString, CancellationToken token)
        {
            //jsonStringはchatGPTから渡されるJSON
            
            Debug.Log("GetHoroscopeAsync started");

            Debug.Log("jsonString"+jsonString);
            //デバッグログ例
            //jsonString {
            //    "zodiac_sign": "双子座",
            //    "date": "2012/7/20"
            //}
            
            var funcArgs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            var zodiacSign = funcArgs.GetValueOrDefault("zodiac_sign");
            var date = funcArgs.GetValueOrDefault("date");
            
            Debug.Log("Zodiac Sign : " +zodiacSign);
            Debug.Log("Date : "+date);

            if (string.IsNullOrEmpty(zodiacSign))
            {
                Debug.Log("Zodiac sign is empty or null"); // 星座が指定されていない場合のログ
                return "Please specify your zodiac sign.";
            }

            if (string.IsNullOrEmpty(date))
            {
                Debug.Log("Date is empty or null"); // 日付が指定されていない場合のログ
                return "Please specify date.";
            }

            Debug.Log("Sending request to horoscope API"); // APIリクエストを送る前にログを出力

            var horoscopeResponse = await client.GetJsonAsync<HoroscopeResponse>(
                $"http://api.jugemkey.jp/api/horoscope/free/{date}",
                cancellationToken: token
            );

            Debug.Log($"Received response: {JsonConvert.SerializeObject(horoscopeResponse)}"); // APIからのレスポンスをログに出力

            // 星座データを探す
            ZodiacData zodiacData = null;

            // foreachを使って星座データのリストの中から、指定された星座のデータを探す
            foreach (var z in horoscopeResponse.Horoscope[date])
            {
                if (z.Sign != zodiacSign)
                    continue;
                zodiacData = z;
                break;
            }

            //retに += を使って情報を加えていく
            if (zodiacData != null)
            {
                var ret = $"- 星座: {zodiacSign}\n- 今日の運勢: {zodiacData.Content}\n";
                ret += "- ラッキーアイテム: " + zodiacData.Item + "\n";
                ret += "- 金運: " + zodiacData.Money + "\n";
                ret += "- 総合運: " + zodiacData.Total + "\n";
                ret += "- 仕事運: " + zodiacData.Job + "\n";
                ret += "- ラッキーカラー: " + zodiacData.Color + "\n";
                ret += "- 恋愛運: " + zodiacData.Love + "\n";
                ret += "- ラッキーランキング: " + zodiacData.Rank + "\n";
                ret += "- 星座: " + zodiacData.Sign + "\n";

                //retに応じて、ユーザーに返すメッセージを変える
                return $"Below are the fortunes for the given date and constellation. Please organize each element in an easy-to-understand manner and communicate it to the user in your own words,in Japanese.\n\n\"\"\"{ret}\"\"\"";
            }
            else
            {
                return "What is the data for the given constellation?";
            }
        }

        //ここも変更する
        private class HoroscopeResponse
        {
            public Dictionary<string, List<ZodiacData>> Horoscope { get; set; }
        }

        //ここはJSONのデータ構造なので、APIの渡すJSONに合わせて変更する
        //ChatGPTにAPI側から渡されるJSONの見本を渡せばそれっぽくなる
        private class ZodiacData
        {
            public string Content { get; set; }
            public string Item { get; set; }
            public int Money { get; set; }
            public int Total { get; set; }
            public int Job { get; set; }
            public string Color { get; set; }
            public int Love { get; set; }
            public int Rank { get; set; }
            public string Sign { get; set; }
        }
    }
}
