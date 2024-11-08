using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private const string ApiUrl = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/";

        private Dictionary<string, Dictionary<string, double>> exchangeRates;

        public MainWindow()
        {
            InitializeComponent();
            LoadCurrencies();
        }

        private async void LoadCurrencies()
        {
            try
            {
                exchangeRates = await FetchCurrencyData(ApiUrl);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Failed to load currencies: {ex.Message}");
                return;
            }

            if (exchangeRates != null && exchangeRates.Count > 0)
            {
                FromCurrency.ItemsSource = exchangeRates.Keys;
                ToCurrency.ItemsSource = exchangeRates.Keys;
            }
            else
            {
                MessageBox.Show("No currencies found.");
            }
        }

        private async Task<Dictionary<string, Dictionary<string, double>>> FetchCurrencyData(string baseUrl)
        {
            var exchangeRates = new Dictionary<string, Dictionary<string, double>>();
            using HttpClient client = new HttpClient();

            // List of currencies to fetch
            var currencies = new List<string> { "usd", "eur", "gbp", "btc", "irr", "eth" }; // Add more currencies as needed

            foreach (var currency in currencies)
            {
                string url = $"{baseUrl}{currency}.json";
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error fetching data from {url}: {response.ReasonPhrase}");
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    var rates = new Dictionary<string, double>();
                    if (doc.RootElement.TryGetProperty(currency, out JsonElement currencyElement))
                    {
                        foreach (JsonProperty property in currencyElement.EnumerateObject())
                        {
                            rates[property.Name] = property.Value.GetDouble();
                        }

                        exchangeRates[currency.ToUpper()] = rates;
                    }
                    else
                    {
                        throw new JsonException(
                            $"The '{currency}' property is missing in the JSON response from {url}");
                    }
                }
            }

            return exchangeRates;
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromCurrency.SelectedItem == null || ToCurrency.SelectedItem == null || string.IsNullOrEmpty(AmountTextBox.Text))
            {
                MessageBox.Show("Please select currencies and enter an amount.");
                return;
            }

            string fromCurrency = FromCurrency.SelectedItem.ToString();
            string toCurrency = ToCurrency.SelectedItem.ToString();
            if (double.TryParse(AmountTextBox.Text, out double amount))
            {
                if (exchangeRates.TryGetValue(fromCurrency, out var fromRates) && fromRates.TryGetValue(toCurrency.ToLower(), out double toRate))
                {
                    double convertedAmount = amount * toRate;
                    string formattedAmount = convertedAmount > 0.01 ? convertedAmount.ToString("F2") : convertedAmount.ToString("F10");
                    ResultTextBlock.Text = $"{amount} {fromCurrency} = {formattedAmount} {toCurrency}";
                }
                else
                {
                    MessageBox.Show("Conversion rate not available.");
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid amount.");
            }
        }
    }
}    