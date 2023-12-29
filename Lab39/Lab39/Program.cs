using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Чтение списка акций из файла ticker.txt
        List<string> tickers = await ReadTickersFromFile("ticker.txt");

        // Запуск задач на получение и расчет средней цены для каждой акции
        List<Task> tasks = new List<Task>();
        foreach (string ticker in tickers)
        {
            tasks.Add(ProcessStockAsync(ticker));
        }

        // Ожидание завершения всех задач
        await Task.WhenAll(tasks);

        Console.WriteLine("Все задачи завершены.");
    }

    static async Task<List<string>> ReadTickersFromFile(string fileName)
    {
        List<string> tickers = new List<string>();

        using (StreamReader reader = new StreamReader(fileName))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                tickers.Add(line.Trim());
            }
        }

        return tickers;
    }

    static async Task ProcessStockAsync(string ticker)
    {
        DateTime startDate = DateTime.Now.AddYears(-1);
        DateTime endDate = DateTime.Now;
        string url = $"https://query1.finance.yahoo.com/v7/finance/download/{ticker}?period1={ToUnixTimestamp(startDate)}&period2={ToUnixTimestamp(endDate)}&interval=1d&events=history&includeAdjustedClose=true";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                string csvData = await client.GetStringAsync(url);

                double sum = 0;
                int count = 0;

                using (StringReader reader = new StringReader(csvData))
                {
                    // Пропустить заголовок CSV-файла
                    await reader.ReadLineAsync();

                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        string[] values = line.Split(',');

                        double high = double.Parse(values[2]);
                        double low = double.Parse(values[3]);

                        double average = (high + low) / 2;

                        sum += average;
                        count++;
                    }
                }

                double averagePrice = sum / count;
                string result = $"{ticker}:{averagePrice}";

                while (IsFileLocked("result.txt"))
                {
                    await Task.Delay(1000);
                }

                // Потокобезопасная запись результата в файл

                using (StreamWriter writer = File.AppendText("result.txt"))
                {
                    await writer.WriteLineAsync(result);
                }

                Console.WriteLine($"Задача для {ticker} выполнена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке акции {ticker}: {ex.Message}");
            }
        }
    }

    static bool IsFileLocked(string path)
    {
        try
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                return false;
            }
        }
        catch (IOException)
        {
            return true;
        }
    }

    static long ToUnixTimestamp(DateTime dateTime)
    {
        return (long)(dateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    }
}

